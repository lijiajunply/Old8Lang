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
    public void Import_WithAlias_ImportsModuleWithAlias()
    {
        // Arrange
        var testFilePath = "../../../OldLib/ImportTests_Import_WithAlias.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
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
        var testFilePath = "../../../OldLib/ImportTests_Import_SpecificFunction.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
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
        var testFilePath = "../../../OldLib/ImportTests_Import_MultipleModules.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
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
        var testFilePath = "../../../OldLib/ImportTests_Import_RelativePath.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
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
        var testFilePath = "../../../OldLib/ImportTests_Import_NestedModule.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value);
    }

    [Fact]
    public void Import_WithWildCard_ImportsAllFunctions()
    {
        // Arrange
        var testFilePath = "../../../OldLib/ImportTests_Import_WithWildCard.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
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
        var testFilePath = "../../../OldLib/ImportTests_Import_DynamicImport.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
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
        var testFilePath = "../../../OldLib/ImportTests_Import_ConditionalImport.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
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
        var testFilePath = "../../../OldLib/ImportTests_Import_ImportedInFunction.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
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
        var testFilePath = "../../../OldLib/ImportTests_Import_ImportedInClass.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
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
        var testFilePath = "../../../OldLib/ImportTests_Import_WithValidation.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
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
        var testFilePath = "../../../OldLib/ImportTests_Import_CircularDependency.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
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
        var testFilePath = "../../../OldLib/ImportTests_Import_ReimportSameModule.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
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
        var testFilePath = "../../../OldLib/ImportTests_Import_WithConfiguration.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
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
        var testFilePath = "../../../OldLib/ImportTests_Import_NamespaceImport.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value);
    }

    [Fact]
    public void Import_VersionedImport_ImportsSpecificVersion()
    {
        // Arrange
        var testFilePath = "../../../OldLib/ImportTests_Import_VersionedImport.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
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
        var testFilePath = "../../../OldLib/ImportTests_Import_AliasFunction.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
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
        var testFilePath = "../../../OldLib/ImportTests_Import_LazyImport.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
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
        var testFilePath = "../../../OldLib/ImportTests_Import_ConstantImport.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
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
        var testFilePath = "../../../OldLib/ImportTests_Import_TypeImport.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
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
        var testFilePath = "../../../OldLib/ImportTests_Import_PluginImport.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
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
        var testFilePath = "../../../OldLib/ImportTests_Import_NetworkImport.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value);
    }

    [Fact]
    public void Import_ImportChain_HandlesImportChains()
    {
        // Arrange
        var testFilePath = "../../../OldLib/ImportTests_Import_ImportChain.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Combined data from all submodules", ((StringLangValue)result).Value);
    }
}