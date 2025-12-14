using Old8Lang.AST;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.LangParser;
using System.Reflection.Emit;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 异步函数声明语句
/// 表示 async func 定义
/// </summary>
public class AsyncFuncInit : OldStatement
{
    public readonly AsyncFuncLangValue AsyncFuncValue;

    /// <summary>
    /// 判断是否为 Lambda 表达式
    /// </summary>
    public bool IsLambda => AsyncFuncValue.Id == null;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AsyncFuncInit(AsyncFuncLangValue funcValue, SourcePosition position = default)
        : base(position)
    {
        AsyncFuncValue = funcValue;
    }

    /// <summary>
    /// 解释执行：注册异步函数到变量管理器
    /// </summary>
    public override void Run(VariateManager manager)
    {
        // 检查函数重复声明
        if (AsyncFuncValue.Id != null)
        {
            var existingFunc = manager.ImportInfos.FirstOrDefault(info =>
                info is AsyncFuncLangValue func &&
                func.Id?.IdName == AsyncFuncValue.Id.IdName &&
                func.Ids?.Count == AsyncFuncValue.Ids?.Count);

            if (existingFunc != null)
            {
                throw new DuplicateNameError(
                    this,
                    AsyncFuncValue.Id.IdName,
                    "异步函数"
                );
            }
        }

        // 添加到导入信息列表
        manager.AddClassAndFunc(AsyncFuncValue);
    }

    /// <summary>
    /// 生成 IL 代码（编译器模式暂不支持）
    /// </summary>
    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        throw new NotImplementedError(Position, "编译模式暂不支持异步函数");
    }

    /// <summary>
    /// AST 子节点访问（无子节点）
    /// </summary>
    public override OldStatement? this[int index] => null;

    /// <summary>
    /// AST 子节点数量
    /// </summary>
    public override int Count => 0;
}
