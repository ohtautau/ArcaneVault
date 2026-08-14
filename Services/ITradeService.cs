using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using ArcaneVault.Models.Results;
namespace ArcaneVault.Services;
public interface ITradeService
{
    Task<IReadOnlyList<TradeResponse>> GetForUserAsync(string userName, CancellationToken ct = default);
    Task<IReadOnlyList<TradeResponse>> GetAllActiveAsync(CancellationToken ct = default);
    Task<TradeResult> GetByIdAsync(int id, string userName, bool isStaff, CancellationToken ct = default);
    Task<TradeResult> CreateAsync(string userName, CreateTradeRequest request, CancellationToken ct = default);
    Task<TradeResult> UpdateStatusAsync(int id, string userName, string status, CancellationToken ct = default);
    Task<TradeResult> StaffCancelAsync(int id, string staffUserName, string note, CancellationToken ct = default);
}
