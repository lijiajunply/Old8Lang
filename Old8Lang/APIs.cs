using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang;
public class APIs
{
    public static (VariateManager,List<string>) CslyUsing(string code)
    {
        var a = new OldLangInterpreter(code);
        return (a.GetVariateManager(), a.GetError());
    }
}