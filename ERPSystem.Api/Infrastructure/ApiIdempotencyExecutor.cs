using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ERPSystem.Application.Abstractions.Services;
using Microsoft.AspNetCore.Http;

namespace ERPSystem.Api.Infrastructure;

public static class ApiIdempotencyExecutor
{
    public const string HeaderName = "Idempotency-Key";

    public static string ComputeRequestHash(object payload) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload))));

    public static async Task<IResult?> TryBeginAsync(
        HttpContext httpContext,
        IAccountingIdempotencyService idempotencyService,
        Guid companyId,
        Guid userId,
        string operation,
        string requestHash,
        CancellationToken cancellationToken)
    {
        if (!httpContext.Request.Headers.TryGetValue(HeaderName, out var keyValues))
            return null;

        var idempotencyKey = keyValues.ToString();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return null;

        var begin = await idempotencyService.BeginAsync(
            companyId,
            userId,
            operation,
            idempotencyKey,
            requestHash,
            cancellationToken);

        if (begin.IsConflict)
            return Results.Conflict(new { error = "idempotency_key_conflict", message = "Idempotency key reused with a different request body." });

        if (begin.IsReplay && begin.StoredResponseJson is not null)
            return Results.Content(begin.StoredResponseJson, "application/json");

        httpContext.Items["IdempotencyRecordId"] = begin.RecordId;
        return null;
    }

    public static async Task CompleteAsync(
        HttpContext httpContext,
        IAccountingIdempotencyService idempotencyService,
        object responseBody,
        CancellationToken cancellationToken)
    {
        if (httpContext.Items["IdempotencyRecordId"] is not Guid recordId)
            return;

        var json = JsonSerializer.Serialize(responseBody);
        await idempotencyService.CompleteAsync(recordId, json, cancellationToken);
    }
}
