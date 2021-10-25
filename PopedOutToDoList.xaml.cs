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

// Add Load system and try to create sync between the two windows

namespace OClock
{
    /// <summary>
    /// Interaction logic for PopedOutToDoList.xaml
    /// </summary>
    public partial class PopedOutToDoList : Window
    {
        public PopedOutToDoList()
        {
            InitializeComponent();
            PositionMainWindow();
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
                };

                if (ToDoText.Text.Length == 0)
                {
                    Label WarningLabelInToDo = new Label();
                    WarningLabelInToDo.Content = "Please Enter ToDo";
                    AddStackPanel.Children.Add(WarningLabelInToDo);
                }
                else
                {
                    AddStackPanel.Children.Clear();
                    AddStackPanel.Children.Add(ToDoCheckBox);
                }
            };

            AddStackPanel.Children.Add(ToDoText);
            AddStackPanel.Children.Add(ToDoTextAddButton);
            ToDoListStack.Children.Add(AddStackPanel);
        }
    }
}
