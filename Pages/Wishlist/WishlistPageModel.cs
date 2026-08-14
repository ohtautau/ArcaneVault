// Name:
// Student Admin No.:
// Tutorial Group:

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ArcaneVault.Pages.Wishlist;

public abstract class WishlistPageModel : PageModel
{
    protected IActionResult? HandleFailure(HttpStatusCode status, Microsoft.AspNetCore.Mvc.ProblemDetails? problem, string fallback)
    {
        if (status == HttpStatusCode.Unauthorized) return Challenge();
        if (status == HttpStatusCode.Forbidden) return Forbid();
        if (status == HttpStatusCode.NotFound) return NotFound();
        ModelState.AddModelError(string.Empty, problem?.Detail ?? problem?.Title ?? fallback);
        return null;
    }
}
