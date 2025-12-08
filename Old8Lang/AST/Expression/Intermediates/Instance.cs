using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;

// ReSharper disable once CheckNamespace
namespace Old8Lang.AST.Expression.Value;

public class Instance(LangId langId, List<OldExpr> ids, SourcePosition position = default) : LangValueType(position)
{
    public readonly List<OldExpr> Ids = ids;
    public readonly LangId Id = langId;

    public override LangValueType Run(LangParser.VariateManager manager)
    {
        var results = Ids.Select(t => t.Run(manager)).ToList();

        switch (Id.IdName)
        {
            case "Type":
                return new TypeLangValue(results[0]).Run(manager);
            case "Exec":
            {
                if (results[0] is not StringLangValue execStringValue) throw new TypeError(this, "StringValue", results[0].GetType().Name);
                var a = manager.Interpreter.Build(code: execStringValue.Value);
                a.Run(manager);
                return new VoidLangValue();
            }
            case "ShowValues":
            {
                manager.Interpreter.UseClass.WriteLine(manager.ToString());
                return new VoidLangValue();
            }
            case "Json":
            {
                if (results[0] is not AnyLangValue jsonAnyValue) throw new TypeError(this, "AnyValue", results[0].GetType().Name);
                return jsonAnyValue.ToJson();
            }
            case "ToObj":
                if (results[0] is not StringLangValue stringValue) throw new TypeError(this, "StringValue", results[0].GetType().Name);
                return stringValue.ToObj();
            case "PrintLine":
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
            case "Print":
            {
                if (results.Count == 0) return new VoidLangValue();

                var value = results[0].ToDisplayString();
                for (var i = 1; i < results.Count; i++) value += results[i].ToDisplayString();

                manager.Interpreter.UseClass.Write(value);
                return new VoidLangValue();
            }
            case "Compiler":
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
            case "Len":
            {
                var value = results[0].Run(manager);
                if (value is ILangList list) return new IntLangValue(list.GetLength());
                throw new InvalidOperationError(this, $"{results[0]} 不是列表类型");
            }
        }

        var result = Id.Run(manager);
        if (result is FuncLangValue funcValue)
        {
            result = funcValue.Run(manager, Ids);
        }

        // 初始化 调用init方法
        if (result is AnyLangValue anyValue)
        {
            if (anyValue.Result.TryGetValue("init", out result))
            {
                if (result is not FuncLangValue value) throw new TypeError(this, "FuncValue", "init 不是函数类型");
                value.Run(anyValue.Manager, Ids);
            }
            else if (results.Count != 0)
            {
                throw new InvalidOperationError(this, "找不到对应的init函数");
            }

            result = anyValue;
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
        var r = m?.Invoke(baseLangValue, [.. os]);
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