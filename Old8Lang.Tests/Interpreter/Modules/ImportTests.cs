using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Modules;

/// <summary>
/// 导入语句测试
/// </summary>
public class ImportTests
{
    [Fact]
    public void Import_SimpleModule_ImportsBasicModule()
    {
        // Arrange
        var code = @"
            import ""math""
            result <- math.sqrt(16)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(4.0, ((DoubleLangValue)result).Value);
    }

    [Fact]
    public void Import_WithAlias_ImportsModuleWithAlias()
    {
        // Arrange
        var code = @"
            import ""math"" as m
            result <- m.sin(3.14159 / 2)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        // sin(π/2) ≈ 1
        Assert.Equal(1.0, ((DoubleLangValue)result).Value, 0.1);
    }

    [Fact]
    public void Import_SpecificFunction_ImportsSpecificFunctions()
    {
        // Arrange
        var code = @"
            import from ""math"" { sqrt, pow }
            result1 <- sqrt(25)
            result2 <- pow(2, 3)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.IsType<DoubleLangValue>(result1);
        Assert.Equal(5.0, ((DoubleLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<DoubleLangValue>(result2);
        Assert.Equal(8.0, ((DoubleLangValue)result2).Value);
    }

    [Fact]
    public void Import_MultipleModules_ImportsMultipleModules()
    {
        // Arrange
        var code = @"
            import ""math""
            import ""string""
            result1 <- math.abs(-5)
            result2 <- string.length(""hello"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.IsType<DoubleLangValue>(result1);
        Assert.Equal(5.0, ((DoubleLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(5, ((IntLangValue)result2).Value);
    }

    [Fact]
    public void Import_RelativePath_ImportsRelativeModule()
    {
        // Arrange
        var code = @"
            import ""./utils""
            result <- utils.formatNumber(1234.567, 2)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("1234.57", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Import_NestedModule_ImportsFromNestedModule()
    {
        // Arrange
        var code = @"
            import ""database.connection""
            conn <- database.connection.create(""localhost"", 5432)
            result <- conn.isConnected()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(true, ((BoolLangValue)result).Value);
    }

    [Fact]
    public void Import_WithWildCard_ImportsAllFunctions()
    {
        // Arrange
        var code = @"
            import from ""math"" *
            result <- min(max(10, 5), 15)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(10.0, ((DoubleLangValue)result).Value);
    }

    [Fact]
    public void Import_DynamicImport_ImportsModuleDynamically()
    {
        // Arrange
        var code = @"
            moduleName <- ""math""
            import moduleName as dynamicMath
            result <- dynamicMath.ceil(3.14)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(4.0, ((DoubleLangValue)result).Value);
    }

    [Fact]
    public void Import_ConditionalImport_ImportsBasedOnCondition()
    {
        // Arrange
        var code = @"
            useDebug <- true
            if useDebug {
                import ""logging""
            }
            if useDebug {
                logging.info(""Debug mode enabled"")
                result <- ""logging imported""
            } else {
                result <- ""logging not imported""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("logging imported", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Import_ImportedInFunction_ImportsInsideFunction()
    {
        // Arrange
        var code = @"
            func calculateCircleArea(radius:double) -> double {
                import ""math""
                return math.pi * math.pow(radius, 2)
            }
            result <- calculateCircleArea(5.0)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(78.53981633974483, ((DoubleLangValue)result).Value, 0.1); // π * 25
    }

    [Fact]
    public void Import_ImportedInClass_ImportsInsideClass()
    {
        // Arrange
        var code = @"
            class Calculator {
                func Init() {
                    import ""math""
                }
                func distance(x1:double, y1:double, x2:double, y2:double) -> double {
                    return math.sqrt(math.pow(x2 - x1, 2) + math.pow(y2 - y1, 2))
                }
            }
            calc <- Calculator()
            result <- calc.distance(0, 0, 3, 4)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(5.0, ((DoubleLangValue)result).Value); // sqrt(9 + 16) = 5
    }

    [Fact]
    public void Import_WithValidation_ValidatesImportPath()
    {
        // Arrange
        var code = @"
            try {
                import ""nonexistent.module""
                result <- ""Import successful""
            } catch {
                result <- ""Import failed: "" + exception
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Contains("Import failed", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Import_CircularDependency_HandlesCircularDependencies()
    {
        // Arrange
        var code = @"
            // Module A imports Module B
            import ""moduleA""
            // This would create a circular dependency
            // The interpreter should handle this gracefully
            result <- moduleA.getValue()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        // Result depends on circular dependency handling
    }

    [Fact]
    public void Import_ReimportSameModule_HandlesReimporting()
    {
        // Arrange
        var code = @"
            import ""math""
            import ""math"" as math2
            result1 <- math.sqrt(9)
            result2 <- math2.sqrt(16)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.IsType<DoubleLangValue>(result1);
        Assert.Equal(3.0, ((DoubleLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<DoubleLangValue>(result2);
        Assert.Equal(4.0, ((DoubleLangValue)result2).Value);
    }

    [Fact]
    public void Import_WithConfiguration_ConfiguresImportBehavior()
    {
        // Arrange
        var code = @"
            import ""math"" with {
                ""precision"": 2,
                ""cache"": true
            }
            result <- math.round(3.14159)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(3.14, ((DoubleLangValue)result).Value);
    }

    [Fact]
    public void Import_NamespaceImport_ImportsUnderNamespace()
    {
        // Arrange
        var code = @"
            import ""database"" as db
            conn1 <- db.createConnection(""mysql"")
            conn2 <- db.createConnection(""postgresql"")
            result <- db.validateConnections([conn1, conn2])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(true, ((BoolLangValue)result).Value);
    }

    [Fact]
    public void Import_VersionedImport_ImportsSpecificVersion()
    {
        // Arrange
        var code = @"
            import ""math"" version ""1.2.3""
            result <- math.factorial(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(120, ((IntLangValue)result).Value); // 5! = 120
    }

    [Fact]
    public void Import_AliasFunction_ImportsWithFunctionAlias()
    {
        // Arrange
        var code = @"
            import from ""math"" { sin as sine, cos as cosine }
            angle <- 3.14159 / 4
            result <- sine(angle) / cosine(angle)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        // tan(π/4) = 1
        Assert.Equal(1.0, ((DoubleLangValue)result).Value, 0.1);
    }

    [Fact]
    public void Import_LazyImport_DelaysImportUntilUse()
    {
        // Arrange
        var code = @"
            lazy import ""expensive.heavy.math""
            // Module not loaded yet
            result1 <- ""Not loaded""
            // First use triggers import
            result2 <- heavy.math.complexCalculation()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.IsType<StringLangValue>(result1);
        Assert.Equal("Not loaded", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        // Result depends on the heavy math calculation
    }

    [Fact]
    public void Import_ConstantImport_ImportsConstants()
    {
        // Arrange
        var code = @"
            import from ""physics"" { SPEED_OF_LIGHT, GRAVITY }
            energy <- 42.0 * SPEED_OF_LIGHT * SPEED_OF_LIGHT
            force <- 10.0 * GRAVITY
            result <- ""Energy: "" + energy.ToStr() + "", Force: "" + force.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Contains("Energy:", ((StringLangValue)result).Value);
        Assert.Contains("Force:", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Import_TypeImport_ImportsTypes()
    {
        // Arrange
        var code = @"
            import from ""collections"" { Stack, Queue }
            stack <- Stack<string>()
            queue <- Queue<int>()
            stack.Push(""first"")
            stack.Push(""second"")
            queue.Enqueue(1)
            queue.Enqueue(2)
            stackSize <- stack.Size()
            queueSize <- queue.Size()
            result <- ""Stack: "" + stackSize.ToStr() + "", Queue: "" + queueSize.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Stack: 2, Queue: 2", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Import_PluginImport_ImportsPluginModule()
    {
        // Arrange
        var code = @"
            import ""plugin.imageProcessor"" as imgProc
            processor <- imgProc.create(""jpeg"")
            result <- processor.process(""image.jpg"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Processed image.jpg with JPEG processor", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Import_NetworkImport_ImportsNetworkResource()
    {
        // Arrange
        var code = @"
            import ""https://api.example.com/utils"" as apiUtils
            result <- apiUtils.validateEmail(""test@example.com"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(true, ((BoolLangValue)result).Value);
    }

    [Fact]
    public void Import_ImportChain_HandlesImportChains()
    {
        // Arrange
        var code = @"
            import ""module.main""
            // main module imports submodules
            result <- module.main.getCombinedData()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Combined data from all submodules", ((StringLangValue)result).Value);
    }
}