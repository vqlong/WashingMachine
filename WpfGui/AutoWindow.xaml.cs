using HslCommunication.Profinet.Melsec;
using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using XLib.UserControls;

namespace WpfGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class AutoWindow : Window
    {
        DispatcherTimer timerBlink = new DispatcherTimer();
        MelsecFxSerial plc = new MelsecFxSerial();
        ManualWindow manual = new ManualWindow();
        internal bool IsRunning { get; set; } = false;
        internal bool IsError { get; set; } = false;
        internal bool IsOrigin { get; set; } = true;
        internal bool IsManualWindow { get; set; } = false;

        public AutoWindow()
        {
            InitializeComponent();

            manual.Auto = this;
            manual.Plc = plc;

            timerBlink.Interval = TimeSpan.FromMilliseconds(100);
            timerBlink.Tick += TimerBlink_Tick;
        }

        private void TimerBlink_Tick(object? sender, EventArgs e)
        {
            if (IsRunning)
            {
                if (lbAuto.Background == Brushes.Lime) lbAuto.Background = Brushes.Orange;
                else lbAuto.Background = Brushes.Lime;
            }
            else if (IsError)
            {
                if (lbAuto.Background == Brushes.Lime) lbAuto.Background = Brushes.Crimson;
                else lbAuto.Background = Brushes.Lime;
            }
            else
            {
                lbAuto.Background = Brushes.Lime;
            }

            if (IsOrigin)
            {
                btnOrigin.Background = Brushes.Orange;
            }
            else
            {
                if (btnOrigin.Background == Brushes.Orange) btnOrigin.Background = Brushes.Gray;
                else btnOrigin.Background = Brushes.Orange;
            }

            if (IsManualWindow)
            {
                Hide();
                manual.IsShowing = true;
                manual.Show();
            }
            else
            {
                manual.IsShowing = false;
                manual.Hide();
                Show();
            }
        }

        private void main_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            plc.Close();
            timerBlink.Stop();
            manual.ShouldClose = true;
            manual.Close();
        }

        private void main_Loaded(object sender, RoutedEventArgs e)
        {
            plc.SerialPortInni(sp =>
            {
                sp.PortName = "COM3";
                sp.BaudRate = 9600;
                sp.DataBits = 7;
                sp.StopBits = StopBits.One;
                sp.Parity = Parity.Even;
            });

            var result = plc.Open();

            if (result.IsSuccess)
            {
                StartCommunication();
                timerBlink.Start();

                DialogBox.Show($"Connect to PLC successfully!", DialogBoxIcon.Information, DialogBoxButton.OK);
            }
            else
            {
                DialogBox.Show($"Error {result.ErrorCode}: {result.Message}");
            }
        }

        private void StartCommunication()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(100);

                    Dispatcher.Invoke(() => lbDateTime.Content = DateTime.Now.ToString("G"));

                    if (plc.ReadBool("M100").Content) IsRunning = true;
                    else IsRunning = false;

                    if (plc.ReadBool("M72").Content) IsError = true;
                    else IsError = false;

                    if (plc.ReadBool("M73").Content) IsOrigin = false;
                    else IsOrigin = true;

                    if (plc.ReadInt16("D101").Content == 2) IsManualWindow = true;
                    else IsManualWindow = false;


                    if (plc.ReadBool("M10").Content) LabelLightOn(lbVacuum);
                    else LabelLightOff(lbVacuum);

                    if (plc.ReadBool("M20").Content) LabelLightOn(lbCover);
                    else LabelLightOff(lbCover);

                    if (plc.ReadBool("M30").Content) LabelLightOn(lbChuck);
                    else LabelLightOff(lbChuck);

                    if (plc.ReadBool("M40").Content) LabelLightOn(lbMotor);
                    else LabelLightOff(lbMotor);

                    if (plc.ReadBool("M50").Content) LabelLightOn(lbWater);
                    else LabelLightOff(lbWater);

                    if (plc.ReadBool("M60").Content) LabelLightOn(lbDry);
                    else LabelLightOff(lbDry);

                    var D18 = (plc.ReadInt16("D18").Content * 0.1).ToString("N1");
                    var D2 = plc.ReadFloat("D2").Content.ToString("N1");
                    var D58 = (plc.ReadInt16("D58").Content * 0.1).ToString("N1");
                    var D68 = (plc.ReadInt16("D68").Content * 0.1).ToString("N1");
                    var D48 = (plc.ReadInt16("D48").Content * 0.1).ToString("N1");
                    var D100 = (plc.ReadInt16("D100").Content * 0.1).ToString("N1");

                    var D10 = plc.ReadInt16("D10").Content;
                    var D14 = plc.ReadFloat("D14").Content.ToString("N1");
                    var D50 = plc.ReadInt16("D50").Content;
                    var D60 = plc.ReadInt16("D60").Content;
                    var D40 = plc.ReadInt16("D40").Content;
                    var D44 = plc.ReadInt16("D44").Content;

                    Dispatcher.Invoke(() =>
                    {
                        lbVacRunPV.Content = D18;
                        lbVacMetterPV.Content = D2;
                        lbCleanTimePV.Content = D58;
                        lbDryTimePV.Content = D68;
                        lbMotorStopTimePV.Content = D48;
                        lbCycleTime.Content = D100;

                        lbVacRunSV.Content = D10;
                        lbVacMetterSV.Content = D14;
                        lbCleanTimeSV.Content = D50;
                        lbDryTimeSV.Content = D60;
                        lbMotorStopTimeSV.Content = D40;
                        lbMotorSpeed.Content = D44;
                    });

                }
            });
        }

        private void LabelLightOn(Label label) => Dispatcher.Invoke(() => { if (label.Background == Brushes.Gray) label.Background = Brushes.Lime; });
        private void LabelLightOff(Label label) => Dispatcher.Invoke(() => { if (label.Background == Brushes.Lime) label.Background = Brushes.Gray; });

        private void PressBit(string address)
        {
            plc.Write(address, true);
            plc.Write(address, false);
        }
        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            PressBit("M0");
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            PressBit("M1");
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            PressBit("M5");
        }

        private void btnOrigin_Click(object sender, RoutedEventArgs e)
        {
            PressBit("M4");
        }

        private void lbMode_MouseDown(object sender, MouseButtonEventArgs e)
        {
            PressBit("M2");
        }
    }
}
