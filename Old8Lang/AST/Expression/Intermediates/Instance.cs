using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;

// ReSharper disable once CheckNamespace
namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 实例，a(b,c)
/// </summary>
/// <param name="langId"></param>
/// <param name="ids"></param>
/// <param name="position"></param>
public class Instance(LangId langId, List<OldExpr> ids, SourcePosition position = default) : LangValueType(position)
{
    public readonly List<OldExpr> Ids = ids;
    public readonly LangId Id = langId;

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
                if (results[0] is not AnyLangValue jsonAnyValue)
                    throw new TypeError(this, "AnyValue", results[0].GetType().Name);
                return jsonAnyValue.ToJson();
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
            case "Test" or "test":
            {
                var value = results[0].Run(manager);
                var value1 = results[1].Run(manager);
                return value.Equal(value1) ? new BoolLangValue(true) : throw new InvalidOperationError(this, "测试失败");
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
        return Id.IdName switch
        {
            "PrintLine" => $"print({string.Join(", ", Ids)})",
            "Print" => $"print({string.Join(", ", Ids)})",
            _ => $"{Id}({string.Join(", ", Ids)})"
        };
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        switch (Id.IdName)
        {
            case "PrintLine":
                var id = Ids[0];
                id.LoadIlValue(ilGenerator, local);
                var type = id.OutputType(local)!;
                ilGenerator.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", [type])!);
                return;
            case "Print":
                id = Ids[0];
                id.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Call,
                    typeof(Console).GetMethod("Write", [id.OutputType(local)!])!);
                return;
            case "Json":
                return;
            case "ToObj":
                return;
            case "Len":
                id = Ids[0];
                id.LoadIlValue(ilGenerator, local);
                type = id.OutputType(local)!;
                var lengthProp = type.GetProperty(type.IsAssignableTo(typeof(object[])) ? "Length" : "Count");
                ilGenerator.Emit(OpCodes.Call, lengthProp!.GetGetMethod()!);
                return;
            case "Type":
                id = Ids[0];
                id.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Call, typeof(object).GetMethod("GetType")!);
                return;
            case "Compiler":
                ilGenerator.Emit(OpCodes.Ldstr, "编译环境不需要使用Compiler方法");
                ilGenerator.Emit(OpCodes.Call,
                    typeof(Console).GetMethod("WriteLine", [typeof(string)])!);
                return;
            case "Exec":
                return;
        }

        var result = local.DelegateVar.GetValueOrDefault(Id.IdName);

        if (result == null)
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
            ilGenerator.Emit(OpCodes.Ldloc, localA.LocalIndex);

            var initFunc = classType.GetMethod("init");
            if (initFunc == null) return;
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

            ilGenerator.Emit(OpCodes.Call, initFunc);
            ilGenerator.Emit(OpCodes.Ldloc, localA.LocalIndex);

            return;
        }

        if (result is MethodBuilder)
        {
            foreach (var id in Ids)
            {
                id.LoadIlValue(ilGenerator, local);
            }
        }
        else
        {
            var a = result.GetParameters();
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
        }

        ilGenerator.Emit(OpCodes.Call, result);
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
        }

        var result = local.DelegateVar.GetValueOrDefault(Id.IdName);
        if (result != null) return result.ReturnType;
        var classType = local.ClassVar.GetValueOrDefault(Id.IdName);
        return classType ?? typeof(object);
    }
}