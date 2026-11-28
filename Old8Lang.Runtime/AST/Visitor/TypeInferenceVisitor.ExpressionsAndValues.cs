using Old8Lang.Syntax;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Linq;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.AST.Expression.OperationHelpers;
using Old8Lang.AST.Expression.ValueFunctions;
using Old8Lang.AST.Statement;
using Old8Lang.AST.Visitor;
using Old8Lang.AsyncGeneration;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Compiler.Helpers;
using Old8Lang.Compiler.Specialization;
using Old8Lang.Concurrency;
using Old8Lang.Error;
using Old8Lang.ExternProviders;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;
using Old8Lang.ModuleSystem.Core;
using Old8Lang.ModuleSystem.Loading;
using Old8Lang.ModuleSystem.Resolution;
using Old8Lang.ModuleSystem.StandardLibrary;
using Old8Lang.ModuleSystem.Symbols;
using Old8Lang.ProjectManagement;
using Old8Lang.Runtime;
using Old8Lang.TypeSystem;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// TypeInferenceVisitor - Expression 和 Value 节点的类型推断实现
/// </summary>
public partial class TypeInferenceVisitor
{
    // ===== Expression 节点实现 =====

    public Type VisitAsyncStreamExpression(AsyncStreamExpression node)
    {

        return typeof(AsyncStreamLangValue);
    
    
    }

    public Type VisitAwaitExpression(AwaitExpression node)
    {

        // await Task<T> 返回 T
        var exprType = node.Expression.Accept(this);
        if (exprType == typeof(Task)) return typeof(object); // Task 返回 void -> null

        // 检查是否是泛型 Task<T>
        if (exprType is { IsGenericType: true } && exprType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            return exprType.GetGenericArguments()[0];
        }

        // 鸭子类型：查找 GetAwaiter().GetResult() 返回类型
        var getAwaiter = exprType?.GetMethod("GetAwaiter");
        if (getAwaiter != null)
        {
            var awaiterType = getAwaiter.ReturnType;
            var getResult = awaiterType.GetMethod("GetResult");
            if (getResult != null)
            {
                var resultType = getResult.ReturnType;
                if (resultType == typeof(void)) return typeof(object);
                return resultType;
            }
        }

        return typeof(object);
    
    
    }

    public Type VisitFunctionCallExpression(FunctionCallExpression node)
    {

        // 1. 尝试分析函数表达式的类型
        var funcType = node.FunctionExpression.Accept(this);

        // 2. 如果是委托类型（包括 Action/Func），获取 Invoke 方法的返回类型
        if (typeof(Delegate).IsAssignableFrom(funcType))
        {
            var invokeMethod = funcType.GetMethod("Invoke");
            if (invokeMethod != null)
            {
                return invokeMethod.ReturnType;
            }
        }

        // 3. 如果是 LangId，尝试从 DelegateVar 中查找（针对直接函数调用）
        if (node.FunctionExpression is LangId funcId)
        {
            // 尝试构建委托键查找
            // 这里有一个循环依赖问题：我们需要参数类型来构建键，但参数类型推断可能依赖于函数返回类型？
            // 通常参数类型是独立的。
            try
            {
                var paramTypes = node.Arguments.Select(arg => arg.Accept(this) ?? typeof(object)).ToArray();
                var paramTypeNames = string.Join("_", paramTypes.Select(t => t.Name));
                var delegateKey = $"{funcId.IdName}${paramTypeNames}";

                if (local.DelegateVar.TryGetValue(delegateKey, out var method))
                {
                    if (method is DynamicMethod dm) return dm.ReturnType;
                    return method.ReturnType;
                }
            }
            catch
            {
                // 忽略错误，回退到 object
            }
        }

        // return typeof(object);

        // 如果无法推断，返回 object
        // 这是一个妥协，允许编译继续，但在运行时可能会有问题（除非我们处理了 object）
        // 如果是 Async 函数，通常返回 Task<object>
        if (node.FunctionExpression is LangId id && (id.IdName.StartsWith("Async") || id.IdName.EndsWith("Async")))
        {
            return typeof(Task<object>);
        }

        return typeof(object);

        // Debug:
        // throw new InvalidOperationError(node, $"无法推断函数调用返回类型。函数表达式类型: {funcType.FullName}, IsDelegate: {typeof(Delegate).IsAssignableFrom(funcType)}");
    
    
    }

    public Type VisitLangId(LangId node)
    {

        // 处理 node 关键字
        if (node.IdName == "this")
        {
            // 如果在类环境中，返回当前类类型
            if (local.InClassEnv is not null)
            {
                return local.InClassEnv;
            }
            // 否则返回 object
            return typeof(object);
        }

        if (!string.IsNullOrEmpty(node.AssumptionType))
        {
            // 在泛型类特化的方法体中，注解可能引用当前正在创建的类型，
            // 例如 ComparableBox<T>。必须先交给当前泛型解析器，它会复用
            // 已缓存的 TypeBuilder；否则下面的集合类型分支会把未知泛型类型
            // 降级为 object，进而让字段访问和数值比较发射出无效 IL。
            if (local.CurrentGenericTypeResolver is not null)
            {
                var resolvedType = local.CurrentGenericTypeResolver.ResolveType(node.AssumptionType.Trim());
                if (resolvedType != typeof(object) ||
                    string.Equals(node.AssumptionType.Trim(), "object", StringComparison.OrdinalIgnoreCase))
                {
                    return resolvedType;
                }
            }

            // 解析泛型类型注解，如 "list<int>" 或 "array<string>"
            var typeName = node.AssumptionType.Trim().ToLower();

            // 处理可空类型（例如 "int?", "string?"）
            // 在编译器中，可空类型被视为其基础类型（因为.NET的可空类型会自动处理null）
            if (typeName.EndsWith('?'))
            {
                typeName = typeName[..^1].Trim();
            }

            // 检查是否为泛型类型
            if (typeName.Contains('<') && typeName.EndsWith('>'))
            {
                // 提取泛型类型名称和参数
                var genericIndex = typeName.IndexOf('<');
                var baseTypeName = typeName[..genericIndex].Trim();
                var genericArg = typeName[(genericIndex + 1)..^1].Trim();

                // 首先尝试使用泛型类型解析器解析泛型参数
                Type argType;
                if (local.CurrentGenericTypeResolver is not null)
                {
                    var resolvedType = local.CurrentGenericTypeResolver.ResolveType(genericArg);
                    if (resolvedType is not null)
                    {
                        argType = resolvedType;
                    }
                    else
                    {
                        // 回退到基本类型映射
                        argType = genericArg switch
                        {
                            "int" => typeof(int),
                            "double" => typeof(double),
                            "string" => typeof(string),
                            "bool" => typeof(bool),
                            "char" => typeof(char),
                            "cancellationtoken" => typeof(CancellationToken),
                            "cancellationtokensource" => typeof(CancellationTokenSource),
                            "object" => typeof(object),
                            _ => typeof(object) // 默认为object
                        };
                    }
                }
                else
                {
                    // 没有泛型解析器时使用基本类型映射
                    argType = genericArg switch
                    {
                        "int" => typeof(int),
                        "double" => typeof(double),
                        "string" => typeof(string),
                        "bool" => typeof(bool),
                        "char" => typeof(char),
                        "cancellationtoken" => typeof(CancellationToken),
                        "cancellationtokensource" => typeof(CancellationTokenSource),
                        "object" => typeof(object),
                        _ => typeof(object) // 默认为object
                    };
                }

                // 返回泛型类型。IL 运行期集合统一用 object 容器（字面量
                // `{1,2,3}` 生成 List<object>、`[...]` 生成 object[]），
                // 注解 `list<int>` / `array<int>` 若映射成 List<int> / int[]
                // 会与实参/返回值类型不一致（返回类型不匹配或无效 IL）。
                return baseTypeName switch
                {
                    "list" => typeof(List<object>),
                    "array" => typeof(object[]),
                    "dictionary" => typeof(Dictionary<object, object>),
                    _ => typeof(object) // 未知泛型类型，默认为object
                };
            }

            // 检查是否为自定义泛型类
            if (typeName.Contains('<') && typeName.EndsWith('>'))
            {
                var genericIndex = typeName.IndexOf('<');
                var baseTypeName = typeName[..genericIndex].Trim();
                var genericArg = typeName[(genericIndex + 1)..^1].Trim();

                if (local.GenericClasses.TryGetValue(baseTypeName, out var typeTemplate))
                {
                    var typeMapping = new Dictionary<string, Type>();
                    // 简单处理泛型参数（不支持嵌套泛型参数的逗号分割，如 Map<string, List<int>>）
                    // 如果需要支持复杂嵌套，需要完整的类型解析器
                    var args = genericArg.Split(',').Select(s => s.Trim()).ToArray();
                    
                    if (typeTemplate.GenericParameters != null)
                    {
                        for (int i = 0; i < typeTemplate.GenericParameters.Count && i < args.Length; i++)
                        {
                            var paramName = typeTemplate.GenericParameters[i].Name;
                            var argTypeName = args[i];
                            
                            // 解析参数类型
                            Type t = typeof(object);
                            if (local.CurrentGenericTypeResolver is not null)
                            {
                                t = local.CurrentGenericTypeResolver.ResolveType(argTypeName) ?? typeof(object);
                            }
                            
                            if (t == typeof(object))
                            {
                                t = argTypeName switch
                                {
                                    "int" => typeof(int),
                                    "double" => typeof(double),
                                    "string" => typeof(string),
                                    "bool" => typeof(bool),
                                    "char" => typeof(char),
                                    "cancellationtoken" => typeof(CancellationToken),
                                    "cancellationtokensource" => typeof(CancellationTokenSource),
                                    _ => typeof(object)
                                };
                            }
                            
                            typeMapping[paramName] = t;
                        }
                    }
                    return GenericClassSpecializer.CreateSpecialization(typeTemplate, typeMapping, local);
                }
            }

            // 首先尝试使用泛型类型解析器
            if (local.CurrentGenericTypeResolver is not null)
            {
                var resolvedType = local.CurrentGenericTypeResolver.ResolveType(typeName);
                if (resolvedType is not null)
                {
                    return resolvedType;
                }
            }

            // 非泛型类型或解析失败时的默认映射
            var basicType = typeName switch
            {
                "int" => typeof(int),
                "double" => typeof(double),
                "string" => typeof(string),
                "bool" => typeof(bool),
                "char" => typeof(char),
                "void" => typeof(void),
                "list" => typeof(List<object>),
                "array" => typeof(object[]),
                "dict" => typeof(Dictionary<object, object>),
                "dictionary" => typeof(Dictionary<object, object>),
                "tuple" => typeof(ValueTuple<object, object>),
                "cancellationtoken" => typeof(CancellationToken),
                "cancellationtokensource" => typeof(CancellationTokenSource),
                "object" => typeof(object),
                _ => (Type?)null
            };

            if (basicType is not null)
            {
                return basicType;
            }

            // 检查是否是自定义类类型（使用原始的 AssumptionType，因为类名是大小写敏感的）
            var originalTypeName = node.AssumptionType.Trim();
            if (originalTypeName.EndsWith('?'))
            {
                originalTypeName = originalTypeName[..^1].Trim();
            }
            if (local.ClassVar.TryGetValue(originalTypeName, out var customClassType))
            {
                return customClassType;
            }

            // 如果找不到，返回 object
            return typeof(object);
        }

        // 如果没有显式类型注解，但有默认值，从默认值推断类型
        if (node.DefaultValue is not null)
        {
            return node.DefaultValue.Accept(this) ?? typeof(object);
        }

        var value = local.GetLocalVar(node.IdName);
        if (value is not null)
        {
            return value.LocalType;
        }

        // 如果LocalVar中没有，检查LocalVarTypes（用于函数参数类型推断）
        if (local.LocalVarTypes.TryGetValue(node.IdName, out var varType))
        {
            return varType;
        }

        // 与 LoadIlValue 保持一致：类实例方法内没有同名局部变量或参数时，
        // 裸标识符解析为当前实例字段。若把字段误报为 object，返回语句会对
        // 已在栈上的值类型再执行 Unbox_Any，生成无效 IL 或在运行时抛出异常。
        if (local.FieldVar.TryGetValue(node.IdName, out var field))
        {
            return field.FieldType;
        }

        // 检查是否是全局静态类（Assert, Task, Thread等）
        if (local.GlobalStaticClasses.ContainsKey(node.IdName))
        {
            // 返回对应静态类的实际类型
            return node.IdName switch
            {
                "Assert" => typeof(AssertHelper),
                "Task" => typeof(Task),
                "Thread" => typeof(Thread),
                _ => typeof(object)
            };
        }

        // 检查是否是类或枚举类型（在 IL 模式下，枚举和类都存储在 ClassVar 中）
        if (local.ClassVar.TryGetValue(node.IdName, out var classType))
        {
            return classType;
        }

        // 被函数体访问的顶层变量：使用分析阶段记录的静态类型。
        if (local.Globals is { } globals && globals.IsSlot(node.IdName))
        {
            return globals.GetType(node.IdName);
        }

        return typeof(object);
    
    
    }

    public Type? VisitOperation(Operation node)
    {
        var right = node.Opera == LangTokenType.Dot ? CallSyntaxAdapter.Normalize(node.Right) : node.Right;

        // 同一个方法 AST 会被多个泛型类特化复用；node 的字段类型依当前特化而变。
        var isThisField = node.Opera == LangTokenType.Dot &&
                          (node.Left is LangId { IdName: "this" } || node.Left is ThisExpression) && right is LangId;
        if (!isThisField && OperationTypeCache.Get(node) is { } cachedType) return cachedType;
        if (node.Opera == LangTokenType.Dot && StandardLibraryILHelper.TryResolve(node.Left, right, local, out var library, out var libraryCall))
            return StandardLibraryILHelper.GetReturnType(library, libraryCall);
        if (node.Opera == LangTokenType.Dot && StandardLibraryILHelper.TryResolveStaticMember(node.Left, right, local, out _, out _))
            return typeof(object);
        if (node.Opera == LangTokenType.Dot && node.Left is LangId { IdName: "Thread" } &&
            (right is LangId { IdName: "CurrentThread" } ||
             right is Instance { Id.IdName: "CurrentThread", Ids.Count: 0 }))
            return typeof(Thread);
        // 直接返回类型信息，不创建临时方法
        var leftType = node.Left?.Accept(new TypeInferenceVisitor(local));
        var rightType = right?.Accept(new TypeInferenceVisitor(local));

        // 枚举成员（Color.Red）的底层类型是 int；IL 发射的也是 ldc.i4。
        // 此前这里返回枚举类型本身，match / ToStr / 赋值都会按引用类型处理。
        if (node.Opera == LangTokenType.Dot && right is LangId)
        {
            // 枚举名可能没有局部变量类型记录，需要从编译期类表里取。
            if (leftType is { IsEnum: true })
            {
                return typeof(int);
            }

            if (node.Left is LangId enumId &&
                local.ClassVar.TryGetValue(enumId.IdName, out var enumType) && enumType.IsEnum)
            {
                return typeof(int);
            }
        }

        // 处理 node.member 访问（类环境内）- 需要在 TypeBuilder 转换之前处理
        // 支持 LangId { IdName: "this" } 和 ThisExpression 两种形式
        if (node.Opera == LangTokenType.Dot && (node.Left is LangId { IdName: "this" } || node.Left is ThisExpression) && right is LangId thisMemberId)
        {
            // 优先从 FieldVar 中查找字段类型
            if (local.FieldVar.TryGetValue(thisMemberId.IdName, out var fieldInfo))
            {
                return fieldInfo.FieldType;
            }
            // 如果在类环境中，尝试从 TypeBuilder 的基类中查找
            if (local.InClassEnv is TypeBuilder classTypeBuilder)
            {
                var baseType = classTypeBuilder.BaseType;
                while (baseType is not null && baseType != typeof(object))
                {
                    var baseField = baseType.GetField(thisMemberId.IdName, BindingFlags.Public | BindingFlags.Instance);
                    if (baseField is not null)
                    {
                        return baseField.FieldType;
                    }
                    baseType = baseType.BaseType;
                }
            }
            // 如果找不到字段，返回 object
            return typeof(object);
        }

        // 如果leftType是TypeBuilder，需要查找对应的已完成的类型
        if (leftType is TypeBuilder typeBuilder && typeBuilder != local.InClassEnv)
        {
            // 尝试从local.ClassVar中查找对应的已完成类型
            var typeName = typeBuilder.Name;
            leftType = local.ClassVar.TryGetValue(typeName, out var completedType)
                ? completedType
                : typeof(object); // 如果找不到对应的类型，说明类还在编译中，返回object
        }

        // 一元 not / ! 恒返回 bool（此前落到末尾返回 null，调用方再兜底成 object，
        // 条件发射就会对原生 bool 再调一次 ToBoolean，生成无效 IL）
        if (node.Left is null && node.Opera == LangTokenType.Exclamation)
        {
            return typeof(bool);
        }

        // 处理成员访问（Dot操作符）
        if (node.Opera == LangTokenType.Dot && right is LangId rightId)
        {
            if (leftType is not null)
            {
                // IL 发射阶段会把集合的语言层 Length 属性映射到 CLR ICollection.Count。
                // 这里必须同步返回 int，否则 Length 参与比较时会被当作 object，
                // 进而生成错误的装箱/比较 IL。
                if (rightId.IdName == "Length" &&
                    typeof(System.Collections.ICollection).IsAssignableFrom(leftType) &&
                    !leftType.IsValueType)
                {
                    return typeof(int);
                }

                // 检查是否是枚举类型的成员访问
                if (leftType.IsEnum)
                {
                    // 枚举成员访问返回枚举类型本身
                    var enumField = leftType.GetField(rightId.IdName);
                    if (enumField is not null)
                    {
                        return leftType; // 返回枚举类型
                    }
                }

                // 正在构建的类还不能通过 TypeBuilder 反射查询字段；类自身的
                // FieldBuilder 已登记在 FieldVar 中，供静态成员访问和类型推断复用。
                FieldInfo? field = leftType is TypeBuilder && leftType == local.InClassEnv
                    ? local.FieldVar.GetValueOrDefault(rightId.IdName)
                    : leftType.GetField(rightId.IdName);
                if (field is not null)
                {
                    return field.FieldType;
                }

                // 尝试获取属性类型
                var property = leftType.GetProperty(rightId.IdName);
                if (property is not null)
                {
                    return property.PropertyType;
                }
            }

            return typeof(object);
        }

        // 处理方法调用（Dot操作符 + Instance）
        if (node.Opera == LangTokenType.Dot && right is Instance instance)
        {
            // 特殊处理ToStr()方法，返回string类型
            if (instance.Id.IdName == "ToStr" && instance.Ids.Count == 0)
            {
                return typeof(string);
            }

            if (instance.Id.IdName == "Sqrt" && instance.Ids.Count == 0 &&
                leftType is not null && OperationTypeHelper.IsNumericType(leftType))
            {
                return typeof(double);
            }

            // 特殊处理Assert静态方法调用，返回void类型
            if (node.Left is LangId { IdName: "Assert" })
            {
                return typeof(void);
            }

            // 特殊处理 Task 静态方法调用，确保返回类型与 DotOperatorILHelper 发射一致
            if (node.Left is LangId { IdName: "Task" })
            {
                return instance.Id.IdName switch
                {
                    "Completed" => typeof(Task),
                    "WaitAll" => typeof(void),
                    "WaitAny" => typeof(int),
                    "Delay" => typeof(Task),
                    "FromResult" or "FromException" or "Run" or "StartNew" => typeof(Task<object>),
                    "WhenAll" => typeof(Task<object[]>),
                    "WhenAny" => typeof(Task<Task<object>>),
                    _ => typeof(object)
                };
            }

            // 处理内置类型的扩展方法返回类型
            var builtInReturnType = OperationTypeHelper.GetBuiltInMethodReturnType(leftType, instance.Id.IdName, instance.Ids.Count);
            if (builtInReturnType is not null)
            {
                return builtInReturnType;
            }

            if (leftType is not null)
            {
                var registeredReturnType = InstanceMethodTypeResolver.InferInstanceMethodReturnType(leftType, instance, local);
                if (registeredReturnType is not null)
                {
                    return registeredReturnType;
                }
            }

            // 对于其他方法调用，尝试查找方法并返回其返回类型。
            // 正在创建的类型（TypeBuilder）在 CreateType 之前不支持反射查询，
            // 类体内 this.method() 的调用改由运行期按实际类型分派，这里只能给出 object，
            // 与 DotOperatorILHelper 的发射路径保持一致（否则会在编译期抛
            // NotSupportedException: The invoked member is not supported before the type is created）。
            if (leftType is not null && leftType is not TypeBuilder)
            {
                MethodInfo? method;
                if (instance.NamedArgs is { Count: > 0 })
                {
                    var totalArgCount = instance.Ids.Count + instance.NamedArgs.Count;
                    method = leftType.GetMethods().FirstOrDefault(m =>
                                 m.Name == instance.Id.IdName &&
                                 m.GetParameters().Length == totalArgCount &&
                                 instance.NamedArgs.All(na => m.GetParameters().Any(p => p.Name == na.Name)))
                             ?? leftType.GetMethods().FirstOrDefault(m =>
                                 m.Name == instance.Id.IdName &&
                                 m.GetParameters().Length == totalArgCount);
                }
                else
                {
                    var paramTypes = instance.Ids.Select(id => id.Accept(this) ?? typeof(object)).ToArray();
                    method = leftType.GetMethod(instance.Id.IdName, paramTypes);
                }

                if (method is not null)
                {
                    return method.ReturnType;
                }
            }

            return typeof(object);
        }

        // 处理空值合并运算符 ??
        if (node.Opera == LangTokenType.NullishCoalescing)
        {
            // 空值合并运算符返回左操作数的类型，如果左操作数是null则返回右操作数的类型
            return leftType ?? rightType;
        }

        // 处理类型转换运算符 as
        if (node.Opera == LangTokenType.As && right is LangId rightAsId)
        {
            // 返回目标类型
            var typeName = rightAsId.IdName;
            return typeName.ToLower() switch
            {
                "int" => typeof(int),
                "double" => typeof(double),
                "string" => typeof(string),
                "bool" => typeof(bool),
                "char" => typeof(char),
                "list" => typeof(List<object>),
                "array" => leftType is { IsGenericType: true } listType &&
                           listType.GetGenericTypeDefinition() == typeof(List<>)
                    ? listType.GetGenericArguments()[0].MakeArrayType()
                    : typeof(object[]),
                "dictionary" => typeof(Dictionary<object, object>),
                _ => typeof(object)
            };
        }

        // 处理类型检查运算符 is 和 is not
        if (node.Opera == LangTokenType.Is || node.Opera == LangTokenType.IsNot)
        {
            return typeof(bool);
        }

        // 处理比较运算符和逻辑运算符
        // `in` 也在这里：它恒返回 bool。此前漏掉后落到末尾「返回左操作数类型」，
        // 于是 `"x" + (1 in list)` 会把右侧的 bool 当作 int/string 处理，
        // 轻则拼出错误文本（"x1"），重则 `string.Concat(object, object)` 收到未装箱的
        // bool，生成无效 IL（InvalidProgramException）。
        if (node.Opera == LangTokenType.GreaterThan || node.Opera == LangTokenType.LessThan || 
            node.Opera == LangTokenType.Equals || node.Opera == LangTokenType.NotEquals || 
            node.Opera == LangTokenType.GreaterThanEquals || node.Opera == LangTokenType.LessThanEquals ||
            node.Opera == LangTokenType.And || node.Opera == LangTokenType.Or || node.Opera == LangTokenType.Xor ||
            node.Opera == LangTokenType.In)
        {
            return typeof(bool);
        }

        // 对于加法运算，如果任一操作数是字符串，则返回字符串类型
        if (node.Opera == LangTokenType.Plus && (leftType == typeof(string) || rightType == typeof(string)))
        {
            return typeof(string);
        }

        // 对于二元运算，根据操作类型返回合适的类型
        if (node.Opera == LangTokenType.Star || node.Opera == LangTokenType.Plus || node.Opera == LangTokenType.Minus ||
            node.Opera == LangTokenType.Slash || node.Opera == LangTokenType.Percent || node.Opera == LangTokenType.Caret)
        {
            if (node.Opera == LangTokenType.Star && leftType == typeof(string))
            {
                return typeof(string);
            }

            // 任一操作数静态类型为 object 时走运行期动态算术，结果类型为 object
            if (leftType == typeof(object) || rightType == typeof(object))
            {
                return typeof(object);
            }

            // 如果有浮点数，返回double
            if (leftType == typeof(double) || rightType == typeof(double))
            {
                return typeof(double);
            }
            // 否则返回int
            return typeof(int);
        }

        return leftType == typeof(object) ? rightType : leftType;
    
    
    }

    public Type VisitSuperExpression(SuperExpression node)
    {
        if (local.InClassEnv?.BaseType is { } baseType && baseType != typeof(object))
        {
            return baseType;
        }

        return typeof(object);
    }

    public Type VisitThisExpression(ThisExpression node)
    {

        if (local.InClassEnv is not null)
        {
            // 返回当前类的类型（可能是 TypeBuilder 或 Type）
            return local.InClassEnv;
        }

        // 如果不在类环境中，返回 object 类型
        return typeof(object);
    
    
    }

    public Type? VisitTernaryExpression(TernaryExpression node)
    {

        // 三元表达式的输出类型是两个分支类型的公共父类型
        var trueType = node.TrueExpression.Accept(this);
        var falseType = node.FalseExpression.Accept(this);

        if (trueType == falseType)
        {
            return trueType;
        }

        // 如果类型不同，返回object
        return typeof(object);
    
    
    }

    // ===== Value 节点实现 =====
    // 所有Value节点通过OutputType方法推断类型

    public Type? VisitAnyLangValue(AnyLangValue node)
    {

        return local.ClassVar.GetValueOrDefault(node.ClassId.IdName);
    
    
    }

    public Type VisitArrayLangValue(ArrayLangValue node)
    {

return typeof(object[]);
    
    }

    public Type VisitAsyncGeneratorLangValue(AsyncGeneratorLangValue node)
    {

return typeof(object);
    
    }

    public Type VisitAsyncStreamLangValue(AsyncStreamLangValue node)
    {

        return typeof(AsyncStreamLangValue);
    
    
    }

    public Type VisitBoolLangValue(BoolLangValue node)
    {

return node.Value.GetType();
    
    }

    public Type VisitCancellationTokenLangValue(CancellationTokenLangValue node)
    {

        return typeof(CancellationToken);
    
    
    }

    public Type VisitCancellationTokenSourceLangValue(CancellationTokenSourceLangValue node)
    {

        return typeof(CancellationTokenSource);
    
    
    }

    public Type VisitCharLangValue(CharLangValue node)
    {

return node.Value.GetType();
    
    }

    public Type VisitClassMemberId(ClassMemberId node)
    {
        return typeof(object);
    }

    public Type VisitDictionaryLangValue(DictionaryLangValue node)
    {

return typeof(Dictionary<object, object>);
    
    }

    public Type VisitDoubleLangValue(DoubleLangValue node)
    {

return node.Value.GetType();
    
    }

    public Type? VisitErrorLangValue(ErrorLangValue node)
    {
        return typeof(object);
    }

    public Type VisitFuncLangValue(FuncLangValue node)
    {

        if (node.IsLambdaExpression)
        {
            return typeof(Delegate);
        }

        var idType = node.Id?.Accept(new TypeInferenceVisitor(local));
        if (idType is not null && idType != typeof(object)) return idType;
        var a = FuncLangValue.GetItemType(node.BlockStatement, local);
        return a;
    
    
    }

    public Type VisitGeneratorLangValue(GeneratorLangValue node)
    {

return typeof(object);
    
    }

    public Type VisitInstance(Instance node)
    {
        if (StandardLibraryILHelper.TryResolveFunction(node, local, out var importedFunction, out _))
            return StandardLibraryILHelper.GetFunctionReturnType(importedFunction);


        // 首先尝试通过全局函数注册器获取返回类型
        var globalFuncReturnType = node.TryGetGlobalFunctionReturnType(local);
        if (globalFuncReturnType is not null)
        {
            return globalFuncReturnType;
        }

        if (local.LocalDelegateReturnTypes.TryGetValue(node.Id.IdName, out var localDelegateReturnType))
        {
            return localDelegateReturnType;
        }

        // 尝试泛型类型推断
        if (local.GenericFunctions.TryGetValue(node.Id.IdName, out var genericFunc))
        {
            // 注意：OutputType 不应该有副作用，也不应该修改 AST
            // 我们只需要推断返回类型
            var argTypes = node.Ids.Select(id => id.Accept(this) ?? typeof(object)).ToList();
            var typeArgs = GenericMethodSpecializer.InferTypeArguments(genericFunc, argTypes, local);

            if (typeArgs != null)
            {
                // 使用 GenericTypeResolver 解析返回类型
                var resolver = new GenericTypeResolver(typeArgs, local, local.Interpreter);
                return resolver.ResolveReturnType(genericFunc.Id?.AssumptionType);
            }
        }

        // 尝试使用函数名+参数类型查找（支持重载）
        // 先尝试精确匹配参数类型
        var actualParamTypes = node.Ids.Select(id => id.Accept(this)).ToArray();
        var actualParamTypeNames = string.Join("_", actualParamTypes.Select(t => t?.Name ?? "object"));
        var exactDelegateKey = $"{node.Id.IdName}${actualParamTypeNames}";
        var result = local.DelegateVar.GetValueOrDefault(exactDelegateKey);

        // 如果找不到，可能是带默认参数的函数，尝试查找参数数量匹配的方法
        if (result is null)
        {
            // 遍历所有委托变量，查找参数数量匹配的方法
            foreach (var (key, method) in local.DelegateVar)
            {
                if (!key.StartsWith($"{node.Id.IdName}$"))
                    continue;

                // 获取方法参数信息
                var methodParams = method.GetParameters();

                // 检查参数数量是否匹配
                if (methodParams.Length >= node.Ids.Count)
                {
                    // 获取函数的参数列表信息
                    if (local.FuncParameters.TryGetValue(key, out var funcParams))
                    {
                        // 计算必需参数的数量（没有默认值的参数）
                        int requiredParamsCount = funcParams.Count(t => t.DefaultValue is null);

                        // 如果实际参数数量大于等于必需参数数量，则可以匹配
                        if (node.Ids.Count >= requiredParamsCount)
                        {
                            result = method;
                            break;
                        }
                    }
                    else if (methodParams.Length == node.Ids.Count)
                    {
                        // 完全匹配
                        result = method;
                        break;
                    }
                }
            }
        }

        if (result is not null) return result.ReturnType;

        var classType = local.ClassVar.GetValueOrDefault(node.Id.IdName);
        if (classType is null)
        {
            // 如果找不到类类型，尝试从ClassVar中查找TypeBuilder
            foreach (var kv in local.ClassVar)
            {
                if (kv.Key == node.Id.IdName)
                {
                    classType = kv.Value;
                    break;
                }
            }
        }

        return classType ?? typeof(object);
    
    
    }

    public Type VisitIntLangValue(IntLangValue node)
    {

return node.Value.GetType();
    
    }

    public Type VisitLangListItem(LangListItem node)
    {

        var listType = node.ListId.Accept(this);

        if (listType == typeof(string))
        {
            return typeof(char); // 字符串索引访问返回 char 类型
        }

        if (listType!.IsArray)
        {
            // 数组索引访问返回数组元素类型
            return listType.GetElementType() ?? typeof(object);
        }

        if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>))
        {
            // List<T>索引访问返回T类型
            return listType.GetGenericArguments()[0];
        }

        if (listType.FullName?.StartsWith("System.ValueTuple") == true)
        {
            // ValueTuple 索引访问
            // 如果索引是非负常量，可以推断出具体的元素类型；负数需要运行期才知道长度
            if (node.Key is ILiteralSyntax { Kind: LiteralKind.Int, LiteralValue: int and >= 0 } intLiteral)
            {
                var index = (int)intLiteral.LiteralValue!;
                var elementType = node.GetValueTupleElementType(listType, index);
                if (elementType != null)
                {
                    return elementType;
                }
            }

            // 如果索引不是常量，返回 object（因为 ITuple 索引器返回 object）
            return typeof(object);
        }

        if (listType == typeof(Dictionary<object, object>))
        {
            // 字典值可以是任意类型
        }

        return typeof(object);
    
    
    }

    public Type VisitListComprehension(ListComprehension node)
    {

        return typeof(List<object>);
    
    
    }

    public Type VisitListLangValue(ListLangValue node)
    {

        // 与 LoadIlValue 保持一致：列表字面量在 IL 模式下统一是 List<object>
        return typeof(List<object>);
    
    
    }

    public Type VisitMethodOverloadList(MethodOverloadList node)
    {
        // MethodOverloadList 是方法重载列表，返回 Delegate 类型
        // 避免循环调用 OutputType -> Accept -> VisitMethodOverloadList -> OutputType
        return typeof(Delegate);
    }

    public Type VisitNestedIndexAccess(NestedIndexAccess node)
    {

        var baseType = node.BaseExpression.Accept(this) ?? typeof(object);

        if (baseType == typeof(string))
        {
            return typeof(char);
        }

        if (baseType.IsArray)
        {
            return baseType.GetElementType() ?? typeof(object);
        }

        if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(List<>))
        {
            return baseType.GetGenericArguments()[0];
        }

        if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            return baseType.GetGenericArguments()[1];
        }

        // 处理 ValueTuple 类型
        if (baseType.FullName?.StartsWith("System.ValueTuple") == true)
        {
            // 如果索引是常量，我们可以推断出具体的元素类型
            if (node.NestedIndex is ILiteralSyntax { Kind: LiteralKind.Int, LiteralValue: int indexValue })
            {
                var index = indexValue;
                var elementType = node.GetValueTupleElementType(baseType, index);
                if (elementType != null)
                {
                    return elementType;
                }
            }

            // 如果索引不是常量，返回 object（因为 ITuple 索引器返回 object）
        }

        return typeof(object);
    
    
    }

    public Type VisitNestedSliceAccess(NestedSliceAccess node)
    {

        var baseType = node.BaseExpression.Accept(this);

        // 如果基础类型为null，返回object类型
        if (baseType is null)
        {
            return typeof(object);
        }

        // 切片操作返回与基础类型相同的类型
        // 例如：object[] 切片后仍然是 object[]，List<object> 切片后仍然是 List<object>
        return baseType;
    
    
    }

    public Type VisitNullLangValue(NullLangValue node)
    {

return typeof(object);
    
    }

    public Type VisitRangeLangValue(RangeLangValue node)
    {

return typeof(int[]);
    
    }

    public Type VisitSliceLangValue(SliceLangValue node)
    {

        // 获取 Id 的类型
        var idType = node.Id.Accept(this);

        // 如果类型为 null，返回 object 类型
        if (idType is null)
        {
            return typeof(object);
        }

        // 切片操作返回与原类型相同的类型
        return idType;
    
    
    }

    public Type VisitStringLangValue(StringLangValue node)
    {

return node.Value.GetType();
    
    }

    public Type VisitStringTemplateValue(StringTemplateValue node)
    {

        return typeof(string);
    
    
    }

    public Type? VisitSuperProxy(SuperProxy node)
    {

        // super 表达式返回父类实例，类型为 object
        return typeof(object);
    
    
    }

    public Type VisitTaskClassLangValue(TaskClassLangValue node)
    {

        return typeof(Type);
    
    
    }

    public Type VisitTaskCompletionSourceLangValue(TaskCompletionSourceLangValue node)
    {

        return typeof(TaskCompletionSource<LangValueType>);
    
    
    }

    public Type? VisitTaskFactoryClassLangValue(TaskFactoryClassLangValue node)
    {
        return typeof(object);
    }

    public Type? VisitTaskFactoryStaticMethodWrapper(TaskFactoryStaticMethodWrapper node)
    {
        return typeof(object);
    }

    public Type VisitTaskLangValue(TaskLangValue node)
    {

        return typeof(Task<object>);
    
    
    }

    public Type? VisitTaskSchedulerClassLangValue(TaskSchedulerClassLangValue node)
    {
        return typeof(object);
    }

    public Type? VisitTaskSchedulerLangValue(TaskSchedulerLangValue node)
    {
        return typeof(object);
    }

    public Type VisitTaskStaticMethodWrapper(TaskStaticMethodWrapper node)
    {

        return typeof(Type);
    
    
    }

    public Type? VisitThreadClassLangValue(ThreadClassLangValue node)
    {
        return typeof(object);
    }

    public Type VisitThreadLangValue(ThreadLangValue node)
    {

        return typeof(ThreadLangValue);
    
    
    }

    public Type? VisitThreadStaticMethodWrapper(ThreadStaticMethodWrapper node)
    {
        return typeof(object);
    }

    public Type VisitTupleLangValue(TupleLangValue node)
    {

        var types = node.Elements.Select(e => e.Accept(this) ?? typeof(object)).ToArray();
        return node.GetValueTupleTypeForCompiler(types);
    
    
    }

    public Type VisitTypeLangValue(TypeLangValue node)
    {

        // TypeLangValue 表示类型信息，输出为 TypeLangValue 类型
        return typeof(TypeLangValue);
    
    
    }

    public Type VisitVoidLangValue(VoidLangValue node)
    {

        return typeof(void);
    
    
    }

    public Type VisitMatchExpression(MatchExpression node)
    {

        // 各分支结果类型一致时才采用该类型，否则退回 object。
        // 只取第一个分支会与实际运行结果不符：混合类型的 match 在 IL 里统一按
        // object 存储，推断出具体值类型会让调用方对装箱引用做 unbox.any。
        if (node.Cases.Count == 0)
        {
            return typeof(object);
        }

        Type? commonType = null;
        foreach (var matchCase in node.Cases)
        {
            var caseType = matchCase.ResultExpression.Accept(this) ?? typeof(object);
            if (commonType is null)
            {
                commonType = caseType;
                continue;
            }

            if (commonType != caseType)
            {
                return typeof(object);
            }
        }

        return commonType ?? typeof(object);
    
    
    }

    public Type VisitEnumLangValue(EnumLangValue node)
    {
        // 枚举值的类型是 int（枚举的底层类型）
        return typeof(int);
    }

    public Type VisitGenericInstanceExpression(GenericInstanceExpression node)
    {

        // IL 模式下的类型推断
        // 对于泛型实例，需要根据基础表达式和类型参数推断最终类型

        // 获取基础表达式名称
        if (node.BaseExpression is not LangId identifier)
        {
            return typeof(object);
        }

        var name = identifier.IdName;

        // 判断是泛型类还是泛型函数
        if (local.GenericClasses.TryGetValue(name, out var typeTemplate))
        {
            // 泛型类实例化：返回特化后的类型
            var typeMapping = node.CreateTypeMapping(typeTemplate.GenericParameters, local);
            var specializedType = GenericClassSpecializer.CreateSpecialization(typeTemplate, typeMapping, local);
            return specializedType;
        }

        if (local.GenericFunctions.TryGetValue(name, out var genericFunc))
        {
            // 泛型函数调用：返回函数的返回类型
            var typeMapping = node.CreateTypeMapping(genericFunc.GenericParameters, local);
            var resolver = new GenericTypeResolver(typeMapping, local, local.Interpreter);
            return resolver.ResolveReturnType(genericFunc.Id?.AssumptionType);
        }

        // 默认返回 object 类型
        return typeof(object);
    
    
    }

    public Type VisitLinqExpression(LinqExpression node)
    {

        if (node.CanGenerateStreamingQuery())
        {
            return typeof(List<object>);
        }
        // 非流式 IL 路径最终调用 ConvertToListLangValue。
        return typeof(ListLangValue);
    
    
    }

    public Type? VisitAssertClassLangValue(AssertClassLangValue node)
    {

        return typeof(Delegate);
    
    
    }

    public Type VisitAssertStaticMethodWrapper(AssertStaticMethodWrapper node)
    {

        return typeof(Delegate);
    
    
    }

    public Type? VisitLockedVariableLangValue(LockedVariableLangValue node)
    {
        return typeof(object);
    }

    public Type? VisitMockLibClassLangValue(MockLibClassLangValue node)
    {

        return typeof(MockLib.MockObject);
    
    
    }

    public Type VisitMockLibStaticMethodWrapper(MockLibStaticMethodWrapper node)
    {

        return typeof(MockLib.MockObject);
    
    
    }

    public Type VisitMockObjectLangValue(MockObjectLangValue node)
    {

        return typeof(MockLib.MockObject);
    
    
    }

    public Type? VisitTestRunnerClassLangValue(TestRunnerClassLangValue node)
    {

        return typeof(Delegate);
    
    
    }

    public Type VisitTestRunnerStaticMethodWrapper(TestRunnerStaticMethodWrapper node)
    {

        return typeof(Delegate);
    
    
    }

    public Type? VisitAsyncFuncLangValue(AsyncFuncLangValue node)
    {
        // 迁移自 AsyncFuncLangValue.OutputType()
        var parameterTypes = node.Ids!
            .Select(item => item.Accept(new TypeInferenceVisitor(local)))
            .ToArray();
        var returnType = typeof(Task<object>);
        return System.Linq.Expressions.Expression.GetDelegateType(
            parameterTypes!.Cast<Type>().Concat([returnType]).ToArray());
    }

    public Type? VisitNativeAnyLangValue(NativeAnyLangValue node)
    {
        return typeof(object);
    }

    public Type? VisitVMThreadLangValue(VMThreadLangValue node)
    {
        return typeof(object);
    }
}
