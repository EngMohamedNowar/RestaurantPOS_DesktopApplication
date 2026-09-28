// Views/ForceChangePinWindow.cs
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PizzaPOS.Helpers;
using PizzaPOS.Services;

namespace PizzaPOS.Views
{
    /// <summary>
    /// نافذة إجبارية بتظهر بعد الدخول لو الحساب لسه على PIN ضعيف — أي الحسابات
    /// المزروعة (PBKDF2 + MustChangePin=1) أو أي حساب قديم مخزّن بـ SHA256 بدون salt.
    /// مفيش زرار إغلاق ولا X — لازم يغيّر الـ PIN أو يعدّل الـ DB يدوياً.
    /// الـ PIN الحالي بيتحقق منه مقابل الهاش المخزّن في الـ DB (مش hardcoded).
    /// </summary>
    public class ForceChangePinWindow : Window
    {
        readonly UserService _svc = new();
        readonly int _userId;
        readonly string _userName;

        readonly TextBox _tbCurrent;
        readonly TextBox _tbNew;
        readonly TextBox _tbConfirm;
        readonly TextBlock _txtError;

        public ForceChangePinWindow(int userId, string userName)
        {
            _userId = userId;
            _userName = userName;

            Title = "تغيير PIN مطلوب";
            Width = 430;
            SizeToContent = SizeToContent.Height;
            FlowDirection = FlowDirection.RightToLeft;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FontFamily = new FontFamily("Tahoma");
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.None;

            _tbCurrent = MakePinBox();
            _tbNew = MakePinBox();
            _tbConfirm = MakePinBox();
            _txtError = new TextBlock
            {
                FontSize = 11,
                Foreground = UiHelper.B("#E63946"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12),
                MinHeight = 14
            };

            var root = new StackPanel { Margin = new Thickness(24) };

            var banner = new Border
            {
                Background = UiHelper.B("#2a1a1a"),
                BorderBrush = UiHelper.B("#E63946"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 18)
            };
            banner.Child = new TextBlock
            {
                Text = "⚠️ لازم تغيّر الـ PIN قبل ما تدخل نقطة البيع.\n"
                     + "الحساب ده لسه شغال برقم ضعيف معروف في الكود، فأي حد عنده نسخة "
                     + "من البرنامج يقدر يدخل بيه.",
                FontSize = 12,
                Foreground = UiHelper.B("#ffd166"),
                TextWrapping = TextWrapping.Wrap
            };
            root.Children.Add(banner);

            root.Children.Add(new TextBlock
            {
                Text = "👤 المستخدم",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = UiHelper.B("#4a6080"),
                Margin = new Thickness(0, 0, 0, 4)
            });
            root.Children.Add(new TextBlock
            {
                Text = userName,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = UiHelper.B("#a78bfa"),
                Margin = new Thickness(0, 0, 0, 18)
            });

            root.Children.Add(UiHelper.FieldLabel("الـ PIN الحالي (اللي دخلت بيه) *"));
            _tbCurrent.Margin = new Thickness(0, 4, 0, 14);
            root.Children.Add(_tbCurrent);

            root.Children.Add(UiHelper.FieldLabel("الـ PIN الجديد (4 أرقام) *"));
            _tbNew.Margin = new Thickness(0, 4, 0, 14);
            root.Children.Add(_tbNew);

            root.Children.Add(UiHelper.FieldLabel("تأكيد الـ PIN الجديد *"));
            _tbConfirm.Margin = new Thickness(0, 4, 0, 14);
            root.Children.Add(_tbConfirm);

            root.Children.Add(_txtError);

            var saveBtn = UiHelper.MakeBtn("حفظ ومتابعة", "#06d6a0", UiHelper.B("#0a0a14"), Submit, 12, 14);
            saveBtn.MinHeight = 42;
            root.Children.Add(saveBtn);

            var quitBtn = UiHelper.MakeBtn("إغلاق البرنامج", "#2a1a1a", UiHelper.B("#E63946"),
                () =>
                {
                    DialogResult = false;
                    Application.Current.Shutdown();
                }, 10, 12);
            quitBtn.MinHeight = 34;
            quitBtn.Margin = new Thickness(0, 8, 0, 0);
            root.Children.Add(quitBtn);

            Content = new Border
            {
                Background = UiHelper.B("#0a0a14"),
                BorderBrush = UiHelper.B("#E63946"),
                BorderThickness = new Thickness(2),
                Child = root
            };

            _tbCurrent.KeyDown += OnEnter;
            _tbNew.KeyDown += OnEnter;
            _tbConfirm.KeyDown += OnEnter;
            foreach (var tb in new[] { _tbCurrent, _tbNew, _tbConfirm })
            {
                tb.PreviewTextInput += (_, e) =>
                {
                    if (!char.IsDigit(e.Text, 0)) e.Handled = true;
                };
            }

            Loaded += (_, _) => _tbCurrent.Focus();
        }

        void OnEnter(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            Submit();
        }

        TextBox MakePinBox()
        {
            var tb = UiHelper.MakeTB("", "#a78bfa");
            tb.FontSize = 20;
            tb.MaxLength = 4;
            tb.TextAlignment = TextAlignment.Center;
            tb.FlowDirection = FlowDirection.LeftToRight;
            return tb;
        }

        void Submit()
        {
            _txtError.Text = "";

            if (_tbCurrent.Text.Length != 4)
            {
                _txtError.Text = "❌ أدخل الـ PIN الحالي";
                _tbCurrent.Clear(); _tbCurrent.Focus();
                return;
            }

            string? validation = UserService.ValidateNewPin(_tbNew.Text);
            if (validation != null)
            {
                _txtError.Text = "❌ " + validation;
                _tbNew.Clear(); _tbNew.Focus();
                return;
            }

            if (_tbNew.Text != _tbConfirm.Text)
            {
                _txtError.Text = "❌ الـ PIN الجديد والتأكيد مش متطابقين";
                _tbConfirm.Clear(); _tbConfirm.Focus();
                return;
            }

            string? error = _svc.TryChangeOwnPin(_userId, _tbCurrent.Text, _tbNew.Text);
            if (error != null)
            {
                _txtError.Text = "❌ " + error;
                _tbNew.Clear(); _tbConfirm.Clear(); _tbNew.Focus();
                return;
            }

            AppLogger.Info($"User Id={_userId} ('{_userName}') rotated a weak/bootstrap PIN.");
            DialogResult = true;
            Close();
        }
    }
}
