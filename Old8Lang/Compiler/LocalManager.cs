using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

namespace Old8Lang.Compiler;

/// <summary>
/// 局部变量管理器，用于在编译过程中管理局部变量、委托、类和字段
/// </summary>
/// <remarks>
/// 该类是编译器生成IL代码时的重要组件，主要负责：
/// - 管理局部变量的创建、访问和移除
/// - 管理委托、类和字段的信息
/// - 提供类型兼容性验证
/// - 支持break和continue标签的管理
/// - 提供调试信息记录和错误报告功能
/// - 支持状态克隆和恢复
/// </remarks>
public class LocalManager
{
    /// <summary>
    /// 局部变量字典，键为变量名，值为LocalBuilder实例
    /// </summary>
    public readonly Dictionary<string, LocalBuilder> LocalVar = [];
    
    /// <summary>
    /// 委托方法字典，键为委托名，值为MethodInfo实例
    /// </summary>
    public readonly Dictionary<string, MethodInfo> DelegateVar = [];
    
    /// <summary>
    /// 类类型字典，键为类名，值为Type实例
    /// </summary>
    public readonly Dictionary<string, Type> ClassVar = [];
    
    /// <summary>
    /// 字段信息字典，键为字段名，值为FieldInfo实例
    /// </summary>
    public readonly Dictionary<string, FieldInfo> FieldVar = [];

    /// <summary>
    /// 局部变量类型字典，键为变量名，值为Type实例
    /// </summary>
    public readonly Dictionary<string, Type> LocalVarTypes = [];

    /// <summary>
    /// 存储函数的参数列表信息（用于支持默认参数）
    /// </summary>
    public readonly Dictionary<string, List<LangId>> FuncParameters = [];

    /// <summary>
    /// 泛型函数特化方法缓存
    /// 键为 "函数名$类型参数1_类型参数2"，值为特化后的MethodInfo
    /// </summary>
    public readonly Dictionary<string, MethodInfo> GenericSpecializations = [];

    /// <summary>
    /// 泛型函数定义缓存，键为函数名，值为FuncLangValue
    /// 用于在运行时创建特化版本
    /// </summary>
    public readonly Dictionary<string, FuncLangValue> GenericFunctions = [];

    /// <summary>
    /// 当前泛型类型解析器，用于在特化方法生成时解析泛型参数
    /// </summary>
    public GenericTypeResolver? CurrentGenericTypeResolver { get; set; }

    /// <summary>
    /// 全局静态类实例字典，键为类名，值为静态类实例
    /// </summary>
    public readonly Dictionary<string, LangValueType> GlobalStaticClasses = [];

    /// <summary>
    /// 当前所在的类环境类型
    /// </summary>
    public Type? InClassEnv { get; init; }
    
    /// <summary>
    /// 当前源代码文件路径
    /// </summary>
    public string FilePath { get; set; } = "";
    
    /// <summary>
    /// 关联的解释器实例
    /// </summary>
    public LangInterpreter? Interpreter { get; init; }
    
    /// <summary>
    /// 动态程序集
    /// </summary>
    public AssemblyBuilder? DynamicAssembly { get; set; }
    
    /// <summary>
    /// 动态模块
    /// </summary>
    public ModuleBuilder? DynamicModule { get; set; }

    /// <summary>
    /// break语句的目标标签
    /// </summary>
    public Label? BreakLabel { get; set; }
    
    /// <summary>
    /// continue语句的目标标签
    /// </summary>
    public Label? ContinueLabel { get; set; }

    /// <summary>
    /// 标记是否在finally块中生成IL代码
    /// </summary>
    public bool IsInFinallyBlock { get; set; }

    /// <summary>
    /// 函数返回值的局部变量（用于defer支持）
    /// </summary>
    /// <remarks>
    /// 当函数使用defer时，return语句会将返回值存储到这个局部变量
    /// 而不是直接返回，以便在finally块中执行defer
    /// </remarks>
    public LocalBuilder? ReturnValueLocal { get; set; }

    /// <summary>
    /// 函数结束标签（用于defer支持）
    /// </summary>
    /// <remarks>
    /// 当函数使用defer时，return语句会跳转到这个标签
    /// 而不是直接ret，以便在finally块中执行defer
    /// </remarks>
    public Label? ReturnLabel { get; set; }

    /// <summary>
    /// defer语句栈，用于延迟执行（后进先出LIFO）
    /// </summary>
    /// <remarks>
    /// defer语句在函数返回前执行，多个defer按后进先出顺序执行
    /// 编译器需要在函数结束前生成对应的IL代码
    /// </remarks>
    private readonly Stack<OldStatement> DeferStack = new();

    /// <summary>
    /// 注册一个defer语句
    /// </summary>
    /// <param name="statement">要延迟执行的语句</param>
    public void RegisterDefer(OldStatement statement)
    {
        DeferStack.Push(statement);
    }

    /// <summary>
    /// 生成所有defer语句的IL代码（按后进先出顺序）
    /// </summary>
    /// <param name="ilGenerator">IL指令生成器</param>
    public void GenerateDeferIL(ILGenerator ilGenerator)
    {
        // 将defer语句从栈中取出，按后进先出顺序生成IL
        var defers = DeferStack.ToList(); // ToList会保持栈的顺序（先进的在后面）
        foreach (var deferStatement in defers)
        {
            // 为每个defer生成try-catch包装，确保异常不会影响其他defer的执行
            var exceptionLocal = ilGenerator.DeclareLocal(typeof(Exception));
            ilGenerator.BeginExceptionBlock();

            // 生成defer语句的IL代码
            deferStatement.GenerateIl(ilGenerator, this);

            // catch块：捕获但不处理异常（符合defer语义）
            ilGenerator.BeginCatchBlock(typeof(Exception));
            ilGenerator.Emit(OpCodes.Stloc, exceptionLocal);
            // 异常被捕获，继续执行后续defer

            ilGenerator.EndExceptionBlock();
        }
    }

    /// <summary>
    /// 是否启用严格类型检查
    /// </summary>
    /// <remarks>
    /// 当设置为 false (默认值) 时，编译器允许变量类型改变（动态类型行为）。
    /// 当设置为 true 时，编译器强制要求变量类型在整个生命周期内保持不变（静态类型行为）。
    /// </remarks>
    public bool StrictTypeChecking { get; set; } = false;

    /// <summary>
    /// 记录调试信息
    /// </summary>
    /// <param name="message">调试信息内容</param>
    /// <param name="position">源代码位置</param>
    public void LogDebug(string message, SourcePosition position)
    {
        Console.WriteLine($"[DEBUG] {FilePath}:{position.Line}:{position.Column} - {message}");
    }

    /// <summary>
    /// 报告编译错误
    /// </summary>
    /// <param name="message">错误信息</param>
    /// <param name="position">源代码位置</param>
    /// <exception cref="CompilerException">编译异常</exception>
    public void ReportError(string message, SourcePosition position)
    {
        // 构建详细的错误信息
        var errorBuilder = new StringBuilder();
        
        // 错误位置信息
        errorBuilder.AppendLine($"错误位置: {FilePath}:{position.Line}:{position.Column}");
        errorBuilder.AppendLine(new string('-', 60));
        
        // 错误详情
        errorBuilder.AppendLine($"错误信息: {message}");
        errorBuilder.AppendLine(new string('-', 60));
        
        // 通用编译模式提示
        errorBuilder.AppendLine("编译模式下的类型检查规则：");
        errorBuilder.AppendLine("1. 所有函数必须显式声明返回类型");
        errorBuilder.AppendLine("2. 所有函数参数必须有类型注解或默认值");
        errorBuilder.AppendLine("3. 变量赋值必须保持类型一致");
        errorBuilder.AppendLine("4. 函数调用的参数类型必须与声明匹配");
        errorBuilder.AppendLine("5. 支持的类型：int, double, string, bool, char, void, list<T>, array<T>, dictionary<K,V>");
        errorBuilder.AppendLine(new string('-', 60));
        
        // 通用修复建议
        errorBuilder.AppendLine("通用修复建议：");
        errorBuilder.AppendLine("- 确保所有变量声明都有类型注解");
        errorBuilder.AppendLine("- 检查赋值语句的类型一致性");
        errorBuilder.AppendLine("- 验证函数调用的参数类型");
        errorBuilder.AppendLine("- 确保函数返回类型与return语句匹配");
        errorBuilder.AppendLine(new string('-', 60));
        
        var errorMessage = errorBuilder.ToString();
        throw new CompilerException(errorMessage, position);
    }

    /// <summary>
    /// 验证类型兼容性
    /// </summary>
    /// <param name="expected">预期类型</param>
    /// <param name="actual">实际类型</param>
    /// <param name="position">源代码位置</param>
    /// <returns>如果类型兼容返回true，否则报告错误</returns>
    public bool ValidateType(Type? expected, Type? actual, SourcePosition position)
    {
        if (expected == null || actual == null)
        {
            ReportError("类型无效: 预期类型或实际类型为null", position);
            return false;
        }

        // 类型完全匹配
        if (expected == actual)
            return true;

        // 如果不是严格类型检查模式，允许任何类型改变（动态类型行为）
        if (!StrictTypeChecking)
            return true;

        // 严格模式下进行详细的类型兼容性检查

        // 可赋值类型检查
        if (expected.IsAssignableFrom(actual))
            return true;

        // 基本类型转换检查
        if (IsBasicTypeConversionAllowed(expected, actual))
            return true;

        // 集合类型兼容性检查
        if (IsCollectionTypeCompatible(expected, actual))
            return true;

        ReportError($"类型不兼容: 预期 {expected.Name}, 实际 {actual.Name}", position);
        return false;
    }

    /// <summary>
    /// 检查基本类型转换是否允许
    /// </summary>
    /// <param name="expected">预期类型</param>
    /// <param name="actual">实际类型</param>
    /// <returns>如果基本类型转换允许返回true，否则返回false</returns>
    private bool IsBasicTypeConversionAllowed(Type expected, Type actual)
    {
        // 允许int到double的转换
        if (expected == typeof(double) && actual == typeof(int))
            return true;
        // 允许char到int的转换
        if (expected == typeof(int) && actual == typeof(char))
            return true;
        // 允许char到double的转换
        if (expected == typeof(double) && actual == typeof(char))
            return true;
        // 允许bool到其他类型的转换
        if (actual == typeof(bool) && (expected == typeof(int) || expected == typeof(double)))
            return true;
        return false;
    }

    /// <summary>
    /// 检查集合类型是否兼容
    /// </summary>
    /// <param name="expected">预期类型</param>
    /// <param name="actual">实际类型</param>
    /// <returns>如果集合类型兼容返回true，否则返回false</returns>
    private bool IsCollectionTypeCompatible(Type expected, Type actual)
    {
        // 检查是否为泛型集合
        if (!expected.IsGenericType || !actual.IsGenericType)
            return false;

        // 检查集合类型是否相同（如List<>和List<>）
        if (expected.GetGenericTypeDefinition() != actual.GetGenericTypeDefinition())
            return false;

        // 检查泛型参数是否兼容
        var expectedArgs = expected.GetGenericArguments();
        var actualArgs = actual.GetGenericArguments();

        if (expectedArgs.Length != actualArgs.Length)
            return false;

        // 检查每个泛型参数是否兼容
        for (int i = 0; i < expectedArgs.Length; i++)
        {
            if (expectedArgs[i] != actualArgs[i] && !expectedArgs[i].IsAssignableFrom(actualArgs[i]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 验证变量是否有类型注解
    /// </summary>
    /// <param name="varName">变量名</param>
    /// <param name="position">源代码位置</param>
    /// <returns>如果变量有类型注解返回true，否则报告错误</returns>
    public bool ValidateVarHasTypeAnnotation(string varName, SourcePosition position)
    {
        if (!LocalVarTypes.ContainsKey(varName))
        {
            ReportError($"变量 '{varName}' 缺少类型注解（编译模式要求所有变量必须显式声明类型）", position);
            return false;
        }
        return true;
    }

    /// <summary>
    /// 获取变量类型
    /// </summary>
    /// <param name="varName">变量名</param>
    /// <param name="position">源代码位置</param>
    /// <returns>变量类型，如果变量不存在则报告错误</returns>
    public Type GetVarType(string varName, SourcePosition position)
    {
        if (LocalVarTypes.TryGetValue(varName, out var type))
            return type;
        
        ReportError($"变量 '{varName}' 未声明或缺少类型注解", position);
        return typeof(object); // 不会执行到这里，因为前面已经抛出异常
    }

    /// <summary>
    /// 验证赋值类型兼容性
    /// </summary>
    /// <param name="varName">变量名</param>
    /// <param name="valueType">值类型</param>
    /// <param name="position">源代码位置</param>
    /// <returns>如果赋值类型兼容返回true，否则报告错误</returns>
    public bool ValidateAssignmentType(string varName, Type valueType, SourcePosition position)
    {
        var varType = GetVarType(varName, position);
        return ValidateType(varType, valueType, position);
    }

    /// <summary>
    /// 创建一个新的LocalManager实例，复制当前实例的FilePath和Interpreter属性
    /// </summary>
    /// <returns>新的LocalManager实例</returns>
    public LocalManager New()
    {
        return new LocalManager() { FilePath = FilePath, Interpreter = Interpreter };
    }

    /// <summary>
    /// 克隆当前LocalManager实例
    /// </summary>
    /// <returns>克隆后的LocalManager实例</returns>
    /// <remarks>
    /// 克隆过程会复制所有字典和属性，但不会复制InClassEnv，因为它是init-only属性
    /// </remarks>
    public LocalManager Clone()
    {
        var cloned = new LocalManager
        {
            FilePath = FilePath,
            Interpreter = Interpreter,
            InClassEnv = InClassEnv,
            BreakLabel = BreakLabel,
            ContinueLabel = ContinueLabel,
            DynamicAssembly = DynamicAssembly,
            DynamicModule = DynamicModule
        };

        // 克隆局部变量
        foreach (var (name, local) in LocalVar)
        {
            cloned.LocalVar[name] = local;
        }

        // 克隆委托变量
        foreach (var (name, method) in DelegateVar)
        {
            cloned.DelegateVar[name] = method;
        }

        // 克隆类变量
        foreach (var (name, type) in ClassVar)
        {
            cloned.ClassVar[name] = type;
        }

        // 克隆字段变量
        foreach (var (name, field) in FieldVar)
        {
            cloned.FieldVar[name] = field;
        }

        // 克隆函数参数信息
        foreach (var (name, @params) in FuncParameters)
        {
            cloned.FuncParameters[name] = @params;
        }

        // 克隆全局静态类
        foreach (var (name, instance) in GlobalStaticClasses)
        {
            cloned.GlobalStaticClasses[name] = instance;
        }

        return cloned;
    }

    /// <summary>
    /// 从克隆实例中恢复当前LocalManager的状态
    /// </summary>
    /// <param name="cloned">克隆的LocalManager实例</param>
    /// <remarks>
    /// 恢复过程会替换当前实例的所有字典和属性，但不会修改InClassEnv，因为它是init-only属性
    /// </remarks>
    public void Restore(LocalManager cloned)
    {
        // 清空当前局部变量
        LocalVar.Clear();

        // 恢复局部变量
        foreach (var (name, local) in cloned.LocalVar)
        {
            LocalVar[name] = local;
        }

        // 清空当前委托变量
        DelegateVar.Clear();

        // 恢复委托变量
        foreach (var (name, method) in cloned.DelegateVar)
        {
            DelegateVar[name] = method;
        }

        // 清空当前类变量
        ClassVar.Clear();

        // 恢复类变量
        foreach (var (name, type) in cloned.ClassVar)
        {
            ClassVar[name] = type;
        }

        // 清空当前字段变量
        FieldVar.Clear();

        // 恢复字段变量
        foreach (var (name, field) in cloned.FieldVar)
        {
            FieldVar[name] = field;
        }

        // 恢复其他属性（注意：InClassEnv是init-only属性，不能修改）
        FilePath = cloned.FilePath;
        BreakLabel = cloned.BreakLabel;
        ContinueLabel = cloned.ContinueLabel;

        // 清空并恢复全局静态类
        GlobalStaticClasses.Clear();
        foreach (var (name, instance) in cloned.GlobalStaticClasses)
        {
            GlobalStaticClasses[name] = instance;
        }
    }

    /// <summary>
    /// 获取指定名称的局部变量
    /// </summary>
    /// <param name="name">变量名</param>
    /// <returns>LocalBuilder实例，如果变量不存在则返回null</returns>
    public LocalBuilder? GetLocalVar(string name)
    {
        return LocalVar.GetValueOrDefault(name);
    }

    /// <summary>
    /// 添加局部变量
    /// </summary>
    /// <param name="name">变量名</param>
    /// <param name="index">LocalBuilder实例</param>
    public void AddLocalVar(string name, LocalBuilder index)
    {
        LocalVar[name] = index;
    }

    /// <summary>
    /// 移除指定名称的局部变量
    /// </summary>
    /// <param name="name">变量名</param>
    public void RemoveLocalVar(string name)
    {
        LocalVar.Remove(name);
    }

    /// <summary>
    /// 检查是否存在指定名称的局部变量
    /// </summary>
    /// <param name="name">变量名</param>
    /// <returns>如果变量存在则返回true，否则返回false</returns>
    public bool IsHasVar(string name) => LocalVar.ContainsKey(name);

    /// <summary>
    /// 获取局部变量的数量
    /// </summary>
    /// <returns>局部变量的数量</returns>
    public int GetCount() => LocalVar.Count;

    /// <summary>
    /// 获取或创建局部变量
    /// </summary>
    /// <param name="name">变量名</param>
    /// <param name="type">变量类型</param>
    /// <returns>LocalBuilder实例</returns>
    public LocalBuilder GetOrCreateLocalVar(string name, Type type)
    {
        if (LocalVar.TryGetValue(name, out var localVar))
        {
            return localVar;
        }
        
        // 创建新的局部变量
        throw new InvalidOperationException("GetOrCreateLocalVar方法需要ILGenerator实例来创建新的局部变量");
    }

    /// <summary>
    /// 获取或创建局部变量（带ILGenerator实例）
    /// </summary>
    /// <param name="ilGenerator">ILGenerator实例</param>
    /// <param name="name">变量名</param>
    /// <param name="type">变量类型</param>
    /// <returns>LocalBuilder实例</returns>
    public LocalBuilder GetOrCreateLocalVar(ILGenerator ilGenerator, string name, Type type)
    {
        if (LocalVar.TryGetValue(name, out var localVar))
        {
            return localVar;
        }

        // 创建新的局部变量
        localVar = ilGenerator.DeclareLocal(type);
        LocalVar[name] = localVar;
        return localVar;
    }

    /// <summary>
    /// 尝试推断变量的类型
    /// </summary>
    /// <param name="variableName">变量名</param>
    /// <returns>推断出的类型，如果无法推断则返回null</returns>
    public Type? TryInferVariableType(string variableName)
    {
        // 首先检查 LocalVarTypes 中是否已有类型记录
        if (LocalVarTypes.TryGetValue(variableName, out var type))
        {
            return type;
        }

        // 检查是否为函数参数
        foreach (var (funcKey, paramList) in FuncParameters)
        {
            var param = paramList.FirstOrDefault(p => p.IdName == variableName);
            if (param != null)
            {
                return param.OutputType(this);
            }
        }

        // 检查是否为字段
        if (FieldVar.TryGetValue(variableName, out var fieldInfo))
        {
            return fieldInfo.FieldType;
        }

        return null;
    }

    /// <summary>
    /// 记录推断出的变量类型
    /// </summary>
    /// <param name="variableName">变量名</param>
    /// <param name="inferredType">推断出的类型</param>
    public void RecordInferredType(string variableName, Type inferredType)
    {
        if (!LocalVarTypes.ContainsKey(variableName))
        {
            LocalVarTypes[variableName] = inferredType;

            if (TypeInferenceConfig.Instance.DebugOutput)
            {
                Console.WriteLine($"  ✓ 记录推断类型: {variableName} = {inferredType.Name}");
            }
        }
    }

    /// <summary>
    /// 批量记录推断出的类型
    /// </summary>
    /// <param name="inferredTypes">变量名到类型的映射</param>
    public void RecordInferredTypes(Dictionary<string, Type> inferredTypes)
    {
        foreach (var (varName, type) in inferredTypes)
        {
            RecordInferredType(varName, type);
        }
    }

    /// <summary>
    /// 获取变量的类型（优先使用已推断的类型）
    /// </summary>
    /// <param name="variableName">变量名</param>
    /// <returns>变量类型，如果不存在则返回null</returns>
    public Type? GetVarType(string variableName)
    {
        return TryInferVariableType(variableName);
    }
}