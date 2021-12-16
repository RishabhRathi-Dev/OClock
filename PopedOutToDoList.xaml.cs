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
using System.Data.SQLite;
using System.Threading;


// Add Load system and try to create sync between the two windows
// Addstackpanel are not adding in todostack from loading


namespace OClock
{
    /// <summary>
    /// Interaction logic for PopedOutToDoList.xaml
    /// </summary>
    public partial class PopedOutToDoList : Window
    {
        List<string> ToDoList = new List<string>();

        public PopedOutToDoList()
        {
            InitializeComponent();
            PositionMainWindow();
            LoadToDoList();
        }

        private void SaveToDoListThreadRun()
        {
            Thread SaveToDoListThread = new Thread(SaveToDoList);
            SaveToDoListThread.Start();
        }

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

        private void PositionMainWindow()
        {
            double width = NeededWidth(300);
            

            PopedOutToDoListWindow.Left = width;
            PopedOutToDoListWindow.Top = 0;
            
        }

        private static double NeededWidth(int x)
        {
            return SystemParameters.FullPrimaryScreenWidth - x;
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
                foreach (string SM in ToDoList)
                {
                    string databaseString = SM.Replace("'", "\""); // to handle "'"
                    SQLiteConnection DBConnection = new SQLiteConnection("Data Source = OClockSaveFile.sqlite; Version = 3;");
                    DBConnection.Open();
                    
                    string sql = string.Format("INSERT INTO ToDoList (Task) values ('{0}')", databaseString);
                    SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
                    command.ExecuteNonQuery();
                    DBConnection.Close();
                }

                ToDoList.Clear();
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
                    ToDoList.Remove(ToDoCheckBox.Content.ToString());
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
    }
}
