using System.Windows;
using System.Windows.Input;
using FeiPos.Presentation.ViewModels;

namespace FeiPos.Presentation.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Loaded += (_, _) => PasswordInput.Focus();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            TryLogin();
        }

        private void PasswordInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TryLogin();
                e.Handled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void TryLogin()
        {
            if (DataContext is not LoginViewModel viewModel)
            {
                return;
            }

            if (viewModel.IsPasswordChangeRequired)
            {
                if (viewModel.ChangePassword(PasswordInput.Password, NewPasswordInput.Password, ConfirmPasswordInput.Password))
                {
                    DialogResult = true;
                }

                return;
            }

            viewModel.LoginCommand.Execute(PasswordInput.Password);
            if (viewModel.LoginSucceeded)
            {
                DialogResult = true;
            }
            else if (viewModel.IsPasswordChangeRequired)
            {
                NewPasswordInput.Focus();
            }
        }
    }
}
