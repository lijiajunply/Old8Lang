using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 实例，a(b,c)
/// </summary>
/// <param name="langId"></param>
/// <param name="ids"></param>
/// <param name="position"></param>
public class Instance(LangId langId, List<LangExpression> ids, SourcePosition position = default)
    : LangValueType(position)
{
    public readonly List<LangExpression> Ids = ids;
    public readonly LangId Id = langId;

    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

    public override LangValueType Run(LangParser.VariateManager manager)
    {
        LangValueType result;
        var results = Ids.Select(t => t.Run(manager)).ToList();

        switch (Id.IdName)
        {
            case "Type" or "type":
                return new TypeLangValue(results[0]).Run(manager);
            case "Exec" or "exec":
            {
                if (results[0] is not StringLangValue execStringValue)
                    throw new TypeError(this, "StringValue", results[0].GetType().Name);
                var a = manager.Interpreter.Build(code: execStringValue.Value);
                a.Run(manager);
                return new VoidLangValue();
            }
            case "ShowValues" or "showValues":
            {
#if DEBUG
                manager.Interpreter.UseClass.WriteLine(manager.ToString());
                return new VoidLangValue();
#endif
            }
            case "Json" or "json":
            {
                // 支持多种类型的 JSON 序列化
                switch (results[0])
                {
                    case AnyLangValue jsonAnyValue:
                        return jsonAnyValue.ToJson();

                    case DictionaryLangValue dictValue:
                    {
                        // 将字典转换为 JSON 字符串
                        var dict = new Dictionary<string, object>();
                        foreach (var (key, value) in dictValue.Value)
                        {
                            var keyStr = key.ToDisplayString();
                            dict[keyStr] = value.GetValue();
                        }

                        var jsonStr = System.Text.Json.JsonSerializer.Serialize(dict);
                        return new StringLangValue(jsonStr);
                    }

                    case ArrayLangValue arrayValue:
                    {
                        // 将数组转换为 JSON 字符串
                        var list = arrayValue.GetItems().Select(item => item.GetValue()).ToList();
                        var jsonStr = System.Text.Json.JsonSerializer.Serialize(list);
                        return new StringLangValue(jsonStr);
                    }

                    case ListLangValue listValue:
                    {
                        // 将列表转换为 JSON 字符串
                        var list = listValue.GetItems().Select(item => item.GetValue()).ToList();
                        var jsonStr = System.Text.Json.JsonSerializer.Serialize(list);
                        return new StringLangValue(jsonStr);
                    }

                    default:
                        throw new TypeError(this, "AnyValue/DictionaryValue/ArrayValue/ListValue",
                            results[0].GetType().Name);
                }
            }
            case "ToObj" or "toObj":
                if (results[0] is not StringLangValue stringValue)
                    throw new TypeError(this, "StringValue", results[0].GetType().Name);
                return stringValue.ToObj();
            case "PrintLine" or "printLine":
            {
                if (results.Count == 0)
                {
                    manager.Interpreter.UseClass.WriteLine("");
                    return new VoidLangValue();
                }

                var value = results[0].ToDisplayString();
                for (var i = 1; i < results.Count; i++) value += results[i].ToDisplayString();

                manager.Interpreter.UseClass.WriteLine(value);
                return new VoidLangValue();
            }
            case "Print" or "print":
            {
                if (results.Count == 0) return new VoidLangValue();

                var value = results[0].ToDisplayString();
                for (var i = 1; i < results.Count; i++) value += results[i].ToDisplayString();

                manager.Interpreter.UseClass.Write(value);
                return new VoidLangValue();
            }
            case "Error" or "error":
            {
                if (results.Count == 0)
                {
                    manager.Interpreter.UseClass.WriteLine("");
                    return new VoidLangValue();
                }

                var value = results[0].ToDisplayString();
                for (var i = 1; i < results.Count; i++) value += results[i].ToDisplayString();

                manager.Interpreter.UseClass.Error(value);
                return new VoidLangValue();
            }
            case "ReadLine" or "readLine":
            {
                var res = manager.Interpreter.UseClass.ReadLine();
                return new StringLangValue(res);
            }
            case "Clear" or "clear":
            {
                manager.Interpreter.UseClass.Clear();
                return new VoidLangValue();
            }
            case "Compiler" or "compiler":
            {
                if (results.Count == 0) return new VoidLangValue();
                string value;
                if (results[0] is StringLangValue sv) // 使用不同的变量名，避免冲突
                {
                    value = sv.Value; // 直接访问Value属性，避免带引号
                }
                else
                {
                    value = results[0].ToString();
                }

                var statement = manager.Interpreter.Build(code: value);
                var dynamicMethod = new DynamicMethod("OldLangRun", null, null, true);
                var ilGenerator = dynamicMethod.GetILGenerator();
                var local = new LocalManager();
                statement.GenerateIl(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Ret);
                foreach (var info in local.DelegateVar)
                {
                    manager.AddClassAndFunc(new FuncLangValue(info.Key, info.Value));
                }

                return new VoidLangValue();
            }
            case "Len" or "len":
            {
                var value = results[0].Run(manager);
                if (value is ILangList list) return new IntLangValue(list.GetLength());
                throw new InvalidOperationError(this, $"{results[0]} 不是列表类型");
            }
            case "Assert" or "assert":
            {
                var value = results[0].Run(manager);
                var value1 = results[1].Run(manager);
                if (!value.Equal(value1))
                {
                    var message = $"断言失败: 期望 {value1}，但得到 {value}";
                    throw new AssertionError(this, message);
                }

                return new BoolLangValue(true);
            }
        }

        // 先尝试根据函数名和参数数量查找重载函数
        var func = manager.GetFunc(Id, Ids.Count);
        if (func != null)
        {
            // 找到匹配的重载函数，直接调用
            result = func.Run(manager, Ids);
        }
        else
        {
            // 如果没有找到重载函数，使用原来的方式查找
            var idResult = Id.Run(manager);
            result = idResult;

            // 如果idResult是TypeTemplate，则创建其实例
            if (idResult is TypeTemplate typeTemplate)
            {
                // 创建类的实例
                var instance = typeTemplate.CreateInstance(manager);

                // 初始化实例，设置Interpreter
                instance.Init(manager.Interpreter);

                // 保存init方法的引用
                if (instance.Result.TryGetValue("init", out var initResult))
                {
                    if (initResult is not FuncLangValue initFunc) throw new TypeError(this, "FuncValue", "init 不是函数类型");

                    // 在调用init方法前，将当前实例添加到AnyInfo中，以便this关键字访问
                    instance.Manager.Set(new LangId("this"), instance);
                    instance.Manager.IsFunc = true; // 设置为函数上下文

                    // 调用init方法，并将参数传递给它
                    initFunc.Run(instance.Manager, Ids);

                    // 恢复非函数上下文标志
                    instance.Manager.IsFunc = false;
                }
                else if (Ids.Count != 0)
                {
                    throw new InvalidOperationError(this, "找不到对应的init函数");
                }

                result = instance;
            }
            // 如果idResult是FuncLangValue，则调用它
            else if (idResult is FuncLangValue funcValue)
            {
                // 直接调用函数，参数表达式会在函数体内执行
                result = funcValue.Run(manager, Ids);
            }
        }

        // 原来的AnyLangValue处理逻辑，用于兼容旧代码
        if (result is AnyLangValue anyValue)
        {
            // 保存init方法的引用，避免覆盖result变量
            if (anyValue.Result.TryGetValue("init", out var initResult))
            {
                if (initResult is not FuncLangValue initFunc) throw new TypeError(this, "FuncValue", "init 不是函数类型");

                // 在调用init方法前，将当前实例添加到AnyInfo中，以便this关键字访问
                anyValue.Manager.Set(new LangId("this"), anyValue);
                anyValue.Manager.IsFunc = true; // 设置为函数上下文

                // 调用init方法，并将参数传递给它
                initFunc.Run(anyValue.Manager, Ids);

                // 恢复非函数上下文标志
                anyValue.Manager.IsFunc = false;
            }
            else if (results.Count != 0)
            {
                throw new InvalidOperationError(this, "找不到对应的init函数");
            }
        }

        if (result is NativeAnyLangValue nativeAnyValue)
        {
            List<LangValueType> a = [];
            a.AddRange(Ids.Select(id => id.Run(manager)));
            nativeAnyValue.New([.. Apis.ListToObjects(a)]);
            result = nativeAnyValue;
        }

        return result;
    }

    public LangValueType FromClassToResult(LangValueType baseLangValue)
    {
        var type = baseLangValue.GetType();
        var m = type.GetMethod(Id.IdName);
        if (m == null)
        {
            type = baseLangValue switch
            {
                DictionaryLangValue => Type.GetType("Old8Lang.AST.Expression.DictionaryValueFuncStatic"),
                ListLangValue => Type.GetType("Old8Lang.AST.Expression.ListValueFuncStatic"),
                _ => Type.GetType("Old8Lang.AST.Expression.ValueTypeFuncStatic")
            };
            m = type?.GetMethod(Id.IdName);
        }

        if (m == null && baseLangValue is not DictionaryLangValue or ListLangValue)
        {
            type = Type.GetType("Old8Lang.AST.Expression.ValueTypeFuncStatic");
            m = type?.GetMethod(Id.IdName);
        }

        var os = new List<object>() { baseLangValue };
        os.AddRange(Ids);

        // 对于静态方法，第一个参数应该是 null，因为静态方法没有实例
        object? invokeInstance = null;
        // 检查是否是扩展方法（静态方法）
        if (m?.IsStatic == false)
        {
            // 非静态方法，使用 baseLangValue 作为实例
            invokeInstance = baseLangValue;
        }

        var r = m?.Invoke(invokeInstance, [.. os]);
        if (r is LangValueType v) return v;
        return ObjToValue(r!);
    }

    public override string ToString()
    {
        return $"{Id}({string.Join(", ", Ids)})";
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        switch (Id.IdName)
        {
            case "PrintLine" or "printLine":
                // 处理多个参数，将它们转换为字符串并拼接
                if (Ids.Count == 0)
                {
                    // 没有参数，调用 Console.WriteLine()
                    var writeLineNoArg = typeof(Console).GetMethod("WriteLine", Type.EmptyTypes);
                    if (writeLineNoArg != null)
                    {
                        ilGenerator.Emit(OpCodes.Call, writeLineNoArg);
                    }

                    return;
                }

                // 简化实现：只处理第一个参数，将其转换为字符串
                var printLineExpr = Ids[0];
                printLineExpr.LoadIlValue(ilGenerator, local);
                var printLineType = printLineExpr.OutputType(local);

                // 直接调用Console.WriteLine(object)方法，让CLR处理类型转换
                var writeLineObject = typeof(Console).GetMethod("WriteLine", [typeof(object)]);
                if (writeLineObject != null)
                {
                    // 如果是值类型，先装箱
                    if (printLineType is { IsValueType: true })
                    {
                        ilGenerator.Emit(OpCodes.Box, printLineType);
                    }

                    ilGenerator.Emit(OpCodes.Call, writeLineObject);
                }

                return;
            case "Print" or "print":
                // 处理多个参数，将它们转换为字符串并拼接
                if (Ids.Count == 0)
                {
                    // 没有参数，直接返回
                    return;
                }

                // 简化实现：只处理第一个参数，将其转换为字符串
                var printExpr = Ids[0];
                printExpr.LoadIlValue(ilGenerator, local);
                var printType = printExpr.OutputType(local);

                // 如果参数不是字符串类型，调用 ToString() 方法转换为字符串
                if (printType != typeof(string))
                {
                    // 获取 ToString() 方法
                    var toStringMethod = typeof(object).GetMethod("ToString", Type.EmptyTypes)!;
                    // 如果是值类型，先装箱
                    if (printType is { IsValueType: true })
                    {
                        ilGenerator.Emit(OpCodes.Box, printType);
                    }

                    // 调用 ToString() 方法
                    ilGenerator.Emit(OpCodes.Callvirt, toStringMethod);
                }

                // 调用 Console.Write(string)
                ilGenerator.Emit(OpCodes.Call, typeof(Console).GetMethod("Write", [typeof(string)])!);
                return;
            case "Json" or "json":
                return;
            case "ToObj" or "toObj":
                return;
            case "Len" or "len":
                var lenId = Ids[0];
                lenId.LoadIlValue(ilGenerator, local);
                var lenType = lenId.OutputType(local)!;
                var lengthProp = lenType.GetProperty(lenType.IsAssignableTo(typeof(object[])) ? "Length" : "Count");
                if (lengthProp == null)
                {
                    throw new InvalidOperationError(this, $"类型 {lenType.Name} 没有 Length 或 Count 属性");
                }

                ilGenerator.Emit(OpCodes.Call, lengthProp.GetGetMethod()!);
                return;
            case "Type" or "type":
                // 编译模式下type()函数返回类型名称字符串
                var typeId = Ids[0];
                var typeIdType = typeId.OutputType(local);
                if (typeIdType != null)
                {
                    // 直接返回类型名称字符串，不调用GetType()
                    ilGenerator.Emit(OpCodes.Ldstr, typeIdType.Name);
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Ldstr, "object");
                }

                return;
            case "Compiler" or "compiler":
                ilGenerator.Emit(OpCodes.Ldstr, "编译环境不需要使用Compiler方法");
                ilGenerator.Emit(OpCodes.Call,
                    typeof(Console).GetMethod("WriteLine", [typeof(string)])!);
                return;
            case "Exec" or "exec":
                return;
        }

        // 查找匹配的方法
        MethodInfo? matchingMethod = null;

        // 首先尝试使用方法名查找
        if (local.DelegateVar.TryGetValue(Id.IdName, out var result))
        {
            // 检查参数数量是否匹配
            var methodParams = result.GetParameters();
            if (methodParams.Length == Ids.Count)
            {
                matchingMethod = result;
            }
        }

        if (matchingMethod == null)
        {
            var classType = local.ClassVar.GetValueOrDefault(Id.IdName);
            if (classType == null) return;

            // 获取默认构造函数
            var constructorInfo = classType.GetConstructor(Type.EmptyTypes);
            if (constructorInfo != null)
            {
                ilGenerator.Emit(OpCodes.Newobj, constructorInfo);
            }

            var localA = ilGenerator.DeclareLocal(classType);
            ilGenerator.Emit(OpCodes.Stloc, localA.LocalIndex);

            var initFunc = classType.GetMethod("init");
            if (initFunc != null)
            {
                // 加载 this 指针
                ilGenerator.Emit(OpCodes.Ldloc, localA.LocalIndex);

                // 加载参数
                var a = initFunc.GetParameters();
                for (var i = 0; i < Ids.Count; i++)
                {
                    var id = Ids[i];
                    id.LoadIlValue(ilGenerator, local);
                    var idType = id.OutputType(local);
                    if (a[i].ParameterType == typeof(object) && idType!.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Box, idType);
                    }
                }

                // 调用 init 方法（实例方法使用 Callvirt）
                ilGenerator.Emit(OpCodes.Callvirt, initFunc);
            }

            // 加载对象实例作为返回值
            ilGenerator.Emit(OpCodes.Ldloc, localA.LocalIndex);

            return;
        }

        // 处理所有类型的方法调用，包括DynamicMethod和MethodBuilder
        var matchingParams = matchingMethod.GetParameters();
        for (var i = 0; i < Ids.Count; i++)
        {
            var id = Ids[i];
            id.LoadIlValue(ilGenerator, local);

            // 确保参数类型匹配
            if (i < matchingParams.Length)
            {
                var paramType = matchingParams[i].ParameterType;
                var idType = id.OutputType(local);

                // 确保参数类型与方法期望的类型匹配
                if (idType != null && paramType != idType)
                {
                    if (paramType == typeof(int) && idType == typeof(int))
                    {
                        // 类型已经匹配，不需要转换
                    }
                    else if (paramType == typeof(int) && idType == typeof(object))
                    {
                        // 从object转换为int
                        ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
                    }
                    else if (paramType == typeof(double) && idType == typeof(object))
                    {
                        // 从object转换为double
                        ilGenerator.Emit(OpCodes.Unbox_Any, typeof(double));
                    }
                    else if (paramType == typeof(object) && idType.IsValueType)
                    {
                        // 从值类型转换为object，需要装箱
                        ilGenerator.Emit(OpCodes.Box, idType);
                    }
                    else if (paramType == typeof(int) && idType == typeof(double))
                    {
                        // 从double转换为int
                        ilGenerator.Emit(OpCodes.Call, typeof(Convert).GetMethod("ToInt32", [typeof(double)])!);
                    }
                    else if (paramType == typeof(double) && idType == typeof(int))
                    {
                        // 从int转换为double
                        ilGenerator.Emit(OpCodes.Conv_R8);
                    }
                }
            }
        }

        // 调用方法
        // 对于DynamicMethod，使用Call指令
        ilGenerator.Emit(OpCodes.Call, matchingMethod);
    }

    public override Type OutputType(LocalManager local)
    {
        switch (Id.IdName)
        {
            case "PrintLine":
            case "Print":
            case "Compiler":
                return typeof(void);
            case "Len":
                return typeof(int);
            case "Json":
                return typeof(string);
            case "Type":
                return typeof(string);
        }

        var result = local.DelegateVar.GetValueOrDefault(Id.IdName);
        if (result != null) return result.ReturnType;
        var classType = local.ClassVar.GetValueOrDefault(Id.IdName);
        return classType ?? typeof(object);
    }
}