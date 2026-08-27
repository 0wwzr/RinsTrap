using RinsTrap.UI.Elements.Base;
using System.Windows;

namespace RinsTrap.UI.Elements.Dialogs
{
    public partial class LoadingWindow : WpfUiWindow
    {
        public LoadingWindow(string message = "Loading...")
        {
            InitializeComponent();
            LoadingText.Text = message;
        }

        public void SetDetail(string detail)
        {
            DetailText.Text = detail;
            DetailText.Visibility = string.IsNullOrEmpty(detail) ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}