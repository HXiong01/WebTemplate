param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$env:DOTNET_CLI_HOME = Join-Path $repoRoot ".dotnet_home"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_NOLOGO = "1"
$env:MSBUILDDISABLENODEREUSE = "1"

dotnet run --project (Join-Path $repoRoot "tests\WebTemplate.Harness\WebTemplate.Harness.csproj") -c $Configuration
