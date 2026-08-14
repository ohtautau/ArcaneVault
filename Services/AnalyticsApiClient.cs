// Name:
// Student Admin No.:
// Tutorial Group:

using System.Net.Http.Json;
using ArcaneVault.Models.Responses;
using ArcaneVault.Models.Results;
using Microsoft.AspNetCore.Mvc;

namespace ArcaneVault.Services;

public class AnalyticsApiClient(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor) : IAnalyticsApiClient
{
    private const string AuthenticationCookieName = ".ArcaneVault.Auth";

    public async Task<ApiResult<AnalyticsDashboardResponse>> GetDashboardAsync(
        int days,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/analytics?days={days}");
        var context = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("No active HTTP request is available.");
        if (context.Request.Cookies.TryGetValue(AuthenticationCookieName, out var cookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", $"{AuthenticationCookieName}={cookie}");
        }

        using var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri($"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}/");
        using var response = await client.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return new ApiResult<AnalyticsDashboardResponse>(
                response.StatusCode,
                await response.Content.ReadFromJsonAsync<AnalyticsDashboardResponse>(cancellationToken));
        }

        ProblemDetails? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        }
        catch (Exception exception) when (exception is NotSupportedException or System.Text.Json.JsonException)
        {
            // The page provides a safe fallback message when an API error has no problem body.
        }

        return new ApiResult<AnalyticsDashboardResponse>(response.StatusCode, Problem: problem);
    }
}
