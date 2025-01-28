# Pro Reception Distribution Server Infrastructure
Helper library with common infrastructure code for our distributed apps

## Status
[![GitHub Workflow Status (with branch)](https://img.shields.io/github/actions/workflow/status/ProReception/ProReception.DistributionServerInfrastructure/nuget.yml?branch=main)](https://github.com/ProReception/ProReception.DistributionServerInfrastructure/actions/workflows/nuget.yml?query=branch%3Amain)
[![Nuget](https://img.shields.io/nuget/v/ProReception.DistributionServerInfrastructure)](https://www.nuget.org/packages/ProReception.DistributionServerInfrastructure)

## How to use
1. Install via NuGet: `Install-Package ProReception.DistributionServerInfrastructure`
2. Add custom settings:
   * Create new class for storing your settings, which inherits from `BaseSettings`
     ```csharp
     using DistributionServerInfrastructure.Settings.Models;

     public class MySettings : BaseSettings
     {
     }
     ```
   * Create new `ISettingsManager` interface, which inherits from `ISettingsManagerBase`
     ```csharp
     using DistributionServerInfrastructure.Settings;

     public interface ISettingsManager : ISettingsManagerBase
     {
     }
     ```
   * Create new `SettingsManager` class, which inherits from `SettingsManagerBase` and implements `ISettingsManager`
     ```csharp
     using DistributionServerInfrastructure.Settings;

     public class SettingsManager : SettingsManagerBase<MySettings>, ISettingsManager
     {
         public SettingsManager(string appName, string cryptKey, string authKey) : base(appName, cryptKey, authKey)
         {
         }
     }
     ```
3. In your `Program.cs` file, create a new instance of your `SettingsManager` class, and add the infrastructure configuration
     ```csharp
     var settingsManager = new SettingsManager(
         "My App Name",
         "some crypt key",
         "some auth key");

     builder.AddProReceptionDistributionServerInfrastructure<ISettingsManager, SettingsManager>(settingsManager);
     ```
4. Use the infrastructure config after building the app:
     ```csharp
     app.UseProReceptionDistributionServerInfrastructure();
     ```

## Configuration

These are the expected configuration values.

### Pro Reception API

There has to be a section in the configuration called `ProReceptionApi`:

```json
{
  "ProReceptionApi": {
    "BaseUrl": "https://localhost:7016"
  }
}
```

## Components

The components can be used inside your Razor views.

### Pro Reception login component

```csharp
@using ProReception.DistributionServerInfrastructure.Components

<ProReceptionConnection />
```

### Logs component

```csharp
@using ProReception.DistributionServerInfrastructure.Components

<Logs />
```

## MudBlazor

There is an issue when using MudBlazor from an app running as a Windows Service. See my question on StackOverflow [here](https://stackoverflow.com/questions/73937004/mudblazor-css-and-js-fails-to-load-when-running-as-windows-service).

So, my work around is to host the MudBlazor CSS and JS files on Azure storage, and load them from there. This means that every time I update the MudBlazor library, I also need to add the new CSS and JS files to the [Azure storage](https://portal.azure.com/#@proreception.com/resource/subscriptions/4c45a333-eb90-43f8-a38a-e8b4b257cfb3/resourceGroups/Production/providers/Microsoft.Storage/storageAccounts/proreception/storagebrowser).

To get the CSS and JS files for a specific version, I create a new app, and then copy the files when the app is running.

````shell
dotnet new install MudBlazor.Templates
mkdir MyApplication
dotnet new mudblazor --name MyApplication -o MyApplication
````
