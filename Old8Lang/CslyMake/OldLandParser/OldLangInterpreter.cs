using Old8Lang.CslyMake.OldExpression;
using sly.buildresult;
using sly.parser;
using sly.parser.generator;

namespace Old8Lang.CslyMake.OldLandParser;

public class OldLangInterpreter
{
    public VariateManager Manager = new VariateManager();
    public List<string> Error => new List<string>();
    public OldLangInterpreter(string code)
    {
        ParserBuilder<OldTokenGeneric, OldLangTree> Parser = new ParserBuilder<OldTokenGeneric, OldLangTree>();
        OldParser oldParser = new OldParser();
        var buildResult = Parser.BuildParser(oldParser,
            ParserType.EBNF_LL_RECURSIVE_DESCENT).Result;

        var r = buildResult.Parse(code);
        string aaa = code; //
        var RUN = r.Result;
        if (r.Errors !=null && r.Errors.Any())
        {
            // display errors
            r.Errors.ForEach(error => Error.Add(error.ErrorMessage + "\n"));
        }
    }

    public List<String> GetError() => Error;
    public VariateManager GetVariateManager() => Manager;
}