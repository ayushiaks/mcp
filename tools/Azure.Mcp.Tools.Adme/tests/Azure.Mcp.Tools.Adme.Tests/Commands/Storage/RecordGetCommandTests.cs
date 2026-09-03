// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using Azure.Mcp.Tools.Adme.Commands.Storage;
using Azure.Mcp.Tools.Adme.Models.Storage;
using Azure.Mcp.Tools.Adme.Services;
using Microsoft.Mcp.Tests.Client;
using NSubstitute;
using Xunit;

namespace Azure.Mcp.Tools.Adme.Tests.Commands.Storage;

public sealed class RecordGetCommandTests : CommandUnitTestsBase<RecordGetCommand, IStorageService>
{
    private const string RecordId = "opendes:master-data--Well:W-99";

    [Fact]
    public async Task Execute_WithIdOnly_RequestsLatestVersion()
    {
        Service.GetRecordAsync(
                TestConstants.Endpoint, TestConstants.DataPartition, RecordId, null,
                Arg.Is<IReadOnlyList<string>?>(attributes => attributes == null), TestConstants.Tenant,
                Arg.Any<CancellationToken>())
            .Returns(new StorageRecord { Id = RecordId, Kind = TestConstants.WellKind });

        var response = await ExecuteCommandAsync(
            "--endpoint", TestConstants.Endpoint,
            "--data-partition", TestConstants.DataPartition,
            "--id", RecordId,
            "--tenant", TestConstants.Tenant);

        var result = ValidateAndDeserializeResponse(response, AdmeJsonContext.Default.StorageRecord);
        Assert.Equal(RecordId, result.Id);
    }

    [Fact]
    public async Task Execute_WithVersionAndAttributes_DoesNotCallService()
    {
        var response = await ExecuteCommandAsync(
            "--endpoint", TestConstants.Endpoint,
            "--data-partition", TestConstants.DataPartition,
            "--id", RecordId,
            "--version", "1704779151123456",
            "--attributes", "data.Name");

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
        await Service.DidNotReceiveWithAnyArgs().GetRecordAsync(
            default!, default!, default!, default, default, default,
            TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("--id", "")]
    [InlineData("--version", "0")]
    [InlineData("--endpoint", "https://example.com")]
    [InlineData("--data-partition", " ")]
    public async Task Execute_WithInvalidOption_DoesNotCallService(string option, string value)
    {
        var arguments = new List<string>
        {
            "--endpoint", option == "--endpoint" ? value : TestConstants.Endpoint,
            "--data-partition", option == "--data-partition" ? value : TestConstants.DataPartition,
            "--id", option == "--id" ? value : RecordId,
        };
        if (option == "--version")
        {
            arguments.AddRange(["--version", value]);
        }

        var response = await ExecuteCommandAsync([.. arguments]);

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
        await Service.DidNotReceiveWithAnyArgs().GetRecordAsync(
            default!, default!, default!, default, default, default,
            TestContext.Current.CancellationToken);
    }
}
