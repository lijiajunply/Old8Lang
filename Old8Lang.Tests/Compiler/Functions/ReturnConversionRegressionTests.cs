using System.Reflection.Emit;
using Old8Lang.AST.Statement;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Functions;

[Collection("Console output isolation")]
public class ReturnConversionRegressionTests
{
    [Fact]
    public void StringReturn_UsesLanguageDisplayConversion()
    {
        const string code = "func value() -> string { return 42 } Assert.Equal(\"42\", value())";
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter);
        action();
    }

    [Theory]
    [InlineData("string", "true", "\"true\"")]
    [InlineData("string", "{1, 2}", "\"[1, 2]\"")]
    [InlineData("string", "'a'", "\"a\"")]
    [InlineData("int", "\"42\"", "42")]
    [InlineData("double", "\"2.5\"", "2.5")]
    [InlineData("bool", "1", "true")]
    public void Return_ConvertsCompatibleValues(string type, string expression, string expected)
    {
        var code = $"func value() -> {type} {{ return {expression} }} Assert.Equal({expected}, value())";
        var interpreter = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter)();
    }

    [Fact]
    public void DynamicReturn_ConvertsDoubleToInt()
    {
        const string code = @"
            func value() -> int {
                values <- {3.9}
                return values[0]
            }
            Assert.Equal(3, value())
        ";
        var interpreter = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter)();
    }

    [Fact]
    public void DynamicCharReturn_WorksAndRejectsInvalidType()
    {
        const string okCode = @"
            func getChar() -> char {
                values <- {'z'}
                return values[0]
            }
            Assert.Equal('z', getChar())
        ";
        var interpreter = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(interpreter.Build(okCode), "test", interpreter)();

        const string badCode = @"
            func getChar() -> char {
                values <- {65}
                return values[0]
            }
            getChar()
        ";
        var badInterpreter = new LangInterpreter();
        var badAction = Old8Lang.Compiler.Compiler.Compile(badInterpreter.Build(badCode), "test", badInterpreter);
        Assert.Throws<Old8Lang.Error.TypeError>(() => badAction());
    }

    [Fact]
    public void CharReturn_WithoutTrailingReturn_ProducesValidDefaultIl()
    {
        const string code = @"
            func maybeChar(flag:bool) -> char {
                if flag {
                    return 'k'
                }
            }
            Assert.Equal('k', maybeChar(true))
        ";
        var interpreter = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter)();
    }

    [Fact]
    public void NullableReturns_HandleValueNullTryFinallyAndRejectInvalidTypes()
    {
        const string okCode = @"
            func maybeInt(flag:bool) -> int? {
                try {
                    if flag {
                        return 42
                    }
                    return null
                } finally {
                    x <- 1
                }
            }
            func maybeDouble(flag:bool) -> double? {
                if flag {
                    return 7
                }
                return null
            }
            func maybeChar(flag:bool) -> char? {
                if flag {
                    return 'x'
                }
                return null
            }
            Assert.Equal(42, maybeInt(true))
            Assert.Equal(null, maybeInt(false))
            Assert.Equal(7.0, maybeDouble(true))
            Assert.Equal(null, maybeDouble(false))
            Assert.Equal('x', maybeChar(true))
            Assert.Equal(null, maybeChar(false))
        ";
        var interpreter = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(interpreter.Build(okCode), "test", interpreter)();

        const string badCode = @"
            func maybeInt() -> int? {
                return ""not a number""
            }
            maybeInt()
        ";
        var badInterpreter = new LangInterpreter();
        var badAction = Old8Lang.Compiler.Compiler.Compile(badInterpreter.Build(badCode), "test", badInterpreter);
        Assert.Throws<Old8Lang.Error.TypeError>(() => badAction());
    }

    [Fact]
    public void GenericNullableReturn_HandlesValueAndNull()
    {
        const string code = @"
            func pick<T>(flag:bool, v:T) -> T? {
                if flag {
                    return v
                }
                return null
            }
            Assert.Equal(99, pick<int>(true, 99))
            Assert.Equal(null, pick<int>(false, 99))
            Assert.Equal(""ok"", pick<string>(true, ""ok""))
            Assert.Equal(null, pick<string>(false, ""ok""))
        ";
        var interpreter = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter)();
    }

    [Fact]
    public void ClrNumericReturnConversions_EmitValidIlForAllNumericTypes()
    {
        var numericPairs = new (Type Source, Type Target, object Input, object Expected)[]
        {
            (typeof(uint), typeof(double), 3_000_000_000u, 3_000_000_000.0),
            (typeof(ulong), typeof(double), 10_000_000_000UL, 10_000_000_000.0),
            (typeof(int), typeof(uint), 42, 42u),
            (typeof(int), typeof(ulong), 42, 42UL),
            (typeof(int), typeof(ushort), 65000, (ushort)65000),
            (typeof(int), typeof(sbyte), -12, (sbyte)-12),
            (typeof(int), typeof(decimal), 123, 123m),
            (typeof(decimal), typeof(double), 12.5m, 12.5),
            (typeof(decimal), typeof(int), 19.9m, 20),
            (typeof(double), typeof(decimal), 3.25, 3.25m),
            (typeof(uint), typeof(decimal), 99u, 99m),
            (typeof(sbyte), typeof(int), (sbyte)-7, -7),
            (typeof(ushort), typeof(double), (ushort)500, 500.0),
        };

        var extType = typeof(IlTypeErrors).Assembly.GetType("Old8Lang.AST.Statement.ReturnStatementRuntimeExtensions")!;
        var isNumeric = extType.GetMethod("IsNumericType", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var isAllowed = extType.GetMethod("IsBasicTypeConversionAllowed", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var emitConv = extType.GetMethod("EmitNumericConversion", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, [typeof(ILGenerator), typeof(Type), typeof(Type)])!;

        foreach (var (source, target, input, expected) in numericPairs)
        {
            Assert.True((bool)isNumeric.Invoke(null, [source])!);
            Assert.True((bool)isNumeric.Invoke(null, [target])!);
            Assert.True((bool)isAllowed.Invoke(null, [target, source])!);

            var dm = new DynamicMethod($"conv_{source.Name}_to_{target.Name}", typeof(object), [typeof(object)], true);
            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Unbox_Any, source);
            emitConv.Invoke(null, [il, source, target]);
            il.Emit(OpCodes.Box, target);
            il.Emit(OpCodes.Ret);

            var actual = dm.Invoke(null, [input]);
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void RuntimeConvertReturn_HandlesExtendedClrNumericAndNullableTypes()
    {
        Assert.Equal(42u, IlTypeErrors.ConvertReturn(42, typeof(uint)));
        Assert.Equal(42UL, IlTypeErrors.ConvertReturn("42", typeof(ulong)));
        Assert.Equal((ushort)12, IlTypeErrors.ConvertReturn(12.0, typeof(ushort)));
        Assert.Equal((sbyte)-5, IlTypeErrors.ConvertReturn(-5, typeof(sbyte)));
        Assert.Equal(12.75m, IlTypeErrors.ConvertReturn("12.75", typeof(decimal)));
        Assert.Equal('a', IlTypeErrors.ConvertReturn('a', typeof(char)));
        Assert.Null(IlTypeErrors.ConvertReturn(null, typeof(int?)));
        Assert.Equal(7, IlTypeErrors.ConvertReturn("7", typeof(int?)));
        Assert.Throws<Old8Lang.Error.TypeError>(() => IlTypeErrors.ConvertReturn("bad", typeof(decimal)));
        Assert.Throws<Old8Lang.Error.TypeError>(() => IlTypeErrors.ConvertReturn(null, typeof(uint)));
    }

    [Fact]
    public void InvalidReturn_ThrowsLanguageError()
    {
        const string code = "func value() -> int { return \"not a number\" } value()";
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter);
        Assert.ThrowsAny<Old8Lang.Error.Old8Exception>(() => action());
    }

    [Fact]
    public void InvalidDynamicReturn_ThrowsTypeError()
    {
        const string code = "func value() -> int { values <- {\"bad\"} return values[0] } value()";
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter);
        Assert.Throws<Old8Lang.Error.TypeError>(() => action());
    }

    [Fact]
    public void NumericReturnInTryFinally_IsConvertedBeforeLeaving()
    {
        const string code = @"
            func value() -> double {
                try { return 42 } finally { PrintLine(""finally"") }
            }
            PrintLine(value())
        ";
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter);
        var previous = Console.Out;
        using var output = new StringWriter();
        try { Console.SetOut(output); action(); }
        finally { Console.SetOut(previous); }
        Assert.Equal($"finally{Environment.NewLine}42{Environment.NewLine}", output.ToString());
    }
}
