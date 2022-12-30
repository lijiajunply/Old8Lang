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
        Parser<OldTokenGeneric, OldLangTree> parser = null;
        OldParser oldParser = new OldParser();
        BuildResult<Parser<OldTokenGeneric, OldLangTree>> buildResult =
            new ParserBuilder<OldTokenGeneric, OldLangTree>().BuildParser(oldParser, ParserType.EBNF_LL_RECURSIVE_DESCENT,
                "root");
        if (buildResult.IsOk)
            parser = buildResult.Result;
        else
        {
            foreach (var error in buildResult.Errors)
                Error.Add($"{error.Code} : {error.Message}");
            return;
        }
            
        ParseResult<OldTokenGeneric,OldLangTree> r = parser.Parse(code, null); 
        var RUN = r.Result;
        if (RUN is OldBlock)
        {
            var run = RUN as OldBlock;
            run.Run(ref Manager);
        }
    }

    public List<String> GetError() => Error;
    public VariateManager GetVariateManager() => Manager;
}