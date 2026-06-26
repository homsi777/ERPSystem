using ERPSystem.Core;
using ERPSystem.Core.POS;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ERPSystem.Modules
{
    public partial class POSModule : UserControl
    {
        // ══════════════════════════════════════════════════════════
        //  STATE
        // ══════════════════════════════════════════════════════════

        private POSSession _session = null!;
        private POSOrder _order = null!;
        private List<POSCategory> _categories = null!;
        private List<POSProduct> _allProducts = null!;
        private List<POSProduct> _filteredProducts = null!;

        private string _activeCategoryId = "ALL";
        private PaymentMethod _selectedPayment = PaymentMethod.Cash;
        private DiscountType _discountType = DiscountType.Fixed;
        private bool _isArabic = true;

        private DispatcherTimer _sessionTimer = null!;
        private int _invoiceCounter = 1045;

        // ══════════════════════════════════════════════════════════
        //  INIT
        // ══════════════════════════════════════════════════════════

        public POSModule()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _isArabic = LocalizationManager.Instance.IsArabic;
            LocalizationManager.Instance.LanguageChanged += (_, _) => OnLanguageChanged();

            InitSession();
            InitData();
            BuildCategories();
            FilterProducts("ALL");
            StartNewOrder();
            UpdateLabels();
            StartSessionTimer();
        }

        private void OnLanguageChanged()
        {
            _isArabic = LocalizationManager.Instance.IsArabic;
            UpdateLabels();
            BuildCategories();
            FilterProducts(_activeCategoryId);
            RebuildCartUI();
        }

        // ══════════════════════════════════════════════════════════
        //  SESSION INIT
        // ══════════════════════════════════════════════════════════

        private void InitSession()
        {
            _session = new POSSession
            {
                SessionNumber = "POS-0042",
                CashierNameAr = "أحمد محمد",
                CashierNameEn = "Ahmed Mohammed",
                TerminalId = "T-01",
                OpenedAt = DateTime.Now.AddMinutes(-37),
                Status = SessionStatus.Open
            };

            TxtSessionNumber.Text = _session.SessionNumber;
            TxtSessionTerminal.Text = _session.TerminalId;
            UpdateSessionStats();
        }

        private void InitData()
        {
            _categories = POSSampleData.GetCategories();
            _allProducts = POSSampleData.GetProducts();
            _filteredProducts = new List<POSProduct>(_allProducts);
        }

        private void StartSessionTimer()
        {
            _sessionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _sessionTimer.Tick += (_, _) => UpdateSessionStats();
            _sessionTimer.Start();
            UpdateSessionStats();
        }

        private void UpdateSessionStats()
        {
            TxtCashierName.Text = _session.CashierName(_isArabic);
            TxtSessionElapsed.Text = _session.ElapsedDisplay;
            TxtStatOrders.Text = _session.OrderCount.ToString();
            TxtStatSales.Text = _isArabic
                ? $"{_session.TotalSales:N2} ر.س"
                : $"SAR {_session.TotalSales:N2}";

            HeldBadge.Visibility = _session.HeldCount > 0 ? Visibility.Visible : Visibility.Collapsed;
            TxtHeldCount.Text = _session.HeldCount.ToString();
        }

        // ══════════════════════════════════════════════════════════
        //  LABELS / LOCALIZATION
        // ══════════════════════════════════════════════════════════

        private void UpdateLabels()
        {
            TxtBarcodeHint.Text = _isArabic ? "بحث أو مسح باركود..." : "Search or scan barcode...";

            // Session bar
            TxtBtnNewOrder.Text = _isArabic ? "طلب جديد" : "New Order";
            TxtBtnHeld.Text = _isArabic ? "معلقة" : "Held";
            TxtBtnCloseShift.Text = _isArabic ? "إغلاق الوردية" : "Close Shift";
            TxtStatOrdersLabel.Text = _isArabic ? "طلب" : "Orders";
            TxtStatSalesLabel.Text = _isArabic ? "مبيعات اليوم" : "Today's Sales";

            // Customer row
            TxtCustomerLabel.Text = _isArabic ? "العميل" : "Customer";
            TxtBtnChangeCustomer.Text = _isArabic ? "تغيير" : "Change";

            // Discount
            TxtDiscountLabel.Text = _isArabic ? "خصم الطلب" : "Order Discount";
            TxtDiscountType.Text = _discountType == DiscountType.Fixed
                ? (_isArabic ? "ر.س" : "SAR")
                : "%";
            TxtOrderNotes.Tag = _isArabic ? "ملاحظات الطلب..." : "Order notes...";
            TxtCouponCode.Tag = _isArabic ? "كود الخصم..." : "Coupon code...";

            // Totals labels
            TxtSubtotalLabel.Text = _isArabic ? "المجموع الجزئي" : "Subtotal";
            TxtTotalDiscountLabel.Text = _isArabic ? "إجمالي الخصم" : "Total Discount";
            TxtTaxLabel.Text = _isArabic ? "ضريبة القيمة المضافة 15%" : "VAT 15%";
            TxtGrandTotalLabel.Text = _isArabic ? "الإجمالي" : "Total";

            // Payment methods
            TxtPayCash.Text = _isArabic ? "نقداً" : "Cash";
            TxtPayCard.Text = _isArabic ? "بطاقة" : "Card";
            TxtPayTransfer.Text = _isArabic ? "تحويل" : "Transfer";
            TxtPaySplit.Text = _isArabic ? "دفع مقسم" : "Split Pay";

            // Empty states
            TxtNoProducts.Text = _isArabic ? "لا توجد منتجات" : "No products found";
            TxtNoProductsSub.Text = _isArabic ? "حاول البحث بكلمة مختلفة" : "Try a different search";
            TxtEmptyCartTitle.Text = _isArabic ? "السلة فارغة" : "Cart is Empty";
            TxtEmptyCartSub.Text = _isArabic ? "اضغط على منتج لإضافته" : "Tap a product to add it";

            // Action buttons
            TxtBtnHold.Text = _isArabic ? "تعليق" : "Hold";
            TxtBtnPrint.Text = _isArabic ? "طباعة" : "Print";
            TxtBtnClear.Text = _isArabic ? "مسح" : "Clear";

            // Update totals display with current language
            UpdateTotalsDisplay();
        }

        // ══════════════════════════════════════════════════════════
        //  CATEGORIES
        // ══════════════════════════════════════════════════════════

        private void BuildCategories()
        {
            CategoriesPanel.Children.Clear();
            foreach (var cat in _categories)
            {
                bool isActive = cat.Id == _activeCategoryId;
                var btn = new Button
                {
                    Style = Application.Current.Resources[isActive ? "POSCategoryActiveStyle" : "POSCategoryStyle"] as Style,
                    Tag = cat.Id
                };

                var sp = new StackPanel { Orientation = Orientation.Horizontal };
                sp.Children.Add(new TextBlock
                {
                    Text = cat.Icon,
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 7, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = isActive ? Brushes.White : (Brush)Application.Current.Resources["TextSecondaryBrush"]
                });
                sp.Children.Add(new TextBlock
                {
                    Text = cat.Name(_isArabic),
                    FontSize = 13,
                    FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal,
                    VerticalAlignment = VerticalAlignment.Center
                });

                btn.Content = sp;
                btn.Click += CategoryBtn_Click;
                CategoriesPanel.Children.Add(btn);
            }
        }

        private void CategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string catId)
            {
                _activeCategoryId = catId;
                TxtBarcodeSearch.Clear();
                BuildCategories();
                FilterProducts(catId);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  PRODUCTS
        // ══════════════════════════════════════════════════════════

        private void FilterProducts(string categoryId, string searchTerm = "")
        {
            _filteredProducts = _allProducts
                .Where(p => (categoryId == "ALL" || p.CategoryId == categoryId)
                         && (string.IsNullOrWhiteSpace(searchTerm)
                             || p.NameAr.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                             || p.NameEn.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                             || p.SKU.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            BuildProductGrid();
        }

        private void BuildProductGrid()
        {
            ProductsPanel.Children.Clear();

            if (!_filteredProducts.Any())
            {
                EmptyProductsState.Visibility = Visibility.Visible;
                return;
            }

            EmptyProductsState.Visibility = Visibility.Collapsed;

            foreach (var product in _filteredProducts)
                ProductsPanel.Children.Add(CreateProductCard(product));
        }

        private UIElement CreateProductCard(POSProduct product)
        {
            var btn = new Button
            {
                Style = Application.Current.Resources["POSProductCardStyle"] as Style,
                Tag = product,
                IsEnabled = product.IsAvailable
            };

            var root = new StackPanel { Margin = new Thickness(0) };

            // ── Icon area
            var iconBg = new Border
            {
                Height = 88,
                CornerRadius = new CornerRadius(10, 10, 0, 0),
                Background = TryBrush(product.IconBgColor, "#EFF6FF"),
                Opacity = product.IsAvailable ? 1.0 : 0.4
            };

            var iconStack = new Grid();
            iconStack.Children.Add(new TextBlock
            {
                Text = product.Icon,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 36,
                Foreground = TryBrush(product.IconColor, "#2563EB"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });

            // Stock badge
            if (product.StockStatus == StockStatus.Low || product.StockStatus == StockStatus.OutOfStock)
            {
                var badge = new Border
                {
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(5, 2, 5, 2),
                    Margin = new Thickness(6),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top
                };

                if (product.StockStatus == StockStatus.OutOfStock)
                {
                    badge.Background = (Brush)Application.Current.Resources["DangerBgBrush"];
                    badge.Child = new TextBlock
                    {
                        Text = _isArabic ? "نفد" : "Out",
                        FontSize = 9, FontWeight = FontWeights.Bold,
                        Foreground = (Brush)Application.Current.Resources["DangerBrush"]
                    };
                }
                else
                {
                    badge.Background = (Brush)Application.Current.Resources["WarningBgBrush"];
                    badge.Child = new TextBlock
                    {
                        Text = _isArabic ? $"باقي {product.Stock}" : $"{product.Stock} left",
                        FontSize = 9, FontWeight = FontWeights.Bold,
                        Foreground = (Brush)Application.Current.Resources["WarningBrush"]
                    };
                }
                iconStack.Children.Add(badge);
            }

            iconBg.Child = iconStack;
            root.Children.Add(iconBg);

            // ── Info area
            var info = new StackPanel { Margin = new Thickness(10, 8, 10, 10) };

            info.Children.Add(new TextBlock
            {
                Text = product.Name(_isArabic),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"],
                FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 36
            });

            info.Children.Add(new TextBlock
            {
                Text = product.SKU,
                FontSize = 10,
                Foreground = (Brush)Application.Current.Resources["TextMutedBrush"],
                FontFamily = new FontFamily("Segoe UI"),
                Margin = new Thickness(0, 2, 0, 4)
            });

            info.Children.Add(new TextBlock
            {
                Text = product.PriceFormatted(_isArabic),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = TryBrush(product.IconColor, "#2563EB"),
                FontFamily = new FontFamily("Segoe UI")
            });

            root.Children.Add(info);
            btn.Content = root;
            btn.Click += ProductCard_Click;

            return btn;
        }

        private void ProductCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is POSProduct product)
                AddToCart(product);
        }

        // ══════════════════════════════════════════════════════════
        //  ORDER MANAGEMENT
        // ══════════════════════════════════════════════════════════

        private void StartNewOrder()
        {
            _invoiceCounter++;
            _order = new POSOrder();
            _order.CustomerName = _isArabic ? "بيع نقدي عام" : "Walk-in Customer";
            _discountType = DiscountType.Fixed;
            _selectedPayment = PaymentMethod.Cash;

            TxtDiscountValue.Text = "0";
            TxtOrderNotes.Text = "";
            TxtCouponCode.Text = "";

            UpdateInvoiceHeader();
            UpdatePaymentButtons();
            RebuildCartUI();
            UpdateTotalsDisplay();
        }

        private void UpdateInvoiceHeader()
        {
            TxtInvoiceNumber.Text = _isArabic
                ? $"فاتورة #{_invoiceCounter}"
                : $"Invoice #{_invoiceCounter}";
            TxtInvoiceDate.Text = DateTime.Now.ToString(_isArabic ? "dd/MM/yyyy HH:mm" : "MM/dd/yyyy hh:mm tt");
            TxtCustomerName.Text = _order.CustomerName;
        }

        // ══════════════════════════════════════════════════════════
        //  CART OPERATIONS
        // ══════════════════════════════════════════════════════════

        private void AddToCart(POSProduct product)
        {
            var existing = _order.Items.FirstOrDefault(i => i.Product.Id == product.Id);
            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                _order.Items.Add(new POSCartItem { Product = product });
            }

            _order.NotifyTotals();
            RebuildCartUI();
            UpdateTotalsDisplay();

            // Scroll cart to bottom
            CartScrollViewer.ScrollToBottom();
        }

        private void RemoveFromCart(POSCartItem item)
        {
            _order.Items.Remove(item);
            _order.NotifyTotals();
            RebuildCartUI();
            UpdateTotalsDisplay();
        }

        private void ChangeQty(POSCartItem item, int delta)
        {
            int newQty = item.Quantity + delta;
            if (newQty <= 0)
            {
                RemoveFromCart(item);
                return;
            }
            item.Quantity = newQty;
            _order.NotifyTotals();
            RebuildCartUI();
            UpdateTotalsDisplay();
        }

        // ══════════════════════════════════════════════════════════
        //  CART UI BUILDER
        // ══════════════════════════════════════════════════════════

        private void RebuildCartUI()
        {
            CartItemsPanel.Children.Clear();

            bool isEmpty = _order.IsEmpty;
            EmptyCartState.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
            CartScrollViewer.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;

            ItemCountBadge.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;

            if (!isEmpty)
            {
                int totalItems = _order.TotalItemCount;
                TxtItemCountBadge.Text = _isArabic
                    ? $"{totalItems} منتج"
                    : $"{totalItems} item{(totalItems != 1 ? "s" : "")}";

                foreach (var item in _order.Items)
                    CartItemsPanel.Children.Add(CreateCartItemRow(item));
            }

            BtnCheckout.IsEnabled = !isEmpty;
            BtnHoldOrder.IsEnabled = !isEmpty;
            BtnClearCart.IsEnabled = !isEmpty;
            BtnPrintReceipt.IsEnabled = !isEmpty;

            // Update checkout button text
            string total = _isArabic
                ? $"{_order.GrandTotal:N2} ر.س"
                : $"SAR {_order.GrandTotal:N2}";
            TxtBtnCheckout.Text = isEmpty
                ? (_isArabic ? "إتمام البيع" : "Checkout")
                : (_isArabic ? $"إتمام البيع — {total}" : $"Checkout — {total}");
        }

        private UIElement CreateCartItemRow(POSCartItem item)
        {
            var rowBorder = new Border
            {
                Padding = new Thickness(12, 11, 12, 11),
                BorderBrush = (Brush)Application.Current.Resources["BorderLightBrush"],
                BorderThickness = new Thickness(0, 0, 0, 1),
                Background = item.HasDiscount
                    ? (Brush)Application.Current.Resources["SuccessBgBrush"]
                    : Brushes.White
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Remove button
            var removeBtn = new Button
            {
                Style = Application.Current.Resources["POSRemoveBtnStyle"] as Style,
                Tag = item,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            removeBtn.Content = new TextBlock
            {
                Text = "\uE711",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 10
            };
            removeBtn.Click += (s, _) => { if (s is Button b && b.Tag is POSCartItem ci) RemoveFromCart(ci); };
            Grid.SetColumn(removeBtn, 0);
            grid.Children.Add(removeBtn);

            // Product info
            var infoPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            infoPanel.Children.Add(new TextBlock
            {
                Text = item.Product.Name(_isArabic),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"],
                FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            infoPanel.Children.Add(new TextBlock
            {
                Text = _isArabic
                    ? $"{item.UnitPrice:N2} ر.س × {item.Quantity}"
                    : $"SAR {item.UnitPrice:N2} × {item.Quantity}",
                FontSize = 11,
                Foreground = (Brush)Application.Current.Resources["TextMutedBrush"],
                FontFamily = new FontFamily("Segoe UI"),
                Margin = new Thickness(0, 2, 0, 0)
            });
            Grid.SetColumn(infoPanel, 1);
            grid.Children.Add(infoPanel);

            // Right panel: total + qty controls
            var rightPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            rightPanel.Children.Add(new TextBlock
            {
                Text = item.LineTotalFormatted(_isArabic),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"],
                HorizontalAlignment = HorizontalAlignment.Right,
                FontFamily = new FontFamily("Segoe UI"),
                Margin = new Thickness(0, 0, 0, 6)
            });

            // Qty controls
            var qtyRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            var decBtn = new Button
            {
                Style = Application.Current.Resources["POSQtyBtnStyle"] as Style,
                Background = (Brush)Application.Current.Resources["BorderLightBrush"],
                Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                Tag = item
            };
            decBtn.Content = new TextBlock { Text = "−", FontSize = 16 };
            decBtn.Click += (s, _) => { if (s is Button b && b.Tag is POSCartItem ci) ChangeQty(ci, -1); };

            var qtyLabel = new TextBlock
            {
                Text = item.Quantity.ToString(),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"],
                FontFamily = new FontFamily("Segoe UI"),
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 28,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(4, 0, 4, 0)
            };

            var incBtn = new Button
            {
                Style = Application.Current.Resources["POSQtyBtnStyle"] as Style,
                Background = (Brush)Application.Current.Resources["PrimaryBrush"],
                Foreground = Brushes.White,
                Tag = item
            };
            incBtn.Content = new TextBlock { Text = "+", FontSize = 16 };
            incBtn.Click += (s, _) => { if (s is Button b && b.Tag is POSCartItem ci) ChangeQty(ci, +1); };

            qtyRow.Children.Add(decBtn);
            qtyRow.Children.Add(qtyLabel);
            qtyRow.Children.Add(incBtn);
            rightPanel.Children.Add(qtyRow);

            Grid.SetColumn(rightPanel, 2);
            grid.Children.Add(rightPanel);

            rowBorder.Child = grid;
            return rowBorder;
        }

        // ══════════════════════════════════════════════════════════
        //  TOTALS DISPLAY
        // ══════════════════════════════════════════════════════════

        private void UpdateTotalsDisplay()
        {
            string cur = _isArabic ? "ر.س" : "SAR";

            TxtSubtotalValue.Text = _isArabic
                ? $"{_order.Subtotal:N2} {cur}"
                : $"{cur} {_order.Subtotal:N2}";

            bool hasDiscount = _order.TotalDiscount > 0;
            DiscountTotalRow.Visibility = hasDiscount ? Visibility.Visible : Visibility.Collapsed;
            TxtTotalDiscountValue.Text = _isArabic
                ? $"− {_order.TotalDiscount:N2} {cur}"
                : $"− {cur} {_order.TotalDiscount:N2}";

            TxtTaxValue.Text = _isArabic
                ? $"{_order.TaxAmount:N2} {cur}"
                : $"{cur} {_order.TaxAmount:N2}";

            TxtGrandTotalValue.Text = _isArabic
                ? $"{_order.GrandTotal:N2} {cur}"
                : $"{cur} {_order.GrandTotal:N2}";

            // Update checkout button text
            if (!_order.IsEmpty)
            {
                string total = _isArabic
                    ? $"{_order.GrandTotal:N2} {cur}"
                    : $"{cur} {_order.GrandTotal:N2}";
                TxtBtnCheckout.Text = _isArabic
                    ? $"إتمام البيع — {total}"
                    : $"Checkout — {total}";
            }
        }

        // ══════════════════════════════════════════════════════════
        //  PAYMENT METHODS
        // ══════════════════════════════════════════════════════════

        private void UpdatePaymentButtons()
        {
            var activeStyle = Application.Current.Resources["POSPaymentActiveBtnStyle"] as Style;
            var normalStyle = Application.Current.Resources["POSPaymentBtnStyle"] as Style;

            BtnPayCash.Style = _selectedPayment == PaymentMethod.Cash ? activeStyle : normalStyle;
            BtnPayCard.Style = _selectedPayment == PaymentMethod.Card ? activeStyle : normalStyle;
            BtnPayTransfer.Style = _selectedPayment == PaymentMethod.Transfer ? activeStyle : normalStyle;
            BtnPaySplit.Style = _selectedPayment == PaymentMethod.Split ? activeStyle : normalStyle;
        }

        private void PaymentMethod_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                _selectedPayment = btn.Tag?.ToString() switch
                {
                    "Card" => PaymentMethod.Card,
                    "Transfer" => PaymentMethod.Transfer,
                    "Split" => PaymentMethod.Split,
                    _ => PaymentMethod.Cash
                };
                _order.PaymentMethod = _selectedPayment;
                UpdatePaymentButtons();
            }
        }

        // ══════════════════════════════════════════════════════════
        //  DISCOUNT
        // ══════════════════════════════════════════════════════════

        private void BtnDiscountType_Click(object sender, RoutedEventArgs e)
        {
            _discountType = _discountType == DiscountType.Fixed ? DiscountType.Percentage : DiscountType.Fixed;
            _order.OrderDiscountType = _discountType;
            TxtDiscountType.Text = _discountType == DiscountType.Fixed
                ? (_isArabic ? "ر.س" : "SAR")
                : "%";
            ApplyOrderDiscount();
        }

        private void TxtDiscountValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyOrderDiscount();
        }

        private void ApplyOrderDiscount()
        {
            if (_order == null) return;
            if (decimal.TryParse(TxtDiscountValue.Text, out decimal val))
            {
                _order.OrderDiscountType = _discountType;
                _order.OrderDiscount = val;
                UpdateTotalsDisplay();
            }
        }

        private void TxtCouponCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                ApplyCoupon(TxtCouponCode.Text.Trim().ToUpperInvariant());
        }

        private void ApplyCoupon(string code)
        {
            // Stub: ready for backend integration
            if (string.IsNullOrEmpty(code)) return;

            // Demo coupon codes
            if (code == "SAVE10")
            {
                _order.OrderDiscountType = DiscountType.Percentage;
                _order.OrderDiscount = 10;
                _discountType = DiscountType.Percentage;
                TxtDiscountValue.Text = "10";
                TxtDiscountType.Text = "%";
                UpdateTotalsDisplay();
                MessageBox.Show(
                    _isArabic ? "تم تطبيق كود الخصم SAVE10 — خصم 10%" : "Coupon SAVE10 applied — 10% off",
                    _isArabic ? "كود الخصم" : "Coupon",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    _isArabic ? "كود الخصم غير صحيح" : "Invalid coupon code",
                    _isArabic ? "خطأ" : "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  SESSION ACTIONS
        // ══════════════════════════════════════════════════════════

        private void BtnNewOrder_Click(object sender, RoutedEventArgs e)
        {
            if (!_order.IsEmpty)
            {
                var result = MessageBox.Show(
                    _isArabic
                        ? "الطلب الحالي لم يُكتمل. هل تريد إنشاء طلب جديد؟"
                        : "Current order is not completed. Start a new order?",
                    _isArabic ? "طلب جديد" : "New Order",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;
            }

            StartNewOrder();
        }

        private void BtnHeldOrders_Click(object sender, RoutedEventArgs e)
        {
            if (_session.HeldCount == 0)
            {
                MessageBox.Show(
                    _isArabic ? "لا توجد طلبات معلقة" : "No held orders",
                    _isArabic ? "الطلبات المعلقة" : "Held Orders",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Recall most recent held order
            var recalled = _session.RecallOrder(_session.HeldCount - 1);
            if (recalled != null)
            {
                _order = recalled;
                UpdateSessionStats();
                RebuildCartUI();
                UpdateTotalsDisplay();
                UpdateInvoiceHeader();
            }
        }

        private void BtnCloseShift_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                _isArabic
                    ? $"هل تريد إغلاق الوردية؟\nعدد الطلبات: {_session.OrderCount}\nإجمالي المبيعات: {_session.TotalSales:N2} ر.س"
                    : $"Close shift?\nOrders: {_session.OrderCount}\nTotal Sales: SAR {_session.TotalSales:N2}",
                _isArabic ? "إغلاق الوردية" : "Close Shift",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _sessionTimer.Stop();
                _session.Status = SessionStatus.Closed;
                MessageBox.Show(
                    _isArabic ? "تم إغلاق الوردية بنجاح" : "Shift closed successfully",
                    _isArabic ? "مغلق" : "Closed",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  ORDER ACTIONS
        // ══════════════════════════════════════════════════════════

        private void BtnCheckout_Click(object sender, RoutedEventArgs e)
        {
            if (_order.IsEmpty) return;

            string summary = _isArabic
                ? $"المبلغ الإجمالي: {_order.GrandTotal:N2} ر.س\nطريقة الدفع: {GetPaymentLabel()}"
                : $"Grand Total: SAR {_order.GrandTotal:N2}\nPayment: {GetPaymentLabel()}";

            var result = MessageBox.Show(summary,
                _isArabic ? "تأكيد البيع" : "Confirm Sale",
                MessageBoxButton.OKCancel, MessageBoxImage.Question);

            if (result == MessageBoxResult.OK)
                CompleteSale();
        }

        private void CompleteSale()
        {
            _session.RecordSale(_order);
            UpdateSessionStats();
            MessageBox.Show(
                _isArabic ? "تم إتمام البيع بنجاح ✓" : "Sale completed successfully ✓",
                _isArabic ? "تم البيع" : "Sale Complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
            StartNewOrder();
        }

        private string GetPaymentLabel() => _selectedPayment switch
        {
            PaymentMethod.Card => _isArabic ? "بطاقة" : "Card",
            PaymentMethod.Transfer => _isArabic ? "تحويل بنكي" : "Bank Transfer",
            PaymentMethod.Split => _isArabic ? "دفع مقسم" : "Split Payment",
            _ => _isArabic ? "نقداً" : "Cash"
        };

        private void BtnHoldOrder_Click(object sender, RoutedEventArgs e)
        {
            if (_order.IsEmpty) return;
            _session.HoldOrder(_order);
            UpdateSessionStats();
            StartNewOrder();
            MessageBox.Show(
                _isArabic ? "تم تعليق الطلب" : "Order held",
                _isArabic ? "تعليق" : "Hold",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnPrintReceipt_Click(object sender, RoutedEventArgs e)
        {
            // Stub: wired for printer integration
            MessageBox.Show(
                _isArabic ? "جاري الطباعة..." : "Printing...",
                _isArabic ? "طباعة الفاتورة" : "Print Receipt",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnClearCart_Click(object sender, RoutedEventArgs e)
        {
            if (_order.IsEmpty) return;
            var result = MessageBox.Show(
                _isArabic ? "هل تريد مسح كل المنتجات من السلة؟" : "Clear all items from cart?",
                _isArabic ? "مسح السلة" : "Clear Cart",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _order.Items.Clear();
                _order.OrderDiscount = 0;
                TxtDiscountValue.Text = "0";
                _order.NotifyTotals();
                RebuildCartUI();
                UpdateTotalsDisplay();
            }
        }

        // ══════════════════════════════════════════════════════════
        //  CUSTOMER
        // ══════════════════════════════════════════════════════════

        private void BtnChangeCustomer_Click(object sender, RoutedEventArgs e)
        {
            // Stub: will open customer search dialog
            MessageBox.Show(
                _isArabic ? "سيتم فتح بحث العملاء قريباً" : "Customer search coming soon",
                _isArabic ? "اختيار العميل" : "Select Customer",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ══════════════════════════════════════════════════════════
        //  BARCODE / SEARCH
        // ══════════════════════════════════════════════════════════

        private void TxtBarcodeSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string term = TxtBarcodeSearch.Text.Trim();
            TxtBarcodeHint.Visibility = string.IsNullOrEmpty(term) ? Visibility.Visible : Visibility.Collapsed;

            if (string.IsNullOrEmpty(term))
            {
                FilterProducts(_activeCategoryId);
                return;
            }

            // Search across all categories when typing
            _activeCategoryId = "ALL";
            BuildCategories();
            FilterProducts("ALL", term);
        }

        private void TxtBarcodeSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string term = TxtBarcodeSearch.Text.Trim();
                // Barcode scan: exact SKU match → add immediately
                var match = _allProducts.FirstOrDefault(p =>
                    p.SKU.Equals(term, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    AddToCart(match);
                    TxtBarcodeSearch.Clear();
                }
            }
            else if (e.Key == Key.Escape)
            {
                TxtBarcodeSearch.Clear();
            }
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        private static SolidColorBrush TryBrush(string hex, string fallback)
        {
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            }
            catch
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(fallback));
            }
        }
    }
}
