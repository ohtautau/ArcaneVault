using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using ArcaneVault.Models.Results;
using ArcaneVault.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ArcaneVault.Controllers;
[ApiController, Authorize, Route("api/trades")]
public class TradesController(ITradeService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<TradeResponse>>> GetMine(CancellationToken ct) => Ok(await service.GetForUserAsync(CurrentUser, ct));
    [HttpGet("active"), Authorize(Roles = "Staff")] public async Task<ActionResult<IReadOnlyList<TradeResponse>>> GetActive(CancellationToken ct) => Ok(await service.GetAllActiveAsync(ct));
    [HttpGet("{id:int}")] public async Task<ActionResult<TradeResponse>> Get(int id, CancellationToken ct) => Map(await service.GetByIdAsync(id, CurrentUser, User.IsInRole("Staff"), ct));
    [HttpPost] public async Task<ActionResult<TradeResponse>> Create(CreateTradeRequest request, CancellationToken ct) { var r = await service.CreateAsync(CurrentUser, request, ct); return r.Status == TradeStatus.Success && r.Response is not null ? StatusCode(201, r.Response) : Map(r); }
    [HttpPatch("{id:int}/status")] public async Task<ActionResult<TradeResponse>> Status(int id, UpdateTradeStatusRequest request, CancellationToken ct) => Map(await service.UpdateStatusAsync(id, CurrentUser, request.Status, ct));
    [HttpPost("{id:int}/staff-cancel"), Authorize(Roles = "Staff")] public async Task<ActionResult<TradeResponse>> StaffCancel(int id, StaffCancelTradeRequest request, CancellationToken ct) => Map(await service.StaffCancelAsync(id, CurrentUser, request.ResolutionNote, ct));
    private string CurrentUser => User.Identity?.Name ?? throw new InvalidOperationException("Authenticated username unavailable.");
    private ActionResult<TradeResponse> Map(TradeResult r) => r.Status switch
    {
        TradeStatus.Success when r.Response is not null => Ok(r.Response), TradeStatus.NotFound => Problem(statusCode: 404, title: "Trade not found."),
        TradeStatus.Forbidden => Forbid(), TradeStatus.InvalidItems or TradeStatus.SameUser => Problem(statusCode: 400, title: "Invalid trade.", detail: r.Detail),
        TradeStatus.ItemsInActiveTrade or TradeStatus.NotPending or TradeStatus.CannotReverse => Problem(statusCode: 409, title: "Trade conflict.", detail: r.Detail),
        _ => Problem(statusCode: 409, title: "Trade conflict.", detail: r.Detail ?? "The trade could not be updated.")
    };
}
