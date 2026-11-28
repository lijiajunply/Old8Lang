using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Linq;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
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
/// InterpreterVisitor - Value 节点的 Visit 方法实现
/// </summary>
public partial class InterpreterVisitor
{
    /// <summary>
    /// 访问 IntLangValue 节点
    /// </summary>
    public LangValueType VisitIntLangValue(IntLangValue node)
    {
        // 值类型节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 DoubleLangValue 节点
    /// </summary>
    public LangValueType VisitDoubleLangValue(DoubleLangValue node)
    {
        return node;
    }

    /// <summary>
    /// 访问 StringLangValue 节点
    /// </summary>
    public LangValueType VisitStringLangValue(StringLangValue node)
    {
        return node;
    }

    /// <summary>
    /// 访问 BoolLangValue 节点
    /// </summary>
    public LangValueType VisitBoolLangValue(BoolLangValue node)
    {
        return node;
    }

    /// <summary>
    /// 访问 CharLangValue 节点
    /// </summary>
    public LangValueType VisitCharLangValue(CharLangValue node)
    {
        return node;
    }

    /// <summary>
    /// 访问 NullLangValue 节点
    /// </summary>
    public LangValueType VisitNullLangValue(NullLangValue node)
    {
        return node;
    }

    /// <summary>
    /// 访问 VoidLangValue 节点
    /// </summary>
    public LangValueType VisitVoidLangValue(VoidLangValue node)
    {
        return node;
    }

    /// <summary>
    /// 访问 ArrayLangValue 节点
    /// </summary>
    public LangValueType VisitArrayLangValue(ArrayLangValue node)
    {
        // 完整迁移自 ArrayLangValue.Run()
        // 执行数组中的表达式并返回自身
        for (var i = 0; i < node.Values.Count; i++)
            node.RunResult[i] = node.Values[i].Accept(this);
        return node;
    }

    /// <summary>
    /// 访问 ListLangValue 节点
    /// </summary>
    public LangValueType VisitListLangValue(ListLangValue node)
    {
        // 完整迁移自 ListLangValue.Run()
        // 只有当Values为空且Value中有表达式时才需要执行，且没有被手动清空过
        if (node.Values.Count == 0 && node.Value.Count > 0 && !IsListCleared(node))
        {
            foreach (var expr in node.Value)
                node.Values.Add(expr.Accept(this));
        }

        return node;
    }

    /// <summary>
    /// 检查列表是否被清空（通过反射访问私有字段）
    /// </summary>
    private static bool IsListCleared(ListLangValue list)
    {
        var field = typeof(ListLangValue).GetField("HasBeenCleared",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field is not null && (bool)field.GetValue(list)!;
    }

    /// <summary>
    /// 访问 DictionaryLangValue 节点
    /// </summary>
    public LangValueType VisitDictionaryLangValue(DictionaryLangValue node)
    {
        // 完整迁移自 DictionaryLangValue.Run()
        // 清空之前的值，避免重复添加
        node.Value.Clear();

        foreach (var tuple in node.Tuples)
        {
            tuple.Accept(this);
            node.Value.Add((tuple.Get(0), tuple.Get(1)));
        }

        return node;
    }

    /// <summary>
    /// 访问 TupleLangValue 节点
    /// </summary>
    public LangValueType VisitTupleLangValue(TupleLangValue node)
    {
        // 完整迁移自 TupleLangValue.Run()
        // 清空之前的值
        node.ItemValues.Clear();
        
        // 运行所有元素
        foreach (var element in node.Elements)
        {
            var result = element.Accept(this);
            node.ItemValues.Add(result);
        }

        return node;
    }

    /// <summary>
    /// 访问 RangeLangValue 节点
    /// </summary>
    public LangValueType VisitRangeLangValue(RangeLangValue node)
    {
        // 完整迁移自 RangeLangValue.Run()
        // 范围表达式，创建一个整数数组
        var results = new List<LangValueType>();

        var startValue = node.Start?.Accept(this);
        var endValue = node.End?.Accept(this);

        if (startValue is not IntLangValue startIntValue || endValue is not IntLangValue endIntValue)
            throw new TypeError(node, "IntValue",
                $"RangeValue: start 或 end 不是 IntValue，实际得到了 {startValue?.GetType().Name} 和 {endValue?.GetType().Name}");

        // 与 IL 模式共用同一套区间算法：方向由原始端点决定，排除端点只收窄区间。
        // 不要在这里各自实现——两份实现曾对 [5>~<5] 给出 [6, 5, 4]（应为空）。
        var rangeValues = RangeLangValue.CreateRangeArray(
            startIntValue.Value, endIntValue.Value, node.IncludeStart, node.IncludeEnd);
        foreach (var value in rangeValues)
        {
            results.Add(new IntLangValue(value));
        }

        return new ArrayLangValue(results);
    }

    /// <summary>
    /// 访问 SliceLangValue 节点
    /// </summary>
    public LangValueType VisitSliceLangValue(SliceLangValue node)
    {
        // 完整迁移自 SliceLangValue.Run()
        // 切片表达式
        var value = node.Id.Accept(this);
        var start1 = node.Start?.Accept(this);
        var end1 = node.End?.Accept(this);
        var step1 = node.Step?.Accept(this);

        if (value is not ILangList list)
            throw new InvalidOperationError(node, $"类型 '{value.GetType().Name}' 不支持切片操作");

        var length = list.GetLength();
        var stepValue = step1?.GetValue<int>() ?? 1;

        // 如果步长为0，抛出错误
        if (stepValue == 0)
            throw new InvalidOperationError(node, "切片步长不能为0");

        // 处理负数步长
        int startValue, endValue;
        if (stepValue > 0)
        {
            // 正向切片
            startValue = start1?.GetValue<int>() ?? 0;
            endValue = end1?.GetValue<int>() ?? length;
        }
        else
        {
            // 反向切片
            startValue = start1?.GetValue<int>() ?? length - 1;
            endValue = end1?.GetValue<int>() ?? -1;
        }

        return list.Slice(startValue, endValue, stepValue);
    }

    /// <summary>
    /// 访问 Instance 节点
    /// </summary>
    public LangValueType VisitInstance(Instance node)
    {

        // 首先尝试通过全局函数注册器执行
        if (node.TryExecuteGlobalFunction(manager, out var globalFuncResult))
        {
            return globalFuncResult!;
        }

        LangValueType result;

        // 计算总参数数量（位置参数 + 命名参数）
        var totalArgCount = node.Ids.Count + (node.NamedArgs?.Count ?? 0);

        // 获取所有可能的匹配函数（函数名和参数数量匹配）
        var matchingFunctions = manager.ImportInfos
            .Where(x => (x is FuncLangValue func && func.Id?.IdName == node.Id.IdName && func.Ids?.Count == totalArgCount)
                        || (x is AsyncFuncLangValue asyncFunc && asyncFunc.Id?.IdName == node.Id.IdName &&
                            asyncFunc.Ids?.Count == totalArgCount))
            .ToList();

        if (matchingFunctions.Count > 0)
        {
            // 查找最匹配的函数
            object? bestMatch = null;

            // 如果有多个重载，选择第一个（参数数量已经匹配）
            // 命名参数的类型检查将在FuncLangValue.Run中进行
            if (matchingFunctions.Count == 1)
            {
                bestMatch = matchingFunctions[0];
            }
            else if (matchingFunctions.Count > 1 && node.Ids.Count > 0)
            {
                // 有多个重载且有位置参数时，进行类型匹配
                // 计算位置参数的实际值和类型
                var paramValues = node.Ids.Select(t => t.Accept(this)).ToList();

                foreach (var func in matchingFunctions)
                {
                    if (func is FuncLangValue { Ids: not null } funcValue)
                    {
                        bool isMatch = true;
                        bool isExactMatch = true;

                        // 只检查位置参数的类型是否匹配
                        for (int i = 0; i < node.Ids.Count && i < funcValue.Ids.Count; i++)
                        {
                            var paramType = funcValue.Ids[i].AssumptionType;
                            var argValue = paramValues[i];

                            // 如果参数没有类型注解，视为匹配
                            if (string.IsNullOrEmpty(paramType))
                            {
                                isExactMatch = false;
                                continue;
                            }

                            // 检查参数类型是否匹配
                            string argTypeName = argValue.GetType().Name;
                            if (argTypeName.StartsWith(paramType, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            // 类型不匹配
                            isMatch = false;
                            break;
                        }

                        if (isMatch)
                        {
                            if (isExactMatch)
                            {
                                // 找到精确匹配，直接使用
                                bestMatch = funcValue;
                                break;
                            }
                            else if (bestMatch is null)
                            {
                                // 记录第一个匹配的函数
                                bestMatch = funcValue;
                            }
                        }
                    }
                    else if (func is AsyncFuncLangValue asyncFuncValue)
                    {
                        // 异步函数暂时只检查数量
                        bestMatch = asyncFuncValue;
                        break;
                    }
                }

                // 如果没有找到匹配的，使用第一个
                if (bestMatch is null && matchingFunctions.Count > 0)
                {
                    bestMatch = matchingFunctions[0];
                }
            }
            else
            {
                // 只有命名参数或者只有一个匹配的函数，直接使用第一个
                bestMatch = matchingFunctions[0];
            }

            // 如果找到匹配的函数，调用它
            if (bestMatch is not null)
            {
                if (bestMatch is AsyncFuncLangValue asyncFunc)
                {
                    // 已捕获闭包的异步函数值（如异步 Lambda / 装饰器返回闭包）保留其原始 CapturedScope
                    var closedFunc = asyncFunc.CapturedScope is not null && !asyncFunc.IsAsyncGenerator
                        ? asyncFunc
                        : asyncFunc.Accept(this);

                    // 如果是异步生成器，需要设置参数
                    if (closedFunc is AsyncGeneratorLangValue asyncGen)
                    {
                        // 计算参数值
                        var paramValuesCopy = node.Ids.Select(t => t.Accept(this)).ToList();

                        // 处理默认参数，补全缺失的参数值
                        if (asyncFunc.Ids is { Count: > 0 })
                        {
                            for (var i = paramValuesCopy.Count; i < asyncFunc.Ids.Count; i++)
                            {
                                var id = asyncFunc.Ids[i];
                                if (id.DefaultValue is not null)
                                {
                                    var defaultValue = id.DefaultValue.Accept(this);
                                    paramValuesCopy.Add(defaultValue);
                                }
                            }
                        }

                        // 将参数值设置到异步生成器
                        if (asyncFunc.Ids is { Count: > 0 })
                        {
                            for (var i = 0; i < asyncFunc.Ids.Count; i++)
                            {
                                var paramId = asyncFunc.Ids[i];
                                var paramValue = paramValuesCopy[i];
                                asyncGen.SetParameter(paramId.IdName, paramValue);
                            }
                        }

                        result = asyncGen;
                    }
                    else if (closedFunc is AsyncFuncLangValue closedAsyncFunc)
                    {
                        // 如果是异步函数，调用 RunAsync()
                        result = closedAsyncFunc.RunAsync(manager, node.Ids);
                    }
                    else
                    {
                        // 不应该到达这里
                        result = closedFunc;
                    }
                }
                else if (bestMatch is FuncLangValue funcValue)
                {
                    // 如果是泛型函数且未实例化，尝试自动推断类型参数
                    if (funcValue is { IsGeneric: true, TypeArgumentMapping: null })
                    {
                        if (manager.Interpreter is null)
                        {
                            throw new InvalidOperationError(node, "无法执行泛型类型推断：解释器未初始化");
                        }

                        var typeAnnotationManager = manager.Interpreter.TypeAnnotationManager;
                        var inference = new GenericTypeInference(typeAnnotationManager);
                        var inferredTypes = inference.InferFunctionTypeArguments(funcValue, node.Ids, manager, node.Position);

                        if (inferredTypes is not null)
                        {
                            // 使用推断出的类型实例化泛型函数
                            var instantiatedFunc = funcValue.InstantiateGeneric(inferredTypes, typeAnnotationManager);
                            result = instantiatedFunc.Run(manager, node.Ids, node.NamedArgs, node.Position);
                        }
                        else
                        {
                            // 无法推断类型，抛出错误
                            throw new InvalidOperationError(node,
                                $"无法推断泛型函数 '{funcValue.Id?.IdName}' 的类型参数，请使用显式类型参数调用：{funcValue.Id?.IdName}<类型>(...)");
                        }
                    }
                    else
                    {
                        // 找到匹配的重载函数，直接调用
                        result = funcValue.Run(manager, node.Ids, node.NamedArgs, node.Position);
                    }
                }
                else
                {
                    // 其他类型的 ImportInfo，使用单参数 Run
                    result = ((ImportInfo)bestMatch).Accept(new InterpreterVisitor(manager));
                }
            }
            else
            {
                // 没有找到匹配的函数，使用原来的方式查找
                var idResult = node.Id.Accept(this);
                result = idResult;

                // 后续处理...
                if (idResult is TypeTemplate typeTemplate)
                {
                    // 创建类的实例（使用 V2 架构）
                    var instance = typeTemplate.CreateInstanceWithInference(node.Ids, manager);

                    // 初始化实例，设置Interpreter
                    instance.Init(manager.Interpreter);

                    // 调用 init 构造函数
                    instance.CallInit(node.Ids, manager);

                    result = instance;
                }
                // 如果idResult是FuncLangValue，则调用它
                else if (idResult is FuncLangValue funcValue)
                {
                    // 如果是泛型函数且未实例化，尝试自动推断类型参数
                    if (funcValue is { IsGeneric: true, TypeArgumentMapping: null })
                    {
                        if (manager.Interpreter is null)
                        {
                            throw new InvalidOperationError(node, "无法执行泛型类型推断：解释器未初始化");
                        }

                        var typeAnnotationManager = manager.Interpreter.TypeAnnotationManager;
                        var inference = new GenericTypeInference(typeAnnotationManager);
                        var inferredTypes = inference.InferFunctionTypeArguments(funcValue, node.Ids, manager, node.Position);

                        if (inferredTypes is not null)
                        {
                            // 使用推断出的类型实例化泛型函数
                            var instantiatedFunc = funcValue.InstantiateGeneric(inferredTypes, typeAnnotationManager);
                            result = instantiatedFunc.Run(manager, node.Ids, node.NamedArgs, node.Position);
                        }
                        else
                        {
                            // 无法推断类型，抛出错误
                            throw new InvalidOperationError(node,
                                $"无法推断泛型函数 '{funcValue.Id?.IdName}' 的类型参数，请使用显式类型参数调用：{funcValue.Id?.IdName}<类型>(...)");
                        }
                    }
                    else
                    {
                        // 直接调用函数，参数表达式会在函数体内执行
                        result = funcValue.Run(manager, node.Ids, node.NamedArgs, node.Position);
                    }
                }
                // 如果idResult是AsyncFuncLangValue，则调用它
                else if (idResult is AsyncFuncLangValue asyncFuncValue)
                {
                    var closedAsyncFunc = asyncFuncValue.CapturedScope is not null && !asyncFuncValue.IsAsyncGenerator
                        ? asyncFuncValue
                        : (AsyncFuncLangValue)asyncFuncValue.Accept(this);
                    result = closedAsyncFunc.RunAsync(manager, node.Ids);
                }
                // 如果idResult是TaskStaticMethodWrapper，则调用它
                else if (idResult is TaskStaticMethodWrapper taskMethodWrapper)
                {
                    // 执行静态方法
                    var args = node.Ids.Select(id => id.Accept(this)).ToList();
                    result = taskMethodWrapper.Invoke(args, node.Position);
                }
                // 如果idResult是ThreadStaticMethodWrapper，则调用它
                else if (idResult is ThreadStaticMethodWrapper threadMethodWrapper)
                {
                    // 执行静态方法
                    var args = node.Ids.Select(id => id.Accept(this)).ToList();
                    result = threadMethodWrapper.Invoke(args, node.Position);
                }
            }
        }
        else
        {
            // 如果没有找到重载函数，使用原来的方式查找
            var idResult = node.Id.Accept(this);
            result = idResult;

            // 如果idResult是TypeTemplate，则创建其实例
            if (idResult is TypeTemplate typeTemplate)
            {
                // 创建类的实例（使用 V2 架构）
                var instance = typeTemplate.CreateInstanceWithInference(node.Ids, manager);

                // 初始化实例，设置Interpreter
                instance.Init(manager.Interpreter);

                // 调用 init 构造函数
                instance.CallInit(node.Ids, manager);

                result = instance;
            }
            // 如果idResult是FuncLangValue，则调用它
            else if (idResult is FuncLangValue funcValue)
            {
                // 如果是泛型函数且未实例化,尝试自动推断类型参数
                if (funcValue is { IsGeneric: true, TypeArgumentMapping: null })
                {
                    if (manager.Interpreter is null)
                    {
                        throw new InvalidOperationError(node, "无法执行泛型类型推断：解释器未初始化");
                    }

                    var typeAnnotationManager = manager.Interpreter.TypeAnnotationManager;
                    var inference = new GenericTypeInference(typeAnnotationManager);
                    var inferredTypes = inference.InferFunctionTypeArguments(funcValue, node.Ids, manager, node.Position);

                    if (inferredTypes is not null)
                    {
                        // 使用推断出的类型实例化泛型函数
                        var instantiatedFunc = funcValue.InstantiateGeneric(inferredTypes, typeAnnotationManager);
                        result = instantiatedFunc.Run(manager, node.Ids, node.NamedArgs, node.Position);
                    }
                    else
                    {
                        // 无法推断类型，抛出错误
                        throw new InvalidOperationError(node,
                            $"无法推断泛型函数 '{funcValue.Id?.IdName}' 的类型参数，请使用显式类型参数调用：{funcValue.Id?.IdName}<类型>(...)");
                    }
                }
                else
                {
                    // 直接调用函数，参数表达式会在函数体内执行
                    result = funcValue.Run(manager, node.Ids, node.NamedArgs, node.Position);
                }
            }
            // 如果idResult是NativeDelegateFuncLangValue，则调用它
            else if (idResult is NativeDelegateFuncLangValue nativeDelegate)
            {
                result = nativeDelegate.Run(manager, node.Ids);
            }
            // 如果idResult是AsyncFuncLangValue，则调用它
            else if (idResult is AsyncFuncLangValue asyncFuncValue)
            {
                var closedAsyncFunc = asyncFuncValue.CapturedScope is not null && !asyncFuncValue.IsAsyncGenerator
                    ? asyncFuncValue
                    : (AsyncFuncLangValue)asyncFuncValue.Accept(this);
                result = closedAsyncFunc.RunAsync(manager, node.Ids);
            }
            // 如果idResult是TaskStaticMethodWrapper，则调用它
            else if (idResult is TaskStaticMethodWrapper taskMethodWrapper)
            {
                // 执行静态方法
                var args = node.Ids.Select(id => id.Accept(this)).ToList();
                result = taskMethodWrapper.Invoke(args, node.Position);
            }
            // 如果idResult是ThreadStaticMethodWrapper，则调用它
            else if (idResult is ThreadStaticMethodWrapper threadMethodWrapper)
            {
                // 执行静态方法
                var args = node.Ids.Select(id => id.Accept(this)).ToList();
                result = threadMethodWrapper.Invoke(args, node.Position);
            }
        }

        // 注意：TypeTemplate.CreateInstanceV2 路径已经处理了构造函数调用
        // 移除了这里的重复 init 调用逻辑，避免构造函数被调用两次

        if (result is NativeAnyLangValue nativeAnyValue)
        {
            List<LangValueType> a = [];
            a.AddRange(node.Ids.Select(id => id.Accept(this)));
            nativeAnyValue.New([.. Apis.ListToObjects(a)]);
            result = nativeAnyValue;
        }

        return result;
    
    
    }

    /// <summary>
    /// 访问 TypeLangValue 节点
    /// </summary>
    public LangValueType VisitTypeLangValue(TypeLangValue node)
    {
        // 迁移自 TypeLangValue.Run()
        var result = node._expression?.Accept(this);
        if (result is null) throw new InvalidOperationError(node, "类型表达式求值失败");
        node.Value = result.TypeToString();
        return node;
    }

    /// <summary>
    /// 访问 StringTemplateValue 节点
    /// </summary>
    public LangValueType VisitStringTemplateValue(StringTemplateValue node)
    {
        // 完整迁移自 StringTemplateValue.Run()
        // 字符串模板插值 - 将所有部分拼接成字符串
        var result = node.ExpressionList.Select(item => item.Accept(this)).Aggregate(string.Empty,
            (current, exprResult) => current + exprResult.ToDisplayString());

        return StringLangValue.Create(result);
    }

    /// <summary>
    /// 访问 ListComprehension 节点
    /// </summary>
    public LangValueType VisitListComprehension(ListComprehension node)
    {

        var resultList = new List<LangValueType>();

        // 执行最外层的可迭代对象
        var iterableValue = node.Iterable.Accept(this);

        // 获取可迭代对象的元素
        IEnumerable<LangValueType> items;

        if (iterableValue is ILangList list)
        {
            items = list.GetItems();
        }
        else if (iterableValue is ArrayLangValue array)
        {
            items = array.GetItems();
        }
        else if (iterableValue is ListLangValue listValue)
        {
            items = listValue.GetItems();
        }
        else if (iterableValue is RangeLangValue range)
        {
            // 处理范围类型，执行range表达式获取实际的数组
            var rangeResult = range.Accept(this);
            if (rangeResult is ArrayLangValue rangeArray)
            {
                items = rangeArray.GetItems();
            }
            else
            {
                throw new TypeError(
                    node.Iterable,
                    "范围表达式必须返回数组类型");
            }
        }
        else if (iterableValue is StringLangValue str)
        {
            // 处理字符串，生成字符列表
            items = str.Value
                .Select(c => new CharLangValue(c) as LangValueType);
        }
        else
        {
            throw new TypeError(
                node.Iterable,
                $"类型不匹配: 列表推导式的可迭代对象必须是数组、列表、范围或字符串，但得到 {iterableValue.GetType().Name}");
        }

        // 遍历可迭代对象的每个元素
        foreach (var item in items)
        {
            // 创建一个全新的变量管理器，复制当前的变量状态
            var newManager = manager.NewManger();

            // 设置当前变量的值
            newManager.Set(node.Variable, item);

            // 如果有嵌套的for循环，递归处理
            if (node.NestedLoops is { Count: > 0 })
            {
                // 处理嵌套的for循环，收集结果
                var nestedResults = node.ProcessNestedLoops(newManager, node.NestedLoops);
                resultList.AddRange(nestedResults);
            }
            else
            {
                // 没有嵌套循环，直接处理当前元素
                if (node.CheckCondition(newManager))
                {
                    // 条件满足，计算表达式值并添加到结果列表
                    var exprValue = node.Expression.Accept(this);
                    resultList.Add(exprValue);
                }
            }
        }

        // 返回生成的列表
        // 创建一个新的ListLangValue，使用对象列表构造函数
        return new ListLangValue(resultList.Select(v => v.GetValue()!).ToList(), position: node.Position);
    
    
    }

    /// <summary>
    /// 访问 NestedIndexAccess 节点
    /// </summary>
    public LangValueType VisitNestedIndexAccess(NestedIndexAccess node)
    {

        // 首先运行基础表达式，获取结果
        var baseResult = node.BaseExpression.Accept(this);

        // 检查基础结果是否支持索引访问
        if (baseResult is ListLangValue list)
        {
            if (node.NestedIndex.Accept(this) is not IntLangValue intResult)
                throw new TypeError(node, "IntValue", node.NestedIndex.Accept(this).GetType().Name);
            return list.Get(intResult);
        }

        if (baseResult is ArrayLangValue array)
        {
            if (node.NestedIndex.Accept(this) is not IntLangValue intResult)
                throw new TypeError(node, "IntValue", node.NestedIndex.Accept(this).GetType().Name);
            return array.Get(intResult);
        }

        if (baseResult is DictionaryLangValue dict)
        {
            if (node.NestedIndex.Accept(this) is not { } keyResult)
                throw new TypeError(node, "ValueType", node.NestedIndex.Accept(this).GetType().Name);
            return dict.Get(keyResult);
        }

        if (baseResult is StringLangValue str)
        {
            if (node.NestedIndex.Accept(this) is not IntLangValue intResult)
                throw new TypeError(node, "IntValue", node.NestedIndex.Accept(this).GetType().Name);
            return str.Get(intResult);
        }

        if (baseResult is TupleLangValue tuple)
        {
            return node.NestedIndex.Accept(this) is not IntLangValue intResult
                ? throw new TypeError(node, "IntValue", node.NestedIndex.Accept(this).GetType().Name)
                : tuple.Get(intResult);
        }

        throw new InvalidOperationError(node, $"不支持的嵌套索引访问类型: {baseResult.GetType().Name}");
    
    
    }

    /// <summary>
    /// 访问 NestedSliceAccess 节点
    /// </summary>
    public LangValueType VisitNestedSliceAccess(NestedSliceAccess node)
    {

        // 首先运行基础表达式，获取结果
        var baseResult = node.BaseExpression.Accept(this);

        // 将切片参数转换为值
        var startValue = node.SliceStart.Accept(this);
        var endValue = node.SliceEnd?.Accept(new InterpreterVisitor(manager));
        var stepValue = node.SliceStep?.Accept(new InterpreterVisitor(manager));

        var start = startValue.GetValue<int>();
        var end = endValue?.GetValue<int>() ?? int.MaxValue;
        var step = stepValue?.GetValue<int>() ?? 1;

        // 根据基础结果的类型执行切片操作
        if (baseResult is ILangList list)
        {
            return list.Slice(start, end, step);
        }

        throw new InvalidOperationError(node, $"不支持的嵌套切片访问类型: {baseResult.GetType().Name}");
    
    
    }

    /// <summary>
    /// 访问 MethodOverloadList 节点
    /// </summary>
    public LangValueType VisitMethodOverloadList(MethodOverloadList node)
    {

        return node;
    
    
    }

    /// <summary>
    /// 访问 SuperProxy 节点
    /// </summary>
    public LangValueType VisitSuperProxy(SuperProxy node)
    {

        // 获取当前实例（node）
        var currentInstance = manager.GetCurrentInstance();

        if (currentInstance is null)
        {
            throw new InvalidOperationError(node.Position, "super只能在类实例方法中使用");
        }

        // 返回一个特殊的SuperProxy对象，用于延迟解析父类成员访问
        return new SuperProxy(currentInstance, manager, node.Position);
    
    
    }

    /// <summary>
    /// 访问 AnyLangValue 节点
    /// </summary>
    public LangValueType VisitAnyLangValue(AnyLangValue node)
    {
        // 迁移自 AnyLangValue.Run()
        // 值类型节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 ErrorLangValue 节点
    /// </summary>
    public LangValueType VisitErrorLangValue(ErrorLangValue node)
    {
        // 迁移自 ErrorLangValue.Run()
        // 错误值节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 LangListItem 节点
    /// </summary>
    public LangValueType VisitLangListItem(LangListItem node)
    {

        var a = manager.GetValue(node.ListId);
        LangExpression result = node.Key.Accept(this);

        // 检查是否是 AnyLangValue（类实例）且定义了 _getitem 方法
        if (a is AnyLangValue anyValue)
        {
            var getitemMethods = anyValue.Metadata.MethodTable.LookupMethod("_getitem");
            if (getitemMethods is not null && getitemMethods.Count > 0)
            {
                // 创建一个 Instance 表达式来调用 _getitem 方法
                var getitemCall = new Instance(
                    new LangId("_getitem"),
                    [result],
                    [],
                    node.Position
                );

                // 通过 Dot 操作调用方法
                return anyValue.Dot(getitemCall, manager);
            }
        }

        if (a is ListLangValue list)
        {
            if (result is not IntLangValue intResult) throw new TypeError(node, "IntValue", result.GetType().Name);
            return list.Get(intResult);
        }

        if (a is ArrayLangValue array)
        {
            if (result is not IntLangValue i) throw new TypeError(node, "IntValue", result.GetType().Name);
            return array.Get(i);
        }

        if (a is DictionaryLangValue dir)
        {
            if (result is not LangValueType keyResult) throw new TypeError(node, "ValueType", result.GetType().Name);
            return dir.Get(keyResult);
        }

        if (a is StringLangValue str)
        {
            if (result is not IntLangValue intResult) throw new TypeError(node, "IntValue", result.GetType().Name);
            return str.Get(intResult);
        }

        if (a is TupleLangValue tuple)
        {
            if (result is not IntLangValue intResult) throw new TypeError(node, "IntValue", result.GetType().Name);
            return tuple.Get(intResult);
        }

        throw new InvalidOperationError(node, $"不支持的集合类型: {a?.GetType().Name ?? "null"}");
    
    
    }

    // ==================== 异步和生成器相关节点 ====================

    /// <summary>
    /// 访问 FuncLangValue 节点
    /// </summary>
    public LangValueType VisitFuncLangValue(FuncLangValue node)
    {

        // 如果这个函数没有方法引用（即是 Old8Lang 函数而非原生方法）
        if (node.Method is null && node.Ids is not null)
        {
            // 对于生成器函数，我们需要特殊处理
            if (node.ContainsYieldStatement(node.BlockStatement))
            {
                // 检查是否有参数需要处理
                if (node.Ids.Count > 0)
                {
                    // 对于生成器函数，我们需要创建一个闭包，捕获当前作用域
                    var generatorClosure =
                        new FuncLangValue(node.Id, node.Ids, node.BlockStatement, node.GenericParameters, node.Position, node.IsLambda)
                        {
                            // 生成器需要独立的作用域副本，使用深拷贝
                            CapturedScope = manager.Clone()
                        };

                    return new GeneratorLangValue(generatorClosure, node.Position);
                }

                // 没有参数的生成器函数
                var noParamClosure = new FuncLangValue(node.Id, node.Ids, node.BlockStatement, node.GenericParameters, node.Position, node.IsLambda)
                {
                    // 生成器需要独立的作用域副本，使用深拷贝
                    CapturedScope = manager.Clone()
                };

                return new GeneratorLangValue(noParamClosure, node.Position);
            }

            // 外层管理器的作用域令牌必须与作用域一一对应，才能安全地判断帧是否存活；
            // 生成器 / 异步恢复出来的管理器可能不维护令牌，此时只用快照。
            var canUseHomeFrame = manager.ScopeTokens.Count == manager.Scopes.Count;

            var closureFunc = new FuncLangValue(node.Id, node.Ids, node.BlockStatement, node.GenericParameters, node.Position, node.IsLambda)
            {
                // 使用深拷贝创建独立的作用域副本
                // 这样即使外层函数返回，lambda 仍然可以访问捕获的变量
                CapturedScope = manager.Clone(),
                // 同时记住外层管理器：外层帧还活着时直接在外层帧上执行，
                // 闭包内对局部变量的赋值才能写回外层（见 ClosureFrameStillAlive）
                CapturedHomeManager = canUseHomeFrame ? manager : null,
                CapturedHomeTokens = canUseHomeFrame ? manager.ScopeTokens.ToArray() : null,
                CapturedHomeThreadId = Environment.CurrentManagedThreadId
            };

            return closureFunc;
        }

        // 原生方法或其他情况直接返回自身
        return node;
    
    
    }

    /// <summary>
    /// 访问 GeneratorLangValue 节点
    /// </summary>
    public LangValueType VisitGeneratorLangValue(GeneratorLangValue node)
    {
        // 迁移自 GeneratorLangValue.Run()
        if (node.StateMachine is null)
        {
            var generatorManager = manager.CloneForGenerator();

            foreach (var (paramName, paramValue) in node.ParameterValues)
            {
                var id = new LangId(paramName);
                generatorManager.Set(id, paramValue);
            }

            try
            {
                node.StateMachine = GeneratorStateMachineBuilder.BuildFromFunc(node.Func, generatorManager);
            }
            catch (InvalidOperationException)
            {
                node.State = GeneratorLangValue.GeneratorState.Completed;
                return new VoidLangValue();
            }
        }

        if (node.StateMachine.MoveNext())
        {
            node.State = GeneratorLangValue.GeneratorState.Suspended;
            node.NextValue = node.StateMachine.CurrentValue;
            return node.NextValue ?? new VoidLangValue();
        }

        node.State = GeneratorLangValue.GeneratorState.Completed;
        return new VoidLangValue();
    }

    /// <summary>
    /// 访问 AsyncGeneratorLangValue 节点
    /// </summary>
    public LangValueType VisitAsyncGeneratorLangValue(AsyncGeneratorLangValue node)
    {
        // 迁移自 AsyncGeneratorLangValue.Run()：阻塞等待异步生成器下一步
        var taskResult = node.RunAsync(manager);
        return taskResult.Await();
    }

    /// <summary>
    /// 访问 AsyncStreamLangValue 节点
    /// </summary>
    public LangValueType VisitAsyncStreamLangValue(AsyncStreamLangValue node)
    {
        // 迁移自 AsyncStreamLangValue.Run()
        return node._generator.Accept(this);
    }

    // ==================== CancellationToken 相关节点 ====================

    /// <summary>
    /// 访问 CancellationTokenLangValue 节点
    /// </summary>
    public LangValueType VisitCancellationTokenLangValue(CancellationTokenLangValue node)
    {
        // 迁移自 CancellationTokenLangValue.Run()
        // CancellationToken 值节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 CancellationTokenSourceLangValue 节点
    /// </summary>
    public LangValueType VisitCancellationTokenSourceLangValue(CancellationTokenSourceLangValue node)
    {
        // 迁移自 CancellationTokenSourceLangValue.Run()
        // CancellationTokenSource 值节点在解释器模式下直接返回自身
        return node;
    }

    // ==================== Task 相关节点 ====================

    /// <summary>
    /// 访问 TaskLangValue 节点
    /// </summary>
    public LangValueType VisitTaskLangValue(TaskLangValue node)
    {
        // 迁移自 TaskLangValue.Run()
        // Task 值节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 TaskClassLangValue 节点
    /// </summary>
    public LangValueType VisitTaskClassLangValue(TaskClassLangValue node)
    {
        // 迁移自 TaskClassLangValue.Run()
        // Task 类值节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 TaskCompletionSourceLangValue 节点
    /// </summary>
    public LangValueType VisitTaskCompletionSourceLangValue(TaskCompletionSourceLangValue node)
    {
        // 迁移自 TaskCompletionSourceLangValue.Run()
        // TaskCompletionSource 值节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 TaskFactoryClassLangValue 节点
    /// </summary>
    public LangValueType VisitTaskFactoryClassLangValue(TaskFactoryClassLangValue node)
    {
        // 迁移自 TaskFactoryClassLangValue.Run()
        // TaskFactory 类值节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 TaskFactoryStaticMethodWrapper 节点
    /// </summary>
    public LangValueType VisitTaskFactoryStaticMethodWrapper(TaskFactoryStaticMethodWrapper node)
    {
        // 迁移自 TaskFactoryStaticMethodWrapper.Run()
        // TaskFactory 静态方法包装器节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 TaskSchedulerClassLangValue 节点
    /// </summary>
    public LangValueType VisitTaskSchedulerClassLangValue(TaskSchedulerClassLangValue node)
    {
        // 迁移自 TaskSchedulerClassLangValue.Run()
        // TaskScheduler 类值节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 TaskSchedulerLangValue 节点
    /// </summary>
    public LangValueType VisitTaskSchedulerLangValue(TaskSchedulerLangValue node)
    {
        // 迁移自 TaskSchedulerLangValue.Run()
        // TaskScheduler 值节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 TaskStaticMethodWrapper 节点
    /// </summary>
    public LangValueType VisitTaskStaticMethodWrapper(TaskStaticMethodWrapper node)
    {
        // 迁移自 TaskStaticMethodWrapper.Run()
        // Task 静态方法包装器节点在解释器模式下直接返回自身
        return node;
    }

    // ==================== Thread 相关节点 ====================

    /// <summary>
    /// 访问 ThreadLangValue 节点
    /// </summary>
    public LangValueType VisitThreadLangValue(ThreadLangValue node)
    {
        // 迁移自 ThreadLangValue.Run()
        // Thread 值节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 ThreadClassLangValue 节点
    /// </summary>
    public LangValueType VisitThreadClassLangValue(ThreadClassLangValue node)
    {
        // 迁移自 ThreadClassLangValue.Run()
        // Thread 类值节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 ThreadStaticMethodWrapper 节点
    /// </summary>
    public LangValueType VisitThreadStaticMethodWrapper(ThreadStaticMethodWrapper node)
    {
        // 迁移自 ThreadStaticMethodWrapper.Run()
        // Thread 静态方法包装器节点在解释器模式下直接返回自身
        return node;
    }

    /// <summary>
    /// 访问 EnumLangValue 节点
    /// </summary>
    public LangValueType VisitEnumLangValue(EnumLangValue node)
    {
        // 迁移自 EnumLangValue.Run()
        // 枚举值节点在解释器模式下直接返回自身
        return node;
    }

    // ==================== 特殊节点 ====================

    /// <summary>
    /// 访问 GenericInstanceExpression 节点
    /// </summary>
    public LangValueType VisitGenericInstanceExpression(GenericInstanceExpression node)
    {

        // 获取基础类型或函数
        var baseValue = node.BaseExpression.Accept(this);

        // 从解释器获取类型注解管理器
        if (manager.Interpreter is null)
        {
            throw new InvalidOperationError(node, "无法获取 TypeAnnotationManager：解释器未初始化");
        }

        var typeAnnotationManager = manager.Interpreter.TypeAnnotationManager;

        var resolvedTypeArgs = new Dictionary<string, ITypeInfo>();

        // 处理泛型类
        if (baseValue is TypeTemplate typeTemplate)
        {
            if (typeTemplate.GenericParameters is null || typeTemplate.GenericParameters.Count == 0)
            {
                throw new InvalidOperationError(node, $"类型 {typeTemplate.ClassName} 不是泛型类");
            }

            // 验证类型参数数量
            if (node.TypeArguments.Count != typeTemplate.GenericParameters.Count)
            {
                throw new InvalidOperationError(node,
                    $"类型参数数量不匹配：期望 {typeTemplate.GenericParameters.Count} 个，实际 {node.TypeArguments.Count} 个");
            }

            // 解析类型参数
            for (int i = 0; i < node.TypeArguments.Count; i++)
            {
                var paramName = typeTemplate.GenericParameters[i].Name;
                var typeArgName = node.TypeArguments[i];

                // 使用 ParseTypeAnnotation 来正确处理可空类型和其他复杂类型
                var parsedType = typeAnnotationManager.ParseTypeAnnotation(typeArgName);
                var typeInfo = node.ConvertParsedTypeToTypeInfo(parsedType, typeAnnotationManager, manager);

                resolvedTypeArgs[paramName] =
                    typeInfo ?? throw new InvalidOperationError(node, $"未知的类型: {typeArgName}");
            }

            // 实例化泛型类
            var instantiatedTemplate = typeTemplate.InstantiateGeneric(resolvedTypeArgs, typeAnnotationManager, manager);

            // 如果后面跟着调用参数，创建实例
            if (node.IsFunctionCall)
            {
                var instance = instantiatedTemplate.CreateInstance(manager);
                instance.Init(manager.Interpreter);

                // 调用用户定义的 init 构造函数
                instance.CallInit(node.CallArguments!, manager);

                return instance;
            }

            // 否则返回实例化的类模板
            return instantiatedTemplate;
        }

        // 处理泛型函数
        if (baseValue is FuncLangValue funcValue)
        {
            if (funcValue.GenericParameters is null || funcValue.GenericParameters.Count == 0)
            {
                throw new InvalidOperationError(node, $"函数 {funcValue.Id?.IdName} 不是泛型函数");
            }

            // 验证类型参数数量
            if (node.TypeArguments.Count != funcValue.GenericParameters.Count)
            {
                throw new InvalidOperationError(node,
                    $"类型参数数量不匹配：期望 {funcValue.GenericParameters.Count} 个，实际 {node.TypeArguments.Count} 个");
            }

            // 解析类型参数
            for (int i = 0; i < node.TypeArguments.Count; i++)
            {
                var paramName = funcValue.GenericParameters[i].Name;
                var typeArgName = node.TypeArguments[i];

                // 使用 ParseTypeAnnotation 来正确处理可空类型和其他复杂类型
                var parsedType = typeAnnotationManager.ParseTypeAnnotation(typeArgName);
                var typeInfo = node.ConvertParsedTypeToTypeInfo(parsedType, typeAnnotationManager, manager);

                resolvedTypeArgs[paramName] =
                    typeInfo ?? throw new InvalidOperationError(node, $"未知的类型: {typeArgName}");
            }

            // 实例化泛型函数
            var instantiatedFunc = funcValue.InstantiateGeneric(resolvedTypeArgs, typeAnnotationManager, manager);

            // 调用实例化后的函数
            if (node.IsFunctionCall)
            {
                return instantiatedFunc.Run(manager, node.CallArguments!);
            }

            // 返回实例化的函数（作为一等公民）
            return instantiatedFunc;
        }

        throw new InvalidOperationError(node, $"表达式 {node.BaseExpression} 不是泛型类型或泛型函数");
    
    
    }

    /// <summary>
    /// 访问 LinqExpression 节点
    /// </summary>
    public LangValueType VisitLinqExpression(LinqExpression node)
    {

        var executor = new LinqQueryExecutor(manager);
        return executor.ExecuteQuery(node);
    
    
    }

    /// <summary>
    /// 访问 AssertClassLangValue 节点
    /// </summary>
    public LangValueType VisitAssertClassLangValue(AssertClassLangValue node)
    {
        return node;
    }

    /// <summary>
    /// 访问 AssertStaticMethodWrapper 节点
    /// </summary>
    public LangValueType VisitAssertStaticMethodWrapper(AssertStaticMethodWrapper node)
    {
        return node;
    }

    /// <summary>
    /// 访问 LockedVariableLangValue 节点
    /// </summary>
    public LangValueType VisitLockedVariableLangValue(LockedVariableLangValue node)
    {
        return node;
    }

    /// <summary>
    /// 访问 MockLibClassLangValue 节点
    /// </summary>
    public LangValueType VisitMockLibClassLangValue(MockLibClassLangValue node)
    {
        return node;
    }

    /// <summary>
    /// 访问 MockLibStaticMethodWrapper 节点
    /// </summary>
    public LangValueType VisitMockLibStaticMethodWrapper(MockLibStaticMethodWrapper node)
    {
        return node;
    }

    /// <summary>
    /// 访问 MockObjectLangValue 节点
    /// </summary>
    public LangValueType VisitMockObjectLangValue(MockObjectLangValue node)
    {
        return node;
    }

    /// <summary>
    /// 访问 TestRunnerClassLangValue 节点
    /// </summary>
    public LangValueType VisitTestRunnerClassLangValue(TestRunnerClassLangValue node)
    {
        return node;
    }

    /// <summary>
    /// 访问 TestRunnerStaticMethodWrapper 节点
    /// </summary>
    public LangValueType VisitTestRunnerStaticMethodWrapper(TestRunnerStaticMethodWrapper node)
    {
        return node;
    }

    public LangValueType VisitAsyncFuncLangValue(AsyncFuncLangValue node)
    {
        // 迁移自 AsyncFuncLangValue.Run()
        if (node.IsAsyncGenerator)
        {
            var generatorClosure = new AsyncFuncLangValue(node.Id, node.Ids, node.BlockStatement, node.Position)
            {
                CapturedScope = manager.Clone()
            };

            return new AsyncGeneratorLangValue(generatorClosure, node.Position);
        }

        var closureFunc = new AsyncFuncLangValue(node.Id, node.Ids, node.BlockStatement, node.Position)
        {
            CapturedScope = manager.Clone()
        };
        return closureFunc;
    }

    /// <summary>
    /// 访问 NativeAnyLangValue 节点（无需 DLL 时返回自身，否则加载类型信息）
    /// </summary>
    public LangValueType VisitNativeAnyLangValue(NativeAnyLangValue node)
    {
        if (node.InstanceObj is not null && node.ClassType is not null)
        {
            manager.Clone();
            return node;
        }

        if (node._path is null || node._dllName is null)
        {
            throw new InvalidOperationError(node, "无法加载类型：缺少 DLL 路径或名称");
        }

        var assembly = System.Reflection.Assembly.LoadFile(node._path);
        node.ClassType = assembly.GetType(node.ClassName);
        if (node.ClassType is null && node._dllName is not null)
        {
            node.ClassType = assembly.GetType($"{node._dllName}.{node.ClassName}");
        }

        if (node.ClassType is null)
        {
            throw new InvalidOperationError(node, $"无法加载类型：在程序集中找不到类型 '{node.ClassName}' 或 '{node._dllName}.{node.ClassName}'");
        }

        var constructors = node.ClassType.GetConstructors();
        if (constructors is { Length: > 0 })
            node.Constructor = constructors[0];
        manager.Clone();
        return node;
    }

    public LangValueType VisitVMThreadLangValue(VMThreadLangValue node)
    {
        // VMThreadLangValue 是 VM 线程句柄，解释模式下按值返回自身
        return node;
    }
}
