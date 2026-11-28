using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Collections.Advanced;

public class MethodChainingRegressionTests
{
    [Fact]
    public void CountResult_CanCallToStr()
    {
        const string code = @"
            values <- {1, 2, 3}
            Assert.Equal(""3"", values.Count().ToStr())
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter)();
    }

    [Fact]
    public void FindAllResult_CanCallCount()
    {
        const string code = @"
            values <- {1, 2, 3, 4}
            Assert.Equal(2, values.FindAll((x:int) -> x % 2 == 0).Count())
            Assert.Equal(""2"", values.FindAll((x:int) -> x % 2 == 0).Count().ToStr())
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter)();
    }

    [Fact]
    public void ChainedReceiver_IsEvaluatedOnlyOnce()
    {
        const string code = @"
            class Factory {
                public calls:int <- 0
                func get() -> list {
                    this.calls <- this.calls + 1
                    return {1, 2, 3, 4}
                }
            }
            factory <- Factory()
            count <- factory.get().FindAll((x:int) -> x % 2 == 0).Count()
            Assert.Equal(2, count)
            Assert.Equal(1, factory.calls)
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter)();
    }

    [Fact]
    public void ClassMemberFluentChaining_AndNestedPropertyAccess_WorkInIl()
    {
        const string code = @"
            class Leaf {
                public label:string
                func init(l:string) -> void { this.label <- l }
                func getLabel() -> string { return this.label }
            }
            class Builder {
                public total:int <- 0
                public calls:int <- 0
                func add(n:int) -> Builder {
                    this.total <- this.total + n
                    return this
                }
                func selfCall() -> int {
                    return this.add(5).add(10).total
                }
                func makeLeaf() -> Leaf {
                    this.calls <- this.calls + 1
                    return Leaf(""  hello  "")
                }
                public static func create() -> Builder {
                    return Builder()
                }
            }
            b <- Builder()
            Assert.Equal(6, b.add(1).add(2).add(3).total)
            Assert.Equal(21, b.selfCall())
            Assert.Equal(""HELLO"", b.makeLeaf().getLabel().Trim().ToUpper())
            Assert.Equal(1, b.calls)
            Assert.Equal(15, Builder.create().add(7).add(8).total)
        ";
        var interpreter = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter)();
    }

    [Fact]
    public void FunctionReturnValueChaining_AndSingleEvaluation_WorkInIl()
    {
        const string code = @"
            calls <- 0
            func makeNums() -> list {
                calls <- calls + 1
                return {1, 2, 3, 4, 5}
            }
            func makeText() -> string {
                return ""  abc  ""
            }
            func makeMap() -> dict {
                return {""items"": {10, 20, 30}}
            }

            result <- makeNums().Map((x:int) -> x * 2).Filter((x:int) -> x > 4).Count().ToStr()
            Assert.Equal(""3"", result)
            Assert.Equal(1, calls)
            Assert.Equal(""ABC"", makeText().Trim().ToUpper())
            Assert.Equal(3, makeMap()[""items""].Count())
            Assert.Equal(""20"", makeMap()[""items""][1].ToStr())

            func makeAdder(a:int) -> function {
                return (b:int) -> (c:int) -> a + b + c
            }
            Assert.Equal(60, makeAdder(10)(20)(30))
        ";
        var interpreter = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter)();
    }
}
