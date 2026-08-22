using System.ComponentModel;
using System.Runtime.CompilerServices;

using RinsTrap.Enums.FlagPresets;

namespace RinsTrap.Models
{
    public class FastFlagPreset : INotifyPropertyChanged
    {
        public string Name { get; set; } = null!;

        public string Title { get; set; } = null!;

        public string Description { get; set; } = null!;

        public string Icon { get; set; } = "Sparkle24";

        public FastFlagPresetCategory Category { get; set; }

        public IReadOnlyDictionary<string, object?> Flags { get; set; } = new Dictionary<string, object?>();

        private bool _isApplied;

        public bool IsApplied
        {
            get => _isApplied;
            private set
            {
                _isApplied = value;
                OnPropertyChanged();
            }
        }

        public void Refresh()
        {
            IsApplied = Flags.All(x => App.FastFlags.GetValue(x.Key) == x.Value?.ToString());
        }

        public void Apply()
        {
            foreach (var flag in Flags)
                App.FastFlags.SetValue(flag.Key, flag.Value);

            Refresh();
        }

        public void Remove()
        {
            foreach (var flag in Flags)
                App.FastFlags.SetValue(flag.Key, null);

            Refresh();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}