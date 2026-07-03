using ERPSystem.Controls;
using ERPSystem.Controls.Capital;
using ERPSystem.Core;
using ERPSystem.Views.Reports;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Views.Capital;

public static class CapitalViews
{
    public static UserControl Create(string key) => key switch
    {
        "List" => Wrap(new CapitalPartnerListPageControl()),
        "Transactions" => Wrap(new CapitalTransactionListPageControl()),
        "Investment" => Wrap(new CapitalInvestmentFormControl()),
        "Form" => Wrap(new CapitalPartnerFormControl()),
        "Workspace" => CreateWorkspace(),
        "Reports" => ModuleReportsViews.CreateHub(AppModule.CapitalPartners),
        "Distributions" => Wrap(new CapitalDistributionsControl()),
        _ => Wrap(new CapitalPartnerListPageControl())
    };

    private static UserControl CreateWorkspace()
    {
        var (id, tab) = Services.Capital.CapitalNavigationContext.TakeWorkspaceContext();
        var ctrl = new CapitalOperationsCenterControl();
        if (id is Guid partnerId)
            ctrl.Initialize(partnerId, tab);
        return Wrap(ctrl);
    }

    private static UserControl Wrap(UIElement content) => new() { Content = content };
}
