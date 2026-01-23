using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Visitor;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Concurrency;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 虚拟机模式下的线程值类型
/// </summary>
public class VMThreadLangValue(int threadId, SourcePosition position = default) : LangValueType(position)
{
    public int ThreadId => threadId;

    /// <summary>
    /// 启动线程
    /// </summary>
    public void Start()
    {
        ResourceManager.StartThread(threadId);
    }

    /// <summary>
    /// 等待线程完成并获取结果
    /// </summary>
    public LangValueType Join()
    {
        var result = ResourceManager.JoinThread(threadId);
        return ConvertToLangValue(result);
    }

    /// <summary>
    /// 检查线程是否存活
    /// </summary>
    public bool IsAlive()
    {
        return ResourceManager.IsThreadAlive(threadId);
    }

    /// <summary>
    /// 释放线程资源
    /// </summary>
    public void Dispose()
    {
        ResourceManager.DisposeThread(threadId);
    }

    /// <summary>
    /// 处理点操作（方法调用）
    /// </summary>
    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        // 处理方法调用
        if (dotExpression is Instance instance)
        {
            switch (instance.Id.IdName)
            {
                case "Join":
                case "Wait":
                    return Join();

                case "Start":
                    Start();
                    return new VoidLangValue(Position);

                case "IsAlive":
                    return new BoolLangValue(IsAlive());

                case "Dispose":
                    Dispose();
                    return new VoidLangValue(Position);
            }
        }

        // 处理属性访问
        if (dotExpression is LangId langId)
        {
            switch (langId.IdName)
            {
                case "IsAlive":
                    return new BoolLangValue(IsAlive());
                case "ThreadId":
                    return new IntLangValue(threadId);
            }
        }

        // 其他情况调用基类方法
        return base.Dot(dotExpression, manager);
    }

    /// <summary>
    /// 将对象转换为语言值类型
    /// </summary>
    private static LangValueType ConvertToLangValue(object? obj)
    {
        if (obj is null)
            return new VoidLangValue();

        if (obj is LangValueType langValue)
            return langValue;

        return obj switch
        {
            int i => new IntLangValue(i),
            double d => new DoubleLangValue(d),
            string s => new StringLangValue(s),
            bool b => new BoolLangValue(b),
            char c => new CharLangValue(c),
            _ => new VoidLangValue()
        };
    }

    public override Type OutputType(LocalManager local)
    {
        return typeof(VMThreadLangValue);
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        ilGenerator.Emit(OpCodes.Ldarg_0);
    }

    public override void SetValueToIl(ILGenerator ilGenerator, LocalManager local, string idName)
    {
        var localVar = ilGenerator.DeclareLocal(typeof(VMThreadLangValue));
        local.AddLocalVar(idName, localVar);
        ilGenerator.Emit(OpCodes.Stloc, localVar);
    }

    public override object GetValue()
    {
        return this;
    }

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        // VMThreadLangValue 是运行时值，不需要 Visitor 处理
        // 返回默认值
        return default!;
    }
}
