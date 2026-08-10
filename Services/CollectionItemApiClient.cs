// Name:
// Student Admin No.:
// Tutorial Group:

using System.Net.Http.Json;
using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using ArcaneVault.Models.Results;
using Microsoft.AspNetCore.Mvc;

namespace ArcaneVault.Services;

public class CollectionItemApiClient(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor) : ICollectionItemApiClient
{
    private const string AuthenticationCookieName = ".ArcaneVault.Auth";

    public Task<ApiResult<IReadOnlyList<CollectionItemResponse>>> GetAllAsync(
        string? search = null,
        CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<CollectionItemResponse>>(
            HttpMethod.Get,
            string.IsNullOrWhiteSpace(search)
                ? "api/collection-items"
                : $"api/collection-items?search={Uri.EscapeDataString(search.Trim())}",
            null,
            cancellationToken);

    public Task<ApiResult<CollectionItemResponse>> GetByIdAsync(
        int itemId,
        CancellationToken cancellationToken = default) =>
        SendAsync<CollectionItemResponse>(
            HttpMethod.Get,
            $"api/collection-items/{itemId}",
            null,
            cancellationToken);

    public Task<ApiResult<CollectionItemResponse>> CreateAsync(
        CreateCollectionItemRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<CollectionItemResponse>(
            HttpMethod.Post,
            "api/collection-items",
            JsonContent.Create(request),
            cancellationToken);

    public Task<ApiResult<CollectionItemResponse>> UpdateAsync(
        int itemId,
        UpdateCollectionItemRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<CollectionItemResponse>(
            HttpMethod.Put,
            $"api/collection-items/{itemId}",
            JsonContent.Create(request),
            cancellationToken);

    public async Task<ApiResult> DeleteAsync(
        int itemId,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Delete, $"api/collection-items/{itemId}");
        using var client = CreateClient();
        using var response = await client.SendAsync(request, cancellationToken);

        return new ApiResult(
            response.StatusCode,
            await ReadProblemAsync(response, cancellationToken));
    }

    private async Task<ApiResult<T>> SendAsync<T>(
        HttpMethod method,
        string requestUri,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(method, requestUri);
        request.Content = content;
        using var client = CreateClient();
        using var response = await client.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            return new ApiResult<T>(response.StatusCode, value);
        }

        return new ApiResult<T>(
            response.StatusCode,
            Problem: await ReadProblemAsync(response, cancellationToken));
    }

    private HttpClient CreateClient()
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("No active HTTP request is available.");
        var request = httpContext.Request;
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri($"{request.Scheme}://{request.Host}{request.PathBase}/");
        return client;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string requestUri)
    {
        var request = new HttpRequestMessage(method, requestUri);
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("No active HTTP request is available.");

        if (httpContext.Request.Cookies.TryGetValue(
            AuthenticationCookieName,
            out var authenticationCookie))
        {
            request.Headers.TryAddWithoutValidation(
                "Cookie",
                $"{AuthenticationCookieName}={authenticationCookie}");
        }

        return request;
    }

    private static async Task<ProblemDetails?> ReadProblemAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength == 0)
        {
            return null;
        }

        try
        {
            return await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        }
        catch (NotSupportedException)
        {
            return null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }
}
