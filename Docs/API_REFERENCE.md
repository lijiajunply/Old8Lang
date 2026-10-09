# Old8Lang API 参考

**最后更新**: 2026-10-09

> **本文档已按源码与实测重建。**
> 此前版本中大量内容与实现不符，最典型的两类错误是：
> 把**实例方法写成全局函数**（如 `Substring("Hello",0,2)`、`Add(list,x)`——这些函数不存在），
> 以及**标准库方法名与实现对不上**（如 `File.Read` 实际叫 `File.FileRead`，
> `Time.Now` 实际叫 `Time.GetLocalTime`）。
>
> 重建办法：从 `Old8LangLib/`、`Old8Lang/GlobalFunctions/`、`Old8Lang/InstanceMethods/`
> 与 `StandardLibraryRegistry` 提取真实签名，再抽样实跑验证。本文档中的方法名均取自实现。
>
> **注意**：用 `-s` 做语法检查**不能**发现这类错误——动态类型语言里未定义的函数照样通过解析。
> 验证 API 是否存在必须实际运行（`-f`）。

---

## 目录

- [1. 三种调用形式](#1-三种调用形式)
- [2. 全局函数（99 个）](#2-全局函数99-个)
- [3. 实例方法](#3-实例方法)
- [4. 标准库模块](#4-标准库模块)
- [5. 资源管理](#5-资源管理)
- [附录：与旧版文档的主要差异](#附录与旧版文档的主要差异)

---

## 1. 三种调用形式

Old8Lang 的 API 分三类，**调用形式各不相同**——这是旧版文档出错最多的地方。

### 1.1 全局函数：直接调用

不带任何前缀，直接写函数名：

```old8
PrintLine("hello")
m <- MutexCreate()
n <- Len({1, 2, 3})
t <- Type(42)          // "Int"
```

### 1.2 实例方法：值 `.` 方法

字符串、列表、字典、数组、元组、类型值的能力都挂在**值本身**上：

```old8
s <- "hello"
s.ToUpper()            // "HELLO"
s.Length()             // 5
s.Substring(0, 2)      // "he"

l <- {1, 2, 3}
l.Add(4)
l.Count()              // 4
l.Join(",")            // "1,2,3,4"

d <- {"a": 1}
d.ContainsKey("a")     // true
d.GetOrElse("z", 0)    // 0
```

> **不存在** `Length(s)`、`Substring(s,0,2)`、`Add(l,x)`、`Contains(s,x)` 这类全局形式，
> 写了会报 `[NAME_ERROR] 名称 'X' 未定义`。

### 1.3 标准库：`import "模块"` 后 `模块.方法(...)`

模块名取注册名，方法名**与 C# 实现里的方法名完全一致**：

```old8
import "Math"
Math.Sqrt(16.0)        // 4
Math.GetPi()           // 3.141592653589793

import "Time"
Time.GetLocalTime("yyyy-MM-dd")

import "Regex"
Regex.RegexIsMatch("a1", "\\d+", false)   // true
```

> 方法名与常见直觉不同：File 模块是 `File.FileRead` / `File.FileWrite` / `File.FileExists`，
> 不是 `File.Read` / `File.Write` / `File.Exists`。以本文档第 4 节为准。

---

## 2. 全局函数（99 个）

### 2.1 输出与输入

| 函数 | 参数 | 说明 |
|------|------|------|
| `Print` / `print` | `values...` | 输出，不换行 |
| `PrintLine` / `printLine` | `values...` | 输出并换行 |
| `ReadLine` / `readLine` | — | 读一行，返回 `string` |
| `Input` / `input` | `prompt?` | 带提示地读一行 |
| `Error` / `error` | `values...` | 输出到错误流 |
| `Clear` / `clear` | — | 清屏 |
| `ShowValues` / `showValues` | — | 转储当前全部变量（调试用） |

### 2.2 类型与转换

| 函数 | 参数 | 说明 |
|------|------|------|
| `Type` / `type` | `value` | 返回值类型名，如 `"Int"`、`"String"`、`"List"`、`"Value"` |
| `Len` / `len` | `value` | 集合长度 / 字符串长度 |
| `int` / `Int` | `value` | 转整数 |
| `double` / `Double` | `value` | 转浮点 |
| `bool` / `Bool` | `value` | 转布尔 |
| `char` / `Char` | `value` | 转字符 |
| `ToObj` / `toObj` | `jsonString` | JSON 字符串转对象 |
| `Dict` / `dict` | — | 创建空字典 |
| `Tuple` / `tuple` | — | 创建空元组 |
| `Range` / `range` | `start, end, step` | 构造范围值 |

> 单个值的转换还可以用**实例方法**：`"123".ToInt()`、`x.ToStr()`、`x.ToDouble()`。
> 见 [3.7 通用实例方法](#37-通用实例方法valuetype)。

### 2.3 JSON

| 函数 | 参数 | 说明 |
|------|------|------|
| `JsonSerialize` / `jsonSerialize` / `json` | `value` | 对象 → JSON 字符串 |
| `JsonDeserialize` / `jsonDeserialize` | `json` | JSON 字符串 → 对象 |
| `JsonSerializeToFile` | `obj, filePath` | 序列化到文件 |
| `JsonDeserializeFromFile` | `filePath` | 从文件反序列化 |
| `JsonIsValid` | `json` | 是否为合法 JSON |
| `JsonPrettify` | `json` | 格式化（缩进） |
| `JsonMinify` | `json` | 压缩 |
| `JsonCompare` | `json1, json2` | 比较两个 JSON 是否等价 |
| `JsonMerge` | `jsonObjects...` | 合并多个 JSON |
| `JsonGetValue` | `json, path` | 按路径取值 |

> 旧版文档写的 `JsonParse` / `JsonStringify` **不存在**。

### 2.4 反射

| 函数 | 参数 | 说明 |
|------|------|------|
| `GetAllTypes` | — | 所有已注册类型 |
| `GetType` | `typeName` | 按名取类型值 |
| `TypeOf` | `obj` | 取对象实例的类型值 |
| `GetTypeInfo` | `typeName` | 类型的完整元信息（字典） |
| `GetClassInfo` | `obj` | 对象所属类的信息（字典） |
| `GetFunctionInfo` | `function, methodName?` | 全局函数 / 函数对象 / 类方法信息 |
| `GetMemberInfo` | `obj, memberName` | 成员（字段或方法）元信息 |
| `HasMember` | `obj, memberName` | 是否存在该成员 |
| `GetField` | `obj, fieldName` | 读字段（含 private） |
| `SetField` | `obj, fieldName, value` | 写字段（含 private） |
| `InvokeMethod` | `obj, methodName, args` | 动态调用方法，`args` 为列表 |
| `CreateInstance` | `className, args` | 按类名创建实例并调用 `init` |
| `IsInstanceOf` | `obj, className` | 是否为指定类实例（类名精确匹配） |

**主要返回结构**

`GetTypeInfo(typeName)` 返回字典，含：
`name`、`isInterface`、`isAbstract`、`isMixin`、`baseClass`、`interfaces`、`mixins`、
`methods`、`fields`、`isGeneric`。

`GetClassInfo(obj)` 返回字典，含：
`className`、`methods`、`fields`、`isInterface`、`isAbstract`、`isMixin`、`baseClass`、`interfaces`。

`GetMemberInfo(obj, memberName)`：成员是方法时含 `name`、`type`（值为 `method`）、`isStatic`、
`isPublic`、`isPrivate`、`parameterCount`、`overloadCount`；是字段时含 `name`、
`type`（值为 `field`）、`isStatic`、`isPublic`、`isPrivate`。

`GetFunctionInfo(...)` 按输入类型返回不同结构，`type` 取值：
`global_function`（传全局函数名）、`user_function`（传函数对象）、
`native_function`（原生函数）、`class_method`（传对象 + 方法名）。
常见字段：`name`、`type`、`parameters` 或 `names`、`parameterCount` / `minParameterCount` /
`maxParameterCount`、`isStatic` / `isPublic` / `isPrivate` / `isAbstract` / `isVirtual`、`returnType`。

**模式差异与当前限制**

- 虚拟机下 `GetFunctionInfo(obj, methodName)`（类方法反射）暂不支持，会抛错。
- 虚拟机下 `Type.IsGeneric()` 与 `Type.IsAssignableFrom()` 固定返回 `false`。
- `IsInstanceOf` 是**类名精确匹配**，不沿继承链或接口实现链判断。
- 多数 API 在解释模式返回 `LangValue`（如 `DictionaryLangValue`），
  虚拟机模式返回原生 .NET 字典/列表；使用方需按运行模式适配。

`TypeLangValue` 的实例方法见 [3.10](#310-类型值实例方法typelangvalue)。

> 旧版语法文档里出现的 `GetClassName` / `GetClassMethods` / `GetClassFields` / `GetMethodInfo` /
> `GetFieldInfo` / `HasMethod` / `HasField` **均不存在**，已从语法文档移除。

### 2.5 并发原语

全部为内置全局函数，无需导入。配套的 `using` 自动释放见 [第 5 节](#5-资源管理)。

**Mutex（互斥锁）**

| 函数 | 参数 |
|------|------|
| `MutexCreate` | — |
| `MutexLock` | `mutexId` |
| `MutexTryLock` | `mutexId, timeoutMs` |
| `MutexUnlock` | `mutexId` |
| `MutexDispose` | `mutexId` |

**Semaphore（信号量）**：`SemaphoreCreate(initialCount, maxCount)` · `SemaphoreAcquire(semaphoreId)` ·
`SemaphoreTryAcquire(semaphoreId, timeoutMs)` · `SemaphoreRelease(semaphoreId)` · `SemaphoreDispose(semaphoreId)`

**AtomicInt（原子整数）**：`AtomicIntCreate(initialValue)` · `AtomicIntGet(atomicId)` ·
`AtomicIntSet(atomicId, newValue)` · `AtomicIntIncrement(atomicId)` · `AtomicIntDecrement(atomicId)` ·
`AtomicIntAdd(atomicId, delta)` · `AtomicIntCompareAndSet(atomicId, expectedValue, newValue)` ·
`AtomicIntDispose(atomicId)`

**Channel（通道）**：`ChannelCreate()` · `ChannelCreateBounded(capacity)` · `ChannelSend(channelId, value)` ·
`ChannelTrySend(channelId, value, timeoutMs)` · `ChannelReceive(channelId)` ·
`ChannelTryReceive(channelId, timeoutMs)` · `ChannelClose(channelId)` · `ChannelDispose(channelId)`

**ReadWriteLock（读写锁）**：`ReadWriteLockCreate()` · `ReadLockAcquire(lockId)` · `ReadLockRelease(lockId)` ·
`WriteLockAcquire(lockId)` · `WriteLockRelease(lockId)` · `ReadLockTryAcquire(lockId, timeoutMs)` ·
`WriteLockTryAcquire(lockId, timeoutMs)` · `ReadWriteLockDispose(lockId)`

**CountDownLatch（倒计时门闩）**：`CountDownLatchCreate(count)` · `CountDownLatchCountDown(latchId)` ·
`CountDownLatchWait(latchId)` · `CountDownLatchWaitTimeout(latchId, timeoutMs)` ·
`CountDownLatchGetCount(latchId)` · `CountDownLatchDispose(latchId)`

**CyclicBarrier（循环栅栏）**：`CyclicBarrierCreate(participantCount)` · `CyclicBarrierAwait(barrierId)` ·
`CyclicBarrierAwaitTimeout(barrierId, timeoutMs)` · `CyclicBarrierGetParticipantCount(barrierId)` ·
`CyclicBarrierGetWaitingCount(barrierId)` · `CyclicBarrierDispose(barrierId)`

**CancellationTokenSource**：`CreateCancellationTokenSource()` · `Cancel(ctsId)` · `CancelAfter(ctsId, delayMs)` ·
`DisposeCancellationTokenSource(ctsId)`

并发相关的还有 `Lock(value)`、`Spawn(func, args...)`。
`Spawn` **只创建线程，必须再调用 `Start()` 才会执行**，之后用 `Join()` 等待并取回返回值——
详见语法文档的多线程一节。

### 2.6 进程与执行

| 函数 | 参数 | 说明 |
|------|------|------|
| `Sleep` | `milliseconds` | 休眠 |
| `GetCurrentThreadId` | — | 当前线程 ID |
| `GetProcessorCount` | — | CPU 核心数 |
| `GetEnv` / `getEnv` | `key` | 读环境变量 |
| `Exec` / `exec` | `code` | 执行系统命令 |
| `Compiler` / `compiler` | `code?` | 编译相关入口 |
| `Assert` / `assert` | `actual, expected` | 断言相等 |

> `Assert` 的行为各模式不一致：虚拟机下失败抛 `AssertionError`（语言层 `try/catch` **能**捕获），
> 解释器下抛普通异常（捕获不到）。详见 [MODE_SUPPORT.md](./MODE_SUPPORT.md)。

---

## 3. 实例方法

### 3.1 通用（`ValueType`）

所有值都可调用：

| 方法 | 说明 |
|------|------|
| `.ToStr()` | 转字符串 |
| `.ToInt()` / `.ToDouble()` / `.ToBool()` / `.ToChar()` | 数值与字符转换 |
| `.ToType(type)` | 转换为指定类型 |
| `.ToHash()` | 哈希值 |
| `.Equal(other)` | 相等比较 |

> `.ToStr()` 在几个容器类型上还有各自的重载（见下），
> 虚拟机模式下**直接 `Print` 集合会输出 .NET 类型名，必须用 `.ToStr()`**。

### 3.2 字符串（`String`）

```
Contains  Count  EndsWith  FromBase64  IndexOf  PadLeft  PadRight  Repeat
Replace  Reverse  Split  StartsWith  Substring  ToBase64  ToCharArray
ToLower  ToUpper  Trim  TrimEnd  TrimStart
```

```old8
s <- "Hello"
s.ToUpper()              // "HELLO"
s.Substring(0, 2)        // "He"
s.Split(",")             // ["Hello"]
"7".PadLeft(5, "0")      // "00007"
"abc".Reverse()          // "cba"
```

**注意**：`.Length()` 是方法带括号；写成属性 `.Length` 会报 `[ATTRIBUTE_ERROR]`。

### 3.3 列表（`List`）

```
Add  AddList  Aggregate  BubbleSort  CartesianProduct  Chunk  Clear  Combinations
ElementAtOrDefault  FindAll  First  FlatMap  Flatten  FlattenDeep
GroupAdjacent  GroupAdjacentBy  HeapSort  Insert  InsertionSort  IsEmpty
IsSubsetOf  IsSupersetOf  Last  LastIndexOf  MergeSort  Overlaps  Pairwise
Partition  Permutations  Pop  QuickSort  Remove  RemoveAt  SelectionSort
SetEquals  Single  SingleOrDefault  SkipLast  SkipWhile  SkipWhileIndexed
Slice  SortWith  SymmetricExcept  TakeLast  TakeWhile  TakeWhileIndexed
ToArray  Window  WithIndex
```

```old8
l <- {1, 2, 3, 4}
l.Add(5)
l.Slice(0, 2)            // [1, 2]
l.Chunk(2)               // [[1, 2], [3, 4]]
l.Window(2)              // [[1, 2], [2, 3]]
l.Flatten()              // 展平一层
```

`List` 同时拥有 `Generic`（见 3.5）的全部高阶方法：`Map` / `Filter` / `Reduce` / `Sum` /
`Sort` / `Reverse` / `Join` / `Count` / `Contains` 等。

> **容器方法返回新集合**：`l.Sort()`、`a.Reverse()` **不修改原值**，
> 而是返回排好序 / 反转后的新集合。要就地生效需自己接回去：`l <- l.Sort()`。

### 3.4 字典（`Dictionary`）

```
Add  Clear  Clone  ContainsKey  ContainsValue  Count  Filter  ForEach
GetOrElse  GetValue  IsEmpty  Keys  Map  Merge  Remove  ToList  Update  Values
```

```old8
d <- {"a": 1}
d.Add("b", 2)
d.ContainsKey("a")       // true
d.GetOrElse("z", 0)      // 0
d.Merge({"c": 3})        // {"a": 1, "b": 2, "c": 3}
d.Keys()                 // ["a", "b", "c"]
```

### 3.5 高阶方法（`Generic`，适用于列表等集合）

```
All  Any  Average  Concat  Contains  Count  Distinct  ElementAt  Except  Filter
Find  First  FirstOrDefault  ForEach  GroupBy  IndexOf  Intersect  IsSorted
Join  Last  LastOrDefault  Map  Max  Min  Reduce  Reverse  SelectMany  Skip
Sort  SortBy  Sum  Take  ToArray  ToDict  ToList  ToStr  ToTuple  Union  Zip  Zip3
```

```old8
l <- {1, 2, 3, 4}
l.Map((x) -> x * 2)              // [2, 4, 6, 8]
l.Filter((x) -> x > 2)           // [3, 4]
l.Reduce((a, b) -> a + b, 0)     // 10
l.Sum()                          // 10
l.Join(",")                      // "1,2,3,4"
```

### 3.6 数组（`Array`）

```
BubbleSort  Get  GroupAdjacent  GroupAdjacentBy  HeapSort  InsertionSort
MergeSort  Overlaps  Permutations  QuickSort  SelectionSort  Set  SetEquals
Slice  ToList
```

数组取长度用 `len(a)` 或 `a.Count()`，**没有 `.Length` 属性**。

### 3.7 元组（`Tuple`）

`Get(index)` · `Slice(start)` · `ToList()`

### 3.8 字符（`Char`）

```
CompareTo  GetNumericValue  IsControl  IsDigit  IsLetter  IsLetterOrDigit
IsLower  IsPunctuation  IsSymbol  IsUpper  IsWhiteSpace  ToInt  ToLower  ToUpper
```

### 3.9 任务与线程（`Task` / `Thread`）

`Task`：`Await` `Catch` `ContinueWith` `Finally` `Then`
`Thread`：`Cancel` `IsAlive` `Join` `Retry` `Start` `Then` `WithTimeout`

### 3.10 类型值实例方法（`TypeLangValue`）

由 `GetType("X")` / `TypeOf(obj)` 返回的类型值：

| 方法 | 说明 |
|------|------|
| `.IsClass()` | 是否为类（非接口、非 mixin） |
| `.IsInterface()` | 是否为接口 |
| `.IsPrimitive()` | 是否为基本类型 |
| `.IsGeneric()` | 是否为泛型类型 |
| `.IsAssignableFrom(otherType)` | 类型兼容性检查 |
| `.GetBaseType()` | 父类型（无则 `null`） |
| `.GetInterfaces()` | 实现的接口列表 |
| `.GetMethodNames()` | 全部方法名 |
| `.GetFieldNames()` | 全部字段名 |

**模式差异**：虚拟机下 `.IsGeneric()` 与 `.IsAssignableFrom()` 目前**固定返回 `false`**（未实现完整检查）。

---

## 4. 标准库模块

导入语法：`import "模块名"`，之后用 `模块名.C#方法名(...)`。
下表的方法名即实现中的方法名。

### 4.1 Math（51 个）

**基础运算**

| 方法 | 签名 |
|------|------|
| `Sqrt` | `(value: double) -> double` |
| `Pow` | `(baseValue: double, exponent: double) -> double` |
| `Abs` / `Ceil` / `Floor` / `Round` / `Trunc` | `(value: double) -> double` |
| `Log` / `Log10` / `Exp` | `(value: double) -> double` |
| `LogBase` | `(value: double, baseValue: double) -> double` |
| `Sign` | `(value: double) -> int` |
| `Factorial` | `(n: int) -> long` |
| `Max` / `Min` | `(a: double, b: double) -> double` |

**三角与双曲**：`Sin` `Cos` `Tan` `Asin` `Acos` `Atan` `Sinh` `Cosh` `Tanh`（均 `(radians: double)`）、
`Atan2(y, x)`

**常量与特殊函数**：`GetPi()` · `GetE()` · `Gamma(value)` · `Beta(x, y)`

**随机**：`Random() -> double` · `RandomInt(minValue: int, maxValue: int) -> int`

**单位换算**：`DegreesToRadians` `RadiansToDegrees` `CelsiusToFahrenheit` `FahrenheitToCelsius`
`MetersToFeet` `FeetToMeters` `KilogramsToPounds` `PoundsToKilograms`

**向量运算**：`VectorMagnitude(vector)` · `VectorDotProduct(v1, v2)` · `VectorAdd(vectors)` ·
`VectorSubtract(v1, v2)` · `VectorMultiply(vector, scalar)` · `VectorNormalize(vector)` ·
`VectorAngle(v1, v2)` · `VectorSin/Cos/Tan/Exp/Log/Abs/Sqrt(vector)`

> 这些方法的 C# 形参是 `params double[]` / `params double[][]`，
> **必须显式传数组，不能展开成变参**：
> `Math.VectorMagnitude([3.0, 4.0])` → `5` ✅；
> `Math.VectorMagnitude(3.0, 4.0)` → `[ARGUMENT_ERROR]` ❌。

```old8
import "Math"
PrintLine(Math.Sqrt(16.0).ToStr())     // 4
PrintLine(Math.GetPi().ToStr())         // 3.141592653589793
```

### 4.2 File（31 个）

> 方法名带 `File` 前缀，**不是** `Read` / `Write` / `Exists`。

| 方法 | 签名 |
|------|------|
| `FileRead` | `(path: string, encoding: object? ) -> string` |
| `FileWrite` | `(path, content: string, encoding?) -> void` |
| `FileAppend` | `(path, content: string, encoding?) -> void` |
| `FileReadLines` / `FileWriteLines` / `FileAppendLines` | 按行读写，元素为 `array<string>` |
| `FileExists` | `(path) -> bool` |
| `GetFileSize` | `(path) -> long` |
| `CopyFile` / `DeleteFile` / `RenameFile` | 文件操作 |
| `CreateDirectory` / `DeleteDirectory` / `DirectoryExists` / `MoveDirectory` | 目录操作 |
| `GetDirectoryInfo` / `GetFileInfo` | 取信息（返回字符串） |
| `GetDirectories` / `GetFiles` | 枚举 |
| `ReadAllBytes` / `WriteAllBytes` / `AppendAllBytes` | 字节流 |
| `UnpackZip` / `CompressZip` / `ZipReadAll` | ZIP |
| `ReadXml` / `WriteXml` / `ReadYaml` / `WriteYaml` | XML / YAML |
| `GetLastWriteTime` / `SetLastWriteTime` | 时间戳 |

**受限**：`encoding` 参数是 .NET 的 `Encoding` 类型，Old8Lang 侧只能省略（用默认）；
`CompressionLevel`、`SearchOption`、`DateTime` 同理——**这些方法从 Old8Lang 调用时参数类型对不上**，
实际只能走默认值或无法调用。

```old8
import "File"
File.FileWrite("/tmp/o8.txt", "hello")
PrintLine(File.FileRead("/tmp/o8.txt"))
PrintLine(File.FileExists("/tmp/o8.txt").ToStr())
```

### 4.3 Time（25 个）

| 方法 | 签名 |
|------|------|
| `GetLocalTime` / `GetUtcTime` | `(format: string?) -> string` |
| `GetTimeInTimeZone` | `(timeZoneId, format?) -> string` |
| `GetUnixTimeSeconds` / `GetUnixTimeMilliseconds` | `() -> long` |
| `FromUnixTimeSeconds` / `FromUnixTimeMilliseconds` | `(秒/毫秒, format?) -> string` |
| `StartTimer` / `StopTimer` / `ResetTimer` / `GetElapsedMilliseconds` | 计时器：`StopTimer()` 与 `GetElapsedMilliseconds()` 返回 `double` |
| `GetCommonFormats` / `TimeFormat` | `() -> array<string>` |
| `TimeStamp` | `() -> string` |
| `AddDays` / `AddHours` / `AddMinutes` / `AddSeconds` | `(dateTime: object, 数量: int, format?) -> string` |
| `LocalToUtc` / `UtcToLocal` / `ConvertTimeBetweenTimeZones` / `Format` | 需要 `DateTime` 实参，**Old8Lang 侧受限** |

```old8
import "Time"
PrintLine(Time.GetLocalTime("yyyy-MM-dd"))     // 2026-10-09
PrintLine(Time.GetUnixTimeSeconds().ToStr())
```

### 4.4 Csv（10 个）

| 方法 | 签名 |
|------|------|
| `ReadCsv` | `(filePath, hasHeader: bool, delimiter: char, quoteChar: char) -> array<array<string>>` |
| `WriteCsv` | `(filePath, data: array<array<string>>, headers, delimiter, quoteChar)` |
| `WriteCsvFromDictionary` | 同上，数据源为字典列表 |
| `ParseCsvLine` | `(line: string, delimiter: char, quoteChar: char) -> array<string>` |
| `FormatCsvLine` | `(values, delimiter, quoteChar) -> string` |
| `ParseCsvContent` | `(csvContent, hasHeader, delimiter, quoteChar) -> array<array<string>>` |
| `ConvertCsvToJson` / `ConvertJsonToCsv` | 内容互转 |
| `ConvertCsvToJsonFile` / `ConvertJsonToCsvFile` | 文件互转 |

```old8
import "Csv"
PrintLine(Csv.ParseCsvLine("a,b", 44, 34).ToStr())    // ["a", "b"]
```
（`delimiter` / `quoteChar` 是 `char`，用 ASCII 码传入：`,`=44，`"`=34。）

### 4.5 Crypto（12 个）

| 方法 | 签名 |
|------|------|
| `AesEncrypt` / `AesDecrypt` | `(文本, key: string, iv: string) -> string` |
| `RsaEncrypt` / `RsaDecrypt` | `(文本, 密钥: string) -> string` |
| `Sha256Hash` / `Sha512Hash` | `(input: string) -> string` |
| `HmacSha256Hash` / `HmacSha512Hash` | `(input, key: string) -> string` |
| `Base64Encode` / `Base64Decode` | `(input: string) -> string` |
| `XorEncrypt` / `XorDecrypt` | `(input, key: string) -> string` |

> 旧版文档写的 `Crypto.MD5` / `Crypto.SHA1` **不存在**。

### 4.6 Regex（4 个）

| 方法 | 签名 |
|------|------|
| `RegexIsMatch` | `(input: string, pattern: string, ignoreCase: bool) -> bool` |
| `RegexMatch` | `(input, pattern, ignoreCase) -> string?`（首个匹配） |
| `RegexMatches` | `(input, pattern, ignoreCase) -> array<string>`（全部匹配） |
| `RegexReplace` | `(input, pattern, replacement: string, ignoreCase) -> string` |

> 注意参数顺序是 **输入在前、模式在后**，与 .NET/.NET 直觉相反；
> 且名字带 `Regex` 前缀（`RegexIsMatch` 而非 `Match`）。

### 4.7 Terminal（5 个）

`Title(title)` · `ReadAscii() -> int` · `ReadKey() -> string` · `Beep()` · `BeepWindow(tone, duration)`

### 4.8 ColorfulTerminal（4 个）

`PrintColorful(context, color)` · `PrintLineColorful(context, color)` ·
`PrintAscii(context)` · `PrintAsciiColorful(context, color)`

### 4.9 TemplateEngine（2 个）

`RenderHtml(template, variables)` · `RenderConfig(template, variables)`
（`variables` 为 `Dictionary<string, object>`）

> 模块名是 **`TemplateEngine`**，不是 `Template`。

### 4.10 OS（2 个）

`OsInfo() -> string` · `Process(code: string) -> string`

> 旧版文档写的 `OS.GetPlatform` / `OS.GetEnv` / `OS.Exec` **不存在**；
> 读环境变量与执行命令用**全局函数** `GetEnv(key)` 与 `Exec(code)`（见 2.6）。

### 4.11 Vector / Net / Database / Serialization / MachineLearning

这五个模块注册在案，但**从 Old8Lang 直接可用性受限**：

| 模块 | 注册实现 | 状况 |
|------|---------|------|
| `Vector` | `Vector2` / `Vector3` / `Vector4` / `VectorN`（普通类，非静态类） | 需先构造实例再用其方法 |
| `Net` | `SocketClient` / `HttpWebClient` / `MqttClientWrapper` / `WebSocketClient` / `WebApiClient` | 同上；主要能力在构造函数与属性上 |
| `Database` | `DatabaseLibBinding` | 方法签名大量使用 .NET 的 `object` / `Type` 参数（如 `QueryById(orm, entityType: Type, id)`），**Old8Lang 侧无法构造这些实参** |
| `Serialization` | `SerializationLibBinding` | 同上（`MsgPackDeserialize(data, targetType: Type)`） |
| `MachineLearning` | `MachineLearningLibBinding` | 同上（`IDataView` / `ITransformer`） |

`import "Database"` + `Database.CreateSqliteConnection("Data Source=:memory:")` 这类**无 .NET 类型参数**的入口
是可以调用的，但随后的 `ExecuteQuery` 等方法需要 `object` 参数，从 Old8Lang 传值不可靠。
**本次未逐方法实测**，使用时请自行验证。

---

## 5. 资源管理

`using` 语句在块结束时自动调用对应的 `Dispose`：

```old8
// 形式 1：创建并管理
using mutex <- MutexCreate() {
    MutexLock(mutex)
    MutexUnlock(mutex)
}   // 自动 MutexDispose

// 形式 2：管理已有变量
ch <- ChannelCreate()
using ch {
    ChannelSend(ch, 123)
}   // 自动 ChannelDispose
```

支持自动释放的资源：`Mutex` `Semaphore` `AtomicInt` `Channel` `ReadWriteLock`
`CountDownLatch` `CyclicBarrier` `CancellationTokenSource` —— 即所有返回资源 ID 且配有
`XxxDispose` 的并发原语。用 try-finally 实现，异常时同样释放。

---

## 附录：与旧版文档的主要差异

本次重建中，旧版 API_REFERENCE 被证实与实现不符的内容：

| 旧版写法 | 实况 |
|---------|------|
| `Length("Hello")` `Substring(s,0,2)` `ToUpper("hi")` `Trim` `Split` `Replace` `Contains` | 这些**全局函数不存在**，全部是实例方法：`s.Length()`、`s.Substring(0,2)`、`s.ToUpper()`… |
| `Abs(-5)` `Sqrt(16)` `Pow(2,3)` `Floor` `Max` | 全局形式不存在；用 `import "Math"` + `Math.Sqrt(...)` |
| `Count(arr)` `Add(lst,x)` `Remove(lst,x)` `Sort(lst)` `Reverse(lst)` `Contains(lst,x)` | 全局形式不存在；`arr.Count()`、`lst.Add(x)`、`lst.Sort()`… |
| `ToInt("123")` `ToDouble(...)` `ToBool(...)` | 全局形式不存在；`"123".ToInt()` 等实例方法 |
| `JsonParse` / `JsonStringify` | 实为 `JsonDeserialize` / `JsonSerialize` |
| `ReadFile` / `WriteFile` / `AppendFile` / `FileExists` / `DeleteFile` | 实为 `File.FileRead` / `File.FileWrite` / `File.FileAppend` / `File.FileExists` / `File.DeleteFile` |
| `File.Read` / `File.Write` / `File.Exists` / `File.GetSize` / `File.ReadLines` | 实为 `File.FileRead` / `File.FileWrite` / `File.FileExists` / `File.GetFileSize` / `File.FileReadLines` |
| `OS.GetPlatform` / `OS.SetEnv` / `OS.Exec` / `OS.GetCurrentDir` | 不存在；OS 只有 `OsInfo` 与 `Process`，环境变量与命令执行是全局函数 |
| `Time.Now` / `Time.Format` / `Time.Diff` / `Time.Sleep` | 实为 `Time.GetLocalTime` / `Time.Format`（需 `DateTime`）/ 无对应项 / 用全局 `Sleep` |
| `Regex.Match` / `Regex.FindAll` / `Regex.Replace` | 实为 `Regex.RegexIsMatch` / `Regex.RegexMatches` / `Regex.RegexReplace`，且参数顺序为「输入, 模式」 |
| `Crypto.MD5` / `Crypto.SHA1` / `Crypto.AESEncrypt` | 实为 `Sha256Hash` / `Sha512Hash` / `AesEncrypt`（无 MD5/SHA1） |
| `CSV.Read` / `CSV.Write` | 模块名是 `Csv`，方法是 `ReadCsv` / `WriteCsv` |
| `Vector.Create` / `Vector.Add` / `Vector.Dot` | 模块注册的是 `Vector2/3/4/N` 类，无这些静态方法 |
| `Template.Render` | 模块名是 `TemplateEngine` |
| `MySQL.*` / `PostgreSQL.*` / `SQLite.*` / `ORM.*` / `Classification.*` / `Regression.*` / `Clustering.*` / `Predictor.*` / `MessagePack.*` / `Protobuf.*` / `HTTP.*` / `MQTT.*` / `Socket.*` / `WebAPI.*` | 这些**都不是模块名**；对应模块分别是 `Database`、`MachineLearning`、`Serialization`、`Net`，且这些模块的方法签名多含 .NET 类型，从 Old8Lang 直接调用受限 |
| 全局反射函数 `GetClassName` / `GetClassMethods` / `HasMethod` / `HasField` 等 | 不存在；真实反射 API 见 [2.4](#24-反射) |
