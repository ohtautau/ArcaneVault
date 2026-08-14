// Name:
// Student Admin No.:
// Tutorial Group:

using System.ComponentModel.DataAnnotations;

namespace ArcaneVault.Models.Requests;

public class UpdateTradeStatusRequest
{
    [Required, RegularExpression("^(Accepted|Declined|Cancelled)$")]
    public string Status { get; set; } = string.Empty;
}
