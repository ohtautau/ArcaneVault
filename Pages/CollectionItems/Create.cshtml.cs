// Name:
// Student Admin No.:
// Tutorial Group:

using ArcaneVault.Models.Requests;
using ArcaneVault.Models.Responses;
using ArcaneVault.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArcaneVault.Pages.CollectionItems;

[Authorize]
public class CreateModel(
    ICollectionItemApiClient collectionItemApiClient,
    ICategoryApiClient categoryApiClient) : CollectionItemPageModel
{
    [BindProperty]
    public CreateCollectionItemRequest Input { get; set; } = new();

    [BindProperty] public IFormFile? Photo { get; set; }

    public IReadOnlyList<CategoryResponse> Categories { get; private set; } = [];
    public IReadOnlyList<ItemTypeResponse> ItemTypes { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken) =>
        await LoadCategoriesAsync(cancellationToken);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(cancellationToken);
            return Page();
        }

        var result = await collectionItemApiClient.CreateAsync(Input, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            if (Photo is not null)
            {
                var imageResult = await collectionItemApiClient.UploadImageAsync(result.Value.ItemId, Photo, cancellationToken);
                if (!imageResult.IsSuccess)
                    TempData["CollectionWarning"] = imageResult.Problem?.Detail ?? "The item was created, but its photo could not be uploaded.";
            }
            return RedirectToPage("Details", new { itemId = result.Value.ItemId });
        }

        var failure = HandleApiFailure(
            result.StatusCode,
            result.Problem,
            "The collection item could not be created.");
        if (failure is not null)
        {
            return failure;
        }

        await LoadCategoriesAsync(cancellationToken);
        return Page();
    }

    private async Task<IActionResult> LoadCategoriesAsync(CancellationToken cancellationToken)
    {
        var typesResult = await collectionItemApiClient.GetItemTypesAsync(null, cancellationToken);
        if (typesResult.IsSuccess && typesResult.Value is not null) ItemTypes = typesResult.Value;
        var result = await categoryApiClient.GetAllAsync(cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            Categories = result.Value;
            return Page();
        }

        return HandleApiFailure(
                result.StatusCode,
                result.Problem,
                "Categories could not be loaded.")
            ?? Page();
    }
}
