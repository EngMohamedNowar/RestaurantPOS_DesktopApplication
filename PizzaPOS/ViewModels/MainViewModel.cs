// ViewModels/MainViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using PizzaPOS.Data;
using PizzaPOS.Models;
using PizzaPOS.Services;
using PizzaPOS.Views;
using PizzaPOS.Helpers;
using OrderTypeConst = PizzaPOS.Models.OrderType;

namespace PizzaPOS.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        void Notify([CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        readonly AppDbContext _db = new();
        readonly InventoryService _inv = new();
        readonly EpsonService _printer = new();

        public ObservableCollection<Category> Categories { get; } = new();
        public ObservableCollection<Product> Products { get; } = new();
        public ObservableCollection<OrderItem> OrderItems { get; } = new();
        public ObservableCollection<Order> HeldOrders { get; } = new();

        // ── باليت الألوان: (CardBg, CardAccent, CardIconBg, CardPriceBg) ──
        static readonly (string Bg, string Accent, string IconBg, string PriceBg)[] _palette =
        {
            ("#2d1200", "#FF6B35", "#3d1a00", "#1a0e00"),  // 🟠 برتقالي
            ("#2d0808", "#E63946", "#3d0f0f", "#1a0808"),  // 🔴 أحمر
            ("#081828", "#60a5fa", "#0f2238", "#081828"),  // 🔵 أزرق
            ("#2d2200", "#ffd166", "#3d2e00", "#1a1400"),  // 🟡 ذهبي
            ("#082018", "#06d6a0", "#0f2e22", "#081a10"),  // 🟢 أخضر
            ("#180820", "#a78bfa", "#220f30", "#100818"),  // 🟣 بنفسجي
            ("#1a1008", "#fb923c", "#281808", "#160e06"),  // 🟤 عنبري
            ("#081820", "#38bdf8", "#0f2230", "#081018"),  // 🩵 سماوي
        };

        // ── Search ──────────────────────────────────
        string _search = "";
        public string Search
        {
            get => _search;
            set { _search = value; Notify(); LoadProducts(); }
        }

        // ── Selected Category ────────────────────────
        int? _selCat;
        public int? SelectedCategoryId
        {
            get => _selCat;
            set { _selCat = value; Notify(); LoadProducts(); }
        }

        // ── Order Type ───────────────────────────────
        string _orderType = OrderTypeConst.Delivery;
        public string OrderType
        {
            get => _orderType;
            set { _orderType = value; Notify(); }
        }

        // ── Discount ─────────────────────────────────
        string _discInput = "";
        public string DiscountInput
        {
            get => _discInput;
            set { _discInput = value; Notify(); Recalc(); }
        }

        bool _discPct = true;
        public bool DiscountIsPercent
        {
            get => _discPct;
            set { _discPct = value; Notify(); Notify(nameof(DiscSym)); Recalc(); }
        }
        public string DiscSym => _discPct ? "%" : "ج";

        // ── Notes ────────────────────────────────────
        string _notes = "";
        public string Notes { get => _notes; set { _notes = value; Notify(); } }

        // ── Order Number ─────────────────────────────
        string _orderNum = "#0001";
        public string OrderNumber { get => _orderNum; set { _orderNum = value; Notify(); } }

        // ── Totals ───────────────────────────────────
        double _sub, _disc, _tax, _srv, _total;
        public double Subtotal { get => _sub; set { _sub = value; Notify(); } }
        public double Discount { get => _disc; set { _disc = value; Notify(); } }
        public double Tax { get => _tax; set { _tax = value; Notify(); } }
        public double ServiceCharge { get => _srv; set { _srv = value; Notify(); } }
        public double Total { get => _total; set { _total = value; Notify(); } }

        double _taxRate;
        public double TaxRate
        {
            get => _taxRate;
            private set { _taxRate = value; Notify(); }
        }

        double _svcRate;
        public double ServiceRate
        {
            get => _svcRate;
            private set { _svcRate = value; Notify(); }
        }

        // ── HasItems ─────────────────────────────────
        public bool HasItems => OrderItems.Count > 0;

        // ── Daily Stats ──────────────────────────────
        double _sales; int _orders; double _avg;
        double _profit; double _loss;

        public double TodaySales { get => _sales; set { _sales = value; Notify(); } }
        public int TodayOrders { get => _orders; set { _orders = value; Notify(); } }
        public double AvgOrder { get => _avg; set { _avg = value; Notify(); } }
        public double TodayProfit { get => _profit; set { _profit = value; Notify(); } }
        public double TodayLoss { get => _loss; set { _loss = value; Notify(); } }

        // ── Commands ─────────────────────────────────
        public ICommand AddProductCmd => new RelayCommand(p => AddProduct((Product)p!));
        public ICommand IncQtyCmd => new RelayCommand(p => ChangeQty((OrderItem)p!, +1));
        public ICommand DecQtyCmd => new RelayCommand(p => ChangeQty((OrderItem)p!, -1));
        public ICommand RemoveItemCmd => new RelayCommand(p => RemoveItem((OrderItem)p!));
        public ICommand PayCashCmd => new RelayCommand(_ => PayCash(), _ => HasItems);
        public ICommand PayCardCmd => new RelayCommand(_ => PayCard(), _ => HasItems);
        public ICommand HoldCmd => new RelayCommand(_ => HoldOrder(), _ => HasItems);
        public ICommand ClearCmd => new RelayCommand(_ => ClearOrder(), _ => HasItems);
        public ICommand ResumeCmd => new RelayCommand(p => ResumeOrder((Order)p!));
        public ICommand ToggleDiscCmd => new RelayCommand(_ => DiscountIsPercent = !DiscountIsPercent);
        public ICommand SetOrderTypeCmd => new RelayCommand(p => OrderType = p!.ToString()!);
        public ICommand SelectCatCmd => new RelayCommand(p =>
            SelectedCategoryId = (int?)p == 0 ? null : (int?)p);
        public ICommand CloseShiftCmd => new RelayCommand(_ => CloseShift());

        // ── Constructor ──────────────────────────────
        public MainViewModel()
        {
            OrderItems.CollectionChanged += (_, _) =>
            {
                Notify(nameof(HasItems));
                Recalc();
                CommandManager.InvalidateRequerySuggested();
            };
            LoadCategories();
            LoadProducts();
            RefreshStats();
            OrderNumber = _db.GetNextOrderNumber();
            Recalc();
        }

        // ── Load Categories ──────────────────────────
        public void LoadCategories()
        {
            Categories.Clear();
            Categories.Add(new Category { Id = 0, Name = "🔥 الكل", Icon = "🔥" });
            foreach (var c in _db.GetCategories()) Categories.Add(c);
        }

        // ── Load Products — مع الألوان ───────────────
        public void LoadProducts()
        {
            Products.Clear();
            int? cat = (_selCat == 0 || _selCat == null) ? null : _selCat;

            var catIds = _db.GetCategories()
                            .Select((c, i) => (c.Id, i))
                            .ToDictionary(x => x.Id, x => x.i);

            foreach (var p in _db.GetProducts(cat,
                string.IsNullOrWhiteSpace(_search) ? null : _search))
            {
                int idx = catIds.TryGetValue(p.CategoryId, out int i)
                          ? i % _palette.Length
                          : 0;
                var pal = _palette[idx];
                p.CardColor = pal.Bg;
                p.CardAccent = pal.Accent;
                p.CardIconBg = pal.IconBg;
                p.CardPriceBg = pal.PriceBg;
                Products.Add(p);
            }
        }

        // ── Add Product ──────────────────────────────
        public void AddProduct(Product p)
        {
            var sizes = _db.GetProductSizes(p.Id);
            var extras = _db.GetProductExtras(p.Id);
            OrderItem item;

            if (sizes.Count > 0 || extras.Count > 0)
            {
                var dlg = new SizeExtrasDialog(p, sizes, extras)
                { Owner = Application.Current.MainWindow };
                if (dlg.ShowDialog() != true) return;
                item = dlg.Result!;
            }
            else
            {
                item = new OrderItem
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Icon = p.Icon,
                    BasePrice = p.Price,
                    Cost = p.Cost,
                    ExtrasKey = $"{p.Id}|",
                    Qty = 1
                };
            }

            var existing = OrderItems.FirstOrDefault(i =>
                i.ProductId == item.ProductId &&
                i.SizeName == item.SizeName &&
                i.ExtrasKey == item.ExtrasKey);

            if (existing != null) existing.Qty++;
            else OrderItems.Add(item);

            Recalc();
        }

        void ChangeQty(OrderItem item, int d)
        {
            item.Qty += d;
            if (item.Qty <= 0) RemoveItem(item);
            else Recalc();
        }

        void RemoveItem(OrderItem item) { OrderItems.Remove(item); Recalc(); }

        // ── Recalc ───────────────────────────────────
        void Recalc()
        {
            double sub = OrderItems.Sum(i => i.Subtotal);
            double dv = double.TryParse(_discInput, out var d) ? d : 0;
            double disc = _discPct ? sub * (dv / 100) : Math.Min(dv, sub);
            double after = sub - disc;

            string taxStr = _db.GetSetting("TaxRate", "14");
            string srvStr = _db.GetSetting("ServiceRate", "0");

            double taxPct = double.TryParse(taxStr, out var t) ? t : 14;
            double srvPct = double.TryParse(srvStr, out var sr) ? sr : 0;

            if (taxPct < 1) taxPct *= 100;
            if (srvPct is > 0 and < 1) srvPct *= 100;

            Subtotal = sub;
            Discount = disc;
            Tax = after * (taxPct / 100);
            ServiceCharge = after * (srvPct / 100);
            Total = after + Tax + ServiceCharge;
            TaxRate = taxPct;
            ServiceRate = srvPct;
        }

        // ── Pay Cash ─────────────────────────────────
        void PayCash()
        {
            if (_orderType == OrderTypeConst.Delivery)
            {
                var delDlg = new DeliveryDialog(_db, Total)
                { Owner = Application.Current.MainWindow };

                delDlg.ShowDialog();

                // ── تعليق مع بيانات التوصيل ──
                if (delDlg.IsHeld)
                {
                    HoldOrderWithDelivery(delDlg);
                    return;
                }

                // ── إلغاء ──
                if (delDlg.DialogResult != true) return;

                var cashDlg = new CashDialog(Total + delDlg.DeliveryFee);
                if (cashDlg.ShowDialog() != true) return;

                Complete(PayMethod.Cash, cashDlg.PaidAmount,
                    cashDlg.PaidAmount - (Total + delDlg.DeliveryFee), delDlg);
            }
            else
            {
                var dlg = new CashDialog(Total);
                if (dlg.ShowDialog() != true) return;
                Complete(PayMethod.Cash, dlg.PaidAmount, dlg.PaidAmount - Total, null);
            }
        }

        // ── Pay Card ─────────────────────────────────
        void PayCard()
        {
            if (_orderType == OrderTypeConst.Delivery)
            {
                var delDlg = new DeliveryDialog(_db, Total)
                { Owner = Application.Current.MainWindow };

                delDlg.ShowDialog();

                // ── تعليق مع بيانات التوصيل ──
                if (delDlg.IsHeld)
                {
                    HoldOrderWithDelivery(delDlg);
                    return;
                }

                // ── إلغاء ──
                if (delDlg.DialogResult != true) return;

                double finalTotal = Total + delDlg.DeliveryFee;
                if (MessageBox.Show(
                        $"تأكيد دفع فيزا\nالإجمالي: {finalTotal:F2} ج", "تأكيد",
                        MessageBoxButton.OKCancel, MessageBoxImage.Question)
                    != MessageBoxResult.OK) return;
                Complete(PayMethod.Card, finalTotal, 0, delDlg);
            }
            else
            {
                if (MessageBox.Show(
                        $"تأكيد دفع فيزا\nالإجمالي: {Total:F2} ج", "تأكيد",
                        MessageBoxButton.OKCancel, MessageBoxImage.Question)
                    != MessageBoxResult.OK) return;
                Complete(PayMethod.Card, Total, 0, null);
            }
        }

        // ── Hold Order With Delivery Info ────────────
        void HoldOrderWithDelivery(DeliveryDialog delivery)
        {
            HeldOrders.Add(new Order
            {
                OrderNumber = _orderNum,
                OrderType = OrderTypeConst.Delivery,
                Total = Total,
                Notes = _notes,
                CreatedAt = DateTime.Now.ToString("HH:mm"),
                Items = OrderItems.ToList(),
                CustomerName = delivery.CustomerName,
                CustomerPhone = delivery.CustomerPhone,
                DeliveryAddress = delivery.DeliveryAddress,
                DriverId = delivery.DriverId,
                DriverName = delivery.DriverName,
                DeliveryFee = delivery.DeliveryFee,
                DeliveryStatus = DeliveryStatuses.Pending
            });
            ClearOrder();
        }

        // ── Complete Order ───────────────────────────
        void Complete(string payMethod, double paid, double change,
                      DeliveryDialog? delivery)
        {
            var snapshot = OrderItems.ToList();
            double finalTotal = Total + (delivery?.DeliveryFee ?? 0);

            var order = new Order
            {
                OrderNumber = _db.GetNextOrderNumber(),
                ShiftId = SessionService.CurrentShift?.Id ?? 0,
                UserId = SessionService.CurrentUser?.Id ?? 0,
                OrderType = _orderType,
                PayMethod = payMethod,
                Subtotal = Subtotal,
                Discount = Discount,
                Tax = Tax,
                ServiceCharge = ServiceCharge,
                Total = finalTotal,
                PaidAmount = paid,
                Change = change,
                Notes = _notes,
                Items = snapshot,
                Status = OrderStatus.New,
                CustomerId = delivery?.CustomerId ?? 0,
                CustomerName = delivery?.CustomerName ?? "",
                CustomerPhone = delivery?.CustomerPhone ?? "",
                DeliveryAddress = delivery?.DeliveryAddress ?? "",
                DriverId = delivery?.DriverId ?? 0,
                DriverName = delivery?.DriverName ?? "",
                DeliveryFee = delivery?.DeliveryFee ?? 0,
                DeliveryStatus = delivery != null ? DeliveryStatuses.InTransit : ""
            };

            _db.SaveOrder(order);
            _inv.DeductForOrder(snapshot, SessionService.CurrentUser?.Id ?? 0);

            // Loyalty: 1 point per 10 EGP spent
            if (order.CustomerId > 0)
            {
                int earnedPoints = (int)(order.Total / 10);
                _db.AddLoyaltyPoints(order.CustomerId, earnedPoints);
            }

            _printer.PrintReceipt(order, _db);
            RefreshStats();
            ClearOrder();
        }

        // ── Hold / Resume ────────────────────────────
        void HoldOrder()
        {
            HeldOrders.Add(new Order
            {
                OrderNumber = _orderNum,
                OrderType = _orderType,
                Total = Total,
                Notes = _notes,
                CreatedAt = DateTime.Now.ToString("HH:mm"),
                Items = OrderItems.ToList()
            });
            ClearOrder();
        }

        void ResumeOrder(Order held)
        {
            ClearOrder();
            foreach (var i in held.Items) OrderItems.Add(i);
            _orderType = held.OrderType;
            _notes = held.Notes ?? "";
            Notify(nameof(OrderType));
            Notify(nameof(Notes));
            HeldOrders.Remove(held);
            Recalc();
        }

        // ── Clear ────────────────────────────────────
        public void ClearOrder()
        {
            OrderItems.Clear();
            _discInput = ""; _notes = "";
            _orderType = OrderTypeConst.Delivery;
            Notify(nameof(OrderType));
            Notify(nameof(DiscountInput));
            Notify(nameof(Notes));
            var defDisc = _db.GetSetting("DefaultDiscount", "0");
            _discInput = double.TryParse(defDisc, out var d) && d > 0 ? defDisc : "";
            _discPct = true;
            Notify(nameof(DiscountInput));
            OrderNumber = _db.GetNextOrderNumber();
            Recalc();
        }

        // ── Refresh Stats ────────────────────────────
        public void RefreshStats()
        {
            var (s, c, a) = _db.GetTodaySummary();
            var (pr, lo) = _db.GetTodayProfitLoss();
            TodaySales = s;
            TodayOrders = c;
            AvgOrder = a;
            TodayProfit = pr;
            TodayLoss = lo;
        }

        // ── Close Shift ──────────────────────────────
        void CloseShift()
        {
            if (SessionService.CurrentShift == null)
            { MessageBox.Show("لا يوجد شفت مفتوح"); return; }
            new CloseShiftDialog(SessionService.CurrentShift).ShowDialog();
        }
    }
}