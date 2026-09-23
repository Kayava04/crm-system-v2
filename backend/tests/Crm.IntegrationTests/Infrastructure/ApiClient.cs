using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Crm.IntegrationTests.Infrastructure;

public sealed class ApiResponse(HttpStatusCode status, string raw)
{
    public HttpStatusCode Status { get; } = status;
    public int Code => (int)Status;
    public string Raw { get; } = raw;
    public JsonNode? Json { get; } = string.IsNullOrWhiteSpace(raw) ? null : TryParse(raw);

    public JsonNode this[string key] => Json?[key] ?? throw new InvalidOperationException($"No '{key}' in response {Code}: {Raw}");

    public Guid Id => Guid.Parse(this["id"].GetValue<string>());

    public JsonArray Items => this["items"].AsArray();

    // Fails with the response body, which makes a broken test explain itself
    public ApiResponse Expect(int code)
    {
        Assert.True(Code == code, $"Expected HTTP {code} but got {Code}: {Raw}");
        return this;
    }

    private static JsonNode? TryParse(string raw)
    {
        try { return JsonNode.Parse(raw); }
        catch (JsonException) { return null; }
    }
}

public sealed record DownloadedFile(HttpStatusCode Status, string? ContentType, string? FileName, byte[] Content)
{
    public int Code => (int)Status;

    public DownloadedFile Expect(int code)
    {
        Assert.True(Code == code, $"Expected HTTP {code} but got {Code}: {System.Text.Encoding.UTF8.GetString(Content)}");
        return this;
    }

    public JsonNode Json => JsonNode.Parse(Content)!;

    public ClosedXML.Excel.XLWorkbook Workbook => new(new MemoryStream(Content));
}

public sealed class Api(HttpClient http)
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public HttpClient Http { get; } = http;

    public async Task<ApiResponse> SendAsync(HttpMethod method, string path, object? body = null, string? token = null)
    {
        using var request = new HttpRequestMessage(method, path);

        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);

        using var response = await Http.SendAsync(request);

        return new ApiResponse(response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    // A file upload as a browser form would send it
    public async Task<ApiResponse> UploadAsync(string path, string fileName, byte[] content, string? token = null, string field = "file", HttpMethod? method = null)
    {
        using var request = new HttpRequestMessage(method ?? HttpMethod.Post, path);

        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, field, fileName);
        request.Content = form;

        using var response = await Http.SendAsync(request);

        return new ApiResponse(response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    public async Task<DownloadedFile> DownloadAsync(string path, string? token = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);

        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await Http.SendAsync(request);
        var bytes = await response.Content.ReadAsByteArrayAsync();

        return new DownloadedFile(
            response.StatusCode,
            response.Content.Headers.ContentType?.MediaType,
            response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"'),
            bytes);
    }

    public Task<ApiResponse> GetAsync(string path, string? token = null) => SendAsync(HttpMethod.Get, path, null, token);
    public Task<ApiResponse> PostAsync(string path, object? body, string? token = null) => SendAsync(HttpMethod.Post, path, body, token);
    public Task<ApiResponse> PutAsync(string path, object? body, string? token = null) => SendAsync(HttpMethod.Put, path, body, token);
    public Task<ApiResponse> PatchAsync(string path, object? body, string? token = null) => SendAsync(HttpMethod.Patch, path, body, token);
    public Task<ApiResponse> DeleteAsync(string path, object? body, string? token = null) => SendAsync(HttpMethod.Delete, path, body, token);
}
