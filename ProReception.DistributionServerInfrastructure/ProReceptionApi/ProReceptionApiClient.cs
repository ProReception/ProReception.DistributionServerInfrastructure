﻿namespace ProReception.DistributionServerInfrastructure.ProReceptionApi;

using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProReception.DistributionServerInfrastructure.Configuration;
using ProReception.DistributionServerInfrastructure.Settings;

public class ProReceptionApiClient(
    ILogger<ProReceptionApiClient> logger,
    ISettingsManagerBase settingsManagerBase,
    IOptions<ProReceptionApiConfiguration> options)
    : ApiClientBase(logger, settingsManagerBase, options), IProReceptionApiClient
{
    public async Task<T> Get<T>(string path) => await Query(req => req.AppendPathSegment(path).GetJsonAsync<T>());

    public async Task<IFlurlResponse> GetRaw(string path) => await Query(req => req.AppendPathSegment(path).GetAsync());

    public async Task Post(string path, object data) => await Command(req => req.AppendPathSegment(path).PostJsonAsync(data));

    public async Task<T> Post<T>(string path, object data) => await Query(req => req.AppendPathSegment(path).PostJsonAsync(data).ReceiveJson<T>());

    public async Task Put(string path, object data) => await Command(req => req.AppendPathSegment(path).PutJsonAsync(data));

    public async Task Patch(string path, object data) => await Command(req => req.AppendPathSegment(path).PatchJsonAsync(data));
}
