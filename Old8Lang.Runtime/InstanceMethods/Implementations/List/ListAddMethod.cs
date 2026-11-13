using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Add 方法 - 向列表中添加元素
/// </summary>
public class ListAddMethod : BaseInstanceMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["Add", "add"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["item"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    /// <summary>
    /// 参数类型：item 可以是任意类型
    /// </summary>
    public override Type?[]? ParameterTypes => [null]; // null 表示接受任意类型

    /// <summary>
    /// 返回类型：返回添加的元素
    /// </summary>
    public override Type? DeclaredReturnType => typeof(LangValueType);

    /// <summary>
    /// 方法文档
    /// </summary>
    public override string? Documentation => "向列表中添加一个元素";

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var item = parameters[0].Run(manager);
        lock (list.GetSyncRoot())
        {
            list.Values.Add(item);
        }

        return item;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // IL 模式下列表字面量与局部变量实际是原生 List<T>（见 ListLangValue.LoadIlValue），
        // 而不是 ListLangValue。对原生列表必须直接调用列表的 Add，
        // 否则 ldfld ListLangValue.Values 会读错对象布局，运行期直接 AccessViolation。
        var instanceType = instance.OutputType(local);
        if (instanceType is { IsGenericType: true } &&
            instanceType.GetGenericTypeDefinition() == typeof(List<>))
        {
            EmitNativeListAdd(instance, parameters[0], ilGenerator, local, castToList: false);
            return;
        }

        // 顶层未注解变量在 IL 里的静态类型是 object（实际值是 List<object>）。
        // 对这类接收者先 castclass IList 再调用 Add；cast 失败会得到可捕获的
        // InvalidCastException，而不是把 List 对象按 ListLangValue 布局解析导致进程崩溃。
        if (instanceType == typeof(object))
        {
            EmitNativeListAdd(instance, parameters[0], ilGenerator, local, castToList: true);
            return;
        }

        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 获取 Values 字段
        var valuesField = typeof(ListLangValue).GetField("Values");
        ilGenerator.Emit(OpCodes.Ldfld, valuesField!);

        // 加载要添加的元素
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 List<LangValueType>.Add 方法
        var addMethod = typeof(List<LangValueType>).GetMethod("Add");
        ilGenerator.Emit(OpCodes.Callvirt, addMethod!);

        // 返回添加的元素
        parameters[0].LoadIlValue(ilGenerator, local);
    }

    /// <summary>
    /// 为原生列表生成 Add 调用（走非泛型 IList，兼容 List&lt;object&gt; / List&lt;int&gt; 等具体泛型）。
    /// </summary>
    private static void EmitNativeListAdd(LangExpression instance, LangExpression item,
        ILGenerator ilGenerator, LocalManager local, bool castToList)
    {
        instance.LoadIlValue(ilGenerator, local);
        if (castToList)
        {
            ilGenerator.Emit(OpCodes.Castclass, typeof(System.Collections.IList));
        }

        item.LoadIlValue(ilGenerator, local);
        var argumentType = item.OutputType(local);
        if (argumentType is { IsValueType: true })
        {
            ilGenerator.Emit(OpCodes.Box, argumentType);
        }

        var addMethod = typeof(System.Collections.IList).GetMethod("Add", [typeof(object)])!;
        ilGenerator.Emit(OpCodes.Callvirt, addMethod);
        // IList.Add 返回新元素的下标（int），必须丢弃，否则栈上会残留一个值
        ilGenerator.Emit(OpCodes.Pop);

        // 声明返回类型是 LangValueType；原生列表的元素是 int/string 等，
        // 必须包装后再返回，否则调用点按 LangValueType 使用时生成无效 IL。
        item.LoadIlValue(ilGenerator, local);
        if (argumentType is { IsValueType: true })
        {
            ilGenerator.Emit(OpCodes.Box, argumentType);
        }

        ilGenerator.Emit(OpCodes.Call, typeof(IlValueBridge).GetMethod(nameof(IlValueBridge.Wrap))!);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is ListLangValue listLangValue)
        {
            var item = arguments[0] switch
            {
                LangValueType langValue => langValue,
                null => NullLangValue.Instance,
                int i => new IntLangValue(i),
                long l => new IntLangValue((int)l),
                double d => new DoubleLangValue(d),
                bool b => new BoolLangValue(b),
                string s => new StringLangValue(s),
                char c => new CharLangValue(c),
                _ => new StringLangValue(arguments[0]?.ToString() ?? string.Empty)
            };
            lock (listLangValue.GetSyncRoot())
            {
                listLangValue.Values.Add(item);
            }

            return arguments[0];
        }

        if (instance is System.Collections.IList list)
        {
            var item = arguments[0];
            list.Add(item);
            return item;
        }

        throw new ArgumentException("实例必须是 List 类型");
    }
}
