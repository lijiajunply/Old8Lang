using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Statement;

namespace Old8Lang.Interpreter;

/// <summary>
/// 生成器 AST 扫描器
/// 分析函数体，识别所有 yield 点和局部变量
/// </summary>
public class GeneratorAstScanner
{
    /// <summary>
    /// Yield 点信息
    /// </summary>
    public class YieldPoint
    {
        /// <summary>
        /// Yield 语句
        /// </summary>
        public YieldStatement Statement { get; set; } = null!;

        /// <summary>
        /// 状态点 ID（分配的唯一标识）
        /// </summary>
        public int StateId { get; set; }

        /// <summary>
        /// 在 AST 中的路径（用于调试）
        /// </summary>
        public string Path { get; set; } = "";
    }

    /// <summary>
    /// 扫描结果
    /// </summary>
    public class ScanResult
    {
        /// <summary>
        /// 所有 yield 点
        /// </summary>
        public List<YieldPoint> YieldPoints { get; set; } = new();

        /// <summary>
        /// 所有局部变量名
        /// </summary>
        public HashSet<string> LocalVariables { get; set; } = new();

        /// <summary>
        /// 是否包含 yield 语句
        /// </summary>
        public bool IsGenerator => YieldPoints.Count > 0;
    }

    /// <summary>
    /// 扫描函数体
    /// </summary>
    /// <param name="functionBody">函数体语句</param>
    /// <returns>扫描结果</returns>
    public ScanResult Scan(OldStatement functionBody)
    {
        var result = new ScanResult();
        var yieldCounter = 0;

        ScanStatement(functionBody, result, ref yieldCounter, "");

        return result;
    }

    /// <summary>
    /// 递归扫描语句
    /// </summary>
    private void ScanStatement(OldStatement statement, ScanResult result, ref int yieldCounter, string path)
    {
        switch (statement)
        {
            case YieldStatement yieldStmt:
                // 找到 yield 语句
                result.YieldPoints.Add(new YieldPoint
                {
                    Statement = yieldStmt,
                    StateId = yieldCounter++,
                    Path = path + "/yield"
                });
                break;

            case SetStatement setStmt:
                // 记录局部变量
                result.LocalVariables.Add(setStmt.Id.IdName);
                break;

            case BlockStatement block:
                // 扫描块中的所有语句
                for (int i = 0; i < block.Count; i++)
                {
                    var child = block[i];
                    if (child != null)
                    {
                        ScanStatement(child, result, ref yieldCounter, $"{path}/block[{i}]");
                    }
                }
                break;

            case ForInStatement forIn:
                // ForInStatement 的成员通过索引访问
                // 扫描循环体（body 是第一个子语句）
                if (forIn.Count > 0 && forIn[0] != null)
                {
                    ScanStatement(forIn[0], result, ref yieldCounter, $"{path}/for-in");
                }
                break;

            case ForStatement forStmt:
                // 扫描初始化、条件、迭代、循环体（使用索引访问）
                for (int i = 0; i < forStmt.Count; i++)
                {
                    var child = forStmt[i];
                    if (child != null)
                    {
                        ScanStatement(child, result, ref yieldCounter, $"{path}/for[{i}]");
                    }
                }
                break;

            case WhileStatement whileStmt:
                // 扫描循环体（使用索引访问）
                for (int i = 0; i < whileStmt.Count; i++)
                {
                    var child = whileStmt[i];
                    if (child != null)
                    {
                        ScanStatement(child, result, ref yieldCounter, $"{path}/while[{i}]");
                    }
                }
                break;

            case IfStatement ifStmt:
                // 扫描所有分支
                for (int i = 0; i < ifStmt.Count; i++)
                {
                    var child = ifStmt[i];
                    if (child != null)
                    {
                        ScanStatement(child, result, ref yieldCounter, $"{path}/if[{i}]");
                    }
                }
                break;

            case SwitchStatement switchStmt:
                // 扫描所有 case（使用索引访问）
                for (int i = 0; i < switchStmt.Count; i++)
                {
                    var child = switchStmt[i];
                    if (child != null)
                    {
                        ScanStatement(child, result, ref yieldCounter, $"{path}/switch[{i}]");
                    }
                }
                break;

            case TryStatement tryStmt:
                // 扫描 try、catch、finally 块（使用索引访问）
                for (int i = 0; i < tryStmt.Count; i++)
                {
                    var child = tryStmt[i];
                    if (child != null)
                    {
                        ScanStatement(child, result, ref yieldCounter, $"{path}/try[{i}]");
                    }
                }
                break;

            default:
                // 其他语句类型可能也包含子语句
                for (int i = 0; i < statement.Count; i++)
                {
                    var child = statement[i];
                    if (child != null)
                    {
                        ScanStatement(child, result, ref yieldCounter, $"{path}/[{i}]");
                    }
                }
                break;
        }
    }
}
