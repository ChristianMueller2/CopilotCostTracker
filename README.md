# Copilot Cost Tracker

A Windows desktop application built with **.NET MAUI** that monitors and tracks token usage and costs for GitHub Copilot sessions.

## Features

- **Session monitoring** — watches configured folders for Copilot session log files and parses them automatically
- **Cost breakdown** — calculates costs per model based on current pricing (input / output / cache tokens)
- **Session detail view** — inspect individual sessions including all events, model calls, and token usage
- **By-model view** — aggregate usage across sessions grouped by model
- **System tray** — runs in the background with a tray icon; the window can be hidden and restored
- **Multiple watched folders** — add and manage multiple source folders to monitor

## Requirements

- Windows 10 (build 19041) or later
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- .NET MAUI workload for Windows

Install the MAUI workload once after installing the SDK:

```powershell
dotnet workload install maui-windows
```

## Dependencies

All dependencies are NuGet packages and are restored automatically on first build.

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.Maui.Controls` | 10.* | Core MAUI UI framework |
| `Microsoft.Maui.Controls.Compatibility` | 10.* | Legacy MAUI compatibility layer |
| `CommunityToolkit.Mvvm` | 8.* | Source-generated MVVM (commands, observable properties) |
| `CommunityToolkit.Maui` | 14.2.0 | Additional MAUI controls and behaviors |
| `Microsoft.WindowsDesktop.App.WindowsForms` | (framework ref) | WinForms `NotifyIcon` for system tray support |

Restore packages manually (optional — build does this automatically):

```powershell
dotnet restore
```

## Getting Started

```powershell
git clone https://bitbucket.bdalswrd.de/scm/~christian.mueller/copilotcosttracker.git
cd CopilotCostTracker

# Restore dependencies
dotnet restore

# Build (unpackaged, no MSIX)
dotnet build -f net10.0-windows10.0.19041.0 -p:WindowsPackageType=None

# Run directly from the console
dotnet run -f net10.0-windows10.0.19041.0 -p:WindowsPackageType=None
```

> The app runs unpackaged (no MSIX required) for local development. See [BUILDING.md](BUILDING.md) for producing a signed MSIX installer.

## Project Structure

```
CopilotCostTracker/
├── Models/          # Data models (CopilotSession, TokenUsage, ModelPricing, …)
├── ViewModels/      # MVVM view models (CommunityToolkit.Mvvm)
├── Views/           # MAUI pages and custom drawables
├── Services/        # Business logic, file watcher, parser, pricing, tray
├── Converters/      # Value converters for XAML bindings
├── Platforms/       # Platform-specific entry points and manifest
└── Resources/       # Icons, fonts, images, styles
```

## Tech Stack

| Component | Library |
|---|---|
| UI framework | .NET MAUI 10 |
| MVVM | CommunityToolkit.Mvvm 8 |
| MAUI extras | CommunityToolkit.Maui 14 |
| System tray | WinForms `NotifyIcon` |

## License

MIT
