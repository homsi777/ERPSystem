using ERPSystem.Services.Inventory;
using ERPSystem.ViewModels.Inventory;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ERPSystem.Views.Inventory;

public partial class KartelaLabelDesignerView : UserControl
{
    private readonly KartelaLabelDesignerViewModel _viewModel;
    private readonly IKartelaLabelRenderer _renderer;

    public KartelaLabelDesignerView(
        KartelaLabelDesignerViewModel viewModel,
        IKartelaLabelRenderer renderer)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _renderer = renderer;
        DataContext = viewModel;

        viewModel.PreviewChanged += OnPreviewChanged;
        viewModel.FocusRowRequested += OnFocusRowRequested;
        viewModel.MessageRequested += OnMessageRequested;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => RefreshPreview();

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _viewModel.PreviewChanged -= OnPreviewChanged;
        _viewModel.FocusRowRequested -= OnFocusRowRequested;
        _viewModel.MessageRequested -= OnMessageRequested;
    }

    private void OnPreviewChanged(object? sender, EventArgs e) => RefreshPreview();

    private void RefreshPreview() =>
        PreviewHost.Content = _renderer.CreateLabelVisual(_viewModel.GetPrintableRows());

    private void OnFocusRowRequested(object? sender, Guid rowId)
    {
        Dispatcher.BeginInvoke(
            DispatcherPriority.Input,
            () =>
            {
                var textBox = FindRowTextBox(RowsItemsControl, rowId);
                if (textBox is null)
                    return;

                textBox.Focus();
                Keyboard.Focus(textBox);
                textBox.CaretIndex = textBox.Text.Length;
            });
    }

    private static TextBox? FindRowTextBox(DependencyObject root, Guid rowId)
    {
        var children = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < children; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is TextBox textBox
                && textBox.DataContext is KartelaLabelRowViewModel row
                && row.Id == rowId)
            {
                return textBox;
            }

            var nested = FindRowTextBox(child, rowId);
            if (nested is not null)
                return nested;
        }

        return null;
    }

    private void OnMessageRequested(object? sender, string message)
    {
        var owner = Window.GetWindow(this);
        if (owner is not null)
        {
            MessageBox.Show(
                owner,
                message,
                "كارتيلة",
                MessageBoxButton.OK,
                MessageBoxImage.Information,
                MessageBoxResult.OK,
                MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            return;
        }

        MessageBox.Show(
            message,
            "كارتيلة",
            MessageBoxButton.OK,
            MessageBoxImage.Information,
            MessageBoxResult.OK,
            MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
    }
}
