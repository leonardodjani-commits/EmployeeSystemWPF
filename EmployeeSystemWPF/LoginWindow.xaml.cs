using System.Windows;
using System.Windows.Input;

namespace EmployeeSystemWPF
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ErrorText.Text = "Please fill all fields.";
                return;
            }

            if (username == "admin" && password == "1234")
            {
                MainWindow main = new MainWindow();
                Application.Current.MainWindow = main;
                main.Show();
                this.Close();
            }
            else
            {
                ErrorText.Text = "❌ Invalid username or password.";
                PasswordBox.Clear();
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                LoginButton_Click(null, null);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}