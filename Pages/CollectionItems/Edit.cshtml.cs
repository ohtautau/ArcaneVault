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
public class EditModel(
    ICollectionItemApiClient collectionItemApiClient,
    ICategoryApiClient categoryApiClient) : CollectionItemPageModel
{
    [BindProperty]
    public UpdateCollectionItemRequest Input { get; set; } = new();

    public int ItemId { get; private set; }

    public int StartingQuantity { get; private set; }
    public string ItemTypeId { get; private set; } = string.Empty;

    public IReadOnlyList<CategoryResponse> Categories { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(
        int itemId,
        CancellationToken cancellationToken)
    {
        var itemResult = await collectionItemApiClient.GetByIdAsync(itemId, cancellationToken);
        if (!itemResult.IsSuccess || itemResult.Value is null)
        {
            return HandleApiFailure(
                    itemResult.StatusCode,
                    itemResult.Problem,
                    "The collection item could not be loaded.")
                ?? Page();
        }

        ItemId = itemResult.Value.ItemId;
        StartingQuantity = itemResult.Value.StartingQuantity;
        ItemTypeId = itemResult.Value.ItemTypeId;
        Input.ItemName = itemResult.Value.ItemName;
        Input.CurrentQuantity = itemResult.Value.CurrentQuantity;
        Input.CategoryCodes = itemResult.Value.Categories
            .Select(category => category.CategoryCode)
            .ToList();

        return await LoadCategoriesAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(
        int itemId,
        CancellationToken cancellationToken)
    {
        ItemId = itemId;
        if (!ModelState.IsValid)
        {
            await LoadSupportingDataAsync(itemId, cancellationToken);
            return Page();
        }

        var result = await collectionItemApiClient.UpdateAsync(
            itemId,
            Input,
            cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            return RedirectToPage("Details", new { itemId = result.Value.ItemId });
        }

        var failure = HandleApiFailure(
            result.StatusCode,
            result.Problem,
            "The collection item could not be updated.");
        if (failure is not null)
        {
            return failure;
        }

        await LoadSupportingDataAsync(itemId, cancellationToken);
        return Page();
    }

    private async Task LoadSupportingDataAsync(
        int itemId,
        CancellationToken cancellationToken)
    {
        var itemResult = await collectionItemApiClient.GetByIdAsync(itemId, cancellationToken);
        if (itemResult.IsSuccess && itemResult.Value is not null)
        {
            StartingQuantity = itemResult.Value.StartingQuantity;
            ItemTypeId = itemResult.Value.ItemTypeId;
        }

        await LoadCategoriesAsync(cancellationToken);
    }

    private async Task<IActionResult> LoadCategoriesAsync(CancellationToken cancellationToken)
    {
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
