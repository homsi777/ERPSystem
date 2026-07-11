using ERPSystem.Core;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Helpers;

/// <summary>Shared Debit (مدين) / Credit (دائن) cell and row tint styling — UI only.</summary>
public static class ErpAccountingColorHelper
{
    public const string DebitTintBrushKey = "AccountingDebitTintBrush";
    public const string CreditTintBrushKey = "AccountingCreditTintBrush";

    public static void AddDebitColumn(DataGrid grid, string header, string bindingPath, object width, string? format = null) =>
        grid.Columns.Add(CreateTintedColumn(header, bindingPath, width, format, DebitTintBrushKey));

    public static void AddCreditColumn(DataGrid grid, string header, string bindingPath, object width, string? format = null) =>
        grid.Columns.Add(CreateTintedColumn(header, bindingPath, width, format, CreditTintBrushKey));

    public static void AddSignedBalanceColumn(DataGrid grid, string header, string bindingPath, object width, string? format = null)
    {
        var column = new DataGridTextColumn
        {
            Header = header,
            Binding = CreateBinding(bindingPath, format),
            Width = ResolveWidth(width),
            ElementStyle = CreateSignedBalanceStyle(bindingPath)
        };
        grid.Columns.Add(column);
    }

    public static void ApplyDebitStyle(DataGridTextColumn column, string bindingPath) =>
        column.ElementStyle = CreatePositiveAmountStyle(bindingPath, DebitTintBrushKey);

    public static void ApplyCreditStyle(DataGridTextColumn column, string bindingPath) =>
        column.ElementStyle = CreatePositiveAmountStyle(bindingPath, CreditTintBrushKey);

    public static void ApplySignedBalanceStyle(DataGridTextColumn column, string bindingPath) =>
        column.ElementStyle = CreateSignedBalanceStyle(bindingPath);

    /// <summary>Apply cell tints to columns whose header is مدين / دائن / الرصيد (signed).</summary>
    public static void ApplyDebitCreditColumnsByHeader(DataGrid grid)
    {
        foreach (var column in grid.Columns.OfType<DataGridTextColumn>())
        {
            var header = column.Header?.ToString()?.Trim();
            if (string.IsNullOrEmpty(header))
                continue;

            var path = GetBindingPath(column);
            if (string.IsNullOrEmpty(path))
                continue;

            switch (header)
            {
                case "مدين":
                    ApplyDebitStyle(column, path);
                    break;
                case "دائن":
                    ApplyCreditStyle(column, path);
                    break;
                case "الرصيد":
                    ApplySignedBalanceStyle(column, path);
                    break;
            }
        }
    }

    public static DataGrid BuildAccountingGrid(IEnumerable? items)
    {
        var grid = ErpUiFactory.BuildGrid(items, autoColumns: true);
        ApplyDebitCreditColumnsByHeader(grid);
        return grid;
    }

    public static Style CreatePaymentTypeRowStyle()
    {
        var style = new Style(typeof(DataGridRow));
        style.Setters.Add(new Setter(FrameworkElement.CursorProperty, System.Windows.Input.Cursors.Hand));

        var cash = new DataTrigger
        {
            Binding = new Binding(nameof(PaymentType)),
            Value = PaymentType.Cash
        };
        cash.Setters.Add(new Setter(Control.BackgroundProperty, Br(DebitTintBrushKey)));
        style.Triggers.Add(cash);

        var credit = new DataTrigger
        {
            Binding = new Binding(nameof(PaymentType)),
            Value = PaymentType.Credit
        };
        credit.Setters.Add(new Setter(Control.BackgroundProperty, Br(CreditTintBrushKey)));
        style.Triggers.Add(credit);

        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(Control.BackgroundProperty, Br("PrimaryVeryLightBrush")));
        style.Triggers.Add(hover);

        var selected = new Trigger { Property = DataGridRow.IsSelectedProperty, Value = true };
        selected.Setters.Add(new Setter(Control.BackgroundProperty, Br("PrimaryVeryLightBrush")));
        style.Triggers.Add(selected);

        return style;
    }

    public static Border TintedAmountBadge(string text, bool isDebit) => new()
    {
        Background = Br(isDebit ? DebitTintBrushKey : CreditTintBrushKey),
        CornerRadius = new CornerRadius(4),
        Padding = new Thickness(8, 4, 8, 4),
        HorizontalAlignment = HorizontalAlignment.Right,
        Child = new TextBlock
        {
            Text = text,
            Foreground = Br("TextPrimaryBrush"),
            FontWeight = FontWeights.SemiBold,
            Typography = { NumeralStyle = FontNumeralStyle.Lining },
            Language = XmlLanguage.GetLanguage("en-US")
        }
    };

    private static DataGridTextColumn CreateTintedColumn(
        string header,
        string bindingPath,
        object width,
        string? format,
        string tintBrushKey)
    {
        var column = new DataGridTextColumn
        {
            Header = header,
            Binding = CreateBinding(bindingPath, format),
            Width = ResolveWidth(width),
            ElementStyle = CreatePositiveAmountStyle(bindingPath, tintBrushKey)
        };
        return column;
    }

    private static Binding CreateBinding(string path, string? format)
    {
        var binding = new Binding(path) { ConverterCulture = AppCulture.FormatCulture };
        if (!string.IsNullOrEmpty(format))
            binding.StringFormat = format;
        return binding;
    }

    private static DataGridLength ResolveWidth(object width) => width switch
    {
        string => new DataGridLength(1, DataGridLengthUnitType.Star),
        int i => i,
        double d => d,
        DataGridLength len => len,
        _ => Convert.ToDouble(width, CultureInfo.InvariantCulture)
    };

    private static Style CreatePositiveAmountStyle(string bindingPath, string tintBrushKey)
    {
        var style = BaseCellTextStyle();
        var trigger = new DataTrigger
        {
            Binding = new Binding(bindingPath) { Converter = PositiveAmountConverter.Instance },
            Value = true
        };
        trigger.Setters.Add(new Setter(TextBlock.BackgroundProperty, Br(tintBrushKey)));
        trigger.Setters.Add(new Setter(TextBlock.PaddingProperty, CellPadding));
        style.Triggers.Add(trigger);
        return style;
    }

    private static Style CreateSignedBalanceStyle(string bindingPath)
    {
        var style = BaseCellTextStyle();

        var debitSide = new DataTrigger
        {
            Binding = new Binding(bindingPath) { Converter = SignedBalanceSideConverter.Instance },
            Value = SignedBalanceSideConverter.DebitSide
        };
        debitSide.Setters.Add(new Setter(TextBlock.BackgroundProperty, Br(DebitTintBrushKey)));
        debitSide.Setters.Add(new Setter(TextBlock.PaddingProperty, CellPadding));
        style.Triggers.Add(debitSide);

        var creditSide = new DataTrigger
        {
            Binding = new Binding(bindingPath) { Converter = SignedBalanceSideConverter.Instance },
            Value = SignedBalanceSideConverter.CreditSide
        };
        creditSide.Setters.Add(new Setter(TextBlock.BackgroundProperty, Br(CreditTintBrushKey)));
        creditSide.Setters.Add(new Setter(TextBlock.PaddingProperty, CellPadding));
        style.Triggers.Add(creditSide);

        return style;
    }

    private static Style BaseCellTextStyle()
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.BackgroundProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, Br("TextPrimaryBrush")));
        style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Left));
        style.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch));
        return style;
    }

    private static Thickness CellPadding => new(6, 2, 6, 2);

    private static string? GetBindingPath(DataGridTextColumn column) =>
        column.Binding is Binding binding ? binding.Path.Path : null;

    private static Brush Br(string key) => (Brush)System.Windows.Application.Current.Resources[key]!;

    private sealed class PositiveAmountConverter : IValueConverter
    {
        public static readonly PositiveAmountConverter Instance = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value switch
            {
                decimal d => d > 0m,
                double d => d > 0d,
                float f => f > 0f,
                int i => i > 0,
                long l => l > 0,
                string s => HasDisplayAmount(s),
                _ => false
            };

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();

        private static bool HasDisplayAmount(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return false;
            var t = s.Trim();
            return t != "—" && t != "-" && t != "0" && t != "0.00";
        }
    }

    private sealed class SignedBalanceSideConverter : IValueConverter
    {
        public const string DebitSide = "Debit";
        public const string CreditSide = "Credit";
        public static readonly SignedBalanceSideConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var amount = value switch
            {
                decimal d => d,
                double d => (decimal)d,
                float f => (decimal)f,
                int i => i,
                long l => l,
                string s when decimal.TryParse(s, NumberStyles.Number, culture, out var parsed) => parsed,
                _ => 0m
            };

            if (amount > 0m) return DebitSide;
            if (amount < 0m) return CreditSide;
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
