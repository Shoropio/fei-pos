using System.Windows.Controls;
using FeiPos.Presentation.ViewModels;

namespace FeiPos.Presentation.Views
{
    public partial class UsersView : UserControl
    {
        public UsersView()
        {
            InitializeComponent();
        }

        private void PasswordInput_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is UsersViewModel viewModel)
            {
                viewModel.Password = PasswordInput.Password;
            }
        }
    }
}
