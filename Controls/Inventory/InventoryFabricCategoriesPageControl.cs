using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Helpers;
using ERPSystem.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Inventory;

public sealed class InventoryFabricCategoriesPageControl : UserControl
{
    private readonly ContentPresenter _host = new();

    public InventoryFabricCategoriesPageControl()
    {
        Content = _host;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (!AppServices.IsInitialized)
        {
            _host.Content = PlaceholderUi.EmptyMessage("لا توجد تصنيفات أقمشة");
            return;
        }

        try
        {
            using var scope = AppServices.CreateScope();
            var branch = scope.ServiceProvider.GetRequiredService<ICurrentBranchService>();
            var catalog = scope.ServiceProvider.GetRequiredService<IFabricCatalogRepository>();
            if (branch.CompanyId is not Guid companyId)
            {
                _host.Content = PlaceholderUi.EmptyMessage("لا توجد تصنيفات أقمشة");
                return;
            }

            var categories = await catalog.GetCategoriesAsync(companyId);
            var items = await catalog.GetItemsAsync(companyId);
            if (categories.Count == 0 && items.Count == 0)
            {
                _host.Content = PlaceholderUi.EmptyMessage(
                    "لا توجد تصنيفات أو أصناف أقمشة",
                    "تُضاف التصنيفات عند استيراد الحاويات أو من إعدادات الكتالوج");
                return;
            }

            var rows = items
                .Select(i => new
                {
                    نوع_البضاعة = categories.FirstOrDefault(c => c.Id == i.CategoryId)?.NameAr ?? "—",
                    كود_التوب = i.Code,
                    الاسم = i.NameAr,
                    الحالة = "نشط"
                })
                .ToArray();

            _host.Content = ErpUiFactory.Card(ErpUiFactory.BuildGrid(rows, false));
        }
        catch
        {
            _host.Content = PlaceholderUi.EmptyMessage("لا توجد تصنيفات أقمشة");
        }
    }
}
