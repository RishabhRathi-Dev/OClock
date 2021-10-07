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


// Big issue :: Games from launchers are not in the list and they run by there own name :: can be solved by allowing user to enter name outside autocomplete

// Known Issues ::
// 1. Slow UI response when all 7 run (to be precise noticible change after 3)
// 2. Timer Accuracy; it is noticed that time jump 1 or 2 secs after some interval; cause can be slow ui response

namespace OClock
{
    public partial class MainWindow : Window
    {
        List<dynamic> MonitoredProcessList = new List<dynamic>(7) { "", "", "", "", "", "", ""}; // for keeping track of software(process) we are monitoring
        List<dynamic> DownloadedSoftwareList = new List<dynamic>(); // for keeping the list of downloaded software on the system

        List<dynamic> MonitorStatus = new List<dynamic>(7) { "", "", "", "", "", "", "" }; // for keeping track of which process monitoring has started
        List<DateTime> StartTime = new List<DateTime>(); // for keeping record of start time, for the calculation of delta time
        List<TimeSpan> DeltaTime = new List<TimeSpan>(); // for keeping record of time difference so that it can be displayed on the label
        List<dynamic> ProcessStatus = new List<dynamic>(7) { "", "", "", "", "", "", "" }; // for keeping record of the status of the monitored process using status like true(run), false(stop) and notify(notify and stop)

        List<bool> AddedStartTime = new List<bool>(7) { false, false, false, false, false, false, false}; // To condition so that right start time can be noted instead of initialized time

        public MainWindow()
        {
            /// <summary>
            /// This is the main window code, can call it as driver code block or main loop
            /// </summary>
            

            InitializeComponent(); // initializing componenets of software
            SoftwareList(); // for adding downloaded software in the DownloadedSoftwareList

            EnterDataInCombobox(DownloadedSoftwareList); // for entrying data from DownloadedSoftwareList to combobox for user to select software to monitor

            ListPrint(DownloadedSoftwareList); //for debugging purpose
            //ListPrint(MonitoredProcessList); //for debugging purpose

            ToSolveIndexIssues();
            CheckProcess(); // Work in Progress but it is suppose to check if the software is running currently

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

        private void ToSolveIndexIssues()
        {
            // This fuction is to fill the collection with temporary members so that the won't cause index issues later; more like it is to initialize the collection(list)

            for (int i = 0; i < 7; i++)
            {
                DeltaTime.Add(DateTime.Now.Subtract(DateTime.Now));
                StartTime.Add(DateTime.Now);
            }
        }

        private async void CheckProcess()
        {
            /// <summary>
            /// Work in Progress !!! This fuction is supposed to check wheather the oberserved programs are on taskbar(running)
            /// </summary>

            await Task.Delay(1); // Optimization is needed to match 7 application tracking at same time 

            for (int i = 0; i < 7; i++)
            {
                if (ProcessStatus[i] == "true")
                {

                    foreach (Process p in Process.GetProcesses())
                    {

                        // this if is for debugging purpose
                        if (p.MainWindowTitle.Length > 0)
                        {
                            Console.WriteLine(p.MainWindowTitle.ToString());
                        }

                        // Checking via searching name in string

                        string MainWinTitle = p.MainWindowTitle.ToString();
                        MainWinTitle = MainWinTitle.ToUpper();

                        string SearchString = MonitoredProcessList[i];
                        SearchString = SearchString.ToUpper();

                        bool DoesIt = MainWinTitle.Contains(SearchString);

                        // Console.WriteLine(MainWinTitle, SearchString, DoesIt);

                        if (DoesIt)
                        {
                            Console.WriteLine(MonitoredProcessList[i]); // For debugging Purpose

                            TimeCountStart(i);

                            Console.WriteLine(DeltaTime[0].ToString(@"hh\:mm\:ss")); // For debugging purpose 
                            
                        }
                        
                    }

                    TimeLabelUpdate(i);

                }

                else if (ProcessStatus[i] == "notify")
                {
                  
                }

                else
                {

                }
            }

            CheckProcess();
        }

        private void TimeLabelUpdate(int i)
        {
            switch (i)
            {
                // This switch handles the display changing of respective time labels 

                case 0:
                    Time.Content = DeltaTime[0].ToString(@"hh\:mm\:ss");
                    break;

                case 1:
                    Time_Copy.Content = DeltaTime[1].ToString(@"hh\:mm\:ss");
                    break;

                case 2:
                    Time_Copy1.Content = DeltaTime[2].ToString(@"hh\:mm\:ss");
                    break;

                case 3:
                    Time_Copy2.Content = DeltaTime[3].ToString(@"hh\:mm\:ss");
                    break;

                case 4:
                    Time_Copy3.Content = DeltaTime[4].ToString(@"hh\:mm\:ss");
                    break;

                case 5:
                    Time_Copy4.Content = DeltaTime[5].ToString(@"hh\:mm\:ss");
                    break;

                case 6:
                    Time_Copy5.Content = DeltaTime[6].ToString(@"hh\:mm\:ss");
                    break;
            }
        }

        private void TimeCountStart(int index)
        {
            /// <summary>
            /// This function add StartTime of given index at the same index on the list 
            /// </summary>

            //Console.WriteLine(StartTime[index].ToString());

            if (!AddedStartTime[index])
            {
                StartTime[index] = DateTime.Now;
                AddedStartTime[index] = true;
            }

            Timer(index);
        }

        private void Timer(int index)
        {
            /// <summary>
            /// This function returns Time Difference from given time (from given index on start list ) in DateTime format in TimeSpan format
            /// </summary>

            Console.WriteLine(index);

            DeltaTime[index] = DateTime.Now.Subtract(StartTime[index]);
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
                ProcessSelect_Copy2.Items.Add(i);
                ProcessSelect_Copy3.Items.Add(i);
                ProcessSelect_Copy4.Items.Add(i);
                ProcessSelect_Copy5.Items.Add(i);
            }
        }


        //  Beyond this Work in Progress .................................. 


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

        private void ObserverdProcess4Start(object sender, RoutedEventArgs e)
        {
            MonitoredProcessList[3] = ProcessSelect_Copy2.Text;
            ProcessStatus[3] = "true";
        }

        private void ObserverdProcess5Start(object sender, RoutedEventArgs e)
        {
            MonitoredProcessList[4] = ProcessSelect_Copy3.Text;
            ProcessStatus[4] = "true";
        }

        private void ObserverdProcess6Start(object sender, RoutedEventArgs e)
        {
            MonitoredProcessList[5] = ProcessSelect_Copy4.Text;
            ProcessStatus[5] = "true";
        }

        private void ObserverdProcess7Start(object sender, RoutedEventArgs e)
        {
            MonitoredProcessList[6] = ProcessSelect_Copy5.Text;
            ProcessStatus[6] = "true";
        }

        // Stop Button Controls

        // Start Time is getting preserved can solve that by falsing respective in the add start list 

        private void ObserverdProcess1Stop(object sender, RoutedEventArgs e)
        {
            ProcessStatus[0] = "false";
        }

        private void ObserverdProcess2Stop(object sender, RoutedEventArgs e)
        {
            ProcessStatus[1] = "false";
        }

        private void ObserverdProcess3Stop(object sender, RoutedEventArgs e)
        {
            ProcessStatus[2] = "false";
        }

        private void ObserverdProcess4Stop(object sender, RoutedEventArgs e)
        {
            ProcessStatus[3] = "false";
        }

        private void ObserverdProcess5Stop(object sender, RoutedEventArgs e)
        {
            ProcessStatus[4] = "false";
        }

        private void ObserverdProcess6Stop(object sender, RoutedEventArgs e)
        {
            ProcessStatus[5] = "false";
        }

        private void ObserverdProcess7Stop(object sender, RoutedEventArgs e)
        {
            ProcessStatus[6] = "false";
        }
    }
}
