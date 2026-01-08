using BenchmarkDotNet.Attributes;
using Old8Lang.AST.Statement;
using Old8Lang.LangParser;
using System.Text;
using LangParserClass = Old8Lang.LangParser.LangParser;

namespace Old8Lang.Benchmarks;

[MemoryDiagnoser]
public class ParserBenchmarkTests
{
    private string SimpleCode = "";
    private string MediumCode = "";
    private string ComplexCode = "";
    private string LoopIntensiveCode = "";
    private string FunctionIntensiveCode = "";
    private string ExpressionIntensiveCode = "";
    private string ClassIntensiveCode = "";
    private string LargeFileCode = "";

    [GlobalSetup]
    public void Setup()
    {
        SimpleCode = """
                    a <- 1
                    b <- 2
                    c <- a + b
                    d <- a * b
                    result <- c + d
                    """;

        MediumCode = """
                     func add(x, y) {
                         return x + y
                     }

                     func calculate(a, b) {
                         if a > b {
                             return a
                         } else {
                             return b
                         }
                     }

                     result <- add(10, 20) + calculate(5, 15)
                     """;

        ComplexCode = GenerateComplexCode();
        LoopIntensiveCode = GenerateLoopIntensiveCode();
        FunctionIntensiveCode = GenerateFunctionIntensiveCode();
        ExpressionIntensiveCode = GenerateExpressionIntensiveCode();
        ClassIntensiveCode = GenerateClassIntensiveCode();
        LargeFileCode = GenerateLargeFileCode();
    }

    private string GenerateComplexCode()
    {
        var code = new StringBuilder();
        code.AppendLine("class Calculator {");
        code.AppendLine("    result <- 0");
        code.AppendLine("");
        code.AppendLine("    func init() {");
        code.AppendLine("        this.result <- 0");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    func add(x) {");
        code.AppendLine("        this.result <- this.result + x");
        code.AppendLine("        return this.result");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    func multiply(x) {");
        code.AppendLine("        this.result <- this.result * x");
        code.AppendLine("        return this.result");
        code.AppendLine("    }");
        code.AppendLine("}");
        code.AppendLine("");
        code.AppendLine("func factorial(n) {");
        code.AppendLine("    if n <= 1 {");
        code.AppendLine("        return 1");
        code.AppendLine("    }");
        code.AppendLine("    return n * factorial(n - 1)");
        code.AppendLine("}");
        code.AppendLine("");
        code.AppendLine("calc <- Calculator()");
        code.AppendLine("calc.add(10)");
        code.AppendLine("calc.multiply(2)");
        code.AppendLine("result <- calc.result + factorial(5)");
        return code.ToString();
    }

    private string GenerateLoopIntensiveCode()
    {
        var code = new StringBuilder();
        code.AppendLine("func loop_test() {");
        code.AppendLine("    sum <- 0");
        code.AppendLine("    for i <- 0, i < 100, i <- i + 1 {");
        code.AppendLine("        sum <- sum + i");
        code.AppendLine("        if sum > 1000 {");
        code.AppendLine("            break");
        code.AppendLine("        }");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    count <- 0");
        code.AppendLine("    while count < 50 {");
        code.AppendLine("        count <- count + 1");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    for item in [1, 2, 3, 4, 5] {");
        code.AppendLine("        sum <- sum + item");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    return sum + count");
        code.AppendLine("}");
        code.AppendLine("result <- loop_test()");
        return code.ToString();
    }

    private string GenerateFunctionIntensiveCode()
    {
        var code = new StringBuilder();
        code.AppendLine("func add(a, b) { return a + b }");
        code.AppendLine("func sub(a, b) { return a - b }");
        code.AppendLine("func mul(a, b) { return a * b }");
        code.AppendLine("func div(a, b) { return a / b }");
        code.AppendLine("");
        code.AppendLine("func calculate(x, y, z) {");
        code.AppendLine("    temp1 <- add(x, y)");
        code.AppendLine("    temp2 <- sub(temp1, z)");
        code.AppendLine("    temp3 <- mul(temp2, temp1)");
        code.AppendLine("    return div(temp3, 2)");
        code.AppendLine("}");
        code.AppendLine("");
        code.AppendLine("func chain_test() {");
        code.AppendLine("    result <- 0");
        code.AppendLine("    for i <- 0, i < 50, i <- i + 1 {");
        code.AppendLine("        result <- calculate(i, i + 1, i + 2)");
        code.AppendLine("    }");
        code.AppendLine("    return result");
        code.AppendLine("}");
        code.AppendLine("result <- chain_test()");
        return code.ToString();
    }

    private string GenerateExpressionIntensiveCode()
    {
        var code = new StringBuilder();
        code.AppendLine("func expression_test() {");
        code.AppendLine("    a <- 10");
        code.AppendLine("    b <- 20");
        code.AppendLine("    c <- 30");
        code.AppendLine("");
        code.AppendLine("    result1 <- ((a + b) * c) - ((a - b) / c)");
        code.AppendLine("    result2 <- a + b * c - a / b + c % a");
        code.AppendLine("");
        code.AppendLine("    result3 <- (a > b) && (b < c) || (a == c)");
        code.AppendLine("    result4 <- (a < 10) || (b > 15) && (c != 30)");
        code.AppendLine("");
        code.AppendLine("    result5 <- a > b ? a : b");
        code.AppendLine("    result6 <- a > b ? (b > c ? a : c) : b");
        code.AppendLine("");
        code.AppendLine("    maybe_null <- null");
        code.AppendLine("    result7 <- maybe_null ?? 100");
        code.AppendLine("");
        code.AppendLine("    result8 <- a as int");
        code.AppendLine("    result9 <- (a + b) as int");
        code.AppendLine("");
        code.AppendLine("    return result1 + result2 + result5 + result7");
        code.AppendLine("}");
        code.AppendLine("result <- expression_test()");
        return code.ToString();
    }

    private string GenerateClassIntensiveCode()
    {
        var code = new StringBuilder();

        for (int i = 1; i <= 5; i++)
        {
            code.AppendLine($"class Class{i} {{");
            code.AppendLine($"    field{i} <- 0");
            code.AppendLine("");
            code.AppendLine($"    func init(value) {{");
            code.AppendLine($"        this.field{i} <- value");
            code.AppendLine("    }");
            code.AppendLine("");
            code.AppendLine($"    func get_value() {{");
            code.AppendLine($"        return this.field{i}");
            code.AppendLine("    }");
            code.AppendLine($"    func set_value(value) {{");
            code.AppendLine($"        this.field{i} <- value");
            code.AppendLine("    }");
            code.AppendLine("}");
            code.AppendLine("");
        }

        code.AppendLine("obj1 <- Class1(10)");
        code.AppendLine("obj2 <- Class2(20)");
        code.AppendLine("obj3 <- Class3(30)");
        code.AppendLine("");
        code.AppendLine("result <- obj1.get_value() + obj2.get_value() + obj3.get_value()");
        return code.ToString();
    }

    private string GenerateLargeFileCode()
    {
        var code = new StringBuilder();

        for (int i = 1; i <= 20; i++)
        {
            code.AppendLine($"func function{i}() {{");
            code.AppendLine($"    sum <- 0");
            code.AppendLine($"    for j <- 0, j < 10, j <- j + 1 {{");
            code.AppendLine($"        sum <- sum + j");
            code.AppendLine("    }");
            code.AppendLine($"    return sum");
            code.AppendLine("}");
            code.AppendLine("");
        }

        for (int i = 1; i <= 5; i++)
        {
            code.AppendLine($"class DataClass{i} {{");
            code.AppendLine($"    data{i} <- 0");
            code.AppendLine("");
            code.AppendLine($"    func init() {{");
            code.AppendLine($"        this.data{i} <- 0");
            code.AppendLine("    }");
            code.AppendLine("}");
            code.AppendLine("");
        }

        code.AppendLine("total <- 0");
        code.AppendLine("for i <- 1, i <= 20, i <- i + 1 {");
        for (int i = 1; i <= 20; i++)
        {
            code.AppendLine($"    if i == {i} {{ total <- total + function{i}() }}");
        }
        code.AppendLine("}");
        code.AppendLine("");
        code.AppendLine("result <- total");

        return code.ToString();
    }

    #region Tokenization Tests

    [Benchmark(Description = "Tokenize Simple Code")]
    public List<LangToken> TokenizeSimpleCode()
    {
        return LangTokenizer.Tokenize(SimpleCode);
    }

    [Benchmark(Description = "Tokenize Medium Code")]
    public List<LangToken> TokenizeMediumCode()
    {
        return LangTokenizer.Tokenize(MediumCode);
    }

    [Benchmark(Description = "Tokenize Complex Code")]
    public List<LangToken> TokenizeComplexCode()
    {
        return LangTokenizer.Tokenize(ComplexCode);
    }

    [Benchmark(Description = "Tokenize Large File")]
    public List<LangToken> TokenizeLargeFile()
    {
        return LangTokenizer.Tokenize(LargeFileCode);
    }

    #endregion

    #region Parsing Tests

    [Benchmark(Description = "Parse Simple Code")]
    public BlockStatement ParseSimpleCode()
    {
        var tokens = LangTokenizer.Tokenize(SimpleCode);
        var parser = new LangParserClass(tokens, SimpleCode);
        return parser.ParseProgram();
    }

    [Benchmark(Description = "Parse Medium Code")]
    public BlockStatement ParseMediumCode()
    {
        var tokens = LangTokenizer.Tokenize(MediumCode);
        var parser = new LangParserClass(tokens, MediumCode);
        return parser.ParseProgram();
    }

    [Benchmark(Description = "Parse Complex Code")]
    public BlockStatement ParseComplexCode()
    {
        var tokens = LangTokenizer.Tokenize(ComplexCode);
        var parser = new LangParserClass(tokens, ComplexCode);
        return parser.ParseProgram();
    }

    [Benchmark(Description = "Parse Large File")]
    public BlockStatement ParseLargeFile()
    {
        var tokens = LangTokenizer.Tokenize(LargeFileCode);
        var parser = new LangParserClass(tokens, LargeFileCode);
        return parser.ParseProgram();
    }

    #endregion

    #region Full Pipeline Tests

    [Benchmark(Description = "Full Pipeline - Simple Code")]
    public BlockStatement FullPipelineSimple()
    {
        var tokens = LangTokenizer.Tokenize(SimpleCode);
        var parser = new LangParserClass(tokens, SimpleCode);
        return parser.ParseProgram();
    }

    [Benchmark(Description = "Full Pipeline - Medium Code")]
    public BlockStatement FullPipelineMedium()
    {
        var tokens = LangTokenizer.Tokenize(MediumCode);
        var parser = new LangParserClass(tokens, MediumCode);
        return parser.ParseProgram();
    }

    [Benchmark(Description = "Full Pipeline - Complex Code")]
    public BlockStatement FullPipelineComplex()
    {
        var tokens = LangTokenizer.Tokenize(ComplexCode);
        var parser = new LangParserClass(tokens, ComplexCode);
        return parser.ParseProgram();
    }

    #endregion

    #region Specific Parser Component Tests

    [Benchmark(Description = "Parse Loop Intensive Code")]
    public BlockStatement ParseLoopIntensive()
    {
        var tokens = LangTokenizer.Tokenize(LoopIntensiveCode);
        var parser = new LangParserClass(tokens, LoopIntensiveCode);
        return parser.ParseProgram();
    }

    [Benchmark(Description = "Parse Function Intensive Code")]
    public BlockStatement ParseFunctionIntensive()
    {
        var tokens = LangTokenizer.Tokenize(FunctionIntensiveCode);
        var parser = new LangParserClass(tokens, FunctionIntensiveCode);
        return parser.ParseProgram();
    }

    [Benchmark(Description = "Parse Expression Intensive Code")]
    public BlockStatement ParseExpressionIntensive()
    {
        var tokens = LangTokenizer.Tokenize(ExpressionIntensiveCode);
        var parser = new LangParserClass(tokens, ExpressionIntensiveCode);
        return parser.ParseProgram();
    }

    [Benchmark(Description = "Parse Class Intensive Code")]
    public BlockStatement ParseClassIntensive()
    {
        var tokens = LangTokenizer.Tokenize(ClassIntensiveCode);
        var parser = new LangParserClass(tokens, ClassIntensiveCode);
        return parser.ParseProgram();
    }

    #endregion

    #region Multiple Parsing Tests

    [Benchmark(Description = "Multiple Parses - Simple Code")]
    public BlockStatement MultipleParsesSimple()
    {
        BlockStatement result = null!;
        for (int i = 0; i < 10; i++)
        {
            var tokens = LangTokenizer.Tokenize(SimpleCode);
            var parser = new LangParserClass(tokens, SimpleCode);
            result = parser.ParseProgram();
        }
        return result;
    }

    [Benchmark(Description = "Multiple Parses - Different Codes")]
    public BlockStatement MultipleParsesDifferent()
    {
        BlockStatement result = null!;
        var codes = new[] { SimpleCode, MediumCode, ComplexCode };
        foreach (var code in codes)
        {
            var tokens = LangTokenizer.Tokenize(code);
            var parser = new LangParserClass(tokens, code);
            result = parser.ParseProgram();
        }
        return result;
    }

    #endregion

    #region Special Syntax Tests

    [Benchmark(Description = "Parse Generic Syntax")]
    public BlockStatement ParseGenericSyntax()
    {
        var code = """
                   func generic_func<T>(value:T) -> T {
                       return value
                   }

                   class GenericClass<T> {
                       data:T
                       func init(value:T) {
                           this.data <- value
                       }
                   }

                   result_int <- generic_func<int>(10)
                   result_str <- generic_func<string>("hello")

                   obj <- GenericClass<int>(20)
                   result <- obj.data
                   """;
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParserClass(tokens, code);
        return parser.ParseProgram();
    }

    [Benchmark(Description = "Parse Lambda Expressions")]
    public BlockStatement ParseLambdaExpressions()
    {
        var code = """
                   add <- (x:int, y:int) -> x + y
                   multiply <- (x:int, y:int) -> x * y

                   calculate <- (x:int, y:int) -> {
                       return x + y
                   }

                   func apply(func:object, x:int, y:int) -> int {
                       return func(x, y)
                   }

                   result1 <- add(10, 20)
                   result2 <- multiply(5, 6)
                   result3 <- apply((a:int, b:int) -> a * b, 3, 4)
                   """;
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParserClass(tokens, code);
        return parser.ParseProgram();
    }

    [Benchmark(Description = "Parse LINQ Syntax")]
    public BlockStatement ParseLinqSyntax()
    {
        var code = """
                   numbers <- [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]

                   result1 <- from n in numbers
                               where n > 5
                               select n * 2

                   result2 <- from n in numbers
                               where n % 2 == 0
                               orderby n descending
                               select n

                   result3 <- from n in numbers
                               group n by n % 3 into g
                               select g
                   """;
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParserClass(tokens, code);
        return parser.ParseProgram();
    }

    [Benchmark(Description = "Parse Match Expressions")]
    public BlockStatement ParseMatchExpressions()
    {
        var code = """
                   func describe_value(value:int) -> string {
                       return match value {
                           case 1 -> "one"
                           case 2 -> "two"
                           case 3 -> "three"
                           case _ -> "other"
                       }
                   }

                   func match_complex(x:object) -> int {
                       return match x {
                           case 10 -> 100
                           case 20 -> 200
                           case _ -> 0
                       }
                   }

                   result1 <- describe_value(1)
                   result2 <- match_complex(10)
                   """;
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParserClass(tokens, code);
        return parser.ParseProgram();
    }

    #endregion
}
