using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Visitor;
using Old8Lang.Compiler;
using Old8Lang.Concurrency;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

/// <summary>
/// Select语句中的单个case
/// </summary>
public class SelectCase
{
    /// <summary>
    /// 是否为接收操作（true: 接收, false: 发送）
    /// </summary>
    public bool IsReceive { get; }

    /// <summary>
    /// Channel表达式
    /// </summary>
    public LangExpression ChannelExpression { get; }

    /// <summary>
    /// 接收时的变量名（可选）
    /// </summary>
    public string? VariableName { get; }

    /// <summary>
    /// 发送时的值表达式（可选）
    /// </summary>
    public LangExpression? SendValueExpression { get; }

    /// <summary>
    /// case块中的语句
    /// </summary>
    public BlockStatement BlockStatement { get; }

    /// <summary>
    /// 源代码位置
    /// </summary>
    public SourcePosition Position { get; }

    /// <summary>
    /// 构造接收case：value <- channel
    /// </summary>
    public SelectCase(
        LangExpression channelExpression,
        string? variableName,
        BlockStatement blockStatement,
        SourcePosition position)
    {
        IsReceive = true;
        ChannelExpression = channelExpression;
        VariableName = variableName;
        SendValueExpression = null;
        BlockStatement = blockStatement;
        Position = position;
    }

    /// <summary>
    /// 构造发送case：channel <- value
    /// </summary>
    public SelectCase(
        LangExpression channelExpression,
        LangExpression sendValueExpression,
        BlockStatement blockStatement,
        SourcePosition position)
    {
        IsReceive = false;
        ChannelExpression = channelExpression;
        VariableName = null;
        SendValueExpression = sendValueExpression;
        BlockStatement = blockStatement;
        Position = position;
    }
}

/// <summary>
/// Select语句，用于Channel多路选择（类似Go的select）
/// </summary>
public partial class SelectStatement(
    List<SelectCase> cases,
    BlockStatement? defaultCase,
    SourcePosition position = default) : OldStatement(position)
{
    /// <summary>
    /// 获取所有case分支
    /// </summary>
    public List<SelectCase> Cases => cases;

    /// <summary>
    /// 获取default分支（可选）
    /// </summary>
    public BlockStatement? DefaultCase => defaultCase;

    public override void Run(VariateManager manager)
    {
        // 检查是否在生成器上下文中
        if (manager.GeneratorContext is not null)
        {
            RunWithGeneratorContext(manager);
        }
        else
        {
            RunStandard(manager);
        }
    }

    /// <summary>
    /// 标准模式执行（非生成器）
    /// </summary>
    private void RunStandard(VariateManager manager)
    {
        // 使用轮询策略实现多路选择
        while (true)
        {
            // 检查每个case
            foreach (var selectCase in cases)
            {
                if (selectCase.IsReceive)
                {
                    // 接收case：尝试非阻塞接收
                    int channelId;

                    // 检查 ChannelExpression 是否为 ChannelReceive 函数调用
                    if (selectCase.ChannelExpression is Instance funcCall &&
                        funcCall.Id.IdName == "ChannelReceive" &&
                        funcCall.Ids.Count > 0)
                    {
                        // 提取实际的 channel ID 参数
                        var channelIdValue = funcCall.Ids[0].Run(manager);
                        if (channelIdValue is not IntLangValue intVal)
                        {
                            continue;
                        }
                        channelId = intVal.Value;
                    }
                    else
                    {
                        // 直接执行 ChannelExpression 获取 channel ID
                        var channelIdValue = selectCase.ChannelExpression.Run(manager);
                        if (channelIdValue is not IntLangValue intVal)
                        {
                            continue;
                        }
                        channelId = intVal.Value;
                    }

                    var receiveResult = ResourceManager.TryReceiveChannel(channelId, 0);
                    if (receiveResult.Success)
                    {
                        // 接收成功
                        if (selectCase.VariableName is not null)
                        {
                            manager.Set(new LangId(selectCase.VariableName), LangValueType.ObjToValue(receiveResult.Value));
                        }

                        selectCase.BlockStatement.Run(manager);
                        return;
                    }
                }
                else
                {
                    // 发送case：尝试非阻塞发送
                    var channelIdValue = selectCase.ChannelExpression.Run(manager);
                    if (channelIdValue is IntLangValue intVal)
                    {
                        int channelId = intVal.Value;
                        var sendValue = selectCase.SendValueExpression!.Run(manager);
                        if (ResourceManager.TrySendChannel(channelId, sendValue.GetValue(), 0))
                        {
                            // 发送成功
                            selectCase.BlockStatement.Run(manager);
                            return;
                        }
                    }
                }
            }

            // 如果有default分支，立即执行
            if (defaultCase is not null)
            {
                defaultCase.Run(manager);
                return;
            }

            // 短暂休眠避免CPU 100%占用
            Thread.Sleep(1);
        }
    }

    /// <summary>
    /// 生成器上下文模式执行
    /// </summary>
    private void RunWithGeneratorContext(VariateManager manager)
    {
        var context = manager.GeneratorContext!;

        // 检查是否从select块内恢复
        bool isResumingFromSelectBlock = !string.IsNullOrEmpty(context.ExecutionPath) &&
                                          context.ExecutionPath.Contains("/select_block");

        if (isResumingFromSelectBlock)
        {
            // 从select块内恢复，继续执行
            RunStandard(manager);
        }
        else
        {
            // 首次进入select语句
            var oldPath = context.ExecutionPath;
            context.ExecutionPath = $"{oldPath}/select_block";

            try
            {
                RunStandard(manager);
            }
            finally
            {
                // 恢复执行路径
                context.ExecutionPath = oldPath;
            }
        }
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 实现 select 语句的轮询策略：
        // 1. 无限循环检查所有 case
        // 2. 如果任意 case 就绪，执行对应块并退出
        // 3. 如果有 default 且所有 case 未就绪，执行 default 并退出
        // 4. 否则短暂休眠后继续循环

        var loopStart = ilGenerator.DefineLabel();      // 轮询循环开始
        var loopEnd = ilGenerator.DefineLabel();        // 轮询循环结束
        var defaultLabel = ilGenerator.DefineLabel();   // default 分支
        var sleepLabel = ilGenerator.DefineLabel();     // 休眠逻辑

        // 循环开始
        ilGenerator.MarkLabel(loopStart);

        // 遍历所有 case，尝试非阻塞操作
        foreach (var selectCase in cases)
        {
            var caseBlockEnd = ilGenerator.DefineLabel();

            if (selectCase.IsReceive)
            {
                // 接收 case: 尝试非阻塞接收
                // var receiveResult = ResourceManager.TryReceiveChannel(channelId, 0);
                // if (receiveResult.Success) { 执行块; return; }

                // 加载 channelId
                selectCase.ChannelExpression.LoadIlValue(ilGenerator, local);

                // 加载超时时间 0（非阻塞）
                ilGenerator.Emit(OpCodes.Ldc_I4_0);

                // 调用 ResourceManager.TryReceiveChannel(int, int)
                var tryReceiveMethod = typeof(ResourceManager).GetMethod(
                    nameof(ResourceManager.TryReceiveChannel),
                    new[] { typeof(int), typeof(int) });
                ilGenerator.Emit(OpCodes.Call, tryReceiveMethod);

                // 结果存储到临时局部变量
                var receiveResultLocal = ilGenerator.DeclareLocal(typeof(ChannelReceiveResult));
                ilGenerator.Emit(OpCodes.Stloc, receiveResultLocal);

                // 检查 receiveResult.Success
                ilGenerator.Emit(OpCodes.Ldloc, receiveResultLocal);
                var successProperty = typeof(ChannelReceiveResult).GetProperty(nameof(ChannelReceiveResult.Success));
                ilGenerator.Emit(OpCodes.Callvirt, successProperty.GetMethod);

                // 如果 Success == false，跳过此 case
                ilGenerator.Emit(OpCodes.Brfalse, caseBlockEnd);

                // Success == true: 设置变量（如果有）并执行块
                if (selectCase.VariableName is not null)
                {
                    // 获取 receiveResult.Value
                    ilGenerator.Emit(OpCodes.Ldloc, receiveResultLocal);
                    var valueProperty = typeof(ChannelReceiveResult).GetProperty(nameof(ChannelReceiveResult.Value));
                    ilGenerator.Emit(OpCodes.Callvirt, valueProperty.GetMethod);

                    // 将值转换为 LangValueType
                    var objToValueMethod = typeof(LangValueType).GetMethod(
                        nameof(LangValueType.ObjToValue),
                        new[] { typeof(object) });
                    ilGenerator.Emit(OpCodes.Call, objToValueMethod);

                    // 存储到变量（获取或创建局部变量）
                    var varLocal = local.GetOrCreateLocalVar(ilGenerator, selectCase.VariableName, typeof(LangValueType));
                    ilGenerator.Emit(OpCodes.Stloc, varLocal);
                }

                // 执行 case 块
                selectCase.BlockStatement.GenerateIl(ilGenerator, local);

                // 跳转到循环结束（退出 select）
                ilGenerator.Emit(OpCodes.Br, loopEnd);
            }
            else
            {
                // 发送 case: 尝试非阻塞发送
                // if (ResourceManager.TrySendChannel(channelId, value, 0)) { 执行块; return; }

                // 加载 channelId
                selectCase.ChannelExpression.LoadIlValue(ilGenerator, local);

                // 加载要发送的值
                selectCase.SendValueExpression!.LoadIlValue(ilGenerator, local);
                var sendValueType = selectCase.SendValueExpression.OutputType(local);
                if (sendValueType.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Box, sendValueType);
                }

                // 加载超时时间 0（非阻塞）
                ilGenerator.Emit(OpCodes.Ldc_I4_0);

                // 调用 ResourceManager.TrySendChannel(int, object, int)
                var trySendMethod = typeof(ResourceManager).GetMethod(
                    nameof(ResourceManager.TrySendChannel),
                    new[] { typeof(int), typeof(object), typeof(int) });
                ilGenerator.Emit(OpCodes.Call, trySendMethod);

                // 如果返回 false，跳过此 case
                ilGenerator.Emit(OpCodes.Brfalse, caseBlockEnd);

                // 返回 true: 执行 case 块
                selectCase.BlockStatement.GenerateIl(ilGenerator, local);

                // 跳转到循环结束（退出 select）
                ilGenerator.Emit(OpCodes.Br, loopEnd);
            }

            // case 块结束标签
            ilGenerator.MarkLabel(caseBlockEnd);
        }

        // 所有 case 都未就绪
        if (defaultCase is not null)
        {
            // 有 default 分支：执行 default 并退出
            ilGenerator.MarkLabel(defaultLabel);
            defaultCase.GenerateIl(ilGenerator, local);
            ilGenerator.Emit(OpCodes.Br, loopEnd);
        }
        else
        {
            // 无 default 分支：休眠 1ms 后继续轮询
            ilGenerator.MarkLabel(sleepLabel);

            // Thread.Sleep(1)
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            var sleepMethod = typeof(Thread).GetMethod(
                nameof(Thread.Sleep),
                new[] { typeof(int) });
            ilGenerator.Emit(OpCodes.Call, sleepMethod);

            // 继续循环
            ilGenerator.Emit(OpCodes.Br, loopStart);
        }

        // 循环结束标签
        ilGenerator.MarkLabel(loopEnd);
    }

    public override OldStatement this[int index]
    {
        get
        {
            // Select 语句包含所有case块和default块
            if (index >= 0 && index < cases.Count)
            {
                return cases[index].BlockStatement;
            }

            if (defaultCase is not null && index == cases.Count)
            {
                return defaultCase;
            }

            // 超出范围，返回空语句
            return new BlockStatement(new List<IOldLangTree>());
        }
    }

    public override int Count
    {
        get
        {
            // case数量 + default块（如果有）
            return cases.Count + (defaultCase is not null ? 1 : 0);
        }
    }
}
