// Name:
// Student Admin No.:
// Tutorial Group:

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ArcaneVault.Pages.Categories;

public abstract class CategoryPageModel : PageModel
{
    protected IActionResult? HandleApiFailure(
        HttpStatusCode statusCode,
        ProblemDetails? problem,
        string fallbackMessage)
    {
        if (statusCode == HttpStatusCode.Unauthorized)
        {
            return Challenge();
        }

        if (statusCode == HttpStatusCode.Forbidden)
        {
            return Forbid();
        }

        if (statusCode == HttpStatusCode.NotFound)
        {
            return NotFound();
        }

        ModelState.AddModelError(
            string.Empty,
            problem?.Detail ?? problem?.Title ?? fallbackMessage);
        return null;
    }
}
