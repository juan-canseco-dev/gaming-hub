using System.Net;
using System.Text.Json;

namespace GameHub.Web.UI.Infrastructure.Http;

public static class ProblemDetailsReader
{
    public static async Task<ProblemDetailsError> ReadProblemAsync(
        this HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var fallback = new ProblemDetailsError($"Http.{(int)response.StatusCode}", GetFallbackMessage(response.StatusCode))
        {
            Status = (int)response.StatusCode,
            CorrelationId = response.Headers.TryGetValues("X-Correlation-ID", out var values)
                ? values.FirstOrDefault() : null
        };

        if (!string.Equals(response.Content.Headers.ContentType?.MediaType,
                "application/problem+json", StringComparison.OrdinalIgnoreCase))
            return fallback;

        try
        {
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return fallback;

            // RFC 9457: ignore incorrectly typed members and unknown extensions.
            var type = ResolveUri(ReadString(root, "type"), response.RequestMessage?.RequestUri) ?? "about:blank";
            var title = ReadString(root, "title");
            var detail = ReadString(root, "detail");
            var errors = ReadValidationErrors(root);
            var messages = errors.SelectMany(field => field.Value.Select(message =>
                string.IsNullOrWhiteSpace(field.Key) ? message : $"{field.Key}: {message}"));
            var description = string.Join(" ", new[] { detail ?? title }.Concat(messages)
                .Where(message => !string.IsNullOrWhiteSpace(message)).Distinct());

            return new ProblemDetailsError(
                ReadString(root, "code") ?? (type == "about:blank" ? fallback.Code : type),
                string.IsNullOrWhiteSpace(description) ? fallback.Description : description)
            {
                Type = type,
                Title = title,
                Detail = detail,
                // The HTTP status is authoritative; do not let advisory JSON status change behavior.
                Status = (int)response.StatusCode,
                Instance = ResolveUri(ReadString(root, "instance"), response.RequestMessage?.RequestUri),
                CorrelationId = ReadString(root, "correlationId") ?? fallback.CorrelationId,
                Errors = errors
            };
        }
        catch (JsonException)
        {
            return fallback;
        }
    }

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString() : null;

    private static string? ResolveUri(string? value, Uri? baseUri)
    {
        if (value is null || !Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var uri))
            return null;

        if (uri.IsAbsoluteUri)
            return uri.AbsoluteUri;

        return baseUri is { IsAbsoluteUri: true } && Uri.TryCreate(baseUri, uri, out var resolved)
            ? resolved.AbsoluteUri : value;
    }

    private static Dictionary<string, string[]> ReadValidationErrors(JsonElement root)
    {
        var result = new Dictionary<string, string[]>();
        // GameHub's ValidationProblemDetails extension uses field names mapped to string arrays.
        if (!root.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Object)
            return result;

        foreach (var field in errors.EnumerateObject())
        {
            if (field.Value.ValueKind != JsonValueKind.Array)
                continue;

            var messages = field.Value.EnumerateArray()
                .Where(value => value.ValueKind == JsonValueKind.String)
                .Select(value => value.GetString()!)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Distinct().ToArray();
            if (messages.Length > 0)
                result[field.Name] = messages;
        }
        return result;
    }

    private static string GetFallbackMessage(HttpStatusCode status) => status switch
    {
        HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => "The request is invalid. Check your input and try again.",
        HttpStatusCode.Unauthorized => "Please sign in to continue.",
        HttpStatusCode.Forbidden => "You do not have permission to perform this action.",
        HttpStatusCode.NotFound => "The requested resource could not be found.",
        HttpStatusCode.Conflict => "The request conflicts with the current state. Refresh and try again.",
        HttpStatusCode.TooManyRequests => "Too many requests. Please try again later.",
        HttpStatusCode.ServiceUnavailable => "The service is temporarily unavailable. Please try again later.",
        _ => "The request could not be completed. Please try again."
    };
}
