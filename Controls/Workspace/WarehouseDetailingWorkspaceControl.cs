using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ERPSystem.Services;
using System.Windows.Media;

namespace ERPSystem.Controls.Workspace
{
    public class DetailingRollRow
    {
        public int RollIndex { get; set; }
        public string FabricCode { get; set; } = "";
        public string Color { get; set; } = "";
        public string LengthText { get; set; } = "";
        public bool IsCurrent { get; set; }
    }

    /// <summary>Keyboard-friendly warehouse roll length entry with validation.</summary>
    public class WarehouseDetailingWorkspaceControl : UserControl
    {
        private readonly ObservableCollection<DetailingRollRow> _rows = new();
        private readonly TextBlock _txtTotal;
        private readonly TextBlock _txtProgress;
        private readonly TextBlock _txtPercent;
        private readonly TextBlock _txtEstimated;
        private readonly TextBlock _txtCurrentRoll;
        private readonly ProgressBar _progressBar;
        private readonly Button _btnSave;
        private readonly Button _btnComplete;
        private DataGrid? _grid;
        private int _currentIndex;
        private decimal _pricePerMeter = 45m;

        public event EventHandler? DetailingCompleted;

        public WarehouseDetailingWorkspaceControl()
        {
            var root = new StackPanel();

            root.Children.Add(new TextBlock
            {
                Text = "تفصيل أطوال الأثواب — إدخال سريع",
                FontSize = 16, FontWeight = FontWeights.SemiBold,
                Foreground = Br("TextPrimaryBrush"), Margin = new Thickness(0, 0, 0, 8), FontFamily = Ff()
            });

            var header = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var left = new StackPanel();
            _txtCurrentRoll = new TextBlock
            {
                Text = "التوب الحالي: —", FontSize = 14, FontWeight = FontWeights.SemiBold,
                Foreground = Br("PrimaryBrush"), FontFamily = Ff()
            };
            _txtProgress = new TextBlock
            {
                Text = "0 / 0 توب", FontSize = 12, Foreground = Br("TextSecondaryBrush"),
                Margin = new Thickness(0, 4, 0, 0), FontFamily = Ff()
            };
            _txtPercent = new TextBlock
            {
                Text = "0% مكتمل", FontSize = 12, Foreground = Br("SuccessBrush"),
                Margin = new Thickness(0, 2, 0, 0), FontFamily = Ff()
            };
            left.Children.Add(_txtCurrentRoll);
            left.Children.Add(_txtProgress);
            left.Children.Add(_txtPercent);
            Grid.SetColumn(left, 0);
            header.Children.Add(left);

            var totals = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left };
            totals.Children.Add(new TextBlock { Text = "إجمالي الأطوال", FontSize = 11, Foreground = Br("TextMutedBrush"), FontFamily = Ff() });
            _txtTotal = new TextBlock
            {
                Text = "0.00 م", FontSize = 22, FontWeight = FontWeights.Bold,
                Foreground = Br("SuccessBrush"), FontFamily = Ff()
            };
            totals.Children.Add(_txtTotal);
            totals.Children.Add(new TextBlock { Text = "إجمالي الفاتورة التقديري", FontSize = 11, Foreground = Br("TextMutedBrush"), Margin = new Thickness(0, 8, 0, 0), FontFamily = Ff() });
            _txtEstimated = new TextBlock
            {
                Text = "—", FontSize = 16, FontWeight = FontWeights.SemiBold,
                Foreground = Br("PrimaryBrush"), FontFamily = Ff()
            };
            totals.Children.Add(_txtEstimated);
            Grid.SetColumn(totals, 1);
            header.Children.Add(totals);
            root.Children.Add(header);

            _progressBar = new ProgressBar { Height = 8, Margin = new Thickness(0, 0, 0, 8), Minimum = 0, Maximum = 100 };
            root.Children.Add(_progressBar);

            root.Children.Add(new TextBlock
            {
                Text = "Enter للانتقال للتوب التالي • Tab بين الحقول • لا يُكمل قبل إدخال جميع الأطوال",
                FontSize = 11, Foreground = Br("TextMutedBrush"), Margin = new Thickness(0, 0, 0, 8), FontFamily = Ff()
            });

            _grid = new DataGrid
            {
                ItemsSource = _rows, AutoGenerateColumns = false, CanUserAddRows = false,
                RowHeight = 52, MinHeight = 280, FontFamily = Ff(), FontSize = 14,
                BorderBrush = Br("BorderBrush"), BorderThickness = new Thickness(1)
            };
            _grid.LoadingRow += OnLoadingRow;
            _grid.Columns.Add(new DataGridTextColumn { Header = "#", Binding = new System.Windows.Data.Binding(nameof(DetailingRollRow.RollIndex)), Width = 50, IsReadOnly = true });
            _grid.Columns.Add(new DataGridTextColumn { Header = "كود التوب", Binding = new System.Windows.Data.Binding(nameof(DetailingRollRow.FabricCode)), Width = 120, IsReadOnly = true });
            _grid.Columns.Add(new DataGridTextColumn { Header = "اللون", Binding = new System.Windows.Data.Binding(nameof(DetailingRollRow.Color)), Width = 90, IsReadOnly = true });
            var lengthCol = new DataGridTemplateColumn { Header = "الطول (متر)", Width = new DataGridLength(1, DataGridLengthUnitType.Star) };
            lengthCol.CellTemplate = CreateLengthTemplate();
            _grid.Columns.Add(lengthCol);
            root.Children.Add(_grid);

            var stats = new UniformGrid { Columns = 4, Margin = new Thickness(0, 12, 0, 12) };
            foreach (var (l, v) in new[] { ("إجمالي الأثواب", "0"), ("مكتمل", "0"), ("متبقي", "0"), ("الطول الحالي", "0.00 م") })
            {
                var cell = new StackPanel { Margin = new Thickness(4) };
                cell.Children.Add(new TextBlock { Text = l, FontSize = 10, Foreground = Br("TextMutedBrush"), FontFamily = Ff() });
                cell.Children.Add(new TextBlock { Text = v, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = Br("TextPrimaryBrush"), FontFamily = Ff(), Name = $"Stat_{l}" });
                stats.Children.Add(cell);
            }
            root.Children.Add(stats);

            var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
            _btnSave = new Button
            {
                Content = "حفظ التفصيل", Style = S("SecondaryButtonStyle"),
                Height = 38, Padding = new Thickness(18, 0, 18, 0), Margin = new Thickness(0, 0, 8, 0), IsEnabled = false
            };
            _btnSave.Click += (_, _) => TryComplete(saveOnly: true);
            _btnComplete = new Button
            {
                Content = "إكمال التفصيل وإرسال للمحاسب", Style = S("PrimaryButtonStyle"),
                Height = 38, Padding = new Thickness(18, 0, 18, 0), IsEnabled = false
            };
            _btnComplete.Click += (_, _) => TryComplete(saveOnly: false);
            actions.Children.Add(_btnSave);
            actions.Children.Add(_btnComplete);
            root.Children.Add(actions);

            Content = root;
        }

        public void LoadInvoice(string invoiceNumber, string customer, string container, int rollCount, decimal pricePerMeter = 45m)
        {
            _pricePerMeter = pricePerMeter;
            _rows.Clear();
            var codes = new[] { "COL-01", "COL-02", "TRK-05", "LIN-08" };
            var colors = new[] { "أبيض", "بيج", "أسود", "كحلي" };
            int count = Math.Max(rollCount, 3);
            for (int i = 1; i <= count; i++)
            {
                _rows.Add(new DetailingRollRow
                {
                    RollIndex = i,
                    FabricCode = codes[(i - 1) % codes.Length],
                    Color = colors[(i - 1) % colors.Length],
                    IsCurrent = i == 1
                });
            }
            _currentIndex = 0;
            _txtCurrentRoll.Text = $"فاتورة {invoiceNumber} — {customer} — حاوية {container}";
            UpdateTotals();
            Loaded += (_, _) => FocusLengthBox(0);
        }

        private void TryComplete(bool saveOnly)
        {
            if (!ValidateAllRolls(out var message))
            {
                MockInteractionService.ShowWarning(message, "تفصيل غير مكتمل");
                return;
            }
            DetailingCompleted?.Invoke(this, EventArgs.Empty);
            MockInteractionService.ShowSuccess(
                saveOnly ? "تم حفظ التفصيل." : "تم إكمال التفصيل وإرسال إشعار للمحاسب.",
                saveOnly ? "حفظ التفصيل" : "إكمال التفصيل");
        }

        private bool ValidateAllRolls(out string message)
        {
            message = "";
            for (int i = 0; i < _rows.Count; i++)
            {
                var r = _rows[i];
                if (string.IsNullOrWhiteSpace(r.LengthText))
                {
                    message = $"الرجاء إدخال طول التوب رقم {r.RollIndex}.";
                    FocusLengthBox(i);
                    return false;
                }
                if (!decimal.TryParse(r.LengthText.Replace(",", "."), out var len) || len <= 0)
                {
                    message = $"طول التوب رقم {r.RollIndex} يجب أن يكون أكبر من صفر.";
                    FocusLengthBox(i);
                    return false;
                }
            }
            return true;
        }

        private DataTemplate CreateLengthTemplate()
        {
            var factory = new FrameworkElementFactory(typeof(TextBox));
            factory.SetValue(TextBox.HeightProperty, 40.0);
            factory.SetValue(TextBox.FontSizeProperty, 18.0);
            factory.SetValue(TextBox.FontWeightProperty, FontWeights.SemiBold);
            factory.SetValue(TextBox.PaddingProperty, new Thickness(12, 6, 12, 6));
            factory.SetValue(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center);
            factory.SetBinding(TextBox.TextProperty, new System.Windows.Data.Binding(nameof(DetailingRollRow.LengthText))
            {
                Mode = System.Windows.Data.BindingMode.TwoWay,
                UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
            });
            factory.AddHandler(TextBox.PreviewKeyDownEvent, new KeyEventHandler(OnLengthKeyDown));
            factory.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler((_, _) => UpdateTotals()));
            return new DataTemplate { VisualTree = factory };
        }

        private void OnLoadingRow(object? sender, DataGridRowEventArgs e)
        {
            if (e.Row.Item is DetailingRollRow row && row.IsCurrent)
                e.Row.Background = Br("PrimaryVeryLightBrush");
        }

        private void OnLengthKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            if (_currentIndex < _rows.Count - 1)
            {
                _rows[_currentIndex].IsCurrent = false;
                _currentIndex++;
                _rows[_currentIndex].IsCurrent = true;
                _grid?.Items.Refresh();
                FocusLengthBox(_currentIndex);
            }
            UpdateTotals();
        }

        private void FocusLengthBox(int index)
        {
            if (_grid == null) return;
            _grid.ScrollIntoView(_rows[index]);
            _grid.UpdateLayout();
            Dispatcher.BeginInvoke(() =>
            {
                if (_grid.ItemContainerGenerator.ContainerFromIndex(index) is DataGridRow dgRow)
                {
                    var tb = FindVisualChild<TextBox>(dgRow);
                    tb?.Focus();
                    tb?.SelectAll();
                }
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void UpdateTotals()
        {
            decimal total = 0;
            int filled = 0;
            foreach (var r in _rows)
            {
                if (decimal.TryParse(r.LengthText.Replace(",", "."), out var len) && len > 0)
                {
                    total += len;
                    filled++;
                }
            }
            int remaining = _rows.Count - filled;
            double pct = _rows.Count > 0 ? filled * 100.0 / _rows.Count : 0;

            _txtTotal.Text = $"{total:N2} م";
            _txtProgress.Text = $"{filled} / {_rows.Count} توب — متبقي {remaining}";
            _txtPercent.Text = $"{pct:N0}% مكتمل";
            _txtEstimated.Text = total > 0 ? $"{total * _pricePerMeter:N2} ر.س" : "—";
            _progressBar.Value = pct;

            bool complete = filled == _rows.Count && _rows.Count > 0;
            _btnSave.IsEnabled = filled > 0;
            _btnComplete.IsEnabled = complete;
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match) return match;
                var found = FindVisualChild<T>(child);
                if (found != null) return found;
            }
            return null;
        }

        private static Brush Br(string k) => (Brush)Application.Current.Resources[k]!;
        private static Style S(string k) => (Style)Application.Current.Resources[k]!;
        private static FontFamily Ff() => new("Segoe UI, Tahoma, Arial");
    }
}
