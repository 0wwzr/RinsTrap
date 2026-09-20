using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace RinsTrap.UI.Elements.Dialogs
{
    public partial class LoadingWindow
    {
        public LoadingWindow(string message = "Checking for updates...")
        {
            InitializeComponent();
            LoadingText.Text = message;
            StartAnimations();
        }

private void StartAnimations()
        {
            var dotAnim = (Storyboard)MainGrid.Resources["DotAnimation"];
            dotAnim.Begin(this, true);
        }

        public void SetMessage(string message)
        {
            LoadingText.Text = message;
        }

        public void SetDetail(string detail)
        {
            DetailText.Text = detail;
            DetailText.Visibility = string.IsNullOrEmpty(detail) ? Visibility.Collapsed : Visibility.Visible;
        }

        public void SetProgress(double value)
        {
            ProgressBar.Value = value;
            ProgressBar.Visibility = Visibility.Visible;
        }

        public void SetIndeterminate(bool indeterminate)
        {
            ProgressBar.IsIndeterminate = indeterminate;
            ProgressBar.Visibility = Visibility.Visible;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}