using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// BytecodeVisitor - 函数和类定义
/// </summary>
public partial class BytecodeVisitor
{
    public Instruction? VisitFuncInit(FuncInit node)
    {
        // 编译函数定义
        var funcValue = node.FuncValue;
        var funcName = funcValue.Id?.IdName ?? "<lambda>";

        // 检查是否是泛型函数
        if (funcValue.GenericParameters is { Count: > 0 })
        {
            // 泛型函数：注册到泛型函数缓存，不立即编译
            _compiler.RegisterGenericFunction(funcName, funcValue);
            return null;
        }

        // 检查函数是否已经被编译过
        bool alreadyCompiled = _compiler.GetFunctionIndex(funcName) >= 0;

        // 非泛型函数：正常编译
        var paramNames = funcValue.Ids?.Select(id => id.IdName).ToList() ?? [];
        var paramTypes = funcValue.Ids?.Select(id => id.AssumptionType ?? "").ToList() ?? [];

        // 提取默认参数值和params参数索引
        var defaultValues = new List<object?>();
        int paramsIndex = -1;
        if (funcValue.Ids != null)
        {
            for (int i = 0; i < funcValue.Ids.Count; i++)
            {
                var param = funcValue.Ids[i];

                // 检查是否是params参数
                if (param.IsParams)
                {
                    paramsIndex = i;
                }

                if (param.DefaultValue != null)
                {
                    // 尝试计算默认值（仅支持常量表达式）
                    var defaultValue = EvaluateConstantExpression(param.DefaultValue);
                    defaultValues.Add(defaultValue);
                }
                else
                {
                    defaultValues.Add(null);
                }
            }
        }

        // 获取返回类型
        var returnType = funcValue.Id?.AssumptionType ?? "";

        // 如果函数还没有被编译过，编译函数体
        //
        // 嵌套具名函数可能引用外层函数的局部变量。这种名字在函数体里既不是局部变量
        // 也不是全局变量，不加处理会被 VisitLangId 降级成 LoadGlobal，运行时查全局表
        // 失败并报「名称 'x' 未定义」（连带 0:0 的错位位置）。因此这里与 lambda 一样
        // 做捕获分析，把捕获值随 MakeClosure 一起绑定。
        // 顶层函数没有外层局部变量，分析结果为空，行为与以往完全一致。
        // 全局变量不计入捕获（includeGlobals: false）：运行时直接查全局表既能读到最新值，
        // 也避免把每个引用了全局的函数都变成闭包。
        var capturedVars = AnalyzeClosureCaptures(funcValue.BlockStatement, paramNames);

        if (!alreadyCompiled)
        {
            _compiler.CompileFunction(funcName, paramNames, paramTypes, defaultValues,
                funcValue.BlockStatement, paramsIndex, capturedVars, returnType);

            if (capturedVars.Count > 0)
            {
                // 调用点必须走绑定查找才能取到闭包环境，不能按函数索引直达函数体
                _compiler.MarkFunctionNeedsClosureEnvironment(funcName);
            }
        }

        // 检查是否有装饰器
        if (funcValue.Decorators is { Count: > 0 })
        {
            // 装饰器会在运行期用包装后的函数覆盖绑定，与 MakeClosure 携带的闭包环境
            // 无法并存：包装函数拿不到捕获值，函数体开头的取捕获值序言也无从成立。
            // 这种组合此前就会报「名称 'x' 未定义」，原因完全不指向真实问题，因此明确报错。
            if (capturedVars.Count > 0)
            {
                throw new VmUnsupportedError(node,
                    $"嵌套函数 '{funcName}' 同时使用装饰器与对外层局部变量 " +
                    $"'{string.Join("', '", capturedVars)}' 的捕获");
            }

            // 标记为带装饰器：调用点必须走全局绑定，否则会绕过装饰器
            _compiler.MarkFunctionDecorated(funcName);

            // 应用装饰器
            ApplyDecorators(funcName, funcValue.Decorators);
        }
        else
        {
            // 获取函数索引
            int funcIndex = _compiler.GetFunctionIndex(funcName);

            // 有捕获则构造闭包（捕获值按值快照），否则是普通的裸函数元数据。
            // 回读元数据里记录的捕获列表而非本地变量 capturedVars：同一函数声明
            // 可能被访问多次，两次可见的作用域未必相同，操作数必须与编译函数体时
            // 用的那份列表一致，否则闭包环境槽位对不上。
            EmitClosureOrFunction(funcIndex, _compiler.GetFunctionCapturedVariables(funcName));
            Emit(OpCode.StoreGlobal, funcName);
        }

        return null;
    }

    // ===== 其他语句 - 默认实现 =====


    public Instruction? VisitClassInit(ClassInit node)
    {
        // 类定义编译
        // 从 TypeTemplate 中提取类名、字段和方法
        var typeTemplate = node.AnyValue;
        string className = typeTemplate.ClassName;

        // 检查类是否已经被编译过（在PreprocessClassDefinitions阶段）
        // 如果已经编译过，直接返回，避免重复编译
        if (_compiler.GetClassMetadata(className) != null)
        {
            return null;
        }

        // 首先递归处理所有嵌套类
        ProcessNestedClasses(typeTemplate);

        // 检查是否是泛型类
        if (typeTemplate.GenericParameters is { Count: > 0 })
        {
            // 泛型类：注册到泛型类缓存，不立即编译
            _compiler.RegisterGenericClass(className, typeTemplate);
            return null;
        }

        // 处理接口定义
        if (typeTemplate.IsInterface)
        {
            CompileInterfaceDefinition(typeTemplate);
            return null;
        }

        // 处理 Mixin 定义
        if (typeTemplate.IsMixin)
        {
            CompileMixinDefinition(typeTemplate);
            return null;
        }

        // 非泛型类：正常编译
        var fields = new List<(string fieldName, string fieldType, LangExpression? initialValue)>();
        var staticFields = new List<(string fieldName, string fieldType, LangExpression initialValue)>();
        var methods = new List<(string methodName, FuncLangValue funcValue, bool isStatic, AccessModifier accessModifier)>();

        // 遍历实例成员，提取字段和方法
        foreach (var (memberId, memberExpr) in typeTemplate.Variates)
        {
            if (memberExpr is FuncLangValue funcValue)
            {
                // 这是一个实例方法
                var accessModifier = GetAccessModifier(memberId.Modifiers);
                methods.Add((memberId.IdName, funcValue, false, accessModifier));
            }
            else if (memberExpr is TypeTemplate)
            {
                // 这是一个嵌套类，已在上面的 ProcessNestedClasses 中处理，跳过
                continue;
            }
            else
            {
                // 这是一个实例字段，保存字段名、类型和初始值
                var fieldType = memberId.AssumptionType ?? "";
                fields.Add((memberId.IdName, fieldType, memberExpr));
            }
        }

        // 遍历静态成员，提取静态字段和静态方法
        foreach (var (memberId, memberExpr) in typeTemplate.StaticVariates)
        {
            if (memberExpr is FuncLangValue funcValue)
            {
                // 这是一个静态方法
                var accessModifier = GetAccessModifier(memberId.Modifiers);
                methods.Add((memberId.IdName, funcValue, true, accessModifier));
            }
            else if (memberExpr is TypeTemplate)
            {
                // 这是一个嵌套类，已在上面的 ProcessNestedClasses 中处理，跳过
                continue;
            }
            else
            {
                // 这是一个静态字段，保存字段名、类型和初始值
                var fieldType = memberId.AssumptionType ?? "";
                staticFields.Add((memberId.IdName, fieldType, memberExpr));
            }
        }

        // 在编译器中注册类定义（包括方法、接口和Mixin）
        _compiler.DeclareClass(className, fields, staticFields, methods, typeTemplate.ParentClassName,
            typeTemplate.ImplementsNames, typeTemplate.MixinNames);

        // 类定义本身不生成运行时指令
        return null;
    }


}
