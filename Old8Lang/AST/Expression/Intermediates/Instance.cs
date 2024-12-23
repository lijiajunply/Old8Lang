using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

// ReSharper disable once CheckNamespace
namespace Old8Lang.AST.Expression.Value;

public class Instance(OldID oldId, List<OldExpr> ids) : ValueType
{
    public readonly List<OldExpr> Ids = ids;
    public readonly OldID Id = oldId;

    public override ValueType Run(VariateManager Manager)
    {
        var results = Ids.Select(t => t.Run(Manager)).ToList();

        switch (Id.IdName)
        {
            case "Type":
                return new TypeValue(results[0]).Run(Manager);
            case "Exec":
            {
                if (results[0] is not StringValue stringValue) return new VoidValue();
                var a = Manager.Interpreter.Build(code: stringValue.Value);
                a.Run(Manager);
                return new VoidValue();
            }
            case "ShowValues":
            {
                Manager.Interpreter.UseClass.WriteLine(Manager.ToString());
                return new VoidValue();
            }
            case "Json":
            {
                return (results[0] as AnyValue)?.ToJson()
                    as ValueType ?? new VoidValue();
            }
            case "ToObj":
                return (results[0] as StringValue)?.ToObj()
                    as ValueType ?? new VoidValue();
            case "PrintLine":
            {
                if (results.Count == 0)
                {
                    Manager.Interpreter.UseClass.WriteLine("");
                    return new VoidValue();
                }

                var value = results[0].ToString();
                for (var i = 1; i < results.Count; i++) value += results[i].ToString();

                Manager.Interpreter.UseClass.WriteLine(value);
                return new VoidValue();
            }
            case "Print":
            {
                if (results.Count == 0) return new VoidValue();

                var value = results[0].ToString();
                for (var i = 1; i < results.Count; i++) value += results[i].ToString();

                Manager.Interpreter.UseClass.Write(value);
                return new VoidValue();
            }
            case "Compiler":
            {
                if (results.Count == 0) return new VoidValue();
                var value = results[0].ToString();
                var statement = Manager.Interpreter.Build(code: value);
                var dynamicMethod = new DynamicMethod("OldLangRun", null, null, true);
                var ilGenerator = dynamicMethod.GetILGenerator();
                var local = new LocalManager();
                statement.GenerateIL(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Ret);
                foreach (var info in local.DelegateVar)
                {
                    Manager.AddClassAndFunc(new FuncValue(info.Key, info.Value));
                }

                return new VoidValue();
            }
            case "Len":
            {
                var value = results[0].Run(Manager);
                if (value is IOldList list) return new IntValue(list.GetLength());
                throw new Exception($"{results[0]} Not a list");
            }
        }

        var result = Id.Run(Manager);
        if (result is FuncValue funcValue)
        {
            result = funcValue.Run(Manager, Ids);
        }

        // 初始化 调用init方法
        if (result is AnyValue anyValue)
        {
            if (anyValue.Result.TryGetValue("init", out result))
            {
                if (result is not FuncValue value) throw new Exception("init is not function");
                value.Run(anyValue.manager, results.OfType<OldExpr>().ToList());
            }
            else if (results.Count != 0)
            {
                throw new Exception("No corresponding init function found");
            }

            result = anyValue;
        }

        if (result is NativeAnyValue nativeAnyValue)
        {
            List<ValueType> a = [];
            a.AddRange(Ids.Select(id => id.Run(Manager)));
            nativeAnyValue.New(Apis.ListToObjects(a).ToArray());
            result = nativeAnyValue;
        }

        return result;
    }

    public ValueType FromClassToResult(ValueType baseValue)
    {
        var type = baseValue.GetType();
        var m = type.GetMethod(Id.IdName);
        if (m == null)
        {
            type = baseValue switch
            {
                DictionaryValue => Type.GetType("Old8Lang.AST.Expression.DictionaryValueFuncStatic"),
                ListValue => Type.GetType("Old8Lang.AST.Expression.ListValueFuncStatic"),
                _ => Type.GetType("Old8Lang.AST.Expression.ValueTypeFuncStatic")
            };
            m = type?.GetMethod(Id.IdName);
        }

        if (m == null && baseValue is not DictionaryValue or ListValue)
        {
            type = Type.GetType("Old8Lang.AST.Expression.ValueTypeFuncStatic");
            m = type?.GetMethod(Id.IdName);
        }

        var os = new List<object>() { baseValue };
        os.AddRange(Ids);
        var r = m?.Invoke(baseValue, os.ToArray());
        if (r is ValueType v) return v;
        return ObjToValue(r!);
    }

    public override string ToString()
    {
        return Id.IdName switch
        {
            "PrintLine" => $"Console.WriteLine({Apis.ListToString(Ids)});",
            "Print" => $"Console.Write({Apis.ListToString(Ids)});",
            _ => Id + "(" + Apis.ListToString(Ids) + ")"
        };
    }

    public override void LoadILValue(ILGenerator ilGenerator, LocalManager local)
    {
        switch (Id.IdName)
        {
            case "PrintLine":
                var id = Ids[0];
                id.LoadILValue(ilGenerator, local);
                var type = id.OutputType(local)!;
                ilGenerator.Emit(OpCodes.Call,
                    typeof(Console).GetMethod("WriteLine", [type])!);
                return;
            case "Print":
                id = Ids[0];
                id.LoadILValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Call,
                    typeof(Console).GetMethod("Write", [id.OutputType(local)!])!);
                return;
            case "Json":
                return;
            case "ToObj":
                return;
            case "Len":
                id = Ids[0];
                id.LoadILValue(ilGenerator, local);
                type = id.OutputType(local)!;
                if (type.IsAssignableTo(typeof(object[])))
                {
                    var lengthProp = typeof(object[]).GetProperty("Length");
                    ilGenerator.Emit(OpCodes.Call, lengthProp!.GetGetMethod()!);
                    return;
                }

                var countProp = typeof(List<object>).GetProperty("Count");
                ilGenerator.Emit(OpCodes.Call, countProp!.GetGetMethod()!);
                return;
            case "Type":
                id = Ids[0];
                id.LoadILValue(ilGenerator, local);
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

            var local_a = ilGenerator.DeclareLocal(classType);
            ilGenerator.Emit(OpCodes.Stloc, local_a.LocalIndex);
            ilGenerator.Emit(OpCodes.Ldloc, local_a.LocalIndex);

            var initFunc = classType.GetMethod("init");
            if (initFunc == null) return;
            var a = initFunc.GetParameters();
            for (var i = 0; i < Ids.Count; i++)
            {
                var id = Ids[i];
                id.LoadILValue(ilGenerator, local);
                var idType = id.OutputType(local);
                if (a[i].ParameterType == typeof(object) && idType!.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Box, idType);
                }
            }

            ilGenerator.Emit(OpCodes.Call, initFunc);
            ilGenerator.Emit(OpCodes.Ldloc, local_a.LocalIndex);

            return;
        }

        if (result is MethodBuilder)
        {
            foreach (var id in Ids)
            {
                id.LoadILValue(ilGenerator, local);
            }
        }
        else
        {
            var a = result.GetParameters();
            for (var i = 0; i < Ids.Count; i++)
            {
                var id = Ids[i];
                id.LoadILValue(ilGenerator, local);
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