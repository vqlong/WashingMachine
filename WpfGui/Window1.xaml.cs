using System;
using System.Collections.Generic;
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

namespace WpfGui
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        DispatcherTimer timerBlink = new DispatcherTimer();
        CancellationTokenSource tokenSource = new();
        public Window1()
        {
            InitializeComponent();
            timerBlink.Interval = TimeSpan.FromMilliseconds(100);
            timerBlink.Tick += TimerBlink_Tick;
        }

        private void TimerBlink_Tick(object? sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
