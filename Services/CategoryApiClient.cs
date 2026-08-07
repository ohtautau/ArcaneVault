// Name:
// Student Admin No.:
// Tutorial Group:

using System.Net.Http.Json;
using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using ArcaneVault.Models.Results;
using Microsoft.AspNetCore.Mvc;

namespace ArcaneVault.Services;

public class CategoryApiClient(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor) : ICategoryApiClient
{
    private const string AuthenticationCookieName = ".ArcaneVault.Auth";

    public Task<ApiResult<IReadOnlyList<CategoryResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<CategoryResponse>>(
            HttpMethod.Get,
            "api/categories",
            null,
            cancellationToken);

    public Task<ApiResult<CategoryResponse>> GetByCodeAsync(
        string categoryCode,
        CancellationToken cancellationToken = default) =>
        SendAsync<CategoryResponse>(
            HttpMethod.Get,
            $"api/categories/{Uri.EscapeDataString(categoryCode)}",
            null,
            cancellationToken);

    public Task<ApiResult<CategoryResponse>> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<CategoryResponse>(
            HttpMethod.Post,
            "api/categories",
            JsonContent.Create(request),
            cancellationToken);

    public Task<ApiResult<CategoryResponse>> UpdateAsync(
        string categoryCode,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<CategoryResponse>(
            HttpMethod.Put,
            $"api/categories/{Uri.EscapeDataString(categoryCode)}",
            JsonContent.Create(request),
            cancellationToken);

    public async Task<ApiResult> DeleteAsync(
        string categoryCode,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(
            HttpMethod.Delete,
            $"api/categories/{Uri.EscapeDataString(categoryCode)}");
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

        client.BaseAddress = new Uri(
            $"{request.Scheme}://{request.Host}{request.PathBase}/");

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
            return await response.Content
                .ReadFromJsonAsync<ProblemDetails>(cancellationToken);
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
