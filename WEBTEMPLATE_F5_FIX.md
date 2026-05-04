# WebTemplate F5 Launch Fix

Use this note for the original `WebTemplate` repo. This intentionally ignores any later app renaming.

## Problem

Pressing F5 opens the web page, but the API status shows `Unavailable`, and the form shows:

```text
API call failed. Start the API with launch.ps1.
```

This means `WebTemplate.Web` is running, but `WebTemplate.Api` is not running. The page itself is correct; the missing piece is a debug setup that starts both projects.

## Visual Studio Fix

1. Open the solution in Visual Studio.
2. Right-click the solution.
3. Choose `Configure Startup Projects...`.
4. Select `Multiple startup projects`.
5. Set these actions:

```text
WebTemplate.Api    Start
WebTemplate.Web    Start
WebTemplate.Core   None
WebTemplate.Harness None
```

6. Click `Apply`, then `OK`.
7. Press F5.

The web page should open, and the status panel should show:

```text
Web    Healthy
API    Healthy
```

## VS Code Fix

Create `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "WebTemplate Web",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/Web/bin/Debug/net10.0/WebTemplate.Web.dll",
      "args": [],
      "cwd": "${workspaceFolder}",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(http://\\S+)",
        "uriFormat": "%s"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "http://localhost:5257",
        "DOTNET_CLI_HOME": "${workspaceFolder}/.dotnet_home",
        "DOTNET_SKIP_FIRST_TIME_EXPERIENCE": "1",
        "DOTNET_NOLOGO": "1"
      }
    },
    {
      "name": "WebTemplate API",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/Api/bin/Debug/net10.0/WebTemplate.Api.dll",
      "args": [],
      "cwd": "${workspaceFolder}",
      "stopAtEntry": false,
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "http://localhost:5139",
        "DOTNET_CLI_HOME": "${workspaceFolder}/.dotnet_home",
        "DOTNET_SKIP_FIRST_TIME_EXPERIENCE": "1",
        "DOTNET_NOLOGO": "1"
      }
    }
  ],
  "compounds": [
    {
      "name": "WebTemplate Full Stack",
      "configurations": [
        "WebTemplate API",
        "WebTemplate Web"
      ],
      "stopAll": true
    }
  ]
}
```

Create `.vscode/tasks.json`:

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build",
      "command": "dotnet",
      "type": "process",
      "args": [
        "build",
        "${workspaceFolder}/WebTemplate.sln",
        "/property:GenerateFullPaths=true",
        "/consoleloggerparameters:NoSummary"
      ],
      "problemMatcher": "$msCompile"
    }
  ]
}
```

Then choose `WebTemplate Full Stack` in the Run and Debug dropdown and press F5.

## Launch Profile Fix

Make sure `src/Web/Properties/launchSettings.json` has a web profile that launches the root page:

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "WebTemplate.Web": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "/",
      "applicationUrl": "http://localhost:5257",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

The important pieces are:

```text
"launchBrowser": true
"launchUrl": "/"
"applicationUrl": "http://localhost:5257"
```

## Quick Manual Test

If you do not want to use F5, run the full stack from PowerShell:

```powershell
.\launch.ps1 -Project Full
```

Then open:

```text
http://localhost:5222/
```

The API should be available at:

```text
http://localhost:5139/health
```

## Expected Result

After the fix:

- The browser opens the web page, not `/health`.
- The `Web` status shows `Healthy`.
- The `API` status shows `Healthy`.
- Clicking `Call API` returns a greeting instead of an API failure message.
