using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Statement;

namespace Old8Lang.Generators;

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
        public List<YieldPoint> YieldPoints { get; set; } = [];

        /// <summary>
        /// 所有局部变量名
        /// </summary>
        public HashSet<string> LocalVariables { get; set; } = [];

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
                result.LocalVariables.Add(setStmt.Id?.IdName ?? "");

                // 递归扫描 SetStatement 的值表达式中可能包含的子语句（如 lambda）
                // 注意：SetStatement 本身不继承 OldStatement 的子语句访问接口
                break;

            case BlockStatement block:
                // 扫描块中的所有语句
                for (int i = 0; i < block.Count; i++)
                {
                    var child = block[i];
                    ScanStatement(child, result, ref yieldCounter, $"{path}/block[{i}]");
                }

                break;

            case ForInStatement forIn:
                // 记录循环变量
                var idField = typeof(ForInStatement).GetFields(
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)
                    .FirstOrDefault(f => f.Name.Contains("id", StringComparison.OrdinalIgnoreCase) &&
                                       f.FieldType == typeof(LangId));

                if (idField is not null && idField.GetValue(forIn) is LangId loopVar)
                {
                    result.LocalVariables.Add(loopVar.IdName);
                }

                // 记录附加变量（用于字典遍历的键值对）
                var additionalIdsField = typeof(ForInStatement).GetFields(
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)
                    .FirstOrDefault(f => f.Name.Contains("additionalIds", StringComparison.OrdinalIgnoreCase));

                if (additionalIdsField is not null && additionalIdsField.GetValue(forIn) is List<LangId> additionalIds)
                {
                    foreach (var additionalId in additionalIds)
                    {
                        result.LocalVariables.Add(additionalId.IdName);
                    }
                }

                // ForInStatement 的循环体需要通过反射访问
                // C# 12 主构造函数参数被编译为私有字段，但字段名不同

                // 打印所有私有字段以查找正确的字段名
                var allFields = typeof(ForInStatement).GetFields(
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                // 尝试查找包含 "body" 的字段
                var bodyField = allFields.FirstOrDefault(f =>
                    f.Name.Contains("body", StringComparison.OrdinalIgnoreCase) &&
                    f.FieldType == typeof(OldStatement));

                if (bodyField is not null)
                {
                    if (bodyField.GetValue(forIn) is OldStatement body)
                    {
                        ScanStatement(body, result, ref yieldCounter, $"{path}/for-in");
                    }
                }
                else
                {
                    // 回退到索引访问（可能不正确，但保持兼容性）
                    if (forIn.Count > 0)
                    {
                        ScanStatement(forIn[0], result, ref yieldCounter, $"{path}/for-in");
                    }
                }

                break;

            case AsyncForInStatement asyncForIn:
                // 记录循环变量
                var asyncIdField = typeof(AsyncForInStatement).GetFields(
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)
                    .FirstOrDefault(f => f.Name.Contains("id", StringComparison.OrdinalIgnoreCase) &&
                                       f.FieldType == typeof(LangId));

                if (asyncIdField is not null && asyncIdField.GetValue(asyncForIn) is LangId asyncLoopVar)
                {
                    result.LocalVariables.Add(asyncLoopVar.IdName);
                }

                // 记录附加变量（用于字典遍历的键值对）
                var asyncAdditionalIdsField = typeof(AsyncForInStatement).GetFields(
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)
                    .FirstOrDefault(f => f.Name.Contains("additionalIds", StringComparison.OrdinalIgnoreCase));

                if (asyncAdditionalIdsField is not null && asyncAdditionalIdsField.GetValue(asyncForIn) is List<LangId> asyncAdditionalIds)
                {
                    foreach (var additionalId in asyncAdditionalIds)
                    {
                        result.LocalVariables.Add(additionalId.IdName);
                    }
                }

                // AsyncForInStatement 的循环体需要通过反射访问
                var asyncAllFields = typeof(AsyncForInStatement).GetFields(
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                // 尝试查找包含 "body" 的字段
                var asyncBodyField = asyncAllFields.FirstOrDefault(f =>
                    f.Name.Contains("body", StringComparison.OrdinalIgnoreCase) &&
                    f.FieldType == typeof(OldStatement));

                if (asyncBodyField is not null)
                {
                    if (asyncBodyField.GetValue(asyncForIn) is OldStatement asyncBody)
                    {
                        ScanStatement(asyncBody, result, ref yieldCounter, $"{path}/async-for-in");
                    }
                }
                else
                {
                    // 回退到索引访问（可能不正确，但保持兼容性）
                    if (asyncForIn.Count > 0)
                    {
                        ScanStatement(asyncForIn[0], result, ref yieldCounter, $"{path}/async-for-in");
                    }
                }

                break;

            case ForStatement forStmt:
                // 扫描初始化、条件、迭代、循环体（使用索引访问）
                for (int i = 0; i < forStmt.Count; i++)
                {
                    var child = forStmt[i];
                    ScanStatement(child, result, ref yieldCounter, $"{path}/for[{i}]");
                }

                break;

            case WhileStatement whileStmt:
                // 扫描循环体（使用索引访问）
                for (int i = 0; i < whileStmt.Count; i++)
                {
                    var child = whileStmt[i];
                    ScanStatement(child, result, ref yieldCounter, $"{path}/while[{i}]");
                }

                break;

            case IfStatement ifStmt:
                // 扫描所有分支
                for (int i = 0; i < ifStmt.Count; i++)
                {
                    var child = ifStmt[i];
                    if (child is not null)
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
                    ScanStatement(child, result, ref yieldCounter, $"{path}/switch[{i}]");
                }

                break;

            case TryStatement tryStmt:
                // 扫描 try、catch、finally 块（使用索引访问）
                for (int i = 0; i < tryStmt.Count; i++)
                {
                    var child = tryStmt[i];
                    ScanStatement(child, result, ref yieldCounter, $"{path}/try[{i}]");
                }

                break;

            default:
                // 其他语句类型可能也包含子语句
                for (int i = 0; i < statement.Count; i++)
                {
                    var child = statement[i];
                    if (child is not null)
                    {
                        ScanStatement(child, result, ref yieldCounter, $"{path}/[{i}]");
                    }
                }

                break;
        }
    }
}