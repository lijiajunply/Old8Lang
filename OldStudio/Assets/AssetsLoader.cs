using System.IO;
using Old8Lang;

namespace OldStudio.Assets;

public static class AssetsLoader
{
    public static string GetThemePath(Theme theme)
    {
        var name = theme switch
        {
            Theme.Abbys => "abyss-color-theme.json",
            Theme.Dark => "dark_vs.json",
            Theme.DarkPlus => "dark_plus.json",
            Theme.DimmedMonokai => "dimmed-monokai-color-theme.json",
            Theme.KimbieDark => "kimbie-dark-color-theme.json",
            Theme.Light => "light_vs.json",
            Theme.LightPlus => "light_plus.json",
            Theme.Monokai => "monokai-color-theme.json",
            Theme.QuietLight => "quietlight-color-theme.json",
            Theme.Red => "Red-color-theme.json",
            Theme.SolarizedDark => "solarized-dark-color-theme.json",
            Theme.SolarizedLight => "solarized-light-color-theme.json",
            Theme.TomorrowNightBlue => "tomorrow-night-blue-color-theme.json",
            Theme.HighContrastLight => "hc_light.json",
            Theme.HighContrastDark => "hc_black.json",
            _ => string.Empty
        };

        return Path.Combine(BasicInfo.CodePath, "Assets", name);
    }

    public static string GetThemePath(string name)
    {
        return Path.Combine(BasicInfo.CodePath, "Assets", name+".json");
    }
}

public enum Theme
{
    Abbys,
    Dark,
    DarkPlus,
    DimmedMonokai,
    KimbieDark,
    Light,
    LightPlus,
    Monokai,
    QuietLight,
    Red,
    SolarizedDark,
    SolarizedLight,
    TomorrowNightBlue,
    HighContrastLight,
    HighContrastDark,
}