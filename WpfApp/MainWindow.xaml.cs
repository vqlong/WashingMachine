using ActUtlTypeLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Windows.Threading;

namespace WpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        const string MsgCaption = "Wafer Washing Machine";
        DispatcherTimer timerStatus = new DispatcherTimer();
        DispatcherTimer timerBlink = new DispatcherTimer();
        ActUtlType plc = new ActUtlType();
        bool isRunning = false;
        bool isError = false;
        bool isOrigin = false;
        public MainWindow()
        {
            InitializeComponent();

            timerStatus.Interval = TimeSpan.FromMilliseconds(50);
            timerStatus.Tick += Timer_Tick;

            timerBlink.Interval = TimeSpan.FromMilliseconds(50);
            timerBlink.Tick += TimerBlink_Tick;
        }

        private void TimerBlink_Tick(object sender, EventArgs e)
        {
            if (isRunning)
            {
                if (lbAuto.Background == Brushes.Lime) lbAuto.Background = Brushes.Orange;
                else lbAuto.Background = Brushes.Lime;
            }
            else if(isError)
            {
                if (lbAuto.Background == Brushes.Lime) lbAuto.Background = Brushes.Crimson;
                else lbAuto.Background = Brushes.Lime;
            }
            else
            {
                lbAuto.Background = Brushes.Lime;
            }

            if (isOrigin)
            {
                btnOrigin.Background = Brushes.Orange;
            }
            else
            {
                if (btnOrigin.Background == Brushes.Orange) btnOrigin.Background = Brushes.LimeGreen;
                else btnOrigin.Background = Brushes.Orange;
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            lbErrorStatus.Content = DateTime.Now.ToString("G");

            plc.GetDevice("M100", out int M100);
            if (M100 == 1) isRunning = true;
            else isRunning = false;

            plc.GetDevice("M72", out int M72);
            if (M72 == 1) isError = true;
            else isError = false;

            plc.GetDevice("M73", out int M73);
            if (M73 == 1) isOrigin = false;
            else isOrigin = true;

            plc.GetDevice("M10", out int M10);
            if (M10 == 1) lbVacuum.Background = Brushes.Lime;
            else lbVacuum.Background = Brushes.Gray;

            plc.GetDevice("M20", out int M20);
            if (M20 == 1) lbCover.Background = Brushes.Lime;
            else lbCover.Background = Brushes.Gray;

            plc.GetDevice("M30", out int M30);
            if (M30 == 1) lbChuck.Background = Brushes.Lime;
            else lbChuck.Background = Brushes.Gray;

            plc.GetDevice("M40", out int M40);
            if (M40 == 1) lbMotor.Background = Brushes.Lime;
            else lbMotor.Background = Brushes.Gray;

            plc.GetDevice("M50", out int M50);
            if (M50 == 1) lbWater.Background = Brushes.Lime;
            else lbWater.Background = Brushes.Gray;

            plc.GetDevice("M60", out int M60);
            if (M60 == 1) lbDry.Background = Brushes.Lime;
            else lbDry.Background = Brushes.Gray;

            plc.GetDevice("D10", out int D10);
            lbVacRunSV.Content = D10;
            plc.ReadDeviceBlock("D18", 2, out int D18);
            lbVacRunPV.Content = (D18 * 0.1).ToString("##.#");

            plc.ReadDeviceBlock("D14", 4, out int D14);
            lbVacMetterSV.Content = D14;
            plc.GetDevice("D2", out int D2);
            lbVacMetterPV.Content = D2;

            plc.GetDevice("D50", out int D50);
            lbCleanTimeSV.Content = D50;
            plc.GetDevice("D58", out int D58);
            lbCleanTimePV.Content = (D58 * 0.1).ToString("##.#");

            plc.GetDevice("D60", out int D60);
            lbDryTimeSV.Content = D60;
            plc.GetDevice("D68", out int D68);
            lbDryTimePV.Content = (D68 * 0.1).ToString("##.#");

            plc.GetDevice("D40", out int D40);
            lbMotorStopTimeSV.Content = D40;
            plc.GetDevice("D48", out int D48);
            lbMotorStopTimePV.Content = (D48 * 0.1).ToString("##.#");

            plc.GetDevice("D100", out int D100);
            lbCycleTime.Content = (D100*0.1).ToString("##.#");

            plc.GetDevice("D44", out int D44);
            lbMotorSpeed.Content = D44;

        }

        private void main_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            plc.Close();
        }

        private void main_Loaded(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Do you want to connect to Gx Simulator2?", Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                plc.ActLogicalStationNumber = 1;
            }
            else
            {
                plc.ActLogicalStationNumber = 2;
            }
            
            
            short result = (short)plc.Open();

            if (result == 0)
            {
                MessageBox.Show($"Connect to PLC successfully!", Title, MessageBoxButton.OK, MessageBoxImage.Information);
                timerStatus.Start();
                timerBlink.Start();

            }
            else
            {
                MessageBox.Show($"Connect result: {result}", Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void PressBit(string address)
        {
            plc.SetDevice(address, 1);
            plc.SetDevice(address, 0);
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
