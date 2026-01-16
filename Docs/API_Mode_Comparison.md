# Old8Lang 内部 API 模式对比

**最后更新**: 2026年1月16日

本文档详细对比 Old8Lang 三种执行模式在内部 API 方面的支持情况，包括全局函数、标准库导入和基本类型方法。

---

## 模式说明

| 模式 | 命令 | 说明 |
|------|------|------|
| **解释器模式** | `-f` | 直接执行代码，无需编译，功能最完整 |
| **编译器模式** | `-c` | 编译为 IL 中间代码再执行，性能较高 |
| **虚拟机模式** | 字节码 | 编译为字节码后在虚拟机中执行，支持序列化 |

## 支持标记

- ✅ **完全支持**: 功能已实现且测试通过
- ⚠️ **部分支持**: 功能基本可用但有限制
- ❌ **不支持**: 功能未实现

---

## 1. 输出与输入函数

| 函数 | 解释器 | 编译器 | 虚拟机 | 说明 |
|------|--------|--------|--------|------|
| `Print(value)` | ✅ | ✅ | ✅ | 输出文本，无换行 |
| `PrintLine(value)` | ✅ | ✅ | ✅ | 输出文本，有换行 |
| `ReadLine()` | ✅ | ✅ | ✅ | 读取一行输入 |

---

## 2. 类型转换函数

| 函数 | 解释器 | 编译器 | 虚拟机 | 说明 |
|------|--------|--------|--------|------|
| `value.ToStr()` | ✅ | ✅ | ✅ | 转换为字符串 |
| `value.ToInt()` | ✅ | ✅ | ✅ | 转换为整数 |
| `value.ToDouble()` | ✅ | ✅ | ✅ | 转换为浮点数 |
| `value.ToBool()` | ✅ | ✅ | ✅ | 转换为布尔值 |

---

## 3. 并发原语函数

### 3.1 Mutex（互斥锁）

| 函数 | 解释器 | 编译器 | 虚拟机 | 说明 |
|------|--------|--------|--------|------|
| `MutexCreate()` | ✅ | ✅ | ✅ | 创建互斥锁，返回 ID |
| `MutexLock(id)` | ✅ | ✅ | ✅ | 加锁 |
| `MutexTryLock(id, timeoutMs)` | ✅ | ✅ | ✅ | 尝试加锁（带超时） |
| `MutexUnlock(id)` | ✅ | ✅ | ✅ | 解锁 |
| `MutexDispose(id)` | ✅ | ✅ | ✅ | 释放资源 |

### 3.2 Semaphore（信号量）

| 函数 | 解释器 | 编译器 | 虚拟机 | 说明 |
|------|--------|--------|--------|------|
| `SemaphoreCreate(initial, max)` | ✅ | ✅ | ✅ | 创建信号量 |
| `SemaphoreAcquire(id)` | ✅ | ✅ | ✅ | 获取信号 |
| `SemaphoreTryAcquire(id, timeoutMs)` | ✅ | ✅ | ✅ | 尝试获取信号 |
| `SemaphoreRelease(id)` | ✅ | ✅ | ✅ | 释放信号 |
| `SemaphoreDispose(id)` | ✅ | ✅ | ✅ | 释放资源 |

### 3.3 AtomicInt（原子整数）

| 函数 | 解释器 | 编译器 | 虚拟机 | 说明 |
|------|--------|--------|--------|------|
| `AtomicIntCreate(initialValue)` | ✅ | ✅ | ✅ | 创建原子整数 |
| `AtomicIntGet(id)` | ✅ | ✅ | ✅ | 获取值 |
| `AtomicIntSet(id, value)` | ✅ | ✅ | ✅ | 设置值 |
| `AtomicIntIncrement(id)` | ✅ | ✅ | ✅ | 原子递增 |
| `AtomicIntDecrement(id)` | ✅ | ✅ | ✅ | 原子递减 |
| `AtomicIntAdd(id, delta)` | ✅ | ✅ | ✅ | 原子加法 |
| `AtomicIntCompareAndSet(id, expected, new)` | ✅ | ✅ | ✅ | CAS 操作 |
| `AtomicIntDispose(id)` | ✅ | ✅ | ✅ | 释放资源 |

### 3.4 Channel（通道）

| 函数 | 解释器 | 编译器 | 虚拟机 | 说明 |
|------|--------|--------|--------|------|
| `ChannelCreate()` | ✅ | ✅ | ✅ | 创建无界通道 |
| `ChannelCreateBounded(capacity)` | ✅ | ✅ | ✅ | 创建有界通道 |
| `ChannelSend(id, value)` | ✅ | ✅ | ✅ | 发送数据 |
| `ChannelTrySend(id, value, timeoutMs)` | ✅ | ✅ | ✅ | 尝试发送 |
| `ChannelReceive(id)` | ✅ | ✅ | ✅ | 接收数据 |
| `ChannelTryReceive(id, timeoutMs)` | ✅ | ✅ | ✅ | 尝试接收 |
| `ChannelClose(id)` | ✅ | ✅ | ✅ | 关闭通道 |
| `ChannelDispose(id)` | ✅ | ✅ | ✅ | 释放资源 |

### 3.5 ReadWriteLock（读写锁）

| 函数 | 解释器 | 编译器 | 虚拟机 | 说明 |
|------|--------|--------|--------|------|
| `ReadWriteLockCreate()` | ✅ | ✅ | ✅ | 创建读写锁 |
| `ReadLockAcquire(id)` | ✅ | ✅ | ✅ | 获取读锁 |
| `ReadLockRelease(id)` | ✅ | ✅ | ✅ | 释放读锁 |
| `WriteLockAcquire(id)` | ✅ | ✅ | ✅ | 获取写锁 |
| `WriteLockRelease(id)` | ✅ | ✅ | ✅ | 释放写锁 |
| `ReadLockTryAcquire(id, timeoutMs)` | ✅ | ✅ | ✅ | 尝试获取读锁 |
| `WriteLockTryAcquire(id, timeoutMs)` | ✅ | ✅ | ✅ | 尝试获取写锁 |
| `ReadWriteLockDispose(id)` | ✅ | ✅ | ✅ | 释放资源 |

### 3.6 CountDownLatch（倒计时锁）

| 函数 | 解释器 | 编译器 | 虚拟机 | 说明 |
|------|--------|--------|--------|------|
| `CountDownLatchCreate(count)` | ✅ | ✅ | ✅ | 创建倒计时锁 |
| `CountDownLatchCountDown(id)` | ✅ | ✅ | ✅ | 减少计数 |
| `CountDownLatchWait(id)` | ✅ | ✅ | ✅ | 等待计数归零 |
| `CountDownLatchWaitTimeout(id, timeoutMs)` | ✅ | ✅ | ✅ | 带超时等待 |
| `CountDownLatchGetCount(id)` | ✅ | ✅ | ✅ | 获取当前计数 |
| `CountDownLatchDispose(id)` | ✅ | ✅ | ✅ | 释放资源 |

### 3.7 CyclicBarrier（循环栅栏）

| 函数 | 解释器 | 编译器 | 虚拟机 | 说明 |
|------|--------|--------|--------|------|
| `CyclicBarrierCreate(participantCount)` | ✅ | ✅ | ✅ | 创建循环栅栏 |
| `CyclicBarrierAwait(id)` | ✅ | ✅ | ✅ | 等待所有参与者 |
| `CyclicBarrierAwaitTimeout(id, timeoutMs)` | ✅ | ✅ | ✅ | 带超时等待 |
| `CyclicBarrierGetParticipantCount(id)` | ✅ | ✅ | ✅ | 获取参与者数量 |
| `CyclicBarrierGetWaitingCount(id)` | ✅ | ✅ | ✅ | 获取等待数量 |
| `CyclicBarrierDispose(id)` | ✅ | ✅ | ✅ | 释放资源 |

### 3.8 CancellationTokenSource（取消令牌源）

| 函数 | 解释器 | 编译器 | 虚拟机 | 说明 |
|------|--------|--------|--------|------|
| `CreateCancellationTokenSource()` | ✅ | ✅ | ✅ | 创建取消令牌源 |
| `Cancel(id)` | ✅ | ✅ | ✅ | 请求取消 |
| `CancelAfter(id, delayMs)` | ✅ | ✅ | ✅ | 延时取消 |
| `DisposeCancellationTokenSource(id)` | ✅ | ✅ | ✅ | 释放资源 |

### 3.9 工具函数

| 函数 | 解释器 | 编译器 | 虚拟机 | 说明 |
|------|--------|--------|--------|------|
| `Sleep(milliseconds)` | ✅ | ✅ | ✅ | 线程休眠 |
| `GetCurrentThreadId()` | ✅ | ✅ | ✅ | 获取当前线程 ID |
| `GetProcessorCount()` | ✅ | ✅ | ✅ | 获取处理器数量 |

---

## 4. 异步编程 API

### 4.1 Task API

| 函数/特性 | 解释器 | 编译器 | 虚拟机 | 说明 |
|-----------|--------|--------|--------|------|
| `async func` 声明 | ✅ | ⚠️ | ✅ | 异步函数声明 |
| `await` 表达式 | ✅ | ⚠️ | ✅ | 等待异步操作 |
| `Task.Delay(ms)` | ✅ | ⚠️ | ✅ | 延迟执行 |
| `Task.WhenAll(tasks)` | ✅ | ⚠️ | ✅ | 等待所有任务 |
| `Task.WhenAny(tasks)` | ✅ | ⚠️ | ✅ | 等待任一任务 |
| `Task.FromResult(value)` | ✅ | ✅ | ✅ | 创建已完成的 Task |

**编译器模式限制**:
- ✅ await Task（如 `Task.FromResult()`）正常工作
- ❌ await 异步函数（状态机生成有问题）
- ❌ 异步生成器未实现
- ⚠️ 当前使用同步等待，非真正的异步

### 4.2 多线程 API

| 函数/特性 | 解释器 | 编译器 | 虚拟机 | 说明 |
|-----------|--------|--------|--------|------|
| `spawn(func())` | ✅ | ✅ | ✅ | 创建并启动线程 |
| `thread.Join()` | ✅ | ✅ | ✅ | 等待线程完成 |
| `thread.IsAlive()` | ✅ | ✅ | ✅ | 检查线程状态 |
| `Thread.CurrentThread()` | ✅ | ✅ | ✅ | 获取当前线程 |
| `Thread.Sleep(ms)` | ✅ | ✅ | ✅ | 线程休眠 |

---

## 5. 标准库导入

### 5.1 模块导入

| 导入方式 | 解释器 | 编译器 | 虚拟机 | 示例 |
|----------|--------|--------|--------|------|
| 简单导入 | ✅ | ✅ | ✅ | `import "math"` |
| 命名导入 | ✅ | ✅ | ✅ | `import { sqrt, pow } from "math"` |
| 别名导入 | ✅ | ✅ | ✅ | `import { sqrt as square_root } from "math"` |

### 5.2 原生库导入

| 导入方式 | 解释器 | 编译器 | 虚拟机 | 说明 |
|----------|--------|--------|--------|------|
| C# DLL 单方法导入 | ✅ | ✅ | ✅ | `extern "Math.dll" MathLib Sqrt sqrt` |
| C# DLL 批量导入 | ✅ | ✅ | ✅ | `extern "Old8LangLib" MathLib *` |
| C# DLL 选择性导入 | ✅ | ✅ | ✅ | `extern "Old8LangLib" Time { GetTimeNow, TimeStamp }` |
| C# DLL 类导入 | ✅ | ✅ | ✅ | `extern "Math.dll" MathLib -> MathLib` |

### 5.3 P/Invoke（C/C++ 原生函数）

| 特性 | 解释器 | 编译器 | 虚拟机 | 说明 |
|------|--------|--------|--------|------|
| 单函数导入 | ✅ | ❌ | ✅ | `extern "msvcrt.dll" func abs(x:int) -> int` |
| 调用约定 cdecl | ✅ | ❌ | ✅ | 默认调用约定 |
| 调用约定 stdcall | ✅ | ❌ | ✅ | Windows API 常用 |
| 调用约定 winapi | ✅ | ❌ | ✅ | 等同于 stdcall |
| 批量函数导入 | ✅ | ❌ | ✅ | 块语法导入多个函数 |
| 函数别名 | ✅ | ❌ | ✅ | `func GetCurrentProcessId() -> int as GetProcId` |

**示例**:
```old8
extern "kernel32.dll" stdcall {
    func GetCurrentThreadId() -> int,
    func GetCurrentProcessId() -> int,
    func Sleep(milliseconds:int) -> void
}
```

### 5.4 Python 互操作

| 特性 | 解释器 | 编译器 | 虚拟机 | 说明 |
|------|--------|--------|--------|------|
| Python 脚本导入 (.py) | ✅ | ❌ | ✅ | `extern "script.py" { ... }` |
| Python 脚本导入 (py:) | ✅ | ❌ | ✅ | `extern "py:script.py" { ... }` |
| Python 模块导入 | ✅ | ❌ | ✅ | `extern "pymodule:math" { ... }` |
| 函数别名 | ✅ | ❌ | ✅ | `func multiply(a:int, b:int) -> int as mul` |

**示例**:
```old8
extern "pymodule:math" {
    func sqrt(x:double) -> double,
    func pow(base:double, exp:double) -> double
}
```

---

## 6. 数学函数库（MathLib）

需要通过 `extern` 导入 Old8LangLib：

```old8
extern "Old8LangLib" MathLib *
```

| 函数 | 解释器 | 编译器 | 虚拟机 | 说明 |
|------|--------|--------|--------|------|
| `Sqrt(x)` | ✅ | ✅ | ✅ | 平方根 |
| `Pow(base, exp)` | ✅ | ✅ | ✅ | 幂运算 |
| `Sin(x)` | ✅ | ✅ | ✅ | 正弦 |
| `Cos(x)` | ✅ | ✅ | ✅ | 余弦 |
| `Tan(x)` | ✅ | ✅ | ✅ | 正切 |
| `Abs(x)` | ✅ | ✅ | ✅ | 绝对值 |
| `Floor(x)` | ✅ | ✅ | ✅ | 向下取整 |
| `Ceil(x)` | ✅ | ✅ | ✅ | 向上取整 |
| `Round(x)` | ✅ | ✅ | ✅ | 四舍五入 |
| `Log(x)` | ✅ | ✅ | ✅ | 自然对数 |
| `Log10(x)` | ✅ | ✅ | ✅ | 以 10 为底的对数 |
| `Exp(x)` | ✅ | ✅ | ✅ | e 的 x 次方 |
| `GetPi()` | ✅ | ✅ | ✅ | 获取 π 值 |
| `GetE()` | ✅ | ✅ | ✅ | 获取 e 值 |

---

## 7. 基本类型方法

### 7.1 字符串方法

| 方法 | 解释器 | 编译器 | 虚拟机 | 说明 |
|------|--------|--------|--------|------|
| `str.Length` | ✅ | ✅ | ✅ | 获取长度 |
| `str.ToUpper()` | ✅ | ✅ | ✅ | 转大写 |
| `str.ToLower()` | ✅ | ✅ | ✅ | 转小写 |
| `str.Substring(start, length)` | ✅ | ✅ | ✅ | 截取子串 |
| `str.Contains(substr)` | ✅ | ✅ | ✅ | 检查包含 |
| `str.StartsWith(prefix)` | ✅ | ✅ | ✅ | 检查前缀 |
| `str.EndsWith(suffix)` | ✅ | ✅ | ✅ | 检查后缀 |
| `str.IndexOf(substr)` | ✅ | ✅ | ✅ | 查找位置 |
| `str.Split(separator)` | ✅ | ✅ | ✅ | 分割字符串 |
| `str.Replace(old, new)` | ✅ | ✅ | ✅ | 替换字符串 |
| `str.Trim()` | ✅ | ✅ | ✅ | 去除首尾空白 |
| `str.TrimStart()` | ✅ | ✅ | ✅ | 去除首部空白 |
| `str.TrimEnd()` | ✅ | ✅ | ✅ | 去除尾部空白 |

### 7.2 列表方法（List）

| 方法 | 解释器 | 编译器 | 虚拟机 | 说明 |
|------|--------|--------|--------|------|
| `list.Add(item)` | ✅ | ✅ | ✅ | 添加元素 |
| `list.Remove(item)` | ✅ | ✅ | ✅ | 删除元素 |
| `list.RemoveAt(index)` | ✅ | ✅ | ✅ | 按索引删除 |
| `list.Clear()` | ✅ | ✅ | ✅ | 清空列表 |
| `list.Count()` | ✅ | ✅ | ✅ | 获取元素数量 |
| `list.Contains(item)` | ✅ | ✅ | ✅ | 检查包含 |
| `list.IndexOf(item)` | ✅ | ✅ | ✅ | 查找索引 |
| `list.Insert(index, item)` | ✅ | ✅ | ✅ | 插入元素 |
| `list.Join(separator)` | ✅ | ✅ | ✅ | 连接为字符串 |
| `list.Reverse()` | ✅ | ✅ | ✅ | 反转列表 |
| `list.Sort()` | ✅ | ✅ | ✅ | 排序列表 |

### 7.3 数组方法（Array）

| 方法 | 解释器 | 编译器 | 虚拟机 | 说明 |
|------|--------|--------|--------|------|
| `arr.Length` | ✅ | ✅ | ✅ | 获取长度 |
| `arr.Reverse()` | ✅ | ✅ | ✅ | 反转数组 |
| `arr.Sort()` | ✅ | ✅ | ✅ | 排序数组 |
| `arr.Contains(item)` | ✅ | ✅ | ✅ | 检查包含 |
| `arr.IndexOf(item)` | ✅ | ✅ | ✅ | 查找索引 |

### 7.4 字典方法（Dictionary）

| 方法 | 解释器 | 编译器 | 虚拟机 | 说明 |
|------|--------|--------|--------|------|
| `dict.Add(key, value)` | ✅ | ✅ | ✅ | 添加键值对 |
| `dict.Remove(key)` | ✅ | ✅ | ✅ | 删除键值对 |
| `dict.Clear()` | ✅ | ✅ | ✅ | 清空字典 |
| `dict.Count()` | ✅ | ✅ | ✅ | 获取元素数量 |
| `dict.ContainsKey(key)` | ✅ | ✅ | ✅ | 检查键存在 |
| `dict.ContainsValue(value)` | ✅ | ✅ | ✅ | 检查值存在 |
| `dict.GetOrElse(key, default)` | ✅ | ✅ | ✅ | 获取或返回默认值 |
| `dict.Keys()` | ✅ | ✅ | ✅ | 获取所有键 |
| `dict.Values()` | ✅ | ✅ | ✅ | 获取所有值 |

### 7.5 数值方法

| 方法 | 解释器 | 编译器 | 虚拟机 | 说明 |
|------|--------|--------|--------|------|
| `num.ToStr()` | ✅ | ✅ | ✅ | 转换为字符串 |
| `num.ToInt()` | ✅ | ✅ | ✅ | 转换为整数 |
| `num.ToDouble()` | ✅ | ✅ | ✅ | 转换为浮点数 |

---

## 8. 全局函数 `len()`

| 函数 | 解释器 | 编译器 | 虚拟机 | 说明 |
|------|--------|--------|--------|------|
| `len(collection)` | ✅ | ✅ | ✅ | 获取集合长度（数组、列表、字符串、字典） |

---

## 9. 总结对比

### 9.1 完全支持（三种模式一致）

以下 API 在所有三种模式下完全一致：

- **输出/输入函数**: Print, PrintLine, ReadLine
- **类型转换**: ToStr, ToInt, ToDouble, ToBool
- **并发原语**: 全部 50+ 个函数
- **基本类型方法**: 字符串、列表、数组、字典的所有方法
- **模块导入**: import 语句的所有形式
- **C# DLL 导入**: extern 语句的所有形式
- **数学函数库**: MathLib 的所有函数

### 9.2 部分支持差异

| 特性 | 解释器 | 编译器 | 虚拟机 | 备注 |
|------|--------|--------|--------|------|
| async/await | ✅ | ⚠️ | ✅ | 编译器 await 异步函数有问题 |
| Task API | ✅ | ⚠️ | ✅ | 编译器部分支持 |

### 9.3 不支持差异

| 特性 | 解释器 | 编译器 | 虚拟机 | 备注 |
|------|--------|--------|--------|------|
| P/Invoke | ✅ | ❌ | ✅ | 编译器不支持 |
| Python 互操作 | ✅ | ❌ | ✅ | 编译器不支持 |

---

## 10. 推荐使用策略

### 10.1 选择解释器模式 (`-f`) 的场景

- 需要使用 P/Invoke 或 Python 互操作
- 需要完整的异步编程支持
- 开发调试阶段，需要快速迭代
- 使用泛型函数或泛型类

### 10.2 选择编译器模式 (`-c`) 的场景

- 生产环境部署，需要更好的性能
- 不需要 P/Invoke 或 Python 互操作
- 主要使用同步代码或简单的 Task API
- 需要类型安全检查

### 10.3 选择虚拟机模式的场景

- 需要字节码序列化和跨平台分发
- 嵌入式环境或沙箱执行
- 需要完整的功能支持（包括 P/Invoke 和 Python）
- 模块化应用开发

---

**注意**: 本文档基于当前代码库状态（2026-01-16）生成，实际支持情况可能随版本更新而变化。
