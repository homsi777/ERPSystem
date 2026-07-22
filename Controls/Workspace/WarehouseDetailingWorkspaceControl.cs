using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ERPSystem.Application.Commands.Sales;
using ERPSystem.Application.Queries.Inventory;
using ERPSystem.Application.UseCases.Inventory;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Services;
using ERPSystem.Services.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Controls.Workspace
{
    public class DetailingRollRow
    {
        public Guid RollDetailId { get; set; }
        public int RollIndex { get; set; }
        public string FabricCode { get; set; } = "";
        public string Color { get; set; } = "";
        public string SerialText { get; set; } = "";
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
        private readonly TextBlock _txtStatTotalRolls;
        private readonly TextBlock _txtStatCompleted;
        private readonly TextBlock _txtStatRemaining;
        private readonly TextBlock _txtStatCurrentLength;
        private DataGrid? _grid;
        private int _currentIndex;
        private decimal _pricePerMeter;
        private Guid? _invoiceId;
        private bool _isSubmitting;

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
                Text = "أدخل رقم التوب (سيريال DPL) أو الطول • يكفي أحدهما • السيريال أدق ويخصم من نفس التوب في المخزون",
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
            var serialCol = new DataGridTemplateColumn { Header = "رقم التوب (سيريال)", Width = new DataGridLength(1, DataGridLengthUnitType.Star) };
            serialCol.CellTemplate = CreateSerialTemplate();
            _grid.Columns.Add(serialCol);
            var lengthCol = new DataGridTemplateColumn { Header = "أو الطول (م)", Width = new DataGridLength(1, DataGridLengthUnitType.Star) };
            lengthCol.CellTemplate = CreateLengthTemplate();
            _grid.Columns.Add(lengthCol);
            root.Children.Add(_grid);

            var stats = new UniformGrid { Columns = 4, Margin = new Thickness(0, 12, 0, 12) };
            _txtStatTotalRolls = AddStatCell(stats, "إجمالي الأثواب", "0");
            _txtStatCompleted = AddStatCell(stats, "مكتمل", "0");
            _txtStatRemaining = AddStatCell(stats, "متبقي", "0");
            _txtStatCurrentLength = AddStatCell(stats, "الطول الحالي", "0.00 م");
            root.Children.Add(stats);

            var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
            _btnSave = new Button
            {
                Content = "حفظ التقدم", Style = S("SecondaryButtonStyle"),
                Height = 38, Padding = new Thickness(18, 0, 18, 0), Margin = new Thickness(0, 0, 8, 0), IsEnabled = false
            };
            _btnSave.Click += async (_, _) => await TryCompleteAsync(saveOnly: true);
            _btnComplete = new Button
            {
                Content = "إكمال التفصيل وإرسال للمحاسب", Style = S("PrimaryButtonStyle"),
                Height = 38, Padding = new Thickness(18, 0, 18, 0), IsEnabled = false
            };
            _btnComplete.Click += async (_, _) => await TryCompleteAsync(saveOnly: false);
            actions.Children.Add(_btnSave);
            actions.Children.Add(_btnComplete);
            root.Children.Add(actions);

            Content = root;
        }

        public void LoadFromDatabase(
            Guid invoiceId,
            string invoiceNumber,
            string customer,
            string container,
            IReadOnlyList<WarehouseDetailingRollDto> rolls,
            decimal pricePerMeter)
        {
            _invoiceId = invoiceId;
            _pricePerMeter = pricePerMeter;
            _rows.Clear();

            foreach (var roll in rolls.OrderBy(r => r.RollSequence))
            {
                _rows.Add(new DetailingRollRow
                {
                    RollDetailId = roll.RollDetailId,
                    RollIndex = roll.RollSequence,
                    FabricCode = string.IsNullOrWhiteSpace(roll.FabricCode) ? "—" : roll.FabricCode,
                    Color = string.IsNullOrWhiteSpace(roll.ColorDisplayName) ? "—" : roll.ColorDisplayName,
                    // Previously-saved partial progress (Part 4) pre-populates the inputs so the
                    // employee doesn't lose work; final resolved length always takes priority.
                    SerialText = roll.DraftRollNumber is > 0
                        ? roll.DraftRollNumber.Value.ToString(CultureInfo.InvariantCulture)
                        : "",
                    LengthText = roll.HasValidLength && roll.LengthMeters > 0
                        ? roll.LengthMeters.ToString(CultureInfo.InvariantCulture)
                        : (roll.DraftLengthMeters is > 0
                            ? roll.DraftLengthMeters.Value.ToString(CultureInfo.InvariantCulture)
                            : ""),
                    IsCurrent = roll.RollSequence == 1
                });
            }

            _currentIndex = 0;
            _txtCurrentRoll.Text = $"فاتورة {invoiceNumber} — {customer} — حاوية {container}";
            UpdateTotals();
            Dispatcher.BeginInvoke(() => FocusSerialBox(0), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private async Task TryCompleteAsync(bool saveOnly)
        {
            if (_isSubmitting)
                return;

            if (saveOnly)
            {
                await SaveDraftAsync();
                return;
            }

            if (!ValidateAllRolls(out var message))
            {
                MockInteractionService.ShowWarning(message, "تفصيل غير مكتمل");
                return;
            }

            if (_invoiceId is null || !AppServices.IsInitialized)
                return;

            if (!MockInteractionService.Confirm("إكمال التفصيل وحفظ الأطوال في النظام؟", "إكمال التفصيل"))
                return;

            _isSubmitting = true;
            try
            {
                if (!await SalesUiService.Instance.CanCompleteDetailingAsync())
                {
                    MockInteractionService.ShowWarning("لا تملك صلاحية إكمال التفصيل.");
                    return;
                }

                var entries = BuildRollEntries();
                if (!await ConfirmExternalRollReservationsAsync(entries))
                    return;

                var result = await SalesUiService.Instance.CompleteDetailingAsync(_invoiceId.Value, entries);
                if (!ApplicationResultPresenter.Present(result))
                    return;

                SalesListRefreshHub.RequestRefresh();
                DetailingQueueRefreshHub.RequestRefresh();
                DetailingCompleted?.Invoke(this, EventArgs.Empty);

                MockInteractionService.ShowSuccess(
                    "تم إكمال التفصيل وحفظ الأطوال. يمكنك الآن اعتماد الفاتورة من شاشة المبيعات.",
                    "إكمال التفصيل");
            }
            finally
            {
                _isSubmitting = false;
            }
        }

        /// <summary>
        /// Real partial-save (Part 4): persists whatever is currently typed — serial and/or length,
        /// for any subset of rows — without requiring completeness and without resolving a fabric
        /// roll. Upgrades this button from the previous local-only validation to an actual save.
        /// </summary>
        private async Task SaveDraftAsync()
        {
            if (_invoiceId is null || !AppServices.IsInitialized)
                return;

            _isSubmitting = true;
            try
            {
                if (!await SalesUiService.Instance.CanCompleteDetailingAsync())
                {
                    MockInteractionService.ShowWarning("لا تملك صلاحية حفظ التفصيل.");
                    return;
                }

                var entries = BuildDraftEntries();
                var result = await SalesUiService.Instance.SaveDetailingDraftAsync(_invoiceId.Value, entries);
                if (!ApplicationResultPresenter.Present(result))
                    return;

                DetailingQueueRefreshHub.RequestRefresh();
                MockInteractionService.ShowSuccess(
                    "تم حفظ التقدم الحالي. يمكنك إكمال الأثواب المتبقية لاحقاً.",
                    "حفظ التفصيل");
            }
            finally
            {
                _isSubmitting = false;
            }
        }

        private List<RollDraftEntryCommand> BuildDraftEntries()
        {
            var entries = new List<RollDraftEntryCommand>();
            foreach (var row in _rows)
            {
                var hasSerial = TryParseSerial(row.SerialText, out var serial);
                var hasLength = TryParseLength(row.LengthText, out var length);
                entries.Add(new RollDraftEntryCommand
                {
                    RollDetailId = row.RollDetailId,
                    RollNumber = hasSerial ? serial : null,
                    LengthMeters = hasLength ? length : null
                });
            }
            return entries;
        }

        private List<RollLengthEntryCommand> BuildRollEntries()
        {
            var entries = new List<RollLengthEntryCommand>();
            foreach (var row in _rows)
            {
                var hasSerial = TryParseSerial(row.SerialText, out var serial);
                var hasLength = TryParseLength(row.LengthText, out var length);
                if (!hasSerial && !hasLength)
                    continue;

                entries.Add(new RollLengthEntryCommand
                {
                    RollDetailId = row.RollDetailId,
                    RollNumber = hasSerial ? serial : null,
                    LengthMeters = hasLength ? length : 0m
                });
            }
            return entries;
        }

        private async Task<bool> ConfirmExternalRollReservationsAsync(IReadOnlyList<RollLengthEntryCommand> entries)
        {
            if (_invoiceId is not Guid invoiceId || !AppServices.IsInitialized)
                return true;

            var serials = entries
                .Where(e => e.RollNumber is > 0)
                .Select(e => e.RollNumber!.Value)
                .Distinct()
                .ToList();
            if (serials.Count == 0)
                return true;

            try
            {
                using var scope = AppServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
                var rollIds = await db.FabricRolls.AsNoTracking()
                    .Where(r => serials.Contains(r.RollNumber))
                    .Select(r => r.Id)
                    .ToListAsync();
                if (rollIds.Count == 0)
                    return true;

                var handler = scope.ServiceProvider.GetRequiredService<GetFabricRollSalesReservationsHandler>();
                var result = await handler.HandleAsync(
                    new GetFabricRollSalesReservationsQuery(rollIds, invoiceId));

                if (!result.IsSuccess || result.Value is null || result.Value.Count == 0)
                    return true;

                var invoices = string.Join("\n", result.Value
                    .Select(r => $"• مسودة بيع {r.SalesInvoiceNumber}")
                    .Distinct());

                return MockInteractionService.Confirm(
                    "تنبيه غير مانع: يوجد توب مذكور في فاتورة بيع أخرى بعد التفصيل:\n\n" +
                    invoices +
                    "\n\nهل تريد متابعة إكمال التفصيل؟",
                    "حجز بيع سابق");
            }
            catch
            {
                return true;
            }
        }

        private static bool TryParseLength(string text, out decimal length)
        {
            length = 0;
            return decimal.TryParse(text.Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture, out length)
                && length > 0;
        }

        private static bool TryParseSerial(string text, out int serial)
        {
            serial = 0;
            return int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out serial)
                && serial > 0;
        }

        private bool IsRowFilled(DetailingRollRow row) =>
            TryParseSerial(row.SerialText, out _) || TryParseLength(row.LengthText, out _);

        private bool ValidateAllRolls(out string message)
        {
            message = "";
            var seenSerials = new HashSet<int>();
            for (int i = 0; i < _rows.Count; i++)
            {
                var r = _rows[i];
                if (!IsRowFilled(r))
                {
                    message = $"الرجاء إدخال رقم التوب (سيريال) أو الطول للتوب رقم {r.RollIndex}.";
                    FocusSerialBox(i);
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(r.SerialText) && !TryParseSerial(r.SerialText, out var serial))
                {
                    message = $"رقم التوب للتوب رقم {r.RollIndex} غير صالح.";
                    FocusSerialBox(i);
                    return false;
                }

                if (TryParseSerial(r.SerialText, out serial) && !seenSerials.Add(serial))
                {
                    message = $"رقم السيريال {serial} مكرر في نفس الفاتورة. كل توب يجب أن يحمل سيريالاً فريداً.";
                    FocusSerialBox(i);
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(r.LengthText) && !TryParseLength(r.LengthText, out _))
                {
                    message = $"طول التوب رقم {r.RollIndex} يجب أن يكون أكبر من صفر.";
                    FocusLengthBox(i);
                    return false;
                }
            }
            return true;
        }

        private DataTemplate CreateSerialTemplate()
        {
            var factory = new FrameworkElementFactory(typeof(TextBox));
            factory.SetValue(TextBox.HeightProperty, 40.0);
            factory.SetValue(TextBox.FontSizeProperty, 18.0);
            factory.SetValue(TextBox.FontWeightProperty, FontWeights.SemiBold);
            factory.SetValue(TextBox.PaddingProperty, new Thickness(12, 6, 12, 6));
            factory.SetValue(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center);
            factory.SetBinding(TextBox.TextProperty, new System.Windows.Data.Binding(nameof(DetailingRollRow.SerialText))
            {
                Mode = System.Windows.Data.BindingMode.TwoWay,
                UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
            });
            factory.AddHandler(TextBox.PreviewKeyDownEvent, new KeyEventHandler(OnSerialKeyDown));
            factory.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler((_, _) => UpdateTotals()));
            return new DataTemplate { VisualTree = factory };
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

        private void OnSerialKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            // Enter from serial: move to length of same row, or next row serial if length not needed.
            FocusLengthBox(_currentIndex);
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
                FocusSerialBox(_currentIndex);
            }
            UpdateTotals();
        }

        private void FocusSerialBox(int index) => FocusRowTextBox(index, preferSerial: true);

        private void FocusLengthBox(int index) => FocusRowTextBox(index, preferSerial: false);

        private void FocusRowTextBox(int index, bool preferSerial)
        {
            if (_grid == null || index < 0 || index >= _rows.Count) return;
            _grid.ScrollIntoView(_rows[index]);
            _grid.UpdateLayout();
            Dispatcher.BeginInvoke(() =>
            {
                if (_grid.ItemContainerGenerator.ContainerFromIndex(index) is not DataGridRow dgRow)
                    return;

                var boxes = FindVisualChildren<TextBox>(dgRow).ToList();
                TextBox? target = null;
                if (preferSerial)
                    target = boxes.FirstOrDefault();
                else
                    target = boxes.Count > 1 ? boxes[1] : boxes.FirstOrDefault();

                target?.Focus();
                target?.SelectAll();
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void UpdateTotals()
        {
            decimal total = 0;
            int filled = 0;
            foreach (var r in _rows)
            {
                if (IsRowFilled(r))
                    filled++;

                if (TryParseLength(r.LengthText, out var len))
                    total += len;
            }
            int remaining = _rows.Count - filled;
            double pct = _rows.Count > 0 ? filled * 100.0 / _rows.Count : 0;

            _txtTotal.Text = total > 0 ? $"{total:N2} م" : (filled > 0 ? "بالسيريال" : "0.00 م");
            _txtProgress.Text = $"{filled} / {_rows.Count} توب — متبقي {remaining}";
            _txtPercent.Text = $"{pct:N0}% مكتمل";
            _txtEstimated.Text = total > 0 ? $"{total * _pricePerMeter:N2} $" : "—";
            _progressBar.Value = pct;

            bool complete = filled == _rows.Count && _rows.Count > 0;
            _btnSave.IsEnabled = filled > 0;
            _btnComplete.IsEnabled = complete && _invoiceId.HasValue;

            _txtStatTotalRolls.Text = _rows.Count.ToString();
            _txtStatCompleted.Text = filled.ToString();
            _txtStatRemaining.Text = remaining.ToString();
            var currentLength = _currentIndex >= 0 && _currentIndex < _rows.Count && TryParseLength(_rows[_currentIndex].LengthText, out var curLen)
                ? $"{curLen:N2} م"
                : (_currentIndex >= 0 && _currentIndex < _rows.Count && TryParseSerial(_rows[_currentIndex].SerialText, out var sn)
                    ? $"#{sn}"
                    : "—");
            _txtStatCurrentLength.Text = currentLength;
        }

        private TextBlock AddStatCell(Panel parent, string label, string initialValue)
        {
            var cell = new StackPanel { Margin = new Thickness(4) };
            cell.Children.Add(new TextBlock { Text = label, FontSize = 10, Foreground = Br("TextMutedBrush"), FontFamily = Ff() });
            var value = new TextBlock
            {
                Text = initialValue,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = Br("TextPrimaryBrush"),
                FontFamily = Ff()
            };
            cell.Children.Add(value);
            parent.Children.Add(cell);
            return value;
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match)
                    yield return match;
                foreach (var nested in FindVisualChildren<T>(child))
                    yield return nested;
            }
        }

        private static Brush Br(string k) => (Brush)System.Windows.Application.Current.Resources[k]!;
        private static Style S(string k) => (Style)System.Windows.Application.Current.Resources[k]!;
        private static FontFamily Ff() => new("Segoe UI, Tahoma, Arial");
    }
}
