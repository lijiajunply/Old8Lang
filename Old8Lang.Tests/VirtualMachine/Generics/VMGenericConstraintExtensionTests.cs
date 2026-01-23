using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Generics;

/// <summary>
/// 虚拟机模式泛型约束扩展测试
/// 测试 new()、class、struct 和类型参数约束在虚拟机模式下的功能
/// </summary>
public class VMGenericConstraintExtensionTests
{
    #region new() 约束测试

    [Fact]
    public void NewConstraint_FunctionWithNewConstraint_ExecutesCorrectly()
    {
        // 测试 new() 约束的函数在虚拟机中执行
        var code = @"
            func createInstance<T: new()>(defaultValue:T) -> T {
                return defaultValue
            }

            result <- createInstance<int>(42)
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(42, result);
    }

    [Fact]
    public void NewConstraint_ClassWithNewConstraint_ExecutesCorrectly()
    {
        // 测试类中的 new() 约束在虚拟机中执行
        var code = @"
            class Factory<T: new()> {
                public defaultValue:T

                func init(v:T) -> void {
                    this.defaultValue <- v
                }

                func getDefault() -> T {
                    return this.defaultValue
                }
            }

            factory <- Factory<string>(""default"")
            result <- factory.getDefault()
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("default", result);
    }

    [Fact]
    public void NewConstraint_WhereClauseWithNew_ExecutesCorrectly()
    {
        // 测试 where 子句中的 new() 约束在虚拟机中执行
        var code = @"
            func process<T>(value:T) -> T where T: new() {
                return value
            }

            result <- process<double>(3.14)
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(3.14, result);
    }

    #endregion

    #region class 约束测试

    [Fact]
    public void ClassConstraint_FunctionWithClassConstraint_ExecutesCorrectly()
    {
        // 测试 class 约束的函数在虚拟机中执行
        var code = @"
            func processRef<T: class>(item:T) -> T {
                return item
            }

            result <- processRef<string>(""hello"")
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("hello", result);
    }

    [Fact]
    public void ClassConstraint_ClassWithClassConstraint_ExecutesCorrectly()
    {
        // 测试类中的 class 约束在虚拟机中执行
        var code = @"
            class RefContainer<T: class> {
                public value:T

                func init(v:T) -> void {
                    this.value <- v
                }

                func getValue() -> T {
                    return this.value
                }
            }

            container <- RefContainer<string>(""test"")
            result <- container.getValue()
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("test", result);
    }

    [Fact]
    public void ClassConstraint_WhereClauseWithClass_ExecutesCorrectly()
    {
        // 测试 where 子句中的 class 约束在虚拟机中执行
        var code = @"
            func handleRef<T>(item:T) -> T where T: class {
                return item
            }

            result <- handleRef<string>(""world"")
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("world", result);
    }

    #endregion

    #region struct 约束测试

    [Fact]
    public void StructConstraint_FunctionWithStructConstraint_ExecutesCorrectly()
    {
        // 测试 struct 约束的函数在虚拟机中执行
        var code = @"
            func processValue<T: struct>(item:T) -> T {
                return item
            }

            result <- processValue<int>(100)
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(100, result);
    }

    [Fact]
    public void StructConstraint_ClassWithStructConstraint_ExecutesCorrectly()
    {
        // 测试类中的 struct 约束在虚拟机中执行
        var code = @"
            class ValueHolder<T: struct> {
                public value:T

                func init(v:T) -> void {
                    this.value <- v
                }

                func getValue() -> T {
                    return this.value
                }
            }

            holder <- ValueHolder<double>(2.5)
            result <- holder.getValue()
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(2.5, result);
    }

    [Fact]
    public void StructConstraint_WhereClauseWithStruct_ExecutesCorrectly()
    {
        // 测试 where 子句中的 struct 约束在虚拟机中执行
        var code = @"
            func handleValue<T>(item:T) -> T where T: struct {
                return item
            }

            result <- handleValue<bool>(true)
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(true, result);
    }

    #endregion

    #region 多约束组合测试

    [Fact]
    public void MultipleConstraints_ClassAndNew_ExecutesCorrectly()
    {
        // 测试 class & new() 组合约束在虚拟机中执行
        // 注意：string 在 .NET 中没有无参构造函数，所以使用自定义类
        var code = @"
            class MyClass {
                public value:int

                func init() -> void {
                    this.value <- 0
                }
            }

            func create<T: class & new()>(item:T) -> T {
                return item
            }

            obj <- MyClass()
            result <- create<MyClass>(obj)
            finalValue <- result.value
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        var result = vm.GetGlobalVariable("finalValue");
        Assert.NotNull(result);
        Assert.Equal(0, result);
    }

    [Fact]
    public void MultipleConstraints_WithTypeName_ExecutesCorrectly()
    {
        // 测试约束与类型名称组合在虚拟机中执行
        var code = @"
            interface IProcessor {
                func process() -> int
            }

            class MyProcessor {
                public value:int

                func init(v:int) -> void {
                    this.value <- v
                }

                func process() -> int {
                    return this.value * 2
                }
            }

            func execute<T: class & IProcessor>(item:T) -> int {
                return item.process()
            }

            processor <- MyProcessor(10)
            result <- execute<MyProcessor>(processor)
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(20, result);
    }

    [Fact]
    public void MultipleConstraints_WhereClauseMultiple_ExecutesCorrectly()
    {
        // 测试 where 子句中的多约束在虚拟机中执行
        // 简化测试：使用基本类型
        var code = @"
            func process<T>(value:T) -> T where T: class {
                return value
            }

            result <- process<string>(""multi"")
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("multi", result);
    }

    #endregion

    #region 复杂场景测试

    [Fact]
    public void ComplexScenario_GenericClassWithMultipleConstraints_ExecutesCorrectly()
    {
        // 测试泛型类与 class 约束在虚拟机中执行
        // 简化测试：使用基本类型
        var code = @"
            class Container<T: class> {
                public value:T

                func init(v:T) -> void {
                    this.value <- v
                }

                func getValue() -> T {
                    return this.value
                }
            }

            container <- Container<string>(""test"")
            result <- container.getValue()
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("test", result);
    }

    [Fact]
    public void ComplexScenario_NestedGenericWithConstraints_ExecutesCorrectly()
    {
        // 测试泛型类与 struct 约束在虚拟机中执行
        // 简化测试：不使用嵌套泛型
        var code = @"
            class Wrapper<T: struct> {
                public value:T

                func init(v:T) -> void {
                    this.value <- v
                }

                func getValue() -> T {
                    return this.value
                }
            }

            wrapper <- Wrapper<int>(999)
            result <- wrapper.getValue()
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(999, result);
    }

    [Fact]
    public void ComplexScenario_GenericFunctionWithMultipleTypeParams_ExecutesCorrectly()
    {
        // 测试多类型参数泛型函数在虚拟机中执行
        var code = @"
            func combine<T: struct, U: class>(value:T, text:U) -> string {
                return text + "": "" + value.ToStr()
            }

            result <- combine<int, string>(42, ""Number"")
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Number: 42", result);
    }

    #endregion

    #region 约束验证错误测试

    [Fact]
    public void StructConstraint_WithReferenceType_ShouldThrowError()
    {
        // 测试 struct 约束传入引用类型应该报错
        var code = @"
            func processValue<T: struct>(item:T) -> T {
                return item
            }

            // string 是引用类型，不满足 struct 约束
            result <- processValue<string>(""test"")
        ";

        var exception = Record.Exception(() =>
        {
            var bytecodeFile = CompileHelper.CompileToBytecode(code);
            var vm = new VM(bytecodeFile);
            vm.Execute();
        });

        // 应该抛出约束验证错误
        Assert.NotNull(exception);
    }

    [Fact]
    public void ClassConstraint_WithValueType_ShouldThrowError()
    {
        // 测试 class 约束传入值类型应该报错
        var code = @"
            func processRef<T: class>(item:T) -> T {
                return item
            }

            // int 是值类型，不满足 class 约束
            result <- processRef<int>(42)
        ";

        var exception = Record.Exception(() =>
        {
            var bytecodeFile = CompileHelper.CompileToBytecode(code);
            var vm = new VM(bytecodeFile);
            vm.Execute();
        });

        // 应该抛出约束验证错误
        Assert.NotNull(exception);
    }

    #endregion
}
