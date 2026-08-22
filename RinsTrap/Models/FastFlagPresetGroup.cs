namespace RinsTrap.Models
{
    public class FastFlagPresetGroup
    {
        public string Title { get; set; } = null!;

        public IReadOnlyList<FastFlagPreset> Presets { get; set; } = new List<FastFlagPreset>();
    }
}