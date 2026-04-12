# WebTemplate

Starter project for web development modeled after the structure of `AITracker`, but kept intentionally small.

## Structure

- `src/Core`: shared services, configuration models, and rolling file logging.
- `src/Api`: minimal API with health and greeting endpoints.
- `src/Web`: frontend shell served by ASP.NET Core static files.
- `tests/WebTemplate.Harness`: console-based independent test harness.
- `storage`: runtime logs and artifacts.

## Commands

```powershell
.\launch.ps1 -Project Full
.\launch.ps1 -Project Api
.\launch.ps1 -Project Web
.\test.ps1
```

## Default ports

- Web: `5222`
- API: `5139`
