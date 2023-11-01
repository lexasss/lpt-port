using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Port = LptPort.LptPort;

namespace LptPortState
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            if (!IsInpOutAvailable())
                return;

            cmbPorts.ItemsSource = Port.GetPorts();
            if (cmbPorts.Items.Count > 0)
                cmbPorts.SelectedIndex = 0;

            for (int i = 0; i < 13; i++)
            {
                var pin = new Ellipse() { Margin = new Thickness(154, 14 + i*26.5, 0, 0) };
                _pins[i] = pin;
                cnvPins.Children.Add(pin);
            }

            for (int i = 0; i < 12; i++)
            {
                var pin = new Ellipse() { Margin = new Thickness(176, 28 + i*26.5, 0, 0) };
                _pins[i + 13] = pin;
                cnvPins.Children.Add(pin);
            }
        }

        Port? _port;

        Dictionary<int, Ellipse> _pins = new();

        private async Task UpdateStatus()
        {
            try
            {
                await Task.Delay(50, _cancellationTokenSource.Token);
                UpdatePins();
                Dispatcher.BeginInvoke(async () => await UpdateStatus());
            }
            catch (OperationCanceledException)
            {
                Application.Current.Shutdown();
            }
        }

        private void UpdatePins()
        {
            if (_port == null)
                return;

            /*
            int pinValue = _port.ReadAll();
            for (int i = 0; i < 25; i++)
            {
                _pins[i].Visibility = ((1 << i) & pinValue) > 0 ? Visibility.Visible : Visibility.Hidden;
            }*/
            _pins[1].Visibility = _port.D0 ? Visibility.Visible : Visibility.Hidden;
            _pins[2].Visibility = _port.D1 ? Visibility.Visible : Visibility.Hidden;
            _pins[3].Visibility = _port.D2 ? Visibility.Visible : Visibility.Hidden;
            _pins[4].Visibility = _port.D3 ? Visibility.Visible : Visibility.Hidden;
            _pins[5].Visibility = _port.D4 ? Visibility.Visible : Visibility.Hidden;
            _pins[6].Visibility = _port.D5 ? Visibility.Visible : Visibility.Hidden;
            _pins[7].Visibility = _port.D6 ? Visibility.Visible : Visibility.Hidden;
            _pins[8].Visibility = _port.D7 ? Visibility.Visible : Visibility.Hidden;
            _pins[11].Visibility = _port.S3 ? Visibility.Visible : Visibility.Hidden;
            _pins[12].Visibility = _port.S4 ? Visibility.Visible : Visibility.Hidden;
            _pins[13].Visibility = _port.S5 ? Visibility.Visible : Visibility.Hidden;
            _pins[14].Visibility = _port.S6 ? Visibility.Visible : Visibility.Hidden;
            _pins[15].Visibility = _port.S7 ? Visibility.Visible : Visibility.Hidden;
            _pins[16].Visibility = _port.C0 ? Visibility.Visible : Visibility.Hidden;
            _pins[17].Visibility = _port.C1 ? Visibility.Visible : Visibility.Hidden;
            _pins[18].Visibility = _port.C2 ? Visibility.Visible : Visibility.Hidden;
            _pins[19].Visibility = _port.C3 ? Visibility.Visible : Visibility.Hidden;
        }

        private bool IsInpOutAvailable()
        {
            try
            {
                if (Port.IsAvailable() == 0)
                    throw new Exception("LPT port IO is not available.\nIf this is the first time the app was launched, then the issue may be resolved after restart.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await UpdateStatus();
        }

        CancellationTokenSource _cancellationTokenSource = new();

        private void cmbPorts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _port = cmbPorts.SelectedItem as Port;
        }
    }
}
