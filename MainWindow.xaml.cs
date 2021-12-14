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
using System.Data.SQLite;
using LiveCharts;
using LiveCharts.Wpf;




// Known Issues ::
// arranging events according to date in loading



namespace OClock
{
    public partial class MainWindow : Window
    {
        List<dynamic> MonitoredProcessList = new List<dynamic>(3) { "", "", "" }; // for keeping track of software(process) we are monitoring
        List<dynamic> DownloadedSoftwareList = new List<dynamic>(); // for keeping the list of downloaded software on the system
        List<dynamic> ProcessStatus = new List<dynamic>(3) { "", "", "" }; // for keeping record of the status of the monitored process using status like true(run), false(stop) and notify(notify and stop)
        List<string> ToDoList = new List<string>();
        Dictionary<int, (string, string)> EventsList = new Dictionary<int, (string, string)>(); // 
        Dictionary<string, string> CategoriesTempSave = new Dictionary<string, string>();

        // Stopwatches for the timers
        Stopwatch FirstMonitoredProcessWatch = new Stopwatch();
        Stopwatch SecondMonitoredProcessWatch = new Stopwatch();
        Stopwatch ThirdMonitoredProcessWatch = new Stopwatch();
        Stopwatch BackGroundStopwatch = new Stopwatch();
        Stopwatch DataCollectionStopwatch = new Stopwatch();
        Stopwatch TotalTimeStopwatch = new Stopwatch();

        // Threads (Load threads may not work as they will be interecting with UI thread and that has caused problems before)
        Thread SaveTimeThread;
        Thread SaveToDoListThread;
        Thread SaveEventsThread;

        // Hardcodet.Wpf.TaskbarNotification.TaskbarIcon taskbarIcon = new Hardcodet.Wpf.TaskbarNotification.TaskbarIcon();


        public MainWindow()
        {
            /// <summary>
            /// This is the main window code, can call it as driver code block or main loop
            /// </summary>

            InitializeComponent(); // initializing componenets of software
            SoftwareList(); // for adding downloaded software in the DownloadedSoftwareList

            TotalTimeStopwatch.Start();
            BackGroundStopwatch.Start();
            DataCollectionStopwatch.Start();

            CheckInDataBaseForSavedSoftwareName(); // for entrying data from DownloadedSoftwareList to combobox for user to select software to monitor while loading saved software names from database

            //ListPrint(DownloadedSoftwareList); //for debugging purpose
            //ListPrint(MonitoredProcessList); //for debugging purpose
            
            CheckProcess();

            SaveTimeThreadLooping();
            DataCollectionThreadLooping();

            LoadToDoList();
            LoadEvents();
            LoadCategorySection();

            if (PreAnalysisChecks())
            {
                DrawPCUsageGraph();
                DrawIndividualSoftware();
                DrawCategoricalAnalysis();
            }
        }

        private async void SaveTimeThreadLooping()
        {
            
            await Task.Delay(5 * 60 * 1000); // every 5 minutes 

            SaveTimeThread = new Thread(SaveTime);
            SaveTimeThread.Start();
            
            SaveTimeThreadLooping();
        }

        private void ImmediateSaveTime()
        {
            SaveTimeThread = new Thread(SaveTime);
            SaveTimeThread.Start();

            Thread DataCollectionThread = new Thread(DataCollection);
            DataCollectionThread.Start();

            Thread TotalTimeCollectionThread = new Thread(TotalTimeCollection);
            TotalTimeCollectionThread.Start();
        }

        private void SaveToDoListThreadRun()
        {
            SaveToDoListThread = new Thread(SaveToDoList);
            SaveToDoListThread.Start();
        }

        private void SaveEventsListThreadRun()
        {
            SaveEventsThread = new Thread(SaveEvents);
            SaveEventsThread.Start();
        }

        private async void DataCollectionThreadLooping()
        {

            await Task.Delay(5 * 60 * 1000);

            Thread DataCollectionThread = new Thread(DataCollection);
            DataCollectionThread.Start();

            DataCollectionThreadLooping();
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

            DownloadedSoftwareList = ProcessL.ToHashSet().ToList();

        }

        private void CheckInDataBaseForSavedSoftwareName()
        {
            bool DataBaseAvailable = false;
            bool SoftwareListTable = false;

            try
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();
                DataBaseAvailable = true;
                DBConnection.Close();
            }

            catch (Exception)
            {
                SQLiteConnection.CreateFile("OClockSaveFile.sqlite");

            }

            // Check if database has the required table
            try
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();
                string sql = "CREATE TABLE SoftwareList (Name varchar, Time int, Category varchar)";
                SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                command.ExecuteNonQuery();
                DBConnection.Close();
            }

            catch (Exception)
            {
                SoftwareListTable = true;
            }

            if (DataBaseAvailable || SoftwareListTable)
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();
                string sql = "SELECT * FROM SoftwareList";
                SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                var result = command.ExecuteReader();

                while (result.Read())
                {
                    string name = result.GetString(0);

                    if (!DownloadedSoftwareList.Contains(name))
                    {
                        DownloadedSoftwareList.Add(name);
                    }
                }
                DBConnection.Close();

                EnterDataInCombobox(DownloadedSoftwareList);
            }
        }

        private void ListPrint(List<dynamic> l)
        {
            /// <summary>
            /// This function is purely for debugging purpose only, its function is to print list
            /// </summary>

            foreach (string i in l)
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

            bool result = (E & D & B | C & B | A);
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


        // Save and Load ------------------------

        // No need to create a path to save things in doc as if you make executable of this program when it is run database will be created in same directory as executable so it is good for our 
        // needs


        // Loads

        private void LoadToDoList()
        {
            bool DataBaseAvailable = false;
            bool ToDoListTable = false;

            ToDoListStack.Children.Clear(); // Clearing residual since we are loading from database

            // Check if database is available
            try
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();
                DataBaseAvailable = true;
                DBConnection.Close();
            }

            catch (Exception)
            {
                SQLiteConnection.CreateFile("OClockSaveFile.sqlite");

            }

            // Check if database has the required table
            try
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();
                string sql = "CREATE TABLE ToDoList (Task varchar)";
                SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                command.ExecuteNonQuery();
                DBConnection.Close();
            }

            catch (Exception)
            {
                ToDoListTable = true;
            }

            if (DataBaseAvailable || ToDoListTable)
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();

                string sql = "SELECT Task FROM ToDoList";

                SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                var Result = command.ExecuteReader();

                while (Result.Read())
                {
                    string ans = Result.GetString(0);
                    MakeTheLoadedLabel(ans);
                }
            }
        }

        private void MakeTheLoadedLabel(string s)
        {
            StackPanel LoadStackPanel = new StackPanel();
            CheckBox ToDoCheckBox = new CheckBox();
            ToDoCheckBox.Content = s;

            ToDoCheckBox.Checked += (sssss, eeeee) => {
                ToDoListStack.Children.Remove(LoadStackPanel);
                UnloadFromToDoList(ToDoCheckBox.Content);
                ToDoList.Remove(ToDoCheckBox.Content.ToString());
            };

            LoadStackPanel.Children.Add(ToDoCheckBox);
            ToDoListStack.Children.Add(LoadStackPanel);
        }

        private void LoadEvents()
        {
            bool DataBaseAvailable = false;
            bool EventsListTable = false;

            List<string> deleteList = new List<string>(); 

            EventsStackPanel.Children.Clear(); // Clearing residual since we are loading from database

            // Check if database is available
            try
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();
                DataBaseAvailable = true;
                DBConnection.Close();
            }

            catch (Exception)
            {
                SQLiteConnection.CreateFile("OClockSaveFile.sqlite");

            }

            // Check if database has the required table
            try
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();
                string sql = "CREATE TABLE EventsList (Date varchar, Event varchar)";
                SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                command.ExecuteNonQuery();
                DBConnection.Close();
            }

            catch (Exception)
            {
                EventsListTable = true;
            }

            if (DataBaseAvailable || EventsListTable)
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();

                string sql = "SELECT * FROM EventsList";

                SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                var Result = command.ExecuteReader();

                while (Result.Read())
                {
                    string date = Result.GetString(0);
                    string eventdetail = Result.GetString(1);

                    DateTime datetime = DateTime.Parse(date); // to load relevant events
                    
                    if(datetime >= DateTime.Today)
                    {
                        MakeEventForLoad(date, eventdetail);
                    }
                    else
                    {
                        deleteList.Add(date);
                    }

                }

                DBConnection.Close();
            }

            foreach(string s in deleteList)
            {
                // separte cause database is getting locked if i am doing it in the else itself
                string databaseString = s.Replace("'", "\"");
                SQLiteConnection DBConnection1 = new SQLiteConnection("Data Source = OClockSaveFile.sqlite; Version = 3;");
                DBConnection1.Open();
                string sql1 = string.Format("DELETE FROM EventsList WHERE Date = ('{0}')", databaseString);
                SQLiteCommand command1 = new SQLiteCommand(sql1, DBConnection1);
                command1.ExecuteNonQuery();
                DBConnection1.Close();
            }
        }
        
        private void MakeEventForLoad(string date, string eventdetail)
        {
            StackPanel LoadStackPanel = new StackPanel();
            Label Date = new Label();
            Label EventDetail = new Label();

            Date.Content = date;
            EventDetail.Content = eventdetail;

            LoadStackPanel.Children.Add(Date);
            LoadStackPanel.Children.Add(EventDetail);

            EventsStackPanel.Children.Add(LoadStackPanel);
        }

        // Saves
        private void SaveTime()
        {
            bool DataBaseAvailable = false; // if database is available
            bool SoftwareListTable = false; // if table is made

            // Most of this function is with try since we don't know whether the required thing is available in the database and the possible solution is in the catch

            // Making the list of process still in working
            Dictionary<string, bool> RunningStatus = new Dictionary<string, bool>();

            List<dynamic> ForThisThread = DownloadedSoftwareList;

            foreach (string SM in ForThisThread)
            {

                string ToBeChecked = SM.ToUpper();
                string databaseString = SM.Replace("'", "\""); // to handle "'"

                foreach (Process p in Process.GetProcesses())
                {
                    string MainWinTitle = p.MainWindowTitle.ToString();

                    if (MainWinTitle.Length > 0)
                    {
                        MainWinTitle = MainWinTitle.ToUpper();
                        bool DoesIt = MainWinTitle.Contains(ToBeChecked);

                        if (DoesIt)
                        {
                            // incase of two different instances of a single software running
                            if (!RunningStatus.ContainsKey(databaseString))
                            {
                                RunningStatus.Add(databaseString, DoesIt); 
                            }

                            continue;
                        }

                        
                    }
                }
            }

            // Check if database is available
            try
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();
                DataBaseAvailable = true;
                DBConnection.Close();
            }

            catch (Exception)
            {
                SQLiteConnection.CreateFile("OClockSaveFile.sqlite");
                
            }
            
            // Check if database has the required table
            try 
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();
                string sql = "CREATE TABLE SoftwareList (Name varchar, Time int, Category varchar)";
                SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                command.ExecuteNonQuery();
                DBConnection.Close();
            }

            catch (Exception)
            {
                SoftwareListTable = true;
            }

            //Console.WriteLine((SoftwareListTable, DataBaseAvailable)); // For debugging purpose

            if (DataBaseAvailable || SoftwareListTable) {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();

                foreach (string SoftwareName in RunningStatus.Keys){

                    bool isThere = false; // bool to know if the software name is in the database

                    // string SoftwareName = SM.Replace("'", "\""); // to handle "'"   // Handled in RunningStatus itself

                    try
                    {
                        string sql = string.Format("SELECT Name FROM SoftwareList WHERE Name = '{0}'", SoftwareName);

                        SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                        var Result = command.ExecuteReader();

                        int InstanceCount = 0; // this checks how many time given software name has occured in our database if it is 1 or more then update it i.e set isThere to true if not then add it

                        while (Result.Read())
                        {
                            InstanceCount++;
                        }
                        
                        if (InstanceCount > 0)
                        {
                            isThere = true;
                        }

                        //Console.WriteLine(Result); // For Debugging Purpose
                    }
                    catch (SQLiteException)
                    {
                        isThere = false;
                    }

                    if (isThere) { 
                        string sql1 = string.Format("UPDATE SoftwareList SET Time = Time + {0} WHERE Name = '{1}'", BackGroundStopwatch.Elapsed.Minutes, SoftwareName);
                        SQLiteCommand command1 = new SQLiteCommand(sql1, DBConnection);
                        command1.ExecuteNonQuery();
                        
                    }

                    else
                    {
                        string sql1 = string.Format("INSERT INTO SoftwareList (Name, Time) values ('{0}', {1})", SoftwareName, BackGroundStopwatch.Elapsed.Minutes);
                        SQLiteCommand command1 = new SQLiteCommand(sql1, DBConnection);
                        command1.ExecuteNonQuery();
                    }

                    // Console.WriteLine((isThere, DataBaseAvailable, SoftwareListTable)); // For Debugging Purpose
                }

                DBConnection.Close();
                BackGroundStopwatch.Restart();
            }
        }

        private void EnterAddedSoftwareNameInDataBase(string s)
        {
            bool DataBaseAvailable = false; // if database is available
            bool SoftwareListTable = false; // if table is made
            bool TableUp = false;
            string date = DateTime.Today.ToShortDateString();
            // Check if database is available
            try
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();
                DataBaseAvailable = true;
                DBConnection.Close();
            }

            catch (Exception)
            {
                SQLiteConnection.CreateFile("OClockSaveFile.sqlite");

            }

            // Check if database has the required table
            try
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();
                string sql = "CREATE TABLE SoftwareList (Name varchar, Time int, Category varchar)";
                SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                command.ExecuteNonQuery();
                DBConnection.Close();
            }

            catch (Exception)
            {
                SoftwareListTable = true;
            }

            try
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source = OClockSaveFile.sqlite; Version = 3;");
                DBConnection.Open();

                string sql = "CREATE Table CollectedData (Date varchar, Name varchar, Category varchar, Time int)";
                SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                command.ExecuteNonQuery();
                DBConnection.Close();
            }
            catch (SQLiteException)
            {
                TableUp = true;
            }

            if (DataBaseAvailable || SoftwareListTable)
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();
                string sql1 = string.Format("INSERT INTO SoftwareList (Name, Time) values ('{0}', {1})", s, 0);
                SQLiteCommand command1 = new SQLiteCommand(sql1, DBConnection);
                command1.ExecuteNonQuery();
                DBConnection.Close();
            }

            if (DataBaseAvailable || TableUp)
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();
                string sql1 = string.Format("INSERT INTO CollectedData (Date,Name, Time) values ('{2}', '{0}', {1})", s, 0, date);
                SQLiteCommand command1 = new SQLiteCommand(sql1, DBConnection);
                command1.ExecuteNonQuery();
                DBConnection.Close();
            }
        }

        private void UpdateCategoriesInDataBase(string SN, string C)
        {
            bool DataBaseAvailable = false; // if database is available
            bool SoftwareListTable = false; // if table is made
            bool TableUp = false;

            // Check if database is available
            try
            {
                SQLiteConnection Connection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                Connection.Open();
                DataBaseAvailable = true;
                Connection.Close();
            }

            catch (Exception)
            {
                SQLiteConnection.CreateFile("OClockSaveFile.sqlite");

            }

            // Check if database has the required table
            try
            {
                SQLiteConnection Connection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                Connection.Open();
                string sql = "CREATE TABLE SoftwareList (Name varchar, Time int, Category varchar)";
                SQLiteCommand command = new SQLiteCommand(sql, Connection);
                command.ExecuteNonQuery();
                Connection.Close();
            }

            catch (Exception)
            {
                SoftwareListTable = true;
            }

            try
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source = OClockSaveFile.sqlite; Version = 3;");
                DBConnection.Open();

                string sql = "CREATE Table CollectedData (Date varchar, Name varchar, Category varchar, Time int)";
                SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                command.ExecuteNonQuery();
                DBConnection.Close();
            }
            catch (SQLiteException)
            {
                TableUp = true;
            }


            if (DataBaseAvailable || SoftwareListTable)
            {
                SQLiteConnection Connection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                Connection.Open();
                string sql1 = string.Format("UPDATE SoftwareList SET Category = '{1}' WHERE Name = '{0}'", SN, C);
                SQLiteCommand command1 = new SQLiteCommand(sql1, Connection);
                command1.ExecuteNonQuery();
                Connection.Close();
            }

            if (TableUp || DataBaseAvailable)
            {
                SQLiteConnection Connection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                Connection.Open();
                string sql1 = string.Format("UPDATE CollectedData SET Category = '{1}' WHERE Name = '{0}'", SN, C);
                SQLiteCommand command1 = new SQLiteCommand(sql1, Connection);
                command1.ExecuteNonQuery();
                Connection.Close();
            }
        }

        private void SaveToDoList()
        {
            bool DataBaseAvailable = false;
            bool ToDoListTable = false;

            // Check if database is available
            try
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();
                DataBaseAvailable = true;
                DBConnection.Close();
            }

            catch (Exception)
            {
                SQLiteConnection.CreateFile("OClockSaveFile.sqlite");

            }

            // Check if database has the required table
            try
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();
                string sql = "CREATE TABLE ToDoList (Task varchar)";
                SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                command.ExecuteNonQuery();
                DBConnection.Close();
            }

            catch (Exception)
            {
                ToDoListTable = true;
            }

            if (DataBaseAvailable || ToDoListTable)
            {
                foreach(string SM in ToDoList)
                {
                    string databaseString = SM.Replace("'", "\""); // to handle "'"
                    SQLiteConnection DBConnection = new SQLiteConnection("Data Source = OClockSaveFile.sqlite; Version = 3;");
                    DBConnection.Open();
                    string sql = string.Format("INSERT INTO ToDoList (Task) values ('{0}')", databaseString);
                    SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                    command.ExecuteNonQuery();
                    DBConnection.Close();
                }
                
            }
        }

        private void UnloadFromToDoList(dynamic content)
        {
            string converted = content.ToString();
            string databaseString = converted.Replace("'", "\"");
            SQLiteConnection DBConnection = new SQLiteConnection("Data Source = OClockSaveFile.sqlite; Version = 3;");
            DBConnection.Open();
            string sql = string.Format("DELETE FROM ToDoList WHERE Task = ('{0}')", databaseString);
            SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
            command.ExecuteNonQuery();
            DBConnection.Close();
        }

        private void SaveEvents()
        {
            bool DataBaseAvailable = false;
            bool EventsListStatus = false;

            // Check if database is available
            try
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();
                DataBaseAvailable = true;
                DBConnection.Close();
            }

            catch (Exception)
            {
                SQLiteConnection.CreateFile("OClockSaveFile.sqlite");

            }

            // Check if database has the required table
            try
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();
                string sql = "CREATE TABLE EventsList (Date varchar, Event varchar)";
                SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                command.ExecuteNonQuery();
                DBConnection.Close();
            }

            catch (Exception)
            {
                EventsListStatus = true;
            }

            if (EventsListStatus || DataBaseAvailable)
            {
                for (int i = 0; i < EventsList.Values.Count; i++)
                {
                    string date, eventdetail;
                    (date, eventdetail) = EventsList[i];

                    string DBdate = date.Replace("'", "\"");
                    string DBeventdetail = eventdetail.Replace("'", "\"");

                    SQLiteConnection DBConnection = new SQLiteConnection("Data Source = OClockSaveFile.sqlite; Version = 3;");
                    DBConnection.Open();
                    string sql = string.Format("INSERT INTO EventsList (Date, Event) values ('{0}', '{1}')", DBdate, DBeventdetail);
                    SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                    command.ExecuteNonQuery();
                    DBConnection.Close();

                }
            }

        }

        
        private void DataCollection()
        {
            bool TableUp = false;
            bool DataBaseAvailable = false;

            string date = DateTime.Today.ToShortDateString();

            Dictionary<string, bool> RunningStatus = new Dictionary<string, bool>();

            List<dynamic> ForThisThread = DownloadedSoftwareList;

            foreach (string SM in ForThisThread)
            {

                string ToBeChecked = SM.ToUpper();
                string databaseString = SM.Replace("'", "\""); // to handle "'"

                foreach (Process p in Process.GetProcesses())
                {
                    string MainWinTitle = p.MainWindowTitle.ToString();

                    if (MainWinTitle.Length > 0)
                    {
                        MainWinTitle = MainWinTitle.ToUpper();
                        bool DoesIt = MainWinTitle.Contains(ToBeChecked);

                        if (DoesIt)
                        {
                            // incase of two different instances of a single software running
                            if (!RunningStatus.ContainsKey(databaseString))
                            {
                                RunningStatus.Add(databaseString, DoesIt);
                            }

                            continue;
                        }


                    }
                }
            }

            // Check if database is available
            try
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();
                DataBaseAvailable = true;
                DBConnection.Close();
            }

            catch (Exception)
            {
                SQLiteConnection.CreateFile("OClockSaveFile.sqlite");

            }

            try
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source = OClockSaveFile.sqlite; Version = 3;");
                DBConnection.Open();

                string sql = "CREATE Table CollectedData (Date varchar, Name varchar, Category varchar, Time int)";
                SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                command.ExecuteNonQuery();
                DBConnection.Close();
            }
            catch (SQLiteException)
            {
                TableUp = true;
            }

            if (TableUp || DataBaseAvailable)
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();

                foreach (string SoftwareName in RunningStatus.Keys)
                {

                    bool isThere = false; // bool to know if the software name is in the database

                    // string SoftwareName = SM.Replace("'", "\""); // to handle "'"   // Handled in RunningStatus itself

                    try
                    {
                        string sql = string.Format("SELECT Name FROM CollectedData WHERE Name = '{0}' AND Date = '{1}'", SoftwareName, date);

                        SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                        var Result = command.ExecuteReader();

                        int InstanceCount = 0; // this checks how many time given software name has occured in our database if it is 1 or more then update it i.e set isThere to true if not then add it

                        while (Result.Read())
                        {
                            InstanceCount++;
                        }

                        if (InstanceCount > 0)
                        {
                            isThere = true;
                        }

                        //Console.WriteLine(Result); // For Debugging Purpose
                    }
                    catch (SQLiteException)
                    {
                        isThere = false;
                    }

                    if (isThere)
                    {
                        string sql1 = string.Format("UPDATE CollectedData SET Time = Time + {0} WHERE Name = '{1}' AND Date = '{2}'", DataCollectionStopwatch.Elapsed.Minutes, SoftwareName, date);
                        //Console.WriteLine(DataCollectionStopwatch.Elapsed.Minutes);
                        SQLiteCommand command1 = new SQLiteCommand(sql1, DBConnection);
                        command1.ExecuteNonQuery();
                        
                    }

                    else
                    {
                        string sql1 = string.Format("INSERT INTO CollectedData (Date,Name, Time) values ('{2}', '{0}', {1})", SoftwareName, DataCollectionStopwatch.Elapsed.Minutes, date);
                        SQLiteCommand command1 = new SQLiteCommand(sql1, DBConnection);
                        command1.ExecuteNonQuery();
                    }

                    // Console.WriteLine((isThere, DataBaseAvailable, SoftwareListTable)); // For Debugging Purpose
                }

                DBConnection.Close();
                DataCollectionStopwatch.Restart();
            }

        }

        private void TotalTimeCollection()
        {
            bool tableup = false;
            string date = DateTime.Today.ToShortDateString();

            try
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source = OClockSaveFile.sqlite; Version = 3;");
                DBConnection.Open();

                string sql = "CREATE Table TotalPCTime (Date varchar, Time int)";
                SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                command.ExecuteNonQuery();
                DBConnection.Close();

                tableup = true;
            }
            catch (SQLiteException)
            {
                tableup = true;
            }

            //Console.WriteLine(tableup);

            if (tableup)
            {

                bool isThere = false;

                SQLiteConnection DBConnection = new SQLiteConnection("Data Source = OClockSaveFile.sqlite; Version = 3;");
                DBConnection.Open();

                string presql = string.Format("SELECT * FROM TotalPCTime WHERE Date = '{0}'", date);
                SQLiteCommand precommand = new SQLiteCommand(presql, DBConnection);
                var result = precommand.ExecuteReader();

                while (result.Read())
                {
                    isThere = true;
                }

                if (isThere)
                {
                    string sql1 = string.Format("UPDATE TotalPCTime SET Time = Time + {1} WHERE Date = '{0}'", date, TotalTimeStopwatch.Elapsed.Minutes);
                    SQLiteCommand command1 = new SQLiteCommand(sql1, DBConnection);
                    command1.ExecuteNonQuery();
                }

                else
                {
                    string sql = string.Format("INSERT INTO TotalPCTime (Date, Time) values ('{0}', {1})", date, TotalTimeStopwatch.Elapsed.Minutes);
                    SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                    command.ExecuteNonQuery();
                }

                DBConnection.Close();
            }
        }


        // Analysis (Graphs and cleanning of data) --------------------------------------------------------------

        private bool PreAnalysisChecks()
        {
            // Database Checks
            bool DataBaseAvailable = false;
            bool DataCollectionTableAvailable = false;
            bool PCUsageTableAvailable = false;

            try
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();
                DataBaseAvailable = true;
                DBConnection.Close();
            }

            catch (Exception)
            {
                SQLiteConnection.CreateFile("OClockSaveFile.sqlite");

            }

            try
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source = OClockSaveFile.sqlite; Version = 3;");
                DBConnection.Open();

                string sql = "CREATE Table CollectedData (Date varchar, Name varchar, Category varchar, Time int)";
                SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                command.ExecuteNonQuery();
                DBConnection.Close();
            }

            catch (SQLiteException)
            {
                DataCollectionTableAvailable = true;
            }


            try
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source = OClockSaveFile.sqlite; Version = 3;");
                DBConnection.Open();

                string sql = "CREATE Table TotalPCTime (Date varchar, Time int)";
                SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                command.ExecuteNonQuery();
                DBConnection.Close();
            }

            catch (SQLiteException)
            {
                PCUsageTableAvailable = true;
            }

            if (DataBaseAvailable || DataCollectionTableAvailable || PCUsageTableAvailable)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void DrawPCUsageGraph()
        {
            //bool Status = PreAnalysisChecks();

            
                List<string> X = new List<string>();
                List<int> Y = new List<int>();

                SQLiteConnection DBConnection = new SQLiteConnection("Data Source = OClockSaveFile.sqlite; Version = 3;");
                DBConnection.Open();

                string sql = "SELECT * FROM TotalPCTime";
                SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                var result = command.ExecuteReader();

                while (result.Read())
                {
                    string date = result.GetString(0);
                    int time = result.GetInt32(1);

                    X.Add(date);
                    Y.Add(time);
                    
                }

                DBConnection.Close();

                var series1 = new LiveCharts.Wpf.LineSeries()
                {
                    Title = "PC Usage",
                    Values = new LiveCharts.ChartValues<int>(Y),
                };
                PCUsageCartesianChart.AxisY.Add(
                    new Axis
                    {
                        MinValue = 0
                    }
                );

                PCUsageCartesianChart.Series.Add(series1);

                PCUsageCartesianChart.Zoom = ZoomingOptions.Xy;
                

         
        }

        private void DrawIndividualSoftware()
        {
           // PreAnalysisChecks();

            List<string> Name = new List<string>();
            

            SoftwareUsageCartesianChart.AxisY.Add(
                    new Axis
                    {
                        MinValue = 0
                    }
                );

            SoftwareUsageCartesianChart.Zoom = ZoomingOptions.Xy;
            

            SQLiteConnection DBConnection = new SQLiteConnection("Data Source = OClockSaveFile.sqlite; Version = 3;");
            DBConnection.Open();

            string sql = "SELECT * FROM CollectedData";
            SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
            var result = command.ExecuteReader();

            while (result.Read())
            {
                List<int> Y = new List<int>();

                string name = result.GetString(1);
                int time = result.GetInt32(3);

                Name.Add(name);
                Y.Add(time);

                var series1 = new LiveCharts.Wpf.LineSeries()
                {
                    Title = name,
                    Values = new LiveCharts.ChartValues<int>(Y)
                };

                SoftwareUsageCartesianChart.Series.Add(series1);

            }

            DBConnection.Close();
        }

        private void DrawCategoricalAnalysis()
        {
            //PreAnalysisChecks();
            List<string> category = new List<string>();
            List<int> catwisetime = new List<int>();

            CategoricalChart.AxisY.Add(
                    new Axis
                    {
                        MinValue = 0
                    }
                );

            SQLiteConnection DBConnection = new SQLiteConnection("Data Source = OClockSaveFile.sqlite; Version = 3;");
            DBConnection.Open();

            string sql = "SELECT * FROM CollectedData";
            SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
            var result = command.ExecuteReader();

            while (result.Read())
            {
                string cat = string.Empty;
                try
                {
                    cat = result.GetString(2);
                }
                catch (System.InvalidCastException)
                {
                    continue;
                }

                if (!category.Contains(cat))
                {
                    category.Add(cat);
                }
            }

            foreach(string s in category)
            {
                string sql1 = string.Format("SELECT * FROM CollectedData WHERE Category = '{0}'", s);
                SQLiteCommand command1 = new SQLiteCommand(sql1, DBConnection);
                var result1 = command1.ExecuteReader();
                int totalcattime = 0;

                while (result1.Read())
                {
                    int cattime = result1.GetInt32(3);
                    totalcattime += cattime;
                }

                catwisetime.Add(totalcattime);
            }


            for (int i = 0; i < category.Count; i++)
            {
                List<int> Y = new List<int>();
                

                Y.Add(catwisetime[i]);

                var series1 = new LiveCharts.Wpf.LineSeries
                {
                    Title = category[i],
                    Values = new LiveCharts.ChartValues<int>(Y)
                };

                CategoricalChart.Series.Add(series1);

            }

            DBConnection.Close();

        }

        // Controls------------------------------------------------------------------

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

        private void AddProgramButtonFromSettingsClicked(object sender, RoutedEventArgs e)
        {
            DownloadedSoftwareList.Add(AddProgramTextBox.Text);
            EnterAddedSoftwareNameInDataBase(AddProgramTextBox.Text);
            AddProgramTextBox.Clear();
            EnterDataInCombobox(DownloadedSoftwareList);
            LoadCategorySection(); // update category section
        }

        // More and more control stuff... huh can't categorize any more

        // Essential control for Minimizing to system tray

        private void TrayIconGotDoubleClicked(object sender, RoutedEventArgs e)
        {
            MainWindowDesign.WindowState = WindowState.Normal;
            MainWindowDesign.ShowInTaskbar = true;
            
            MainWindowDesign.Topmost = true;
            MainWindowDesign.Topmost = false;
            MainWindowDesign.Focus();
            //Console.WriteLine("Double Click Registered!!!!");

            LoadToDoList();
        }

        private void ExitMenuItemClicked(object sender, RoutedEventArgs e)
        {
            ImmediateSaveTime();
            Application.Current.Shutdown(); // Save implementation needed here........................................!!!!!!!!!!
        }

        // When Close button is clicked

        private void AttemptToClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ///<summary>
            ///This function is to prevent direct closure of application, it gives an option to minimize to tray
            /// </summary>

            ImmediateSaveTime();

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

        // Pop up for ToDo list

        private void ToDoPopButtonClicked(object sender, RoutedEventArgs e)
        {
            MainWindowDesign.WindowState = WindowState.Minimized;
            MainWindowDesign.ShowInTaskbar = false;

            PopedOutToDoList PTD = new PopedOutToDoList();
            PTD.Show();
            PTD.Topmost = true;
            PTD.Topmost = false;
            PTD.Focus();
        }


        // Add Events and to do 

        private void EventsAddButtonClicked(object sender, RoutedEventArgs e)
        {
            /// <summary>
            /// This fuction will create a new stackpanel with text box and add buttton; upon clicking add button previous stackpanel
            /// gets cleared and a label with date and text is placed
            /// 
            /// In case of date not being selected a simple message to select date is displayed 
            /// </summary>

            StackPanel AddStackPanel = new StackPanel();
            
            TextBox EventText = new TextBox();
            EventText.TextWrapping = TextWrapping.Wrap;
            
            Button EventTextAddButton = new Button();
            EventTextAddButton.Content = "Add";

            EventTextAddButton.Click += (s, ee) => {
                Label DateLabel = new Label();
                Label EventDetail = new Label();

                string DateAfterTimerRemoval = CalendarOfCalendarAndEvents.SelectedDate.ToString();
                EventDetail.Content = EventText.Text;

                if (DateAfterTimerRemoval.Length <= 1)
                {
                    DateLabel.Content = "Please Select Date";
                    AddStackPanel.Children.Add(DateLabel);
                }
                else if (EventText.Text.Length == 0)
                {
                    DateLabel.Content = "Please Enter Event";
                    AddStackPanel.Children.Add(DateLabel);
                }
                else
                {
                    DateAfterTimerRemoval = DateAfterTimerRemoval.Substring(0, DateAfterTimerRemoval.Length - 1 - 8);
                    DateLabel.Content = DateAfterTimerRemoval;

                    EventsList.Add(EventsList.Keys.Count, (DateAfterTimerRemoval, EventText.Text)); // for adding it to database

                    AddStackPanel.Children.Clear();

                    AddStackPanel.Children.Add(DateLabel);
                    AddStackPanel.Children.Add(EventDetail);

                    SaveEventsListThreadRun();
                }
                

            };

            AddStackPanel.Children.Add(EventText);
            AddStackPanel.Children.Add(EventTextAddButton);

            EventsStackPanel.Children.Add(AddStackPanel);

        }

        private void ToDoListAddButtonClicked(object sender, RoutedEventArgs e)
        {
            /// <summary>
            /// This fuction creates a stackpanel with textbox and add button upon clicking the add button children of previous stackpanel
            /// are cleared and a checkbox is added in there place
            /// 
            /// When the checkbox is check the parent stackpanel is removed from its parent stackpanel (ToDoListStack)
            /// 
            /// If there is no todo text then there is a warning put in the form of label for entering todo in the text box
            /// </summary>

            StackPanel AddStackPanel = new StackPanel();

            TextBox ToDoText = new TextBox();
            ToDoText.TextWrapping = TextWrapping.Wrap;


            Button ToDoTextAddButton = new Button();
            ToDoTextAddButton.Content = "Add";

            ToDoTextAddButton.Click += (s, ee) =>
            {
                CheckBox ToDoCheckBox = new CheckBox();
                ToDoCheckBox.Content = ToDoText.Text;

                ToDoCheckBox.Checked += (ss, eee) => {
                    ToDoListStack.Children.Remove(AddStackPanel);
                    UnloadFromToDoList(ToDoCheckBox.Content);
                    
                };

                if (ToDoText.Text.Length == 0)
                {
                    Label WarningLabelInToDo = new Label();
                    WarningLabelInToDo.Content = "Please Enter ToDo";
                    AddStackPanel.Children.Add(WarningLabelInToDo);
                }
                else
                {
                    ToDoList.Add(ToDoText.Text); // for adding to list for database
                    AddStackPanel.Children.Clear();
                    AddStackPanel.Children.Add(ToDoCheckBox);
                    SaveToDoListThreadRun();
                }
            };

            AddStackPanel.Children.Add(ToDoText);
            AddStackPanel.Children.Add(ToDoTextAddButton);
            ToDoListStack.Children.Add(AddStackPanel);
        }

        private void LoadCategorySection()
        {

            CategoriesStackPanel.Children.Clear();

            StackPanel TopStackPanel = new StackPanel();
            TopStackPanel.Orientation = Orientation.Horizontal;

            Label TopNameLabel = new Label();
            Label CategoryLabel = new Label();

            TopNameLabel.Content = "Software Name";
            CategoryLabel.Content = "Category";

            TopNameLabel.Width = 100;
            CategoryLabel.Width = 70;

            TopNameLabel.FontWeight = System.Windows.FontWeights.Bold;
            CategoryLabel.FontWeight = System.Windows.FontWeights.Bold;

            TopStackPanel.Children.Add(TopNameLabel);
            TopStackPanel.Children.Add(CategoryLabel);

            Button UpdateButton = new Button();
            UpdateButton.Content = "Update";

            CategoriesStackPanel.Children.Add(TopStackPanel);
            CategoriesStackPanel.Children.Add(UpdateButton);


            bool DataBaseAvailable = false; // if database is available
            bool SoftwareListTable = false; // if table is made

            // Check if database is available
            try
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();
                DataBaseAvailable = true;
                DBConnection.Close();
            }

            catch (Exception)
            {
                SQLiteConnection.CreateFile("OClockSaveFile.sqlite");

            }

            // Check if database has the required table
            try
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();
                string sql = "CREATE TABLE SoftwareList (Name varchar, Time int, Category varchar)";
                SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                command.ExecuteNonQuery();
                DBConnection.Close();
            }

            catch (Exception)
            {
                SoftwareListTable = true;
            }

            if (DataBaseAvailable || SoftwareListTable)
            {
                SQLiteConnection DBConnection = new SQLiteConnection("Data Source=OClockSaveFile.sqlite;Version=3;");
                DBConnection.Open();
                string sql = "SELECT * FROM SoftwareList";
                SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                var result = command.ExecuteReader();

                while (result.Read())
                {
                    string name = result.GetString(0);
                    string category;
                    try
                    {
                        category = result.GetString(2);
                    }
                    catch (System.InvalidCastException)
                    {
                        category = null;
                    }
                    StackPanel AddStackPanel = new StackPanel();
                    AddStackPanel.Orientation = Orientation.Horizontal;

                    TextBlock NameLabel = new TextBlock();
                    TextBox CategoryTextBox = new TextBox();

                    NameLabel.Text = name;
                    CategoryTextBox.Text = category;

                    NameLabel.Width = 100;
                    CategoryTextBox.Width = 70;
                    //CategoryTextBox.Height = 20;

                    NameLabel.TextWrapping = TextWrapping.Wrap;

                    CategoryTextBox.TextChanged += (s, e) => {
                        try
                        {
                            CategoriesTempSave.Add(name, CategoryTextBox.Text);
                        } 
                        catch (System.ArgumentException)
                        {
                            CategoriesTempSave.Remove(name);
                            CategoriesTempSave.Add(name, CategoryTextBox.Text);
                        }
                    };

                    AddStackPanel.Children.Add(NameLabel);
                    AddStackPanel.Children.Add(CategoryTextBox);

                    CategoriesStackPanel.Children.Add(AddStackPanel);
                }

                UpdateButton.Click += (ss, ee) =>
                {
                    foreach (string s in CategoriesTempSave.Keys)
                    {
                        UpdateCategoriesInDataBase(s, CategoriesTempSave[s]);
                    }
                };

                DBConnection.Close();
            }
        }
    }
}
