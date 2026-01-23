using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Metadata;

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
        if (funcValue.GenericParameters != null && funcValue.GenericParameters.Count > 0)
        {
            // 泛型函数：注册到泛型函数缓存，不立即编译
            _compiler.RegisterGenericFunction(funcName, funcValue);
            return null;
        }

        // 检查函数是否已经被编译过（避免重复编译）
        if (_compiler.GetFunctionIndex(funcName) >= 0)
        {
            // 函数已经在预处理阶段被编译过，跳过
            return null;
        }

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

        // 编译函数
        var functionMetadata = _compiler.CompileFunction(funcName, paramNames, paramTypes, defaultValues, funcValue.BlockStatement, paramsIndex, null, returnType);

        // 检查是否有装饰器
        if (funcValue.Decorators != null && funcValue.Decorators.Count > 0)
        {
            // 应用装饰器
            ApplyDecorators(funcName, funcValue.Decorators);
        }
        else
        {
            // 无装饰器：直接将函数加载到栈并存储
            int funcIndex = _compiler.GetFunctionIndex(funcName);
            Emit(OpCode.MakeFunction, funcIndex);
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
        if (typeTemplate.GenericParameters != null && typeTemplate.GenericParameters.Count > 0)
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
