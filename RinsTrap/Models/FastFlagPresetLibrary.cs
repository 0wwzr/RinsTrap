using RinsTrap.Enums.FlagPresets;
using RinsTrap.Resources;

namespace RinsTrap.Models
{
    public static class FastFlagPresetLibrary
    {
        public static IReadOnlyList<FastFlagPreset> Presets { get; } = new List<FastFlagPreset>
        {
            new()
            {
                Icon = "Rocket24",
                Name = "FpsBoost",
                Title = Strings.Menu_FastFlags_Presets_Library_FpsBoost_Title,
                Description = Strings.Menu_FastFlags_Presets_Library_FpsBoost_Description,
                Category = FastFlagPresetCategory.Performance,
                Flags = new Dictionary<string, object?>
                {
                    { "DFIntTaskSchedulerTargetFps", "240" },
                    { "FFlagDebugGraphicsPreferVulkan", "True" },
                    { "FFlagGraphicsQualityFix", "True" },
                    { "FFlagDisablePostFx", "True" },
                    { "FIntRenderShadowIntensity", "0" },
                    { "DFFlagDisableLOD", "True" },
                }
            },
            new()
            {
                Icon = "Flash24",
                Name = "UnlockFps",
                Title = Strings.Menu_FastFlags_Presets_Library_UnlockFps_Title,
                Description = Strings.Menu_FastFlags_Presets_Library_UnlockFps_Description,
                Category = FastFlagPresetCategory.Performance,
                Flags = new Dictionary<string, object?>
                {
                    { "DFIntTaskSchedulerTargetFps", "240" },
                }
            },
            new()
            {
                Icon = "FastForward24",
                Name = "UnlimitedFps",
                Title = Strings.Menu_FastFlags_Presets_Library_UnlimitedFps_Title,
                Description = Strings.Menu_FastFlags_Presets_Library_UnlimitedFps_Description,
                Category = FastFlagPresetCategory.Performance,
                Flags = new Dictionary<string, object?>
                {
                    // forces the "Maximum Frame Rate" dropdown to show up in the in-game ESC menu
                    { "FFlagGameBasicSettingsFramerateCap5", "True" },
                    // removes the engine's hard 240 FPS clamp
                    { "FFlagTaskSchedulerLimitTargetFpsTo2402", "False" },
                    // effectively unlimited frame rate target
                    { "DFIntTaskSchedulerTargetFps", "9999" },
                }
            },
            new()
            {
                Icon = "EyeOff24",
                Name = "DisableLod",
                Title = Strings.Menu_FastFlags_Presets_Library_DisableLod_Title,
                Description = Strings.Menu_FastFlags_Presets_Library_DisableLod_Description,
                Category = FastFlagPresetCategory.Performance,
                Flags = new Dictionary<string, object?>
                {
                    { "DFFlagDisableLOD", "True" },
                }
            },
            new()
            {
                Icon = "TopSpeed24",
                Name = "PreferVulkan",
                Title = Strings.Menu_FastFlags_Presets_Library_PreferVulkan_Title,
                Description = Strings.Menu_FastFlags_Presets_Library_PreferVulkan_Description,
                Category = FastFlagPresetCategory.Visuals,
                Flags = new Dictionary<string, object?>
                {
                    { "FFlagDebugGraphicsPreferVulkan", "True" },
                }
            },
            new()
            {
                Icon = "Blur24",
                Name = "DisablePostFx",
                Title = Strings.Menu_FastFlags_Presets_Library_DisablePostFx_Title,
                Description = Strings.Menu_FastFlags_Presets_Library_DisablePostFx_Description,
                Category = FastFlagPresetCategory.Visuals,
                Flags = new Dictionary<string, object?>
                {
                    { "FFlagDisablePostFx", "True" },
                }
            },
            new()
            {
                Icon = "WeatherSunny24",
                Name = "DisableShadows",
                Title = Strings.Menu_FastFlags_Presets_Library_DisableShadows_Title,
                Description = Strings.Menu_FastFlags_Presets_Library_DisableShadows_Description,
                Category = FastFlagPresetCategory.Visuals,
                Flags = new Dictionary<string, object?>
                {
                    { "FIntRenderShadowIntensity", "0" },
                }
            },
            new()
            {
                Icon = "Wand24",
                Name = "DynamicHeads",
                Title = Strings.Menu_FastFlags_Presets_Library_DynamicHeads_Title,
                Description = Strings.Menu_FastFlags_Presets_Library_DynamicHeads_Description,
                Category = FastFlagPresetCategory.Visuals,
                Flags = new Dictionary<string, object?>
                {
                    { "FFlagEnableDynamicHeads", "True" },
                }
            },
        };
    }
}
