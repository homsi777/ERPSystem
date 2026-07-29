using ERPSystem.ViewModels.Base;

namespace ERPSystem.ViewModels.Inventory;

public enum KartelaLabelType
{
    Ruled,
    Customer,
    Fabric
}

public enum KartelaTextAlignment
{
    Right,
    Center,
    Left
}

public enum KartelaCareSymbol
{
    None,
    Wash30,
    Wash40,
    IronLow,
    IronMedium,
    DoNotIron,
    TumbleDry,
    DryClean
}

public sealed record KartelaOption<T>(T Value, string DisplayName);

public sealed record KartelaLabelRowSnapshot(
    string Text,
    double FontSize,
    bool IsBold,
    KartelaTextAlignment Alignment,
    KartelaCareSymbol CareSymbol);

public sealed class KartelaLabelRowViewModel : ViewModelBase
{
    private string _text = string.Empty;
    private double _fontSize = 18;
    private bool _isBold;
    private KartelaTextAlignment _alignment = KartelaTextAlignment.Right;
    private KartelaCareSymbol _careSymbol;

    public Guid Id { get; } = Guid.NewGuid();

    public event EventHandler? ContentChanged;

    public string Text
    {
        get => _text;
        set
        {
            if (SetProperty(ref _text, value ?? string.Empty))
                ContentChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public double FontSize
    {
        get => _fontSize;
        set
        {
            if (SetProperty(ref _fontSize, Math.Clamp(value, 8, 48)))
                ContentChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsBold
    {
        get => _isBold;
        set
        {
            if (SetProperty(ref _isBold, value))
                ContentChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public KartelaTextAlignment Alignment
    {
        get => _alignment;
        set
        {
            if (SetProperty(ref _alignment, value))
                ContentChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public KartelaCareSymbol CareSymbol
    {
        get => _careSymbol;
        set
        {
            if (SetProperty(ref _careSymbol, value))
                ContentChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public KartelaLabelRowSnapshot ToSnapshot() =>
        new(Text.Trim(), FontSize, IsBold, Alignment, CareSymbol);
}
