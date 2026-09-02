using PrintAndScan4Ukraine.ViewModel;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace PrintAndScan4Ukraine
{
    public partial class VerifyScanWindow2 : Window, System.ComponentModel.INotifyPropertyChanged
    {
        private readonly PackagesViewModel _vm;
        private string _current = string.Empty;

        public ObservableCollection<ScannedItem> ScannedItems { get; } = new();

        public VerifyScanWindow2(PackagesViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = this;
            PreviewKeyDown += VerifyScanWindow2_PreviewKeyDown;

            ScannedItems.CollectionChanged += (s, e) =>
            {
                RaisePropertyChanged(nameof(TotalScanned));
                RaisePropertyChanged(nameof(ExistsCount));
                RaisePropertyChanged(nameof(NotFoundCount));
            };
        }

        private async void VerifyScanWindow2_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                return;
            }

            if (e.Key == Key.Enter)
            {
                var code = _current.Replace("\0", "").Trim();
                if (!string.IsNullOrEmpty(code))
                {
                    await ProcessCode(code);
                }
                _current = string.Empty;
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Back && _current.Length > 0)
            {
                _current = _current[..^1];
            }
            else
            {
                _current += ToChar(e.Key).ToString().Replace("\0", "");
            }
            TxtCurrent.Text = _current == string.Empty ? "Ready to scan..." : _current;
        }

        private async Task ProcessCode(string code)
        {
#if DEBUG
            Regex regex = new("");
#else
            Regex regex = new("^cv\\d{7,9}us$");
#endif
            var match = regex.Match(code);
            string statusText = "";
            bool? exists = null;
            if (match.Success)
            {
                exists = await _vm.VerifyIfExists(code);
                if (exists == null) statusText = "Offline";
                else if (exists.Value) statusText = "Exists";
                else statusText = "Not found";
            }
            else
            {
                statusText = "Invalid format";
            }

            ScannedItems.Insert(0, new ScannedItem { Barcode = code, Exists = exists, StatusText = statusText });

            // Flash animation
            var sb = (Storyboard)FindResource("FlashAnimation");
            sb.Begin();

            TxtCurrent.Text = $"{code} - {statusText}";
        }

        public int TotalScanned => ScannedItems.Count;

        // Use LINQ on IEnumerable so the Count(predicate) extension isn't shadowed by the Count property
        public int ExistsCount => ScannedItems.Where(i => i.Exists == true).Count();

        public int NotFoundCount => ScannedItems.Where(i => i.Exists == false).Count();

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        private void RaisePropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(prop));
        }

        private static char ToChar(Key key)
        {
            char c = '\0';
            if ((key >= Key.A) && (key <= Key.Z))
            {
                c = (char)('a' + (key - Key.A));
            }
            else if ((key >= Key.D0) && (key <= Key.D9))
            {
                c = (char)('0' + (key - Key.D0));
            }
            return c;
        }
    }

    public class ScannedItem
    {
        public string Barcode { get; set; } = string.Empty;
        public bool? Exists { get; set; }
        public string StatusText { get; set; } = string.Empty;
    }
}
