// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using ArcaneVault.Models.Results;
using ArcaneVault.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArcaneVault.Controllers;

[ApiController, Authorize, Route("api/trade-requests")]
public class TradeRequestsController(ITradeRequestService tradeRequestService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TradeRequestResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await tradeRequestService.GetForUserAsync(CurrentUserName, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<TradeRequestResponse>> Create(CreateTradeRequest request, CancellationToken cancellationToken)
    {
        var result = await tradeRequestService.CreateAsync(CurrentUserName, request, cancellationToken);
        return result.Status switch
        {
            TradeRequestStatus.Success when result.Response is not null => StatusCode(201, result.Response),
            TradeRequestStatus.NotFound => Problem(statusCode: 404, title: "Wishlist item not found."),
            TradeRequestStatus.OwnWishlist => Problem(statusCode: 400, title: "Invalid trade.", detail: "You cannot trade against your own wishlist."),
            TradeRequestStatus.InvalidOffer => Problem(statusCode: 400, title: "Invalid offer.", detail: "The offered collection item must belong to you."),
            TradeRequestStatus.AlreadyPending => Problem(statusCode: 409, title: "Trade already pending.", detail: "You already have a pending trade for this wishlist item."),
            _ => Problem(statusCode: 409, title: "Trade conflict.", detail: "The trade request could not be created.")
        };
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<TradeRequestResponse>> UpdateStatus(int id, UpdateTradeStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await tradeRequestService.UpdateStatusAsync(id, CurrentUserName, request.Status, cancellationToken);
        return result.Status switch
        {
            TradeRequestStatus.Success when result.Response is not null => Ok(result.Response),
            TradeRequestStatus.NotFound => Problem(statusCode: 404, title: "Trade request not found."),
            TradeRequestStatus.Forbidden => Forbid(),
            TradeRequestStatus.NotPending => Problem(statusCode: 409, title: "Trade is already resolved."),
            _ => Problem(statusCode: 409, title: "Trade conflict.", detail: "The trade request could not be updated.")
        };
    }
    private string CurrentUserName => User.Identity?.Name ?? throw new InvalidOperationException("Authenticated username is unavailable.");
}
