using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Concurrency;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Intermediates;

namespace Old8Lang.GlobalFunctions.Implementations.Concurrency;

/// <summary>
/// AtomicIntCreate 函数 - 创建原子整数
/// </summary>
public sealed class AtomicIntCreateFunction : BaseGlobalFunction
{
    public override string[] Names => ["AtomicIntCreate"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int initialValue = ((IntLangValue)results[0]).Value;
        int id = ResourceManager.CreateAtomicInt(initialValue);
        return new IntLangValue(id);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 initialValue 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.CreateAtomicInt(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.CreateAtomicInt));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }
}

/// <summary>
/// AtomicIntGet 函数 - 获取原子整数的值
/// </summary>
public sealed class AtomicIntGetFunction : BaseGlobalFunction
{
    public override string[] Names => ["AtomicIntGet"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int atomicId = ((IntLangValue)results[0]).Value;
        int value = ResourceManager.GetAtomicInt(atomicId);
        return new IntLangValue(value);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 atomicId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.GetAtomicInt(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.GetAtomicInt));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }
}

/// <summary>
/// AtomicIntSet 函数 - 设置原子整数的值
/// </summary>
public sealed class AtomicIntSetFunction : BaseGlobalFunction
{
    public override string[] Names => ["AtomicIntSet"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int atomicId = ((IntLangValue)results[0]).Value;
        int newValue = ((IntLangValue)results[1]).Value;
        ResourceManager.SetAtomicInt(atomicId, newValue);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 atomicId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 加载 newValue 参数
        parameters[1].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.SetAtomicInt(int, int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.SetAtomicInt));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }
}

/// <summary>
/// AtomicIntIncrement 函数 - 原子自增
/// </summary>
public sealed class AtomicIntIncrementFunction : BaseGlobalFunction
{
    public override string[] Names => ["AtomicIntIncrement"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int atomicId = ((IntLangValue)results[0]).Value;
        int value = ResourceManager.IncrementAtomicInt(atomicId);
        return new IntLangValue(value);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 atomicId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.IncrementAtomicInt(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.IncrementAtomicInt));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }
}

/// <summary>
/// AtomicIntDecrement 函数 - 原子自减
/// </summary>
public sealed class AtomicIntDecrementFunction : BaseGlobalFunction
{
    public override string[] Names => ["AtomicIntDecrement"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int atomicId = ((IntLangValue)results[0]).Value;
        int value = ResourceManager.DecrementAtomicInt(atomicId);
        return new IntLangValue(value);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 atomicId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.DecrementAtomicInt(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.DecrementAtomicInt));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }
}

/// <summary>
/// AtomicIntAdd 函数 - 原子加法
/// </summary>
public sealed class AtomicIntAddFunction : BaseGlobalFunction
{
    public override string[] Names => ["AtomicIntAdd"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int atomicId = ((IntLangValue)results[0]).Value;
        int delta = ((IntLangValue)results[1]).Value;
        int value = ResourceManager.AddAtomicInt(atomicId, delta);
        return new IntLangValue(value);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 atomicId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 加载 delta 参数
        parameters[1].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.AddAtomicInt(int, int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.AddAtomicInt));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }
}

/// <summary>
/// AtomicIntCompareAndSet 函数 - 原子比较并设置（CAS）
/// </summary>
public sealed class AtomicIntCompareAndSetFunction : BaseGlobalFunction
{
    public override string[] Names => ["AtomicIntCompareAndSet"];
    public override int MinParameterCount => 3;
    public override int MaxParameterCount => 3;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int atomicId = ((IntLangValue)results[0]).Value;
        int expectedValue = ((IntLangValue)results[1]).Value;
        int newValue = ((IntLangValue)results[2]).Value;
        bool success = ResourceManager.CompareAndSetAtomicInt(atomicId, expectedValue, newValue);
        return new BoolLangValue(success);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 atomicId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 加载 expectedValue 参数
        parameters[1].LoadIlValue(ilGenerator, local);

        // 加载 newValue 参数
        parameters[2].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.CompareAndSetAtomicInt(int, int, int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.CompareAndSetAtomicInt));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }
}

/// <summary>
/// AtomicIntDispose 函数 - 销毁原子整数
/// </summary>
public sealed class AtomicIntDisposeFunction : BaseGlobalFunction
{
    public override string[] Names => ["AtomicIntDispose"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int atomicId = ((IntLangValue)results[0]).Value;
        ResourceManager.DisposeAtomicInt(atomicId);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 atomicId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.DisposeAtomicInt(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.DisposeAtomicInt));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }
}
