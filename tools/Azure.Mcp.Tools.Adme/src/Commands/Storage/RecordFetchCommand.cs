// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tools.Adme.Models.Storage;
using Azure.Mcp.Tools.Adme.Options.Storage;
using Azure.Mcp.Tools.Adme.Services;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.Adme.Commands.Storage;

/// <summary>
/// Fetches multiple OSDU records by id in a single call.
/// </summary>
[CommandMetadata(
    Id = "513e8747-ce60-43fd-99bf-19ff211f3679",
    Name = "fetch",
    Title = "Fetch ADME Records",
    Description = """
        Retrieve multiple OSDU records by id in a single call.

        Required: --ids, --endpoint, and --data-partition. --attributes projects dotted-path fields
        such as 'data.Name'. The id limit is 20 without --attributes and 100 with it.

        --frame-of-reference converts measurements to SI, coordinates to WGS84 and dates to UTC.
        It cannot be combined with --attributes.
        """,
    Destructive = false,
    Idempotent = true,
    OpenWorld = false,
    ReadOnly = true,
    LocalRequired = false,
    Secret = false)]
public sealed class RecordFetchCommand(IStorageService storageService)
    : AuthenticatedCommand<RecordFetchOptions, FetchRecordsResponse>
{
    private const int MaxBatchIds = 20;
    private const int MaxProjectionIds = 100;
    private readonly IStorageService _storageService = storageService;

    public override void ValidateOptions(RecordFetchOptions options, ValidationResult validationResult)
    {
        base.ValidateOptions(options, validationResult);
        AdmeServiceHelper.ValidateTarget(options.Endpoint, options.DataPartition, validationResult);

        var projecting = options.Attributes is { Length: > 0 };
        if (options.Ids is not { Length: > 0 })
        {
            validationResult.Errors.Add("--ids must contain at least one record id.");
        }
        else if (options.Ids.Length > (projecting ? MaxProjectionIds : MaxBatchIds))
        {
            validationResult.Errors.Add(projecting
                ? $"--ids must contain at most {MaxProjectionIds} ids when --attributes is used."
                : $"--ids must contain at most {MaxBatchIds} ids.");
        }

        if (projecting && options.FrameOfReference)
        {
            validationResult.Errors.Add("--frame-of-reference cannot be combined with --attributes.");
        }
    }

    public override async Task<CommandResponse> ExecuteAsync(
        CommandContext context, RecordFetchOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _storageService.FetchRecordsAsync(
                options.Endpoint, options.DataPartition, options.Ids, options.Attributes,
                options.FrameOfReference, options.Tenant, cancellationToken);
            context.Response.Results = ResponseResult.Create(result, AdmeJsonContext.Default.FetchRecordsResponse);
        }
        catch (Exception ex)
        {
            HandleException(context, ex);
        }

        return context.Response;
    }
}