using Old8Lang.CslyMake.OldExpression;
using sly.buildresult;
using sly.parser;
using sly.parser.generator;

namespace Old8Lang.CslyMake.OldLandParser;

public class OldLangInterpreter
{
    public VariateManager Manager = new VariateManager();
    public List<string> Error => new List<string>();
    public string Code { get; set; }
    public OldLangInterpreter(string code)
    {
        Code = code;
    }

    public void Use()
    {
        ParserBuilder<OldTokenGeneric, OldLangTree> Parser = new ParserBuilder<OldTokenGeneric, OldLangTree>();
        OldParser oldParser = new OldParser();
        var buildResult = Parser.BuildParser(oldParser,
            ParserType.EBNF_LL_RECURSIVE_DESCENT,"root").Result;

        var r = buildResult.Parse(Code);
        var RUN = r.Result;
        if (r.Errors !=null && r.Errors.Any())
        {
            // display errors
            r.Errors.ForEach(error => Error.Add(error.ErrorMessage)); ;
        }
        else
        {
            var run = RUN as OldStatement;
            run.Run(ref Manager);
        }
    }

    public List<String> GetError() => Error;
    public VariateManager GetVariateManager() => Manager;
}