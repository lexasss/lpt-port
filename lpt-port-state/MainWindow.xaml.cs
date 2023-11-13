using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Port = LptPort.LptPort;

namespace LptPortState
{
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

            Dictionary<int, Action<Port>> switches = new()
            {
                { 1, port => port.D0 = !port.D0 },
                { 2, port => port.D1 = !port.D1 },
                { 3, port => port.D2 = ! port.D2 },
                { 4, port => port.D3 = ! port.D3 },
                { 5, port => port.D4 = ! port.D4 },
                { 6, port => port.D5 = ! port.D5 },
                { 7, port => port.D6 = ! port.D6 },
                { 8, port => port.D7 = ! port.D7 },
                { 0, port => port.C0 = !port.C0 },
                { 13, port => port.C1 = ! port.C1 },
                { 15, port => port.C2 = ! port.C2 },
                { 16, port => port.C3 = ! port.C3 },
            };

            for (int i = 0; i < 13; i++)
            {
                var pin = new Ellipse() { Margin = new Thickness(153, 13 + i*26.5, 0, 0) };
                var id = i;
                if (switches.ContainsKey(id))
                    pin.MouseDown += (s, e) => { if (_port != null) switches[id](_port); };
                else
                    pin.Fill = Brushes.Red;
                _pins[id] = pin;
                cnvPins.Children.Add(pin);
            }

            for (int i = 0; i < 4; i++)
            {
                var pin = new Ellipse() { Margin = new Thickness(175, 27 + i*26.5, 0, 0) };
                var id = i + 13;
                if (switches.ContainsKey(id))
                    pin.MouseDown += (s, e) => { if (_port != null) switches[id](_port); };
                else
                    pin.Fill = Brushes.Red;
                _pins[id] = pin;
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
                UpdateStatus();
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

            _pins[1].Opacity = _port.D0 ? 1 : 0.1;
            _pins[2].Opacity = _port.D1 ? 1 : 0.1;
            _pins[3].Opacity = _port.D2 ? 1 : 0.1;
            _pins[4].Opacity = _port.D3 ? 1 : 0.1;
            _pins[5].Opacity = _port.D4 ? 1 : 0.1;
            _pins[6].Opacity = _port.D5 ? 1 : 0.1;
            _pins[7].Opacity = _port.D6 ? 1 : 0.1;
            _pins[8].Opacity = _port.D7 ? 1 : 0.1;
            _pins[14].Opacity = _port.S3 ? 1 : 0.1;
            _pins[12].Opacity = _port.S4 ? 1 : 0.1;
            _pins[11].Opacity = _port.S5 ? 1 : 0.1;
            _pins[9].Opacity = _port.S6 ? 1 : 0.1;
            _pins[10].Opacity = _port.S7 ? 1 : 0.1;
            _pins[0].Opacity = _port.C0 ? 1 : 0.1;
            _pins[13].Opacity = _port.C1 ? 1 : 0.1;
            _pins[15].Opacity = _port.C2 ? 1 : 0.1;
            _pins[16].Opacity = _port.C3 ? 1 : 0.1;
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
