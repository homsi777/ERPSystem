using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace ERPSystem.Helpers
{
    public static class ErpDataGridHelper
    {
        public static void ApplyEnterpriseStyle(DataGrid grid)
        {
            grid.RowHeight = ErpDesignTokens.GridRowHeight;
            grid.FontSize = ErpDesignTokens.FontBody;
            grid.FontFamily = ErpDesignTokens.UiFont;
            grid.Background = Brushes.White;
            grid.RowBackground = Brushes.White;
            grid.AlternatingRowBackground = Br("SurfaceAltBrush");
            grid.BorderThickness = new Thickness(0);
            grid.GridLinesVisibility = DataGridGridLinesVisibility.Horizontal;
            grid.HorizontalGridLinesBrush = Br("BorderLightBrush");
            grid.HeadersVisibility = DataGridHeadersVisibility.Column;
            grid.CanUserAddRows = false;
            grid.SelectionMode = DataGridSelectionMode.Single;
            grid.SelectionUnit = DataGridSelectionUnit.FullRow;
            grid.EnableRowVirtualization = true;

            grid.ColumnHeaderStyle = CreateHeaderStyle();
            grid.RowStyle = CreateRowStyle();
            grid.CellStyle = CreateCellStyle();
        }

        private static Style CreateHeaderStyle()
        {
            var s = new Style(typeof(DataGridColumnHeader));
            s.Setters.Add(new Setter(Control.BackgroundProperty, Br("SurfaceAltBrush")));
            s.Setters.Add(new Setter(Control.ForegroundProperty, Br("TextSecondaryBrush")));
            s.Setters.Add(new Setter(Control.FontSizeProperty, ErpDesignTokens.FontCaption));
            s.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
            s.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(ErpDesignTokens.SpaceMd, 0, ErpDesignTokens.SpaceMd, 0)));
            s.Setters.Add(new Setter(FrameworkElement.HeightProperty, ErpDesignTokens.GridHeaderHeight));
            s.Setters.Add(new Setter(Control.BorderBrushProperty, Br("BorderBrush")));
            s.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 1, 1)));
            return s;
        }

        private static Style CreateRowStyle()
        {
            var s = new Style(typeof(DataGridRow));
            s.Setters.Add(new Setter(FrameworkElement.CursorProperty, System.Windows.Input.Cursors.Hand));
            var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(Control.BackgroundProperty, Br("PrimaryVeryLightBrush")));
            s.Triggers.Add(hover);
            var sel = new Trigger { Property = DataGridRow.IsSelectedProperty, Value = true };
            sel.Setters.Add(new Setter(Control.BackgroundProperty, Br("PrimaryVeryLightBrush")));
            s.Triggers.Add(sel);
            return s;
        }

        private static Style CreateCellStyle()
        {
            var s = new Style(typeof(DataGridCell));
            s.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(ErpDesignTokens.SpaceMd, 0, ErpDesignTokens.SpaceMd, 0)));
            s.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
            s.Setters.Add(new Setter(Control.FocusVisualStyleProperty, null));
            var sel = new Trigger { Property = DataGridCell.IsSelectedProperty, Value = true };
            sel.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            s.Triggers.Add(sel);
            return s;
        }

        private static Brush Br(string key) => (Brush)System.Windows.Application.Current.Resources[key]!;
    }
}
