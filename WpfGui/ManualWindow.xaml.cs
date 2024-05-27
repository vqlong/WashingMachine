using HslCommunication.Profinet.Melsec;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
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
using System.Windows.Shapes;
using System.Windows.Threading;
using XLib.UserControls;

namespace WpfGui
{
    /// <summary>
    /// Interaction logic for ManualWindow.xaml
    /// </summary>
    public partial class ManualWindow : Window
    {
        DispatcherTimer timerBlink = new DispatcherTimer();
        CancellationTokenSource tokenSource = new();
        internal MelsecFxSerial Plc { get; set; }
        internal AutoWindow Auto { get; set; }
        internal bool ShouldClose { get; set; }
        internal bool IsShowing { get; set; }
        public ManualWindow()
        {
            InitializeComponent();
            timerBlink.Interval = TimeSpan.FromMilliseconds(100);
            timerBlink.Tick += TimerBlink_Tick;
        }

        private void TimerBlink_Tick(object? sender, EventArgs e)
        {
            if (Auto.IsRunning)
            {
                if (lbManual.Background == Brushes.Yellow) lbManual.Background = Brushes.Lime;
                else lbManual.Background = Brushes.Yellow;
            }
            else if (Auto.IsError)
            {
                if (lbManual.Background == Brushes.Yellow) lbManual.Background = Brushes.Crimson;
                else lbManual.Background = Brushes.Yellow;
            }
            else
            {
                lbManual.Background = Brushes.Yellow;
            }

            if (Auto.IsOrigin)
            {
                btnOrigin.Background = Brushes.Orange;
            }
            else
            {
                if (btnOrigin.Background == Brushes.Orange) btnOrigin.Background = Brushes.Gray;
                else btnOrigin.Background = Brushes.Orange;
            }
        }

        private void StartCommunication()
        {
            Task.Run(() =>
            {
                while (!tokenSource.Token.IsCancellationRequested)
                {
                    Thread.Sleep(100);
                    Dispatcher.Invoke(() => lbDateTime.Content = DateTime.Now.ToString("G"));

                    if (Plc.ReadBool("M10").Content) LabelLightOn(lbVacuum);
                    else LabelLightOff(lbVacuum);

                    if (Plc.ReadBool("M20").Content) LabelLightOn(lbCover);
                    else LabelLightOff(lbCover);

                    if (Plc.ReadBool("M30").Content) LabelLightOn(lbChuck);
                    else LabelLightOff(lbChuck);

                    if (Plc.ReadBool("M40").Content) LabelLightOn(lbMotor);
                    else LabelLightOff(lbMotor);

                    if (Plc.ReadBool("M50").Content) LabelLightOn(lbWater);
                    else LabelLightOff(lbWater);

                    if (Plc.ReadBool("M60").Content) LabelLightOn(lbDry);
                    else LabelLightOff(lbDry);
                }
            }, tokenSource.Token);
        }

        private void LabelLightOn(Label label) => Dispatcher.Invoke(() => { if (label.Background == Brushes.Gray) label.Background = Brushes.Lime; });
        private void LabelLightOff(Label label) => Dispatcher.Invoke(() => { if (label.Background == Brushes.Lime) label.Background = Brushes.Gray; });

        private void PressBit(string address)
        {
            Plc.Write(address, true);
            Plc.Write(address, false);
        }
        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            DialogBox.Show("Can not starting the machine in manual mode.");
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            DialogBox.Show("Can not stopping the machine in manual mode.");
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
            PressBit("M3");
        }

        private void nmVacTime_ValueChanged(object sender, ValueChangedEventAgrs e)
        {
            Plc.Write("D10", (short)nmVacTime.Value);
        }

        private void nmVacLimit_ValueChanged(object sender, ValueChangedEventAgrs e)
        {
            Plc.Write("D14", (float)nmVacLimit.Value);
        }

        private void nmMotorSpeed_ValueChanged(object sender, ValueChangedEventAgrs e)
        {
            Plc.Write("D44", (short)nmMotorSpeed.Value);
        }

        private void nmMotorStopTime_ValueChanged(object sender, ValueChangedEventAgrs e)
        {
            Plc.Write("D40", (short)nmMotorStopTime.Value);
        }

        private void nmCleanTime_ValueChanged(object sender, ValueChangedEventAgrs e)
        {
            Plc.Write("D50", (short)nmCleanTime.Value);
        }

        private void nmDryTime_ValueChanged(object sender, ValueChangedEventAgrs e)
        {
            Plc.Write("D60", (short)nmDryTime.Value);
        }

        private void main_Loaded(object sender, RoutedEventArgs e)
        {
            StartCommunication();

            // đọc dữ liệu ban đầu từ plc
            Task.Run(() =>
            {
                var D10 = Plc.ReadInt16("D10").Content;
                var D14 = Plc.ReadFloat("D14").Content;
                var D44 = Plc.ReadInt16("D44").Content;
                var D40 = Plc.ReadInt16("D40").Content;
                var D50 = Plc.ReadInt16("D50").Content;
                var D60 = Plc.ReadInt16("D60").Content;

                Dispatcher.Invoke(() =>
                {
                    nmVacTime.Value = D10;
                    nmVacLimit.Value = D14;
                    nmMotorSpeed.Value = D44;
                    nmMotorStopTime.Value = D40;
                    nmCleanTime.Value = D50;
                    nmDryTime.Value = D60;
                });
            });

            timerBlink.Start();
        }

        private void main_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            if (ShouldClose) 
            {
                tokenSource.Cancel();
                timerBlink.Stop();
            }
            else
            {
                e.Cancel = true;
                PressBit("M3");
                IsShowing = false;
            }
            
        }

        private void btnVacuum_Click(object sender, RoutedEventArgs e)
        {
            PressBit("M11");
        }

        private void btnCover_Click(object sender, RoutedEventArgs e)
        {
            PressBit("M21");
        }

        private void btnChuck_Click(object sender, RoutedEventArgs e)
        {
            PressBit("M31");
        }

        private void btnMotor_Click(object sender, RoutedEventArgs e)
        {
            PressBit("M41");
        }

        private void btnWater_Click(object sender, RoutedEventArgs e)
        {
            PressBit("M51");
        }

        private void btnDry_Click(object sender, RoutedEventArgs e)
        {
            PressBit("M61");
        }
    }
}
