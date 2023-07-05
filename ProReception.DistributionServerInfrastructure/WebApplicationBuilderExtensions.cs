namespace ProReception.DistributionServerInfrastructure;

using System.Reactive.Linq;
using System.Text.Json;
using Configuration;
using Flurl.Http;
using Flurl.Http.Configuration;
using FlurlClientFactories;
using Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProReceptionApi;
using Serilog;
using Serilog.Events;
using Settings;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddProReceptionDistributionServerInfrastructure<TSettingsManagerInterface, TSettingsManagerImplementation>(
        this WebApplicationBuilder builder, TSettingsManagerImplementation settingsImplementation)
        where TSettingsManagerInterface : class, ISettingsManagerBase
        where TSettingsManagerImplementation : class, TSettingsManagerInterface
    {
        builder.Host.UseSerilog((_, serviceProvider, conf) => conf
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .WriteTo.Console()
            .WriteTo.File($"{settingsImplementation.GetLogFilesPath()}\\log.txt", rollingInterval: RollingInterval.Day)
            .WriteTo.Observers(observable =>
                observable.Do(evt =>
                {
                    var hubContext = serviceProvider.GetService<IHubContext<LogsHub, ILogsHub>>();
                    hubContext?.Clients.All.ReceiveLog(new LogsHub.LogMessage { Timestamp = evt.Timestamp, Message = evt.RenderMessage() });
                }).Subscribe()));

        var proxyConfigurationSection = builder.Configuration.GetSection("Proxy");
        var proxyConfiguration = proxyConfigurationSection.Get<ProxyConfiguration>();

        if (proxyConfiguration != null)
        {
            Log.Information("Using proxy: {Proxy}", proxyConfiguration.Address);
        }

        FlurlHttp.Configure(x =>
        {
            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            x.JsonSerializer = new DefaultJsonSerializer(serializeOptions);

            if (proxyConfiguration != null)
            {
                x.HttpClientFactory = new ProxyFlurlClientFactory(proxyConfiguration.GetWebProxy());
            }
        });

        builder.Services.Configure<ProReceptionApiConfiguration>(builder.Configuration.GetSection("ProReceptionApi"));
        builder.Services.Configure<ProxyConfiguration>(proxyConfigurationSection);

        builder.Services.AddSingleton<ISettingsManagerBase>(settingsImplementation);
        builder.Services.AddSingleton<TSettingsManagerInterface>(settingsImplementation);
        builder.Services.AddSingleton<IProReceptionApiClient, ProReceptionApiClient>();

        return builder;
    }
}
