# Pro Reception Distribution Server Infrastructure
Helper library with common infrastructure code for our distributed apps

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
