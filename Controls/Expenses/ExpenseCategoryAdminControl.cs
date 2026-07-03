using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Expenses;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Expenses;

/// <summary>Placeholder for future custom category management — system categories are seeded in PostgreSQL.</summary>
public sealed class ExpenseCategoryAdminControl : UserControl
{
    public ExpenseCategoryAdminControl()
    {
        var stack = new StackPanel { Margin = new Thickness(16) };
        stack.Children.Add(ErpUiFactory.SectionTitle("فئات المصاريف"));
        stack.Children.Add(ErpUxFactory.InfoBanner(
            "الفئات الرئيسية (رأسمالية، شخصية، تشغيلية) مُعرَّفة في قاعدة البيانات ويمكن توسيعها دون تغيير منطق الأعمال.",
            "info"));

        Loaded += async (_, _) =>
        {
            var result = await ExpenseUiService.Instance.GetCategoriesAsync();
            if (!ApplicationResultPresenter.Present(result))
                return;

            stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildGrid(
                (result.Value ?? []).Select(c => new
                {
                    الكود = c.Code,
                    الاسم = c.NameAr,
                    النوع = c.KindDisplay
                }).ToList(), false)));
        };

        Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = stack };
    }
}
