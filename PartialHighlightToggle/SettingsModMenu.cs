using Kingmaker.Localization;
using Kingmaker.UI;
using ModMenu.Settings;

namespace PartialHighlightToggle;

internal class SettingsModMenu
{
    private static readonly string RootKey = "AlterAsc.PartialHighlightToggle".ToLower();

    public bool DefaultHighlightState => ModMenu.ModMenu.GetSettingValue<bool>(GetKey("highlightdefault"));

    internal void Initialize()
    {
        ModMenu.ModMenu.AddSettings(
          SettingsBuilder
            .New(GetKey("title"), CreateString("title", "Partial Highlight Toggle"))
            .AddKeyBinding(
                KeyBinding.New(
                    GetKey("toggle"),
                    KeyboardAccess.GameModesGroup.World,
                    CreateString(GetKey("toggle" + "-desc"), "Partial Highlight Toggle key"))
                .SetPrimaryBinding(UnityEngine.KeyCode.R, withCtrl: true),
                HighlightManager.TogglePassiveHighlight
            )
            .AddToggle(
              Toggle
                .New(GetKey("highlightdefault"), defaultValue: false, CreateString("highlightdefault", "Partial highlighting default state"))
                .WithLongDescription(CreateString("highlightdefault-desc", "Default level of highlight state. On = partial highlighting is enabled by default, Off = disabled."))
            )
        );
    }

    private static LocalizedString CreateString(string partialKey, string text)
    {
        return CreateStringInner(GetKey(partialKey, "--"), text);
    }

    private static string GetKey(string partialKey, string separator = ".")
    {
        return $"{RootKey}{separator}{partialKey}";
    }

    private static LocalizedString CreateStringInner(string key, string value)
    {
        LocalizedString result = new()
        {
            m_Key = key
        };
        LocalizationManager.CurrentPack.PutString(key, value);
        return result;
    }
}
