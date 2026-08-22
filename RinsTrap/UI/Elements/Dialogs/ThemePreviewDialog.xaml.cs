using System.Windows;

using RinsTrap.Enums;
using RinsTrap.UI.Elements.Settings;

namespace RinsTrap.UI.Elements.Dialogs
{
    public partial class ThemePreviewDialog
    {
        private readonly Theme _previewTheme;
        private bool _applied;

        public ThemePreviewDialog(Theme theme)
        {
            _previewTheme = theme;

            InitializeComponent();

            Loaded += (_, _) =>
            {
                var dict = new ResourceDictionary { Source = new Uri($"pack://application:,,,/UI/Style/{Enum.GetName(theme)}.xaml") };
                Application.Current.Resources.MergedDictionaries[2] = dict;
            };

            Closing += (_, _) =>
            {
                if (!_applied)
                {
                    var currentTheme = App.Settings.Prop.Theme.GetFinal();
                    var dict = new ResourceDictionary { Source = new Uri($"pack://application:,,,/UI/Style/{Enum.GetName(currentTheme)}.xaml") };
                    Application.Current.Resources.MergedDictionaries[2] = dict;
                }
            };
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            _applied = true;
            App.Settings.Prop.Theme = _previewTheme;
            ((MainWindow)Owner).ApplyTheme();
            Close();
        }
    }
}
