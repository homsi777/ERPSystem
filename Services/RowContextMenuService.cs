using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Sales;
using ERPSystem.Core.Workspace;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Services.Customers;
using ERPSystem.Services.Suppliers;
using ERPSystem.Services.Capital;
using ERPSystem.Services.Expenses;
using ERPSystem.Services.Inventory;
using ERPSystem.Services.Finance;
using ERPSystem.Services.Sales;
using ERPSystem.Services.Purchases;
using ERPSystem.Core.Purchases;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ERPSystem.Services
{
    /// <summary>
    /// Reusable service for attaching entity-aware right-click menus to DataGrids.
    /// </summary>
    public static class RowContextMenuService
    {
        public static readonly DependencyProperty EntityTypeProperty =
            DependencyProperty.RegisterAttached(
                "EntityType",
                typeof(EntityType?),
                typeof(RowContextMenuService),
                new PropertyMetadata(null, OnEntityTypeChanged));

        public static readonly DependencyProperty SourceModuleProperty =
            DependencyProperty.RegisterAttached(
                "SourceModule",
                typeof(AppModule),
                typeof(RowContextMenuService),
                new PropertyMetadata(AppModule.Dashboard));

        public static EntityType? GetEntityType(DependencyObject obj) =>
            (EntityType?)obj.GetValue(EntityTypeProperty);

        public static void SetEntityType(DependencyObject obj, EntityType? value) =>
            obj.SetValue(EntityTypeProperty, value);

        public static AppModule GetSourceModule(DependencyObject obj) =>
            (AppModule)obj.GetValue(SourceModuleProperty);

        public static void SetSourceModule(DependencyObject obj, AppModule value) =>
            obj.SetValue(SourceModuleProperty, value);

        private static void OnEntityTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DataGrid grid) return;

            grid.PreviewMouseRightButtonDown -= OnPreviewMouseRightButtonDown;
            if (e.NewValue is EntityType)
                grid.PreviewMouseRightButtonDown += OnPreviewMouseRightButtonDown;
        }

        private static void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGrid grid) return;
            if (GetEntityType(grid) is not EntityType entityType) return;

            var row = FindParent<DataGridRow>((DependencyObject)e.OriginalSource);
            if (row == null) return;

            row.IsSelected = true;
            row.Focus();

            var item = row.Item;
            if (item == null) return;

            e.Handled = true;
            var entity = UnwrapEntity(item, entityType) ?? item;

            if (entityType == EntityType.SalesInvoice && entity is SalesInvoiceListRow salesRow)
            {
                SalesContextMenuService.Show(salesRow, row);
                return;
            }

            if (entityType == EntityType.Warehouse && entity is WarehouseListExtendedDto wh)
            {
                WarehouseContextMenuService.Show(wh, row);
                return;
            }

            if (entityType == EntityType.Cashbox && entity is CashboxListDto cb)
            {
                CashboxContextMenuService.Show(cb, row);
                return;
            }

            if (entityType == EntityType.OpeningBalance && entity is OpeningBalanceListDto obRow)
            {
                OpeningBalanceContextMenuService.Show(obRow, row);
                return;
            }

            ShowEntityContextMenu(grid, entityType, entity, GetSourceModule(grid), e.GetPosition(grid));
        }

        public static void ShowContextMenu(DataGrid grid, EntityType entityType, object rowItem, Point position)
        {
            var entity = UnwrapEntity(rowItem, entityType) ?? rowItem;

            if (entityType == EntityType.Warehouse && entity is WarehouseListExtendedDto wh)
            {
                var row = grid.ItemContainerGenerator.ContainerFromItem(rowItem) as DataGridRow;
                if (row is not null)
                    WarehouseContextMenuService.Show(wh, row);
                else
                    WarehouseContextMenuService.Show(wh, grid);
                return;
            }

            ShowEntityContextMenu(grid, entityType, entity, GetSourceModule(grid), position);
        }

        public static void ShowEntityContextMenu(
            UIElement placementTarget,
            EntityType entityType,
            object entity,
            AppModule sourceModule,
            Point? position = null)
        {
            var actions = EntityActionRegistry.GetActions(entityType);
            if (actions.Count == 0) return;

            var menu = new ContextMenu { FlowDirection = FlowDirection.RightToLeft };
            string? currentGroup = null;

            foreach (var action in actions)
            {
                if (action.GroupAr != currentGroup && action.GroupAr != null)
                {
                    if (menu.Items.Count > 0)
                        menu.Items.Add(new Separator());
                    menu.Items.Add(new MenuItem
                    {
                        Header = action.GroupAr,
                        IsEnabled = false,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["TextMutedBrush"]!
                    });
                    currentGroup = action.GroupAr;
                }
                else if (action.GroupAr == null && currentGroup != null)
                {
                    menu.Items.Add(new Separator());
                    currentGroup = null;
                }

                var captured = action;
                var mi = new MenuItem
                {
                    Header = BuildMenuHeader(captured),
                    Tag = captured,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI, Tahoma, Arial"),
                    FontSize = 13,
                    Padding = new Thickness(12, 8, 12, 8)
                };

                if (captured.IsDestructive)
                    mi.Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["DangerBrush"]!;

                mi.Click += (_, _) => HandleActionClick(captured, entityType, entity, sourceModule);

                menu.Items.Add(mi);
            }

            menu.PlacementTarget = placementTarget;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            if (position is Point p)
                menu.HorizontalOffset = p.X;
            menu.IsOpen = true;
        }

        private static void HandleActionClick(
            EntityActionDefinition captured,
            EntityType entityType,
            object entity,
            AppModule sourceModule)
        {
            var displayName = EntityDisplayNameResolver.Resolve(entity, entityType);

            if (captured.RequiresConfirmation &&
                !ConfirmationDialogService.ConfirmDangerous(captured.LabelAr, displayName))
                return;

            if (entityType == EntityType.SalesInvoice && entity is SalesInvoiceListRow salesRow)
            {
                SalesActionRouter.Handle(captured.Id, salesRow);
                return;
            }

            if (entityType == EntityType.PurchaseInvoice && entity is PurchaseListRow purchaseRow &&
                PurchaseActionRouter.TryHandle(captured.Id, purchaseRow))
                return;

            if (CustomerActionRouter.TryHandle(captured.Id, entityType, entity, sourceModule))
                return;

            if (SupplierActionRouter.TryHandle(captured.Id, entityType, entity, sourceModule))
                return;

            if (ExpenseActionRouter.TryHandle(captured.Id, entityType, entity, sourceModule))
                return;

            if (CapitalPartnerActionRouter.TryHandle(captured.Id, entityType, entity, sourceModule))
                return;

            if (InventoryActionRouter.TryHandle(captured.Id, entityType, entity, sourceModule))
                return;

            if (CashboxActionRouter.TryHandle(captured.Id, entityType, entity, sourceModule))
                return;

            if (ChinaContainerActionRouter.TryHandle(captured.Id, entityType, entity, sourceModule))
                return;

            WorkspaceWindowManager.Instance.OpenAction(
                captured.Id, entityType, entity, sourceModule);
        }

        private static object BuildMenuHeader(EntityActionDefinition action)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new TextBlock
            {
                Text = action.IconGlyph,
                FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets"),
                FontSize = 14,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = action.IsDestructive
                    ? (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["DangerBrush"]!
                    : (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["TextSecondaryBrush"]!
            });
            sp.Children.Add(new TextBlock
            {
                Text = action.LabelAr,
                VerticalAlignment = VerticalAlignment.Center
            });
            return sp;
        }

        private static object? UnwrapEntity(object row, EntityType entityType) => row;

        private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T match) return match;
                child = System.Windows.Media.VisualTreeHelper.GetParent(child);
            }
            return null;
        }
    }
}
