using System.Collections;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.Bytecode.VM;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Bytecode.Generators;

/// <summary>
/// 生成器对象（字节码虚拟机模式）
/// 表示一个可以暂停和恢复执行的生成器函数
/// </summary>
public class BytecodeGeneratorLangValue : LangValueType, IEnumerable<LangValueType>
{
    /// <summary>生成器ID</summary>
    public int GeneratorId { get; }

    /// <summary>虚拟机引用</summary>
    private readonly VirtualMachine _vm;

    /// <summary>当前值</summary>
    public LangValueType? Current { get; private set; }

    /// <summary>生成器是否已完成</summary>
    public bool IsCompleted { get; private set; }

    public BytecodeGeneratorLangValue(int generatorId, VirtualMachine vm, SourcePosition position = default)
        : base(position)
    {
        GeneratorId = generatorId;
        _vm = vm;
        IsCompleted = false;
    }

    /// <summary>
    /// 推进生成器到下一个yield点
    /// </summary>
    /// <returns>如果成功yield返回true，如果生成器已完成返回false</returns>
    public bool MoveNext()
    {
        if (IsCompleted)
            throw new Exception("生成器已完成，无法继续调用 MoveNext");

        // 调用虚拟机的ResumeGenerator方法来恢复生成器执行
        var result = _vm.ResumeGenerator(GeneratorId);

        if (result != null)
        {
            Current = result;
            return true;
        }
        else
        {
            IsCompleted = true;
            Current = null;
            // 生成器已完成，抛出异常
            throw new Exception("生成器已完成，无法继续调用 MoveNext");
        }
    }

    public override string ToString() => $"Generator({GeneratorId})";

    public override TResult Accept<TResult>(AST.Visitor.IVisitor<TResult> visitor)
    {
        // GeneratorLangValue 是运行时值，不参与 Visitor 遍历
        throw new NotSupportedException("GeneratorLangValue 不支持 Visitor 模式遍历");
    }

    public override object GetValue() => this;

    public override Type OutputType(LocalManager local) => typeof(BytecodeGeneratorLangValue);

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        throw new NotSupportedException("生成器对象不支持IL代码生成");
    }

    // IEnumerable实现，支持for-in循环
    public IEnumerator<LangValueType> GetEnumerator()
    {
        while (MoveNext())
        {
            if (Current != null)
                yield return Current;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // 不支持的操作
    public override LangValueType Plus(LangValueType otherLangValueType)
        => throw new InvalidOperationError(this, "生成器对象不支持加法操作");

    public override LangValueType Minus(LangValueType otherLangValueType)
        => throw new InvalidOperationError(this, "生成器对象不支持减法操作");

    public override LangValueType Times(LangValueType otherLangValueType)
        => throw new InvalidOperationError(this, "生成器对象不支持乘法操作");

    public override LangValueType Divide(LangValueType otherLangValueType)
        => throw new InvalidOperationError(this, "生成器对象不支持除法操作");

    public override LangValueType Mod(LangValueType otherLangValueType)
        => throw new InvalidOperationError(this, "生成器对象不支持取模操作");

    public override LangValueType Power(LangValueType otherLangValueType)
        => throw new InvalidOperationError(this, "生成器对象不支持幂运算");

    public override bool Less(LangValueType? otherValue)
        => throw new InvalidOperationError(this, "生成器对象不支持比较操作");

    public override bool Greater(LangValueType? otherValue)
        => throw new InvalidOperationError(this, "生成器对象不支持比较操作");

    public override bool LessEqual(LangValueType? otherValue)
        => throw new InvalidOperationError(this, "生成器对象不支持比较操作");

    public override bool GreaterEqual(LangValueType? otherValue)
        => throw new InvalidOperationError(this, "生成器对象不支持比较操作");

    public override bool Equal(LangValueType? otherValueType)
        => otherValueType is BytecodeGeneratorLangValue other && GeneratorId == other.GeneratorId;

    public override LangValueType Converse(LangValueType otherLangValueType, VariateManager manager)
        => throw new TypeError(this, "生成器对象不支持类型转换");
}
