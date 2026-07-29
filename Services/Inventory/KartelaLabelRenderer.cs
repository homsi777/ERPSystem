using ERPSystem.ViewModels.Inventory;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Services.Inventory;

public interface IKartelaLabelRenderer
{
    FrameworkElement CreateLabelVisual(IReadOnlyList<KartelaLabelRowSnapshot> rows);
    KartelaLabelMeasureResult Measure(IReadOnlyList<KartelaLabelRowSnapshot> rows);
}

public sealed record KartelaLabelMeasureResult(
    bool Fits,
    double ContentHeight,
    double AvailableHeight);

public sealed class KartelaLabelRenderer : IKartelaLabelRenderer
{
    public const double LabelWidthMm = 100;
    public const double LabelHeightMm = 80;
    public const double MillimetresPerInch = 25.4;
    public const double WpfUnitsPerInch = 96;
    public const double LabelWidth = LabelWidthMm / MillimetresPerInch * WpfUnitsPerInch;
    public const double LabelHeight = LabelHeightMm / MillimetresPerInch * WpfUnitsPerInch;
    public const double InternalPadding = 3 / MillimetresPerInch * WpfUnitsPerInch;

    public FrameworkElement CreateLabelVisual(IReadOnlyList<KartelaLabelRowSnapshot> rows)
    {
        var stack = CreateContent(rows);
        return new Border
        {
            Width = LabelWidth,
            Height = LabelHeight,
            Background = Brushes.White,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(0.75),
            Padding = new Thickness(InternalPadding),
            ClipToBounds = true,
            FlowDirection = FlowDirection.RightToLeft,
            Child = stack
        };
    }

    public KartelaLabelMeasureResult Measure(IReadOnlyList<KartelaLabelRowSnapshot> rows)
    {
        var content = CreateContent(rows);
        var availableWidth = LabelWidth - (InternalPadding * 2);
        var availableHeight = LabelHeight - (InternalPadding * 2);
        content.Measure(new Size(availableWidth, double.PositiveInfinity));
        return new KartelaLabelMeasureResult(
            content.DesiredSize.Height <= availableHeight + 0.1,
            content.DesiredSize.Height,
            availableHeight);
    }

    private static StackPanel CreateContent(IReadOnlyList<KartelaLabelRowSnapshot> rows)
    {
        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            FlowDirection = FlowDirection.RightToLeft
        };

        foreach (var row in rows.Where(IsPrintable))
            stack.Children.Add(CreateRow(row));

        return stack;
    }

    private static bool IsPrintable(KartelaLabelRowSnapshot row) =>
        !string.IsNullOrWhiteSpace(row.Text) || row.CareSymbol != KartelaCareSymbol.None;

    private static FrameworkElement CreateRow(KartelaLabelRowSnapshot row)
    {
        var grid = new Grid
        {
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Thickness(0, 1.5, 0, 1.5)
        };

        if (row.CareSymbol != KartelaCareSymbol.None)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var symbol = new KartelaCareSymbolElement
            {
                Symbol = row.CareSymbol,
                Width = 27,
                Height = 27,
                Margin = new Thickness(2, 0, 2, 0)
            };
            Grid.SetColumn(symbol, 0);
            grid.Children.Add(symbol);
        }
        else
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        var text = new TextBlock
        {
            Text = row.Text,
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = row.FontSize,
            FontWeight = row.IsBold ? FontWeights.Bold : FontWeights.Normal,
            Foreground = Brushes.Black,
            Background = Brushes.Transparent,
            FlowDirection = FlowDirection.RightToLeft,
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.None,
            TextAlignment = row.Alignment switch
            {
                KartelaTextAlignment.Center => TextAlignment.Center,
                KartelaTextAlignment.Left => TextAlignment.Left,
                _ => TextAlignment.Right
            },
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        return grid;
    }
}

public sealed class KartelaCareSymbolElement : FrameworkElement
{
    public static readonly DependencyProperty SymbolProperty = DependencyProperty.Register(
        nameof(Symbol),
        typeof(KartelaCareSymbol),
        typeof(KartelaCareSymbolElement),
        new FrameworkPropertyMetadata(
            KartelaCareSymbol.None,
            FrameworkPropertyMetadataOptions.AffectsRender));

    public KartelaCareSymbol Symbol
    {
        get => (KartelaCareSymbol)GetValue(SymbolProperty);
        set => SetValue(SymbolProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) =>
        new(double.IsInfinity(availableSize.Width) ? 27 : Math.Min(27, availableSize.Width),
            double.IsInfinity(availableSize.Height) ? 27 : Math.Min(27, availableSize.Height));

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (Symbol == KartelaCareSymbol.None)
            return;

        var scale = Math.Min(ActualWidth, ActualHeight) / 30d;
        if (scale <= 0)
            return;

        dc.PushTransform(new ScaleTransform(scale, scale));
        var pen = new Pen(Brushes.Black, 1.7)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };

        switch (Symbol)
        {
            case KartelaCareSymbol.Wash30:
                DrawWash(dc, pen, "30°");
                break;
            case KartelaCareSymbol.Wash40:
                DrawWash(dc, pen, "40°");
                break;
            case KartelaCareSymbol.IronLow:
                DrawIron(dc, pen, 1, false);
                break;
            case KartelaCareSymbol.IronMedium:
                DrawIron(dc, pen, 2, false);
                break;
            case KartelaCareSymbol.DoNotIron:
                DrawIron(dc, pen, 0, true);
                break;
            case KartelaCareSymbol.TumbleDry:
                dc.DrawRectangle(null, pen, new Rect(4, 4, 22, 22));
                dc.DrawEllipse(null, pen, new Point(15, 15), 7, 7);
                dc.DrawEllipse(Brushes.Black, null, new Point(12.5, 15), 1.2, 1.2);
                dc.DrawEllipse(Brushes.Black, null, new Point(17.5, 15), 1.2, 1.2);
                break;
            case KartelaCareSymbol.DryClean:
                dc.DrawEllipse(null, pen, new Point(15, 15), 11, 11);
                DrawCenteredText(dc, "P", 15, 15, 13);
                break;
        }

        dc.Pop();
    }

    private static void DrawWash(DrawingContext dc, Pen pen, string temperature)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(3, 8), false, false);
            context.LineTo(new Point(6, 25), true, false);
            context.LineTo(new Point(24, 25), true, false);
            context.LineTo(new Point(27, 8), true, false);
            context.BezierTo(new Point(22, 4), new Point(19, 12), new Point(15, 8), true, false);
            context.BezierTo(new Point(11, 4), new Point(8, 12), new Point(3, 8), true, false);
        }
        geometry.Freeze();
        dc.DrawGeometry(null, pen, geometry);
        DrawCenteredText(dc, temperature, 15, 17.5, 8);
    }

    private static void DrawIron(DrawingContext dc, Pen pen, int dots, bool crossed)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(3, 23), false, true);
            context.LineTo(new Point(27, 23), true, false);
            context.LineTo(new Point(23, 11), true, false);
            context.BezierTo(new Point(21, 7), new Point(16, 6), new Point(8, 7), true, false);
            context.LineTo(new Point(3, 23), true, false);
        }
        geometry.Freeze();
        dc.DrawGeometry(null, pen, geometry);

        for (var i = 0; i < dots; i++)
            dc.DrawEllipse(Brushes.Black, null, new Point(13 + (i * 5), 17), 1.4, 1.4);

        if (crossed)
        {
            dc.DrawLine(pen, new Point(4, 4), new Point(26, 27));
            dc.DrawLine(pen, new Point(26, 4), new Point(4, 27));
        }
    }

    private static void DrawCenteredText(
        DrawingContext dc,
        string text,
        double x,
        double y,
        double fontSize)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.GetCultureInfo("ar-SA"),
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            fontSize,
            Brushes.Black,
            1);
        dc.DrawText(formatted, new Point(x - (formatted.Width / 2), y - (formatted.Height / 2)));
    }
}
