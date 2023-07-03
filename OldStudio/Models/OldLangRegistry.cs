using System.Collections.Generic;
using System.IO;
using TextMateSharp.Internal.Themes.Reader;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Themes;
using Theme = OldStudio.Assets.Theme;

namespace OldStudio.Models;

public class OldLangRegistry : IRegistryOptions
{
    public IRawTheme GetTheme(string scopeName)
    {
        string themePath = Assets.AssetsLoader.GetThemePath(scopeName);
        using StreamReader reader = new StreamReader(themePath);
        return ThemeReader.ReadThemeSync(reader);
    }

    public IRawGrammar GetGrammar(string scopeName)
    {
        // Stream grammarStream = ResourceLoader.TryOpenGrammarStream(GetGrammarFile(scopeName));
        //
        // if (grammarStream == null)
        //     return null;
        //
        // using (grammarStream)
        // using (StreamReader reader = new StreamReader(grammarStream))
        // {
        //     return GrammarReader.ReadGrammarSync(reader);
        // }
        return null;
    }

    public ICollection<string> GetInjections(string scopeName)
    {
        return null;
    }

    public IRawTheme GetDefaultTheme()
    {
        string themePath = Assets.AssetsLoader.GetThemePath(Theme.Dark);
        using StreamReader reader = new StreamReader(themePath);
        return ThemeReader.ReadThemeSync(reader);
    }
}