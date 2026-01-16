# Old8Lang API 模式实现对比

**最后更新**: 2026年1月16日

本文档详细对比了 Old8Lang 在 **解释器模式**、**编译器模式** 和 **虚拟机模式** 下的内部 API（全局函数、标准库、基本类型方法）的实现机制与差异。

## 1. 核心架构概述

Old8Lang 通过统一的接口设计和 Visitor 模式，实现了三种执行模式的共存与隔离。

| 特性 | 解释器模式 (`-f`) | 编译器模式 (`-c`) | 虚拟机模式 (VM) |
| :--- | :--- | :--- | :--- |
| **设计目标** | 开发效率、动态灵活性 | 运行性能、类型安全 | 可移植性、细粒度控制 |
| **执行核心** | `LangInterpreter` | `Compiler` | `VirtualMachine` |
| **中间表示** | AST (抽象语法树) | .NET IL (中间语言) | Old8Lang Bytecode (字节码) |
| **执行方式** | 运行时直接执行 C# 代码 | JIT 编译为机器码执行 | 解释执行字节码指令 |

---

## 2. 全局函数 (Global Functions)

全局函数（如 `PrintLine`, `Input`, `Sleep` 等）在所有模式下都通过 `IGlobalFunction` 接口定义，但具体实现逻辑完全不同。

### 2.1 统一接口定义

所有全局函数继承自 `BaseGlobalFunction` 或实现 `IGlobalFunction` 接口：

```csharp
public interface IGlobalFunction
{
    // 解释器模式实现
    Value ExecuteInternal(Value[] args, VariateManager manager);
    
    // 编译器模式实现
    void GenerateIlInternal(ILGenerator il, Type[] paramTypes);
    
    // 虚拟机模式实现
    void ExecuteInVMInternal(VirtualMachine vm, Value[] args);
}
```

### 2.2 实现机制对比

| 机制 | 解释器模式 | 编译器模式 | 虚拟机模式 |
| :--- | :--- | :--- | :--- |
| **函数查找** | 运行时通过 `GlobalFunctionRegistry` 和 `VariateManager` 动态查找。 | 编译时解析函数名，直接绑定到对应的 IL 生成逻辑。 | 运行时通过 `OpCode.Call` 或 `OpCode.CallNative` 指令，在 `_globals` 表中查找。 |
| **参数传递** | 接收 `Value[]` 数组，运行时解析。 | 通过计算栈传递参数，编译时确定类型。 | 通过操作数栈传递参数，运行时弹出。 |
| **执行逻辑** | 直接调用 C# 方法（如 `Console.WriteLine`）。 | 生成对应的 CLR IL 指令（如 `call void [System.Console]System.Console::WriteLine(string)`）。 | 执行特定的字节码指令或调用封装的 C# 委托。 |
| **示例 (Print)** | `OutputProvider.Write(arg.ToString())` | `il.Emit(OpCodes.Call, typeof(Console).GetMethod("Write"))` | 调用 VM 内部封装的 `Console.Write` 逻辑。 |

### 2.3 代码参考

- **接口定义**: `Old8Lang/GlobalFunctions/IGlobalFunction.cs`
- **函数注册**: `Old8Lang/GlobalFunctions/GlobalFunctionRegistry.cs`
- **具体实现**: `Old8Lang/GlobalFunctions/Implementations/` (如 `IOFunctions.cs`)

---

## 3. 标准库 (Old8LangLib)

标准库提供了文件操作、网络、系统交互等功能。

### 3.1 加载与调用

| 机制 | 解释器模式 | 编译器模式 | 虚拟机模式 |
| :--- | :--- | :--- | :--- |
| **库加载** | 使用 `StandardLibraryLoader` 动态加载程序集，注册到当前作用域。 | 使用 `StandardLibraryLoader` 提取类型元数据，用于编译时类型检查和方法绑定。 | 使用 `ModuleRegistry` 管理模块，通过 `OpCode.ImportNative` 指令加载。 |
| **Import 语句** | `ImportStatement.Run()`: 立即执行加载逻辑。 | `ImportStatement.GenerateIl()`: 生成加载指令或静态链接。 | `BytecodeVisitor`: 生成 `LoadModule` 或 `ImportNative` 指令。 |
| **方法调用** | 通过 C# 反射 (`MethodInfo.Invoke`) 调用。 | 生成 `Call` 指令，直接调用库中的静态方法 (高性能)。 | 通过 `OpCode.CallNative` 调用，需要封送 (Marshal) 参数。 |

### 3.2 限制与差异

- **解释器**: 支持最广泛的动态特性，可以加载任意符合规范的 .NET DLL。
- **编译器**: 需要库提供明确的类型签名，不支持部分动态特性。
- **虚拟机**: 需要通过 `ExternFunctionWrapper` 进行封装，目前主要支持核心标准库。

---

## 4. 基本类型方法 (Basic Type Methods)

Old8Lang 中的基本类型（如 `string`, `list`, `int`）拥有内置方法（如 `.Length()`, `.Add()`, `.ToStr()`）。

### 4.1 扩展方法模式

Old8Lang 使用 **静态扩展方法类** 来统一管理这些方法。不同模式使用不同的扩展类集合：

- **解释器模式 (针对 `LangValueType` 包装对象)**:
  - `StringValueFuncStatic`
  - `ListValueFuncStatic`
  - `DictionaryValueFuncStatic`
  - `ValueTypeFuncStatic` (通用方法如 `.ToInt()`)

- **编译器模式 (针对原生 CLR 类型)**:
  - `StringExtensions` (针对 `string`)
  - `ListExtensions` (针对 `List<T>`)
  - `DictionaryExtensions` (针对 `Dictionary<K,V>`)
  - `PrimitiveExtensions` (针对 `int`, `double`, `bool`, `char`) **[新增]**

### 4.2 调用机制对比

| 机制 | 解释器模式 | 编译器模式 | 虚拟机模式 |
| :--- | :--- | :--- | :--- |
| **解析方式** | `FunctionCallExpression`: 运行时检查对象类型，反射查找对应的 `*ValueFuncStatic` 类。 | `DotOperatorILHelper`: 编译时根据变量类型(CLR Type)，查找对应的 `*Extensions` 类。 | `OpCode.CallMethod`: 运行时指令，动态分发。 |
| **重写逻辑** | 动态分发。例如 `str.Length()` -> `StringValueFuncStatic.Length(strVal)`。 | 静态重写。例如 `str.Length()` -> `StringExtensions.Length(str)`。基本类型如 `i.ToStr()` -> `PrimitiveExtensions.ToStr(i)`。 | 动态分发。VM 检查栈顶对象类型，查虚表或元数据调用对应方法。 |
| **性能** | 低 (反射开销)。 | 高 (直接静态方法调用，内联优化，无装箱)。 | 中 (字节码解释 + 动态分发)。 |

---

## 5. 虚拟机 (VM) 独有特性

虚拟机模式拥有独立的指令集架构 (Bytecode)，这使其在某些方面与解释器和编译器有本质区别。

### 5.1 指令集 (OpCodes)

VM 使用专门设计的 `OpCode` 来执行高级操作，而不是依赖底层的 CPU 指令或 CLR IL。

- **对象创建**: `NewList`, `NewDict`, `NewTuple`, `NewRange`
- **函数操作**: `MakeFunction` (创建闭包), `Call`, `CallMethod`
- **切片操作**: `Slice` (直接支持切片语法)
- **并发原语**: `MutexCreate`, `ThreadCreate`, `NewTask`, `Await`

### 5.2 并发模型

- **解释器**: 依赖 C# 的 `Task` 和 `Thread`，并发控制较为松散。
- **编译器**: 映射到 .NET 的并发模型。
- **虚拟机**: 拥有指令级的并发支持 (`OpCode.NewTask`, `OpCode.Await`)，可以实现更精细的调度和状态管理（如异步生成器、协程）。

### 5.3 异常处理

VM 使用显式的异常表指令结构，而不是 C# 的 `try-catch` 块：
- `TryBegin` / `TryEnd`
- `CatchBegin` / `CatchEnd`
- `FinallyBegin` / `FinallyEnd`
- `Throw`

这使得 VM 可以捕获并处理 Old8Lang 内部的异常，同时保持与宿主环境 (C#) 的异常隔离。

---

## 6. 总结

| 维度 | 解释器模式 | 编译器模式 | 虚拟机模式 |
| :--- | :--- | :--- | :--- |
| **API 实现** | 动态反射，灵活但慢 | 静态绑定，快速且类型安全 | 字节码指令，可移植且可控 |
| **扩展性** | 极高，可运行时修改 | 中等，需重新编译 | 高，支持模块化加载 |
| **适用场景** | 脚本、REPL、动态配置 | 高性能服务、生产环境 | 嵌入式、插件系统、跨平台分发 |

---

**相关文档**:
- [Old8Lang 语法文档](Old8Lang_Grammar.md)
- [模式支持总结](Mode_Support_Summary.md)
- [编译器 TODO](TODO_Compiler.md)
- [虚拟机 TODO](TODO_VirtualMachine.md)
