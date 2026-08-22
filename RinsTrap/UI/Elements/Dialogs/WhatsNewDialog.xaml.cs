using System.Windows;

using RinsTrap.Models.APIs.GitHub;

namespace RinsTrap.UI.Elements.Dialogs
{
    /// <summary>
    /// Displays the changelog of a release, fetched from the GitHub repository.
    /// </summary>
    public partial class WhatsNewDialog
    {
        private readonly bool _markSeen;

        public WhatsNewDialog(bool markSeen)
        {
            InitializeComponent();

            _markSeen = markSeen;

            HeaderTextBlock.Text = string.Format(Strings.Dialog_WhatsNew_Header, "v" + App.Version);
            SubHeaderTextBlock.Text = App.ProjectRepository;
            ChangelogTextBlock.MarkdownText = Strings.Dialog_WhatsNew_Loading;

            CloseButton.Click += delegate
            {
                Close();
            };

            Loaded += async delegate
            {
                if (_markSeen)
                {
                    // mark as seen immediately so the window never pops up twice
                    App.Settings.Prop.WhatsNewLastSeenVersion = App.Version;
                    App.Settings.Save();
                }

                await LoadChangelog();
            };
        }

        private async Task LoadChangelog()
        {
            GithubRelease? release = await App.GetLatestRelease();

            if (release is null || String.IsNullOrEmpty(release.Body))
            {
                ChangelogTextBlock.MarkdownText = Strings.Dialog_WhatsNew_Unavailable;
                return;
            }

            HeaderTextBlock.Text = string.Format(Strings.Dialog_WhatsNew_Header, release.TagName);

            if (!String.IsNullOrEmpty(release.Name))
                Title = $"{release.Name} - {App.ProjectName}";

            ChangelogTextBlock.MarkdownText = release.Body;
        }
    }
}
