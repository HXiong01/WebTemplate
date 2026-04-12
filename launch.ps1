param(
    [ValidateSet("Full", "Web", "Api")]
    [string]$Project = "Full",
    [Nullable[int]]$Port = $null,
    [string]$BindAddress = "0.0.0.0",
    [string]$Configuration = "Debug",
    [string]$Environment = "Development",
    [int]$WaitForReadySeconds = 20,
    [switch]$OpenFirewall,
    [switch]$NoBrowser
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

if ($null -eq $Port) {
    $Port = if ($Project -eq "Api") { 5139 } else { 5222 }
}

if ($Project -eq "Full") {
    Write-Host "Starting WebTemplate full stack..."
    & $PSCommandPath -Project Api -Port 5139 -BindAddress $BindAddress -Configuration $Configuration -Environment $Environment -WaitForReadySeconds $WaitForReadySeconds -NoBrowser
    if ($LASTEXITCODE -ne 0) {
        throw "API launch failed."
    }

    & $PSCommandPath -Project Web -Port $Port -BindAddress $BindAddress -Configuration $Configuration -Environment $Environment -WaitForReadySeconds $WaitForReadySeconds -NoBrowser:$NoBrowser
    if ($LASTEXITCODE -ne 0) {
        throw "Web launch failed."
    }

    return
}

$dotnetHome = Join-Path $repoRoot ".dotnet_home"
$projectPath = if ($Project -eq "Api") {
    Join-Path $repoRoot "src\Api\WebTemplate.Api.csproj"
}
else {
    Join-Path $repoRoot "src\Web\WebTemplate.Web.csproj"
}

if (-not (Test-Path $projectPath)) {
    throw "Project not found at $projectPath"
}

function Get-PortPids {
    param([int]$TargetPort)

    $connections = Get-NetTCPConnection -LocalPort $TargetPort -ErrorAction SilentlyContinue
    if ($null -eq $connections) {
        return @()
    }

    return $connections |
        Select-Object -ExpandProperty OwningProcess -Unique |
        Where-Object { $_ -and $_ -gt 0 }
}

function Stop-PortProcesses {
    param([int]$TargetPort)

    $pids = Get-PortPids -TargetPort $TargetPort
    foreach ($processId in $pids) {
        try {
            $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
            if ($null -eq $process) {
                continue
            }

            Write-Host "Stopping process $($process.ProcessName) (PID $processId) on port $TargetPort..."
            Stop-Process -Id $processId -Force -ErrorAction Stop
        }
        catch {
            Write-Warning "Failed to stop PID $processId on port $TargetPort. $_"
        }
    }
}

function Get-LanUrls {
    param([int]$TargetPort)

    $addresses = Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
        Where-Object {
            $_.IPAddress -notlike "127.*" -and
            $_.IPAddress -notlike "169.254.*" -and
            $_.IPAddress -ne "0.0.0.0" -and
            $_.AddressState -eq "Preferred"
        } |
        Select-Object -ExpandProperty IPAddress -Unique

    return $addresses | ForEach-Object { "http://$($_):$TargetPort" }
}

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Ensure-FirewallRule {
    param([int]$TargetPort)

    $ruleName = "WebTemplate Intranet $TargetPort"
    $existingRule = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue

    if ($existingRule) {
        Write-Host "Firewall rule already exists: $ruleName"
        return
    }

    if (-not (Test-IsAdministrator)) {
        Write-Warning "Run PowerShell as Administrator with -OpenFirewall to add the Windows Firewall rule automatically."
        Write-Host "Manual command:"
        Write-Host "  New-NetFirewallRule -DisplayName `"$ruleName`" -Direction Inbound -Action Allow -Protocol TCP -LocalPort $TargetPort -Profile Private"
        return
    }

    New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Action Allow -Protocol TCP -LocalPort $TargetPort -Profile Private | Out-Null
    Write-Host "Added Windows Firewall rule: $ruleName"
}

New-Item -ItemType Directory -Force -Path $dotnetHome | Out-Null
$logRoot = Join-Path $repoRoot "storage\logs"
New-Item -ItemType Directory -Force -Path $logRoot | Out-Null
Stop-PortProcesses -TargetPort $Port

$env:DOTNET_CLI_HOME = $dotnetHome
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_NOLOGO = "1"
$env:ASPNETCORE_ENVIRONMENT = $Environment

Write-Host "Building WebTemplate.$Project ($Configuration)..."
& dotnet build $projectPath -c $Configuration -m:1 /p:BuildInParallel=false /p:RestoreUseStaticGraphEvaluation=true
if ($LASTEXITCODE -ne 0) {
    throw "Build failed."
}

$listenUrl = "http://$BindAddress`:$Port"
$localUrl = "http://localhost:$Port"
$probeUrl = "http://127.0.0.1:$Port"
$lanUrls = @(Get-LanUrls -TargetPort $Port)

if ($OpenFirewall) {
    Ensure-FirewallRule -TargetPort $Port
}

$arguments = @(
    "run",
    "--project",
    $projectPath,
    "-c",
    $Configuration,
    "--no-build",
    "--no-launch-profile",
    "--urls",
    $listenUrl
)

Write-Host "Starting WebTemplate.$Project on $listenUrl..."
$stdoutLog = Join-Path $logRoot "launch-$($Project.ToLowerInvariant())-stdout.log"
$stderrLog = Join-Path $logRoot "launch-$($Project.ToLowerInvariant())-stderr.log"
$process = Start-Process -FilePath "dotnet" -ArgumentList $arguments -WorkingDirectory $repoRoot -RedirectStandardOutput $stdoutLog -RedirectStandardError $stderrLog -PassThru

Write-Host "Started PID $($process.Id)."
Write-Host "Local URL: $localUrl"

if ($Project -eq "Api") {
    Write-Host "Health URL: $localUrl/health"
    Write-Host "Greeting URL: $localUrl/api/greeting?name=Developer"
}

$readyUrl = "$probeUrl/health"
$ready = $false
for ($attempt = 1; $attempt -le $WaitForReadySeconds; $attempt++) {
    if ($process.HasExited) {
        Write-Warning "WebTemplate.$Project exited before becoming ready. Exit code: $($process.ExitCode)"
        break
    }

    try {
        Invoke-WebRequest -Uri $readyUrl -UseBasicParsing -TimeoutSec 2 | Out-Null
        $ready = $true
        break
    }
    catch {
        Start-Sleep -Seconds 1
    }
}

if ($ready) {
    Write-Host "Ready: $readyUrl"
}
else {
    Write-Warning "WebTemplate.$Project did not respond at $readyUrl within $WaitForReadySeconds seconds."
    Write-Host "Stdout log: $stdoutLog"
    Write-Host "Stderr log: $stderrLog"
}

if ($lanUrls.Count -gt 0) {
    Write-Host "Wi-Fi/intranet URL(s):"
    foreach ($lanUrl in $lanUrls) {
        Write-Host "  $lanUrl"
    }
}
else {
    Write-Warning "No LAN IPv4 address was detected. Connect to Wi-Fi or Ethernet and run the launcher again."
}

Write-Host "Logs: $logRoot"
Write-Host "Use -OpenFirewall from an Administrator PowerShell if other devices cannot connect."

if (-not $NoBrowser) {
    Start-Process $localUrl | Out-Null
}
