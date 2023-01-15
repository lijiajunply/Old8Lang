using Old8Lang.CslyMake.OldExpression;
using sly.buildresult;
using sly.parser;
using sly.parser.generator;

namespace Old8Lang.CslyMake.OldLandParser;

public class OldLangInterpreter
{
    public VariateManager Manager;
    public List<string>   Error;
    public string         Code { get; set; }
    public OldLangInterpreter(string code)
    {
        Code    = code;
        Error   = new List<string>();
        Manager = new VariateManager();
    }

    public void Use()
    {
        
        ParserBuilder<OldTokenGeneric, OldLangTree> Parser = new ParserBuilder<OldTokenGeneric, OldLangTree>();
        OldParser oldParser = new OldParser();
        var parserBuilder = Parser.BuildParser(oldParser,
            ParserType.EBNF_LL_RECURSIVE_DESCENT, "root");
        var buildResult = parserBuilder.Result;

        var r = buildResult.Parse(Code);
        var RUN = r.Result;
        if (r.Errors !=null && r.Errors.Any())
            r.Errors.ForEach(x => Error.Add(x.ToString()));
        else
        {
            var run = RUN as OldStatement;
            Console.WriteLine(run);
            run.Run(ref Manager);
        }
    }

    public List<String> GetError() => Error;
    public VariateManager GetVariateManager() => Manager;
}