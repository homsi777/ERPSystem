using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Customers;
using ERPSystem.Services.China;
using ERPSystem.Services.Capital;
using ERPSystem.Services.Expenses;
using ERPSystem.Application.DTOs.Capital;
using ERPSystem.Application.DTOs.Expenses;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace ERPSystem.Controls.OperationsCenter
{
    public static class OperationsCenterShell
    {
        public static UserControl Build(OperationsCenterSpec spec)
        {
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = ErpDesignTokens.PagePadding
            };

            var root = new StackPanel { MaxWidth = 1400 };

            if (!string.IsNullOrEmpty(spec.Breadcrumb))
            {
                root.Children.Add(new TextBlock
                {
                    Text = spec.Breadcrumb,
                    FontSize = ErpDesignTokens.FontCaption,
                    Foreground = Br("TextMutedBrush"),
                    Margin = new Thickness(0, 0, 0, ErpDesignTokens.SpaceSm),
                    FontFamily = Ff()
                });
            }

            root.Children.Add(BuildHeader(spec));

            if (spec.Kpis.Count > 0)
                root.Children.Add(BuildKpiRow(spec));

            if (spec.Workflow is { Count: > 0 })
                root.Children.Add(ErpUxFactory.WorkflowStepper(spec.Workflow.ToArray()));

            TabControl? tabs = null;
            if (spec.Tabs.Count > 0)
            {
                tabs = BuildTabs(spec);
                root.Children.Add(tabs);
            }

            if (spec.QuickActions.Count > 0 && tabs != null)
                root.Children.Insert(root.Children.IndexOf(tabs), BuildQuickActions(spec, tabs));

            scroll.Content = root;
            return new UserControl { Content = scroll, Background = Br("AppBgBrush") as SolidColorBrush };
        }

        private static Border BuildHeader(OperationsCenterSpec spec)
        {
            var card = ErpUiFactory.Card(new StackPanel(), new Thickness(0, 0, 0, ErpDesignTokens.SpaceMd));
            card.Padding = new Thickness(0);
            if (card.Child is StackPanel outer)
            {
                outer.Children.Add(new Border
                {
                    Height = 3,
                    CornerRadius = new CornerRadius(ErpDesignTokens.CardRadius, ErpDesignTokens.CardRadius, 0, 0),
                    Background = spec.Accent
                });

                var body = new Grid { Margin = ErpDesignTokens.CardPadding };
                body.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var iconBadge = ErpUiFactory.IconBadge(
                    spec.IconGlyph, spec.Accent, spec.AccentLight, ErpDesignTokens.IconBadgeSizeLg);
                iconBadge.Margin = new Thickness(0, 0, ErpDesignTokens.SpaceMd, 0);
                Grid.SetColumn(iconBadge, 0);
                body.Children.Add(iconBadge);

                var textCol = new StackPanel();
                var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
                titleRow.Children.Add(new TextBlock
                {
                    Text = spec.Title,
                    FontSize = ErpDesignTokens.FontKpiValue,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Br("TextPrimaryBrush"),
                    FontFamily = Ff(),
                    VerticalAlignment = VerticalAlignment.Center
                });
                if (!string.IsNullOrEmpty(spec.StatusBadge))
                {
                    titleRow.Children.Add(new Border
                    {
                        Background = spec.StatusBadgeBackground ?? Br("SuccessBgBrush"),
                        CornerRadius = new CornerRadius(100),
                        Padding = new Thickness(10, 4, 10, 4),
                        Margin = new Thickness(ErpDesignTokens.SpaceMd, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        Child = new TextBlock
                        {
                            Text = spec.StatusBadge,
                            FontSize = ErpDesignTokens.FontCaption,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = spec.StatusBadgeForeground ?? Br("SuccessBrush"),
                            FontFamily = Ff()
                        }
                    });
                }
                textCol.Children.Add(titleRow);
                if (!string.IsNullOrEmpty(spec.Subtitle))
                {
                    textCol.Children.Add(new TextBlock
                    {
                        Text = spec.Subtitle,
                        FontSize = ErpDesignTokens.FontBody,
                        Foreground = Br("TextSecondaryBrush"),
                        Margin = new Thickness(0, ErpDesignTokens.SpaceXs, 0, 0),
                        FontFamily = Ff()
                    });
                }

                if (spec.HeaderFields.Count > 0)
                {
                    var fields = new UniformGrid
                    {
                        Columns = 4,
                        Margin = new Thickness(0, ErpDesignTokens.SpaceMd, 0, 0)
                    };
                    foreach (var (label, value) in spec.HeaderFields)
                    {
                        var cell = new StackPanel { Margin = new Thickness(0, 0, ErpDesignTokens.SpaceSm, ErpDesignTokens.SpaceSm) };
                        cell.Children.Add(new TextBlock
                        {
                            Text = label,
                            FontSize = ErpDesignTokens.FontCaption - 1,
                            Foreground = Br("TextMutedBrush"),
                            FontFamily = Ff()
                        });
                        cell.Children.Add(new TextBlock
                        {
                            Text = value,
                            FontSize = ErpDesignTokens.FontBody,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = Br("TextPrimaryBrush"),
                            FontFamily = Ff()
                        });
                        fields.Children.Add(cell);
                    }
                    textCol.Children.Add(fields);
                }

                Grid.SetColumn(textCol, 1);
                body.Children.Add(textCol);
                outer.Children.Add(body);
            }
            return card;
        }

        private static FrameworkElement BuildKpiRow(OperationsCenterSpec spec)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, ErpDesignTokens.SpaceMd)
            };
            foreach (var (title, value, icon) in spec.Kpis)
            {
                row.Children.Add(new MetricCardControl
                {
                    CardTitle = title,
                    CardValue = value,
                    CardIcon = icon,
                    AccentColor = (spec.Accent as SolidColorBrush) ?? (SolidColorBrush)Br("PrimaryBrush"),
                    Margin = new Thickness(0, 0, ErpDesignTokens.CardGap, 0),
                    MinWidth = 140
                });
            }
            return row;
        }

        private static TabControl BuildTabs(OperationsCenterSpec spec)
        {
            var tabs = new TabControl
            {
                FontFamily = Ff(),
                FontSize = ErpDesignTokens.FontBody,
                Margin = new Thickness(0)
            };

            foreach (var tab in spec.Tabs)
            {
                tabs.Items.Add(new TabItem
                {
                    Header = tab.Label,
                    Tag = tab.Key,
                    Content = tab.Content
                });
            }

            tabs.SelectedIndex = Math.Clamp(spec.InitialTabIndex, 0, Math.Max(0, spec.Tabs.Count - 1));
            return tabs;
        }

        private static UIElement BuildQuickActions(OperationsCenterSpec spec, TabControl tabs)
        {
            var bar = new WrapPanel { Margin = new Thickness(0, 0, 0, ErpDesignTokens.SpaceMd) };
            foreach (var action in spec.QuickActions)
            {
                var btn = new Button
                {
                    Content = action.Label,
                    Style = S(action.Primary ? "PrimaryButtonStyle" : action.Destructive ? "GhostButtonStyle" : "SecondaryButtonStyle"),
                    Margin = new Thickness(0, 0, ErpDesignTokens.SpaceSm, ErpDesignTokens.SpaceSm),
                    Tag = action
                };
                if (action.Destructive)
                    btn.Foreground = Br("DangerBrush");

                btn.Click += (_, _) =>
                {
                    if (action.RequiresConfirmation &&
                        !ConfirmationDialogService.ConfirmDangerous(action.Label, spec.Title))
                        return;

                    if (!string.IsNullOrEmpty(action.ActionKey))
                    {
                        if (spec.Context?.EntityType == EntityType.ImportContainer &&
                            ChinaContainerQuickActionRouter.TryHandle(action.ActionKey, spec.Context, tabs))
                            return;

                        if (spec.Context is not null &&
                            CustomerActionRouter.TryHandleQuickAction(action.ActionKey, spec.Context))
                            return;

                        if (spec.Context?.EntityType == EntityType.Expense &&
                            spec.Context.EntityRow is ExpenseDetailsDto expenseDetails &&
                            ExpenseQuickActionRouter.TryHandle(action.ActionKey!, expenseDetails))
                            return;

                        if (spec.Context?.EntityType == EntityType.CapitalPartner &&
                            spec.Context.EntityRow is CapitalPartnerDetailsDto capitalDetails &&
                            CapitalPartnerQuickActionRouter.TryHandle(action.ActionKey, capitalDetails))
                            return;

                        MockQuickActionRouter.Execute(action.ActionKey, spec.Context ?? new OperationsCenterContext
                        {
                            EntityType = EntityType.Customer,
                            SourceModule = AppModule.Dashboard,
                            Title = spec.Title
                        }, tabs);
                        return;
                    }

                    if (!string.IsNullOrEmpty(action.TabKey))
                    {
                        for (int i = 0; i < tabs.Items.Count; i++)
                        {
                            if (tabs.Items[i] is TabItem ti &&
                                ti.Tag is string key &&
                                key.Equals(action.TabKey, StringComparison.OrdinalIgnoreCase))
                            {
                                tabs.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                    else
                    {
                        MockInteractionService.ShowComingSoon(action.Label);
                    }
                };
                bar.Children.Add(btn);
            }
            return ErpUiFactory.Card(bar, new Thickness(0, 0, 0, ErpDesignTokens.SpaceMd));
        }

        private static Brush Br(string k) => (Brush)System.Windows.Application.Current.Resources[k]!;
        private static Style S(string k) => (Style)System.Windows.Application.Current.Resources[k]!;
        private static FontFamily Ff() => ErpDesignTokens.UiFont;
    }
}
