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
using System.Diagnostics;
using System.Security.Permissions;
using Microsoft.Win32;
using System.IO;
using System.Management;
using System.Management.Instrumentation;
using System.Threading;
using Hardcodet;
using WPFCustomMessageBox;


// Big issue :: Games from launchers are not in the list and they run by there own name :: can be solved by allowing user to enter name outside autocomplete

// Known Issues ::


namespace OClock
{
    public partial class MainWindow : Window
    {
        List<dynamic> MonitoredProcessList = new List<dynamic>(3) { "", "", "" }; // for keeping track of software(process) we are monitoring
        List<dynamic> DownloadedSoftwareList = new List<dynamic>(); // for keeping the list of downloaded software on the system
        List<dynamic> ProcessStatus = new List<dynamic>(3) { "", "", "" }; // for keeping record of the status of the monitored process using status like true(run), false(stop) and notify(notify and stop)

        // Stopwatches for the timers
        Stopwatch FirstMonitoredProcessWatch = new Stopwatch();
        Stopwatch SecondMonitoredProcessWatch = new Stopwatch();
        Stopwatch ThirdMonitoredProcessWatch = new Stopwatch();

        // Hardcodet.Wpf.TaskbarNotification.TaskbarIcon taskbarIcon = new Hardcodet.Wpf.TaskbarNotification.TaskbarIcon();

        public MainWindow()
        {
            /// <summary>
            /// This is the main window code, can call it as driver code block or main loop
            /// </summary>
            

            InitializeComponent(); // initializing componenets of software
            SoftwareList(); // for adding downloaded software in the DownloadedSoftwareList

            EnterDataInCombobox(DownloadedSoftwareList); // for entrying data from DownloadedSoftwareList to combobox for user to select software to monitor

            //ListPrint(DownloadedSoftwareList); //for debugging purpose
            //ListPrint(MonitoredProcessList); //for debugging purpose
            CheckProcess();
        }

        private void SoftwareList()
        {
            /// <summary>
            /// This function works to extract list of downloaded software by getting names of all the uninstallable programs and add it in the DownloadedSoftwareList.
            /// To learn more about this method :
            /// https://en.wikipedia.org/wiki/Windows_Registry
            /// https://social.msdn.microsoft.com/Forums/en-US/94c2f14d-c45e-4b55-9ba0-eb091bac1035/c-get-installed-programs?forum=csharplanguage
            /// </summary>

            var ProcessL = new List<dynamic>();

            string displayName;
            RegistryKey key;

            // search in: CurrentUser
            key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            foreach (String keyName in key.GetSubKeyNames())
            {
                RegistryKey subkey = key.OpenSubKey(keyName);
                displayName = subkey.GetValue("DisplayName") as string;
                if (displayName != null)
                {
                    ProcessL.Add(displayName);
                }

            }

            // Search in: 32 bit version
            key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            foreach (String keyName in key.GetSubKeyNames())
            {
                RegistryKey subkey = key.OpenSubKey(keyName);
                displayName = subkey.GetValue("DisplayName") as string;
                if (displayName != null)
                {
                    ProcessL.Add(displayName);
                }

            }

            // Search in: 64 bit version
            key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
            foreach (String keyName in key.GetSubKeyNames())
            {
                RegistryKey subkey = key.OpenSubKey(keyName);
                displayName = subkey.GetValue("DisplayName") as string;

                if (displayName != null)
                {
                    ProcessL.Add(displayName);
                }

            }

            DownloadedSoftwareList = ProcessL;

        }

        private void ListPrint(List<dynamic> l)
        {
            /// <summary>
            /// This function is purely for debugging purpose only, its function is to print list
            /// </summary>
            
            foreach(string i in l)
            {
                Console.WriteLine(i);
            }
        }

        private async void CheckProcess()
        {
            /// <summary>
            /// Work in Progress !!! This fuction is supposed to check wheather the oberserved programs are on taskbar(running)
            /// </summary>

            await Task.Delay(400);

            for (int i = 0; i < 3; i++)
            {
                CheckNotify(i);
                //ListPrint(ProcessStatus); // For debugging purpose

                if (ProcessStatus[i] == "true")
                {
                    Dictionary<int, bool> Cumulative = new Dictionary<int, bool>(); // Dictionary is used for direct searching O(1) when you know what you want

                    int justforsakeofkey = 0;

                    foreach (Process p in Process.GetProcesses())
                    {
                        // this if make sure timer does not start without taking input
                        if (MonitoredProcessList[i].Length <= 1)
                        {
                            break;
                        }

                        if (p.MainWindowTitle.ToString().Length <= 1)
                        {
                            // This reduces the load for the further checking as any of 1 or 0 length is not a process worth checking
                            continue;
                        }
                        

                        // Checking via searching name in string

                        string MainWinTitle = p.MainWindowTitle.ToString();
                        MainWinTitle = MainWinTitle.ToUpper();

                        string SearchString = MonitoredProcessList[i];
                        SearchString = SearchString.ToUpper();

                        bool DoesIt = MainWinTitle.Contains(SearchString);
                        Cumulative.Add(justforsakeofkey, DoesIt);

                        justforsakeofkey++;

                        //Console.WriteLine(MainWinTitle, SearchString, DoesIt);
                    }

                    if (Cumulative.ContainsValue(true))
                    {
                        // Console.WriteLine("True runs"); // For debugging purpose
                        switch (i)
                        {
                            case 0:
                                FirstMonitoredProcessWatch.Start();
                                break;

                            case 1:
                                SecondMonitoredProcessWatch.Start();
                                break;

                            case 2:
                                ThirdMonitoredProcessWatch.Start();
                                break;
                        }
                    }
                    else
                    {
                        // Console.WriteLine("False runs"); // For debugging purpose
                        switch (i)
                        {
                            case 0:
                                FirstMonitoredProcessWatch.Stop();
                                break;

                            case 1:
                                SecondMonitoredProcessWatch.Stop();
                                break;

                            case 2:
                                ThirdMonitoredProcessWatch.Stop();
                                break;
                        }
                    }

                    Cumulative.Clear();

                }

                else if (ProcessStatus[i] == "notify")
                {
                    ProcessStatus[i] = "false";
                    Notify(i);
                }

                else
                {

                }

                TimeLabelUpdate();
                
            }

            CheckProcess();
        }

        private void TimeLabelUpdate()
        {
            /// <summary>
            /// This function updates the visual display of time program ran on the respective label
            /// </summary>
            
            Time.Content = FirstMonitoredProcessWatch.Elapsed.ToString(@"hh\:mm\:ss");
            Time_Copy.Content = SecondMonitoredProcessWatch.Elapsed.ToString(@"hh\:mm\:ss");
            Time_Copy1.Content = ThirdMonitoredProcessWatch.Elapsed.ToString(@"hh\:mm\:ss");

        }

        private bool ConditionForNotifying(bool A, bool B, bool C, bool D, bool E)
        {
            ///<summary>
            ///This fuction process the boolean to match the condition when timer should notify
            ///It is true when :
            ///A(h > HH) or B(h == HH) & C(m > MM) or B(h == HH) & D(m == MM) & E(s > SS)
            /// </summary>

            bool result = (E & D & B | C & B | A );
            //Console.WriteLine(result);
            return result;
        }

        private void CheckNotify(int index)
        {
            ///<summary>
            ///This function Gets the necessary comparitions and send it to conditionfornotifying to process and also change the process status
            /// </summary>
            if (ProcessStatus[index] == "true")
            {
                switch (index)
                {
                    case 0:
                        int HH1Number;
                        int MM1Number;
                        int SS1Number;
                        bool isHH1 = Int32.TryParse(HH1.Text, out HH1Number);
                        bool isMM1 = Int32.TryParse(MM1.Text, out MM1Number);
                        bool isSS1 = Int32.TryParse(SS1.Text, out SS1Number);

                        if (!(isHH1 || isMM1 || isSS1))
                        {
                            break;
                        }

                        bool a = HH1Number < FirstMonitoredProcessWatch.Elapsed.Hours;
                        bool b = HH1Number == FirstMonitoredProcessWatch.Elapsed.Hours;
                        bool c = MM1Number < FirstMonitoredProcessWatch.Elapsed.Minutes;
                        bool d = MM1Number == FirstMonitoredProcessWatch.Elapsed.Minutes;
                        bool e = SS1Number <= FirstMonitoredProcessWatch.Elapsed.Seconds;

                        //Console.WriteLine((a, b, c, d, e));

                        if (ConditionForNotifying(a, b, c, d, e))
                        {
                            //Console.WriteLine("Did Notify!!!!");
                            ProcessStatus[0] = "notify";
                        }
                        break;

                    case 1:
                        int HH2Number;
                        int MM2Number;
                        int SS2Number;
                        bool isHH2 = Int32.TryParse(HH2.Text, out HH2Number);
                        bool isMM2 = Int32.TryParse(MM2.Text, out MM2Number);
                        bool isSS2 = Int32.TryParse(SS2.Text, out SS2Number);

                        if (!(isHH2 || isMM2 || isSS2))
                        {
                            break;
                        }

                        bool aa = HH2Number < SecondMonitoredProcessWatch.Elapsed.Hours;
                        bool bb = HH2Number == SecondMonitoredProcessWatch.Elapsed.Hours;
                        bool cc = MM2Number < SecondMonitoredProcessWatch.Elapsed.Minutes;
                        bool dd = MM2Number == SecondMonitoredProcessWatch.Elapsed.Minutes;
                        bool ee = SS2Number <= SecondMonitoredProcessWatch.Elapsed.Seconds;

                        //Console.WriteLine((a, b, c, d, e));

                        if (ConditionForNotifying(aa, bb, cc, dd, ee))
                        {
                            //Console.WriteLine("Did Notify!!!!");
                            ProcessStatus[1] = "notify";
                        }
                        break;

                    case 2:
                        int HH3Number;
                        int MM3Number;
                        int SS3Number;
                        bool isHH3 = Int32.TryParse(HH3.Text, out HH3Number);
                        bool isMM3 = Int32.TryParse(MM3.Text, out MM3Number);
                        bool isSS3 = Int32.TryParse(SS3.Text, out SS3Number);

                        if (!(isHH3 || isMM3 || isSS3))
                        {
                            break;
                        }

                        bool aaa = HH3Number < ThirdMonitoredProcessWatch.Elapsed.Hours;
                        bool bbb = HH3Number == ThirdMonitoredProcessWatch.Elapsed.Hours;
                        bool ccc = MM3Number < ThirdMonitoredProcessWatch.Elapsed.Minutes;
                        bool ddd = MM3Number == ThirdMonitoredProcessWatch.Elapsed.Minutes;
                        bool eee = SS3Number <= ThirdMonitoredProcessWatch.Elapsed.Seconds;

                        //Console.WriteLine((a, b, c, d, e));

                        if (ConditionForNotifying(aaa, bbb, ccc, ddd, eee))
                        {
                            //Console.WriteLine("Did Notify!!!!");
                            ProcessStatus[2] = "notify";
                        }
                        break;

                }
            }
        }


        private void EnterDataInCombobox(List<dynamic> l)
        {
            /// <summary>
            /// This function enters the data from inputed list into the combobox for user to select from. 
            /// </summary>

            foreach (string i in l)
            {
                ProcessSelect.Items.Add(i);
                ProcessSelect_Copy.Items.Add(i);
                ProcessSelect_Copy1.Items.Add(i);
               
            }
        }

        private void CallNotificationWindow(int index)
        {
            /// <summary>
            /// This fuction creates the notification window and kills it after 5 seconds
            /// </summary>

            NotificationWindow win = new NotificationWindow();
            win.ProgramNameLabel.Content = MonitoredProcessList[index];

            // Below is to make it come on top like a notification should
            win.Show();
            win.Activate();
            win.Topmost = true;
            win.Topmost = false;
            win.Focus();
        }


        private void Notify(int index)
        {
            /// <summary>
            /// This function create notification and stops the timer after that 
            /// </summary>
            
            switch (index)
            {
                case 0:
                    FirstMonitoredProcessWatch.Stop();
                    CallNotificationWindow(0);
                    ProcessStatus[0] = "false";
                    break;

                case 1:
                    SecondMonitoredProcessWatch.Stop();
                    CallNotificationWindow(1);
                    ProcessStatus[1] = "false";
                    break;

                case 2:
                    ThirdMonitoredProcessWatch.Stop();
                    CallNotificationWindow(2);
                    ProcessStatus[2] = "false";
                    break;

            }
        }


        // Controls-----------------------------

        // Start Button Controls 

        private void ObserverdProcess1Start(object sender, RoutedEventArgs e)
        {
            MonitoredProcessList[0] = ProcessSelect.Text;
            ProcessStatus[0] = "true";
        }

        private void ObserverdProcess2Start(object sender, RoutedEventArgs e)
        {
            MonitoredProcessList[1] = ProcessSelect_Copy.Text;
            ProcessStatus[1] = "true";
        }

        private void ObserverdProcess3Start(object sender, RoutedEventArgs e)
        {
            MonitoredProcessList[2] = ProcessSelect_Copy1.Text;
            ProcessStatus[2] = "true";
        }

        
        // Stop Button Controls 

        private void ObserverdProcess1Stop(object sender, RoutedEventArgs e)
        {
            FirstMonitoredProcessWatch.Stop();
            ProcessStatus[0] = "false";
        }

        private void ObserverdProcess2Stop(object sender, RoutedEventArgs e)
        {
            SecondMonitoredProcessWatch.Stop();
            ProcessStatus[1] = "false";
        }

        private void ObserverdProcess3Stop(object sender, RoutedEventArgs e)
        {
            ThirdMonitoredProcessWatch.Stop();
            ProcessStatus[2] = "false";
        }

        // Settings and Categories Controls

        private void SettingsButtonClicked(object sender, RoutedEventArgs e)
        {
            CategoriesGrid.Visibility = Visibility.Hidden;
            SettingsGrid.Visibility = Visibility.Visible;
        }

        private void ReturnButtonFromSettingsClicked(object sender, RoutedEventArgs e)
        {
            SettingsGrid.Visibility = Visibility.Hidden;
        }

        private void ReturnButtonFromCategoriesClicked(object sender, RoutedEventArgs e)
        {
            CategoriesGrid.Visibility = Visibility.Hidden;
        }

        private void CategoriesButtonClicked(object sender, RoutedEventArgs e)
        {
            SettingsGrid.Visibility = Visibility.Hidden;
            CategoriesGrid.Visibility = Visibility.Visible;
        }

        private void CheckedToRunOnStartUp(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            key.SetValue("OClock", System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        private void RunOnStartupUnchecked(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            key.DeleteValue("OClock", false);
        }

        private void TrayIconGotDoubleClicked(object sender, RoutedEventArgs e)
        {
            MainWindowDesign.WindowState = WindowState.Normal;
            MainWindowDesign.ShowInTaskbar = true;
            
            MainWindowDesign.Topmost = true;
            MainWindowDesign.Topmost = false;
            MainWindowDesign.Focus();
            //Console.WriteLine("Double Click Registered!!!!");
        }

        private void AttemptToClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ///<summary>
            ///This function is to prevent direct closure of application, it gives an option to minimize to tray
            /// </summary>

            var MessageBoxResult = CustomMessageBox.ShowYesNoCancel("Are you sure you want to close the window?",
         "OClock", "Yes", "Minimize to System tray", "Cancel");
            
            if (MessageBoxResult != MessageBoxResult.Yes && MessageBoxResult != MessageBoxResult.No)
            {
                e.Cancel = true;
            }

            else if (MessageBoxResult == MessageBoxResult.No)
            {
                MainWindowDesign.WindowState = WindowState.Minimized;
                MainWindowDesign.ShowInTaskbar = false;
                e.Cancel = true;

                // Console.WriteLine("Worked!!!"); //For Debugging Purpose
            }

        }

    }
}
