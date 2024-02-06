﻿namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.NoxConnector;

using Models;

public static class NoxConnectorExtensions
{
    public static async Task<NoxUserResponse> GetNoxContractor(this IProReceptionApiClient proReceptionApiClient, int contractorId)
        => await proReceptionApiClient.Get<NoxUserResponse>($"nox-connector/contractors/{contractorId}");

    public static async Task<NoxUserResponse> GetNoxVisitorPass(this IProReceptionApiClient proReceptionApiClient, int visitorPassId)
        => await proReceptionApiClient.Get<NoxUserResponse>($"nox-connector/visitor-passes/{visitorPassId}");
}