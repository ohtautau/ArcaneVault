// Name:
// Student Admin No.:
// Tutorial Group:

using System.Net;
using System.Net.Http.Json;
using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ArcaneVault.Pages.Account;

public class RegisterModel(IHttpClientFactory httpClientFactory) : PageModel
{
    [BindProperty]
    public RegisterRequest Input { get; set; } = new();

    public string? SuccessMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}{Request.PathBase}/");

        try
        {
            var response = await client.PostAsJsonAsync(
                "api/account/register",
                Input,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.Created)
            {
                var registration = await response.Content
                    .ReadFromJsonAsync<RegisterResponse>(cancellationToken);

                SuccessMessage = registration?.Message ?? "Registration successful.";
                ModelState.Clear();
                Input = new RegisterRequest();
                return Page();
            }

            await AddApiErrorsAsync(response, cancellationToken);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The registration service is unavailable. Try again shortly.");
        }

        return Page();
    }

    private async Task AddApiErrorsAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validationProblem = await response.Content
                .ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);

            if (validationProblem?.Errors is not null)
            {
                foreach (var (field, errors) in validationProblem.Errors)
                {
                    var modelField = string.IsNullOrWhiteSpace(field)
                        ? string.Empty
                        : $"Input.{field}";

                    foreach (var error in errors)
                    {
                        ModelState.AddModelError(modelField, error);
                    }
                }

                return;
            }
        }

        var problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>(cancellationToken);

        ModelState.AddModelError(
            string.Empty,
            problem?.Detail ?? problem?.Title ?? "Registration failed. Try again.");
    }
}
