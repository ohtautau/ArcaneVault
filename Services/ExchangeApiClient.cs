// Name:
// Student Admin No.:
// Tutorial Group:

using System.Net.Http.Json;
using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using ArcaneVault.Models.Results;
using Microsoft.AspNetCore.Mvc;

namespace ArcaneVault.Services;

public class ExchangeApiClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor) : IExchangeApiClient
{
    private const string CookieName = ".ArcaneVault.Auth";
    public Task<ApiResult<IReadOnlyList<WishlistItemResponse>>> GetWishlistAsync(string? search, bool othersOnly, CancellationToken ct = default) => SendAsync<IReadOnlyList<WishlistItemResponse>>(HttpMethod.Get, $"api/wishlist?search={Uri.EscapeDataString(search ?? "")}&othersOnly={othersOnly}", null, ct);
    public Task<ApiResult<WishlistItemResponse>> GetWishlistItemAsync(int id, CancellationToken ct = default) => SendAsync<WishlistItemResponse>(HttpMethod.Get, $"api/wishlist/{id}", null, ct);
    public Task<ApiResult<WishlistItemResponse>> CreateWishlistItemAsync(CreateWishlistItemRequest request, CancellationToken ct = default) => SendAsync<WishlistItemResponse>(HttpMethod.Post, "api/wishlist", JsonContent.Create(request), ct);
    public Task<ApiResult<WishlistItemResponse>> UpdateWishlistItemAsync(int id, UpdateWishlistItemRequest request, CancellationToken ct = default) => SendAsync<WishlistItemResponse>(HttpMethod.Put, $"api/wishlist/{id}", JsonContent.Create(request), ct);
    public async Task<ApiResult> DeleteWishlistItemAsync(int id, CancellationToken ct = default) { var result = await SendAsync<object>(HttpMethod.Delete, $"api/wishlist/{id}", null, ct); return new(result.StatusCode, result.Problem); }
    public Task<ApiResult<IReadOnlyList<TradeResponse>>> GetTradesAsync(CancellationToken ct = default) => SendAsync<IReadOnlyList<TradeResponse>>(HttpMethod.Get, "api/trades", null, ct);
    public Task<ApiResult<TradeResponse>> GetTradeAsync(int id, CancellationToken ct = default) => SendAsync<TradeResponse>(HttpMethod.Get, $"api/trades/{id}", null, ct);
    public Task<ApiResult<IReadOnlyList<TradeResponse>>> GetActiveTradesAsync(CancellationToken ct = default) => SendAsync<IReadOnlyList<TradeResponse>>(HttpMethod.Get, "api/trades/active", null, ct);
    public Task<ApiResult<IReadOnlyList<TradeResponse>>> GetAllStaffTradesAsync(CancellationToken ct = default) => SendAsync<IReadOnlyList<TradeResponse>>(HttpMethod.Get, "api/trades/staff-all", null, ct);
    public Task<ApiResult<IReadOnlyList<TradePartnerResponse>>> GetTradePartnersAsync(CancellationToken ct = default) => SendAsync<IReadOnlyList<TradePartnerResponse>>(HttpMethod.Get, "api/trades/partners", null, ct);
    public Task<ApiResult<TradeResponse>> CreateTradeAsync(CreateTradeRequest request, CancellationToken ct = default) => SendAsync<TradeResponse>(HttpMethod.Post, "api/trades", JsonContent.Create(request), ct);
    public Task<ApiResult<TradeResponse>> UpdateTradeStatusAsync(int id, string status, string? disputeReason = null, CancellationToken ct = default) => SendAsync<TradeResponse>(HttpMethod.Patch, $"api/trades/{id}/status", JsonContent.Create(new UpdateTradeStatusRequest { Status = status, DisputeReason = disputeReason }), ct);
    public Task<ApiResult<TradeResponse>> StaffCancelTradeAsync(int id, string note, CancellationToken ct = default) => SendAsync<TradeResponse>(HttpMethod.Post, $"api/trades/{id}/staff-cancel", JsonContent.Create(new StaffCancelTradeRequest { ResolutionNote = note }), ct);
    public Task<ApiResult<TradeResponse>> StaffResolveTradeAsync(int id, string resolution, string note, CancellationToken ct = default) => SendAsync<TradeResponse>(HttpMethod.Post, $"api/trades/{id}/staff-resolve", JsonContent.Create(new StaffResolveTradeRequest { Resolution = resolution, ResolutionNote = note }), ct);

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string uri, HttpContent? content, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, uri) { Content = content };
        var context = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No active request.");
        if (context.Request.Cookies.TryGetValue(CookieName, out var cookie)) request.Headers.TryAddWithoutValidation("Cookie", $"{CookieName}={cookie}");
        using var client = httpClientFactory.CreateClient(); client.BaseAddress = new Uri($"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}/");
        using var response = await client.SendAsync(request, ct);
        if (response.IsSuccessStatusCode)
        {
            if (response.Content.Headers.ContentLength == 0) return new(response.StatusCode);
            return new(response.StatusCode, await response.Content.ReadFromJsonAsync<T>(ct));
        }
        ProblemDetails? problem = null;
        try { problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct); } catch (Exception exception) when (exception is NotSupportedException or System.Text.Json.JsonException) { }
        return new(response.StatusCode, Problem: problem);
    }
}
