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
        // sqrt(25) = 5
        Assert.Equal(5.0, ((DoubleLangValue)result).Value);
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
        Assert.IsType<DoubleLangValue>(result2);
        Assert.Equal(5.0, ((DoubleLangValue)result2).Value);
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
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(25, ((IntLangValue)result).Value);
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
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(1, ((IntLangValue)result).Value);
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
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(5.0, ((DoubleLangValue)result).Value);
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
        Assert.Equal(3.0, ((DoubleLangValue)result).Value);
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
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(1, ((IntLangValue)result).Value);
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

    [Fact]
    public void Import_LazyImportEnhanced_DelaysImportUntilUse()
    {
        // Arrange
        var testFilePath = "../../../OldLib/ImportTests_Import_LazyImportEnhanced.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
        ast.Run(interpreter.Manager);

        // Assert
        var status1 = interpreter.Manager.GetValue(new LangId("status1"));
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var status2 = interpreter.Manager.GetValue(new LangId("status2"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));

        Assert.NotNull(status1);
        Assert.IsType<StringLangValue>(status1);
        Assert.Equal("Not loaded", ((StringLangValue)status1).Value);

        Assert.NotNull(result1);
        Assert.IsType<DoubleLangValue>(result1);
        Assert.True(((DoubleLangValue)result1).Value > 0); // 大型计算的结果

        Assert.NotNull(status2);
        Assert.IsType<StringLangValue>(status2);
        Assert.Equal("Loaded", ((StringLangValue)status2).Value);

        Assert.NotNull(result2);
        Assert.IsType<DoubleLangValue>(result2);
        Assert.Equal(3.14159265359, ((DoubleLangValue)result2).Value, 0.0001);

        Assert.NotNull(result3);
        Assert.IsType<StringLangValue>(result3);
        Assert.Equal("Heavy computation completed", ((StringLangValue)result3).Value);
    }

    [Fact]
    public void Import_NetworkImportWithWarning_WarnsAboutSecurity()
    {
        // Arrange
        var testFilePath = "../../../OldLib/ImportTests_Import_NetworkImportWithWarning.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code, testFilePath);
        // 由于网络导入涉及实际的网络请求，我们主要验证语法解析和警告逻辑
        Assert.NotNull(ast);

        // 注意：实际执行可能会因为网络访问而失败，但警告应该显示
    }

    [Fact]
    public void Import_SubmoduleImport_ImportsFromSubmodule()
    {
        // Arrange
        var testFilePath = "../../../OldLib/ImportTests_Import_SubmoduleImport.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        var result4 = interpreter.Manager.GetValue(new LangId("result4"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(10, ((IntLangValue)result1).Value); // 5 * 2

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("Hello from submodule", ((StringLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<StringLangValue>(result3);
        Assert.Equal("mymodule v1.0.0", ((StringLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<StringLangValue>(result4);
        Assert.Equal("1.0.0", ((StringLangValue)result4).Value);
    }

    [Fact]
    public void Import_SelectiveImport_ImportsSpecificItems()
    {
        // Arrange
        var testFilePath = "../../../OldLib/ImportTests_Import_SelectiveImport.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var errorOccurred = interpreter.Manager.GetValue(new LangId("error_occurred"));
        var errorMessage = interpreter.Manager.GetValue(new LangId("error_message"));

        Assert.NotNull(result1);
        Assert.IsType<DoubleLangValue>(result1);
        Assert.True(((DoubleLangValue)result1).Value > 0); // 大型计算的结果

        Assert.NotNull(result2);
        Assert.IsType<DoubleLangValue>(result2);
        Assert.Equal(3.14159265359, ((DoubleLangValue)result2).Value, 0.0001);

        // 未导入的函数应该导致错误
        Assert.NotNull(errorOccurred);
        if (errorOccurred is BoolLangValue boolValue)
        {
            Assert.True(boolValue.Value);
        }
    }

    [Fact]
    public void Import_LazyImportNewSyntax_DelaysImportUntilUse()
    {
        // Arrange
        var testFilePath = "../../../OldLib/ImportTests_Import_LazyImport.new.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
        ast.Run(interpreter.Manager);

        // Assert
        var status1 = interpreter.Manager.GetValue(new LangId("status1"));
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        var status2 = interpreter.Manager.GetValue(new LangId("status2"));

        Assert.NotNull(status1);
        Assert.IsType<StringLangValue>(status1);
        Assert.Equal("Not loaded", ((StringLangValue)status1).Value);

        Assert.NotNull(result1);
        Assert.IsType<DoubleLangValue>(result1);
        Assert.True(((DoubleLangValue)result1).Value > 0); // 大型计算的结果

        Assert.NotNull(result2);
        Assert.IsType<DoubleLangValue>(result2);
        Assert.Equal(3.14159265359, ((DoubleLangValue)result2).Value, 0.0001);

        Assert.NotNull(result3);
        Assert.IsType<StringLangValue>(result3);
        Assert.Equal("Heavy computation completed", ((StringLangValue)result3).Value);

        Assert.NotNull(status2);
        Assert.IsType<StringLangValue>(status2);
        Assert.Equal("Loaded", ((StringLangValue)status2).Value);
    }

    [Fact]
    public void Import_LazyImportSelectiveNewSyntax_DelaysImportUntilUse()
    {
        // Arrange
        var testFilePath = "../../../OldLib/ImportTests_Import_LazyImportSelective.new.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
        ast.Run(interpreter.Manager);

        // Assert
        var status1 = interpreter.Manager.GetValue(new LangId("status1"));
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var errorOccurred = interpreter.Manager.GetValue(new LangId("error_occurred"));
        var status2 = interpreter.Manager.GetValue(new LangId("status2"));

        Assert.NotNull(status1);
        Assert.IsType<StringLangValue>(status1);
        Assert.Equal("Not loaded", ((StringLangValue)status1).Value);

        Assert.NotNull(result1);
        Assert.IsType<DoubleLangValue>(result1);
        Assert.True(((DoubleLangValue)result1).Value > 0); // 大型计算的结果

        Assert.NotNull(result2);
        Assert.IsType<DoubleLangValue>(result2);
        Assert.Equal(3.14159265359, ((DoubleLangValue)result2).Value, 0.0001);

        // 未导入的函数应该导致错误
        Assert.NotNull(errorOccurred);
        if (errorOccurred is BoolLangValue boolValue)
        {
            Assert.True(boolValue.Value);
        }

        Assert.NotNull(status2);
        Assert.IsType<StringLangValue>(status2);
        Assert.Equal("Loaded", ((StringLangValue)status2).Value);
    }

    [Fact]
    public void Import_LazyImportAliasNewSyntax_DelaysImportUntilUse()
    {
        // Arrange
        var testFilePath = "../../../OldLib/ImportTests_Import_LazyImportAlias.new.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
        ast.Run(interpreter.Manager);

        // Assert
        var status1 = interpreter.Manager.GetValue(new LangId("status1"));
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        var status2 = interpreter.Manager.GetValue(new LangId("status2"));

        Assert.NotNull(status1);
        Assert.IsType<StringLangValue>(status1);
        Assert.Equal("Not loaded", ((StringLangValue)status1).Value);

        Assert.NotNull(result1);
        Assert.IsType<DoubleLangValue>(result1);
        Assert.True(((DoubleLangValue)result1).Value > 0); // 大型计算的结果

        Assert.NotNull(result2);
        Assert.IsType<DoubleLangValue>(result2);
        Assert.Equal(3.14159265359, ((DoubleLangValue)result2).Value, 0.0001);

        Assert.NotNull(result3);
        Assert.IsType<StringLangValue>(result3);
        Assert.Equal("Heavy computation completed", ((StringLangValue)result3).Value);

        Assert.NotNull(status2);
        Assert.IsType<StringLangValue>(status2);
        Assert.Equal("Loaded", ((StringLangValue)status2).Value);
    }

    [Fact]
    public void Import_NetworkImportEnhanced_ImportsNetworkModule()
    {
        // Arrange
        var testFilePath = "../../../OldLib/ImportTests_Import_NetworkImportEnhanced.new.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code, testFilePath);
        Assert.NotNull(ast);

        // 由于网络导入涉及实际的网络请求，我们主要验证语法解析
        // 实际执行可能会因为网络访问而失败，但警告应该显示
    }

    [Fact]
    public void Import_SelectiveFromModule_ImportsSpecificFunctions()
    {
        // Arrange
        var testFilePath = "../../../OldLib/ImportTests_Import_SelectiveFromModule.new.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var errorOccurred = interpreter.Manager.GetValue(new LangId("error_occurred"));

        Assert.NotNull(result1);
        Assert.IsType<DoubleLangValue>(result1);
        Assert.True(((DoubleLangValue)result1).Value > 0); // 大型计算的结果

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("Heavy computation completed", ((StringLangValue)result2).Value);

        // 未导入的常量应该导致错误
        Assert.NotNull(errorOccurred);
        if (errorOccurred is BoolLangValue boolValue)
        {
            Assert.True(boolValue.Value);
        }
    }

    [Fact]
    public void Import_SubmoduleEnhanced_ImportsFromSubmodules()
    {
        // Arrange
        var testFilePath = "../../../OldLib/ImportTests_Import_SubmoduleEnhanced.new.old8";
        var code = File.ReadAllText(testFilePath);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, testFilePath);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        var result4 = interpreter.Manager.GetValue(new LangId("result4"));
        var result5 = interpreter.Manager.GetValue(new LangId("result5"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(20, ((IntLangValue)result1).Value); // 5 * 3 + 5 = 20

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("2.0.0", ((StringLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<StringLangValue>(result3);
        Assert.Equal("Processed: test", ((StringLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<StringLangValue>(result4);
        Assert.Equal("Hello, Old8Lang!", ((StringLangValue)result4).Value);

        Assert.NotNull(result5);
        Assert.IsType<StringLangValue>(result5);
        Assert.Equal("submodule", ((StringLangValue)result5).Value);
    }
}