using System;
using System.Reflection.Emit;

var dm = new DynamicMethod("TestFunc", typeof(int), Type.EmptyTypes, true);
var il = dm.GetILGenerator();

// 模拟函数调用
var innerFunc = new DynamicMethod("GetValue", typeof(int), Type.EmptyTypes, true);
var innerIl = innerFunc.GetILGenerator();
innerIl.Emit(OpCodes.Ldc_I4, 42);
innerIl.Emit(OpCodes.Ret);

// 在主函数中
var resultLocal = il.DeclareLocal(typeof(int));
Console.WriteLine($"Result local index: {resultLocal.LocalIndex}");

// 调用内部函数
il.Emit(OpCodes.Call, innerFunc);

// 存储结果
il.Emit(OpCodes.Stloc, resultLocal.LocalIndex);

// 加载结果
il.Emit(OpCodes.Ldloc, resultLocal.LocalIndex);

// 返回
il.Emit(OpCodes.Ret);

var func = (Func<int>)dm.CreateDelegate(typeof(Func<int>));
var result = func();
Console.WriteLine($"Result: {result}");
