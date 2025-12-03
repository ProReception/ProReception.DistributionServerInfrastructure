# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 10 Razor SDK library that provides common infrastructure components for Pro Reception distributed applications. The library is packaged and distributed as a NuGet package (`ProReception.DistributionServerInfrastructure`).

## Build and Development Commands

```bash
# Build the project
dotnet build

# Build in release mode
dotnet build -c Release

# Package for NuGet (happens automatically via GitHub Actions)
dotnet pack
```

## Architecture

This is a shared infrastructure library with the following key components:

### Core Extension Methods
- `WebApplicationBuilderExtensions.AddProReceptionDistributionServerInfrastructure<TSettingsManagerInterface, TSettingsManagerImplementation>()` - Main configuration method
- `WebApplicationExtensions.UseProReceptionDistributionServerInfrastructure()` - Application middleware setup

### Settings Management System
- `SettingsManagerBase<T>` - Abstract base class for app-specific settings management
- `ISettingsManagerBase` - Core interface for settings operations
- Settings are encrypted and stored in `%LocalAppData%\Pro Reception\{AppName}\`
- Supports secure token storage and retrieval for Pro Reception API authentication

### Pro Reception API Client
- `ProReceptionApiClient` - HTTP client for Pro Reception API endpoints
- `ApiClientBase` - Base class with authentication and retry logic
- Extension methods for specific API endpoints:
  - `ActConnectorExtensions` - ACT system integration
  - `NoxConnectorExtensions` - NOX system integration  
  - `MyExtensions` - User account operations
  - `PrinterLabelExtensions` - Label printing functionality
  - `NotificationExtensions` - Push notification services

### Blazor Components
- `<ProReceptionConnection />` - Pro Reception login/connection component
- `<Logs />` - Real-time log viewing component via SignalR

### SignalR Hub
- `LogsHub` - Real-time log streaming to connected clients
- `SignalRHostedService` - Background service for SignalR operations

### Configuration Requirements
Applications using this library must provide:
```json
{
  "ProReceptionApi": {
    "BaseUrl": "https://localhost:7016"
  }
}
```

Optional proxy configuration:
```json
{
  "Proxy": {
    "Address": "http://proxy-server:8080"
  }
}
```

### Logging
- Uses Serilog with console, file, and SignalR observers
- Log files written to `{SettingsDirectory}\Logs\log.txt` with daily rolling
- Real-time log streaming via SignalR hub

### Dependencies
Key external dependencies:
- MudBlazor 8.14.0 (UI components)
- Flurl.Http 4.0.2 (HTTP client)
- Serilog.AspNetCore 9.0.0 (logging)
- AuthenticatedEncryption 2.0.0 (settings encryption)
- System.Reactive 6.1.0 (reactive extensions)

## Publishing

The library is automatically published to NuGet when `version.props` is updated on the main branch via GitHub Actions workflow.