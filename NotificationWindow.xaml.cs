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
using System.Windows.Shapes;

namespace OClock
{
    /// <summary>
    /// Interaction logic for NotificationWindow.xaml
    /// </summary>
    public partial class NotificationWindow : Window
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        public NotificationWindow()
        {
            InitializeComponent();
            PositionWindow();

            stopwatch.Start();

            CheckClose();
        }

        private async void CheckClose()
        {
            await Task.Delay(1000);

            if (stopwatch.Elapsed.TotalSeconds > 5)
            {
                this.Close();
            }

            CheckClose();
        }

        private void PositionWindow()
        {
            double width = NeededWidth(Convert.ToInt32(NotifyWindow.Width));
            double height = NeededHeight(Convert.ToInt32(NotifyWindow.Height));

            NotifyWindow.Left = width;
            NotifyWindow.Top = height;
        }

        private static double NeededWidth(int x)
        {
            return SystemParameters.FullPrimaryScreenWidth - x;
        }

        private static double NeededHeight(int x)
        {
            return SystemParameters.FullPrimaryScreenHeight - x;
        }
    }
}
