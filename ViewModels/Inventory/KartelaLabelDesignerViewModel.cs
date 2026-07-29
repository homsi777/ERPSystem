using ERPSystem.Services.Inventory;
using ERPSystem.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ERPSystem.ViewModels.Inventory;

public sealed class KartelaLabelDesignerViewModel : ViewModelBase
{
    private readonly IKartelaLabelRenderer _renderer;
    private readonly IKartelaLabelPrintService _printService;
    private readonly Dictionary<KartelaLabelType, ObservableCollection<KartelaLabelRowViewModel>> _sessionRows;
    private KartelaOption<KartelaLabelType> _selectedLabelType;
    private string _copiesText = "1";
    private bool _isOverflowing;
    private bool _isPrinting;

    public KartelaLabelDesignerViewModel(
        IKartelaLabelRenderer renderer,
        IKartelaLabelPrintService printService)
    {
        _renderer = renderer;
        _printService = printService;
        LabelTypes =
        [
            new(KartelaLabelType.Ruled, "كارتيلة مسطرة"),
            new(KartelaLabelType.Customer, "كارتيلة عميل"),
            new(KartelaLabelType.Fabric, "كارتيلة توب")
        ];
        _selectedLabelType = LabelTypes[0];
        _sessionRows = LabelTypes.ToDictionary(
            option => option.Value,
            _ => new ObservableCollection<KartelaLabelRowViewModel>());

        FontSizes = [10, 12, 14, 16, 18, 20, 22, 24, 28, 32, 36, 42, 48];
        Alignments =
        [
            new(KartelaTextAlignment.Right, "يمين"),
            new(KartelaTextAlignment.Center, "وسط"),
            new(KartelaTextAlignment.Left, "يسار")
        ];
        CareSymbols =
        [
            new(KartelaCareSymbol.None, "بدون رمز"),
            new(KartelaCareSymbol.Wash30, "غسيل 30°"),
            new(KartelaCareSymbol.Wash40, "غسيل 40°"),
            new(KartelaCareSymbol.IronLow, "كي منخفض"),
            new(KartelaCareSymbol.IronMedium, "كي متوسط"),
            new(KartelaCareSymbol.DoNotIron, "ممنوع الكي"),
            new(KartelaCareSymbol.TumbleDry, "تجفيف آلي"),
            new(KartelaCareSymbol.DryClean, "تنظيف جاف")
        ];

        AddRowCommand = new RelayCommand(AddRow);
        DeleteRowCommand = new RelayCommand(DeleteRow);
        MoveUpCommand = new RelayCommand(MoveUp);
        MoveDownCommand = new RelayCommand(MoveDown);
        ClearAllCommand = new RelayCommand(ClearAll);
        PrintCommand = new RelayCommand(Print, () => !IsPrinting);
    }

    public IReadOnlyList<KartelaOption<KartelaLabelType>> LabelTypes { get; }
    public IReadOnlyList<double> FontSizes { get; }
    public IReadOnlyList<KartelaOption<KartelaTextAlignment>> Alignments { get; }
    public IReadOnlyList<KartelaOption<KartelaCareSymbol>> CareSymbols { get; }

    public ObservableCollection<KartelaLabelRowViewModel> ActiveRows =>
        _sessionRows[SelectedLabelType.Value];

    public KartelaOption<KartelaLabelType> SelectedLabelType
    {
        get => _selectedLabelType;
        set
        {
            if (value is null || !SetProperty(ref _selectedLabelType, value))
                return;

            OnPropertyChanged(nameof(ActiveRows));
            RefreshPreview();
        }
    }

    public string CopiesText
    {
        get => _copiesText;
        set => SetProperty(ref _copiesText, value ?? string.Empty);
    }

    public bool IsOverflowing
    {
        get => _isOverflowing;
        private set => SetProperty(ref _isOverflowing, value);
    }

    public bool HasRows => ActiveRows.Count > 0;

    public bool IsPrinting
    {
        get => _isPrinting;
        private set
        {
            if (SetProperty(ref _isPrinting, value))
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }
    }

    public ICommand AddRowCommand { get; }
    public ICommand DeleteRowCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }
    public ICommand ClearAllCommand { get; }
    public ICommand PrintCommand { get; }

    public event EventHandler? PreviewChanged;
    public event EventHandler<Guid>? FocusRowRequested;
    public event EventHandler<string>? MessageRequested;

    public IReadOnlyList<KartelaLabelRowSnapshot> GetPrintableRows() =>
        ActiveRows
            .Select(row => row.ToSnapshot())
            .Where(row => !string.IsNullOrWhiteSpace(row.Text)
                          || row.CareSymbol != KartelaCareSymbol.None)
            .ToArray();

    private void AddRow()
    {
        var row = new KartelaLabelRowViewModel();
        row.ContentChanged += OnRowContentChanged;
        ActiveRows.Add(row);
        OnPropertyChanged(nameof(HasRows));
        RefreshPreview();
        FocusRowRequested?.Invoke(this, row.Id);
    }

    private void DeleteRow(object? parameter)
    {
        if (parameter is not KartelaLabelRowViewModel row || !ActiveRows.Remove(row))
            return;

        row.ContentChanged -= OnRowContentChanged;
        OnPropertyChanged(nameof(HasRows));
        RefreshPreview();
    }

    private void MoveUp(object? parameter)
    {
        if (parameter is not KartelaLabelRowViewModel row)
            return;

        var index = ActiveRows.IndexOf(row);
        if (index <= 0)
            return;

        ActiveRows.Move(index, index - 1);
        RefreshPreview();
    }

    private void MoveDown(object? parameter)
    {
        if (parameter is not KartelaLabelRowViewModel row)
            return;

        var index = ActiveRows.IndexOf(row);
        if (index < 0 || index >= ActiveRows.Count - 1)
            return;

        ActiveRows.Move(index, index + 1);
        RefreshPreview();
    }

    private void ClearAll()
    {
        foreach (var row in ActiveRows)
            row.ContentChanged -= OnRowContentChanged;
        ActiveRows.Clear();
        OnPropertyChanged(nameof(HasRows));
        RefreshPreview();
    }

    private void Print()
    {
        if (IsPrinting)
            return;

        var printableRows = GetPrintableRows();
        if (printableRows.Count == 0)
        {
            ShowMessage("أضف نصًا أو رمز عناية واحدًا على الأقل قبل الطباعة.");
            return;
        }

        if (!int.TryParse(CopiesText, out var copies) || copies is < 1 or > 999)
        {
            ShowMessage("أدخل عدد نسخ صحيحًا من 1 إلى 999.");
            return;
        }

        var measurement = _renderer.Measure(printableRows);
        IsOverflowing = !measurement.Fits;
        if (!measurement.Fits)
        {
            ShowMessage("محتوى الملصق يتجاوز مساحة 100 × 80 مم. قلّل النص أو حجم الخط قبل الطباعة.");
            return;
        }

        try
        {
            IsPrinting = true;
            var result = _printService.Print(
                printableRows,
                copies,
                $"كارتيلة - {SelectedLabelType.DisplayName}");
            if (result.Status != KartelaPrintStatus.Cancelled)
                ShowMessage(result.Message);
        }
        finally
        {
            IsPrinting = false;
        }
    }

    private void OnRowContentChanged(object? sender, EventArgs e) => RefreshPreview();

    private void RefreshPreview()
    {
        IsOverflowing = !_renderer.Measure(GetPrintableRows()).Fits;
        PreviewChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ShowMessage(string message) => MessageRequested?.Invoke(this, message);
}
