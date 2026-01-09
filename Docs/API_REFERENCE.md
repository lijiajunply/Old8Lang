# Old8Lang API 参考文档

本文档提供 Old8Lang 标准库的完整 API 参考。

## 目录

- [基础函数](#基础函数)
- [控制台输入输出](#控制台输入输出)
- [数学函数](#数学函数)
- [字符串处理](#字符串处理)
- [数组和列表操作](#数组和列表操作)
- [类型转换](#类型转换)
- [文件操作](#文件操作)
- [JSON 操作](#json-操作)
- [并发原语](#并发原语)
  - [互斥锁 (Mutex)](#互斥锁-mutex)
  - [信号量 (Semaphore)](#信号量-semaphore)
  - [原子整数 (AtomicInt)](#原子整数-atomicint)
  - [通道 (Channel)](#通道-channel)
  - [读写锁 (ReadWriteLock)](#读写锁-readwritelock)
  - [倒计时门闩 (CountDownLatch)](#倒计时门闩-countdownlatch)
  - [循环栅栏 (CyclicBarrier)](#循环栅栏-cyclicbarrier)
  - [取消令牌源 (CancellationTokenSource)](#取消令牌源-cancellationtokensource)
- [实用工具函数](#实用工具函数)

---

## 基础函数

### Print
**签名**: `Print(value: object) -> void`

**描述**: 输出内容到控制台，不换行。

**参数**:
- `value`: 要输出的值，会自动调用 `.ToStr()` 方法转换为字符串

**示例**:
```old8
Print("Hello")
Print(123)
```

### PrintLine
**签名**: `PrintLine(value: object) -> void`

**描述**: 输出内容到控制台并换行。

**参数**:
- `value`: 要输出的值

**示例**:
```old8
PrintLine("Hello World")
PrintLine(42)
```

### ReadLine
**签名**: `ReadLine() -> string`

**描述**: 从控制台读取一行输入。

**返回值**: 用户输入的字符串

**示例**:
```old8
name <- ReadLine()
PrintLine("你好, " + name)
```

---

## 控制台输入输出

### Read
**签名**: `Read() -> string`

**描述**: 读取单个字符输入。

**返回值**: 字符串形式的字符

### Clear
**签名**: `Clear() -> void`

**描述**: 清空控制台屏幕。

---

## 数学函数

### Abs
**签名**: `Abs(value: double) -> double`

**描述**: 返回数值的绝对值。

**参数**:
- `value`: 输入数值

**返回值**: 绝对值

**示例**:
```old8
result <- Abs(-5.5)  // 5.5
```

### Sqrt
**签名**: `Sqrt(value: double) -> double`

**描述**: 返回数值的平方根。

**参数**:
- `value`: 非负数值

**返回值**: 平方根

**示例**:
```old8
result <- Sqrt(16)  // 4.0
```

### Pow
**签名**: `Pow(base: double, exponent: double) -> double`

**描述**: 返回 base 的 exponent 次幂。

**参数**:
- `base`: 底数
- `exponent`: 指数

**返回值**: base^exponent

**示例**:
```old8
result <- Pow(2, 3)  // 8.0
```

### Sin / Cos / Tan
**签名**:
- `Sin(angle: double) -> double`
- `Cos(angle: double) -> double`
- `Tan(angle: double) -> double`

**描述**: 三角函数，参数为弧度。

**示例**:
```old8
result <- Sin(3.14159 / 2)  // 约等于 1.0
```

### Floor / Ceiling / Round
**签名**:
- `Floor(value: double) -> double`
- `Ceiling(value: double) -> double`
- `Round(value: double) -> double`

**描述**: 向下取整、向上取整、四舍五入。

**示例**:
```old8
Floor(3.7)    // 3.0
Ceiling(3.2)  // 4.0
Round(3.5)    // 4.0
```

### Max / Min
**签名**:
- `Max(a: double, b: double) -> double`
- `Min(a: double, b: double) -> double`

**描述**: 返回两个数中的最大值或最小值。

**示例**:
```old8
Max(5, 10)  // 10
Min(5, 10)  // 5
```

---

## 字符串处理

### Length
**签名**: `Length(str: string) -> int`

**描述**: 返回字符串长度。

**示例**:
```old8
len <- Length("Hello")  // 5
```

### Substring
**签名**: `Substring(str: string, startIndex: int, length: int) -> string`

**描述**: 提取子字符串。

**参数**:
- `str`: 原字符串
- `startIndex`: 起始索引（从0开始）
- `length`: 子串长度

**示例**:
```old8
sub <- Substring("Hello World", 0, 5)  // "Hello"
```

### ToUpper / ToLower
**签名**:
- `ToUpper(str: string) -> string`
- `ToLower(str: string) -> string`

**描述**: 转换为大写或小写。

**示例**:
```old8
ToUpper("hello")  // "HELLO"
ToLower("WORLD")  // "world"
```

### Trim
**签名**: `Trim(str: string) -> string`

**描述**: 去除字符串首尾空白字符。

**示例**:
```old8
Trim("  hello  ")  // "hello"
```

### Split
**签名**: `Split(str: string, separator: string) -> array`

**描述**: 按分隔符拆分字符串为数组。

**示例**:
```old8
parts <- Split("a,b,c", ",")  // ["a", "b", "c"]
```

### Replace
**签名**: `Replace(str: string, oldValue: string, newValue: string) -> string`

**描述**: 替换字符串中的子串。

**示例**:
```old8
result <- Replace("Hello World", "World", "Old8")  // "Hello Old8"
```

### Contains
**签名**: `Contains(str: string, substring: string) -> bool`

**描述**: 检查字符串是否包含子串。

**示例**:
```old8
Contains("Hello World", "World")  // true
```

---

## 数组和列表操作

### Count
**签名**: `Count(collection: object) -> int`

**描述**: 返回集合（数组、列表、字典）的元素个数。

**示例**:
```old8
arr <- [1, 2, 3]
count <- Count(arr)  // 3
```

### Add
**签名**: `Add(list: list, item: object) -> void`

**描述**: 向列表添加元素。

**示例**:
```old8
lst <- {1, 2, 3}
Add(lst, 4)  // lst 变为 {1, 2, 3, 4}
```

### Remove
**签名**: `Remove(list: list, item: object) -> bool`

**描述**: 从列表移除指定元素。

**返回值**: 是否成功移除

**示例**:
```old8
lst <- {1, 2, 3}
Remove(lst, 2)  // lst 变为 {1, 3}
```

### Contains (集合)
**签名**: `Contains(collection: object, item: object) -> bool`

**描述**: 检查集合是否包含指定元素。

**示例**:
```old8
lst <- {1, 2, 3}
Contains(lst, 2)  // true
```

### Sort
**签名**: `Sort(list: list) -> void`

**描述**: 对列表进行原地排序。

**示例**:
```old8
lst <- {3, 1, 2}
Sort(lst)  // lst 变为 {1, 2, 3}
```

### Reverse
**签名**: `Reverse(list: list) -> void`

**描述**: 反转列表元素顺序。

**示例**:
```old8
lst <- {1, 2, 3}
Reverse(lst)  // lst 变为 {3, 2, 1}
```

---

## 类型转换

### ToStr
**签名**: `value.ToStr() -> string`

**描述**: 将任意值转换为字符串表示。

**示例**:
```old8
num <- 123
str <- num.ToStr()  // "123"
```

### ToInt
**签名**: `ToInt(value: object) -> int`

**描述**: 转换为整数类型。

**示例**:
```old8
ToInt("123")    // 123
ToInt(123.45)   // 123
```

### ToDouble
**签名**: `ToDouble(value: object) -> double`

**描述**: 转换为双精度浮点数。

**示例**:
```old8
ToDouble("3.14")  // 3.14
ToDouble(42)      // 42.0
```

### ToBool
**签名**: `ToBool(value: object) -> bool`

**描述**: 转换为布尔值。

**示例**:
```old8
ToBool("true")   // true
ToBool(1)        // true
ToBool(0)        // false
```

---

## 文件操作

### ReadFile
**签名**: `ReadFile(path: string) -> string`

**描述**: 读取文件全部内容为字符串。

**参数**:
- `path`: 文件路径

**返回值**: 文件内容

**示例**:
```old8
content <- ReadFile("data.txt")
```

### WriteFile
**签名**: `WriteFile(path: string, content: string) -> void`

**描述**: 将字符串写入文件，覆盖原内容。

**示例**:
```old8
WriteFile("output.txt", "Hello World")
```

### AppendFile
**签名**: `AppendFile(path: string, content: string) -> void`

**描述**: 追加内容到文件末尾。

**示例**:
```old8
AppendFile("log.txt", "新日志条目\n")
```

### FileExists
**签名**: `FileExists(path: string) -> bool`

**描述**: 检查文件是否存在。

**示例**:
```old8
if FileExists("config.json") {
    config <- ReadFile("config.json")
}
```

### DeleteFile
**签名**: `DeleteFile(path: string) -> void`

**描述**: 删除文件。

**示例**:
```old8
DeleteFile("temp.txt")
```

---

## JSON 操作

### JsonParse
**签名**: `JsonParse(jsonString: string) -> object`

**描述**: 解析 JSON 字符串为 Old8Lang 对象。

**返回值**: 字典或数组

**示例**:
```old8
json <- JsonParse("{\"name\": \"Old8\", \"version\": 1}")
PrintLine(json["name"])  // "Old8"
```

### JsonStringify
**签名**: `JsonStringify(obj: object) -> string`

**描述**: 将对象序列化为 JSON 字符串。

**示例**:
```old8
data <- {"name": "Old8", "version": 1}
jsonStr <- JsonStringify(data)
PrintLine(jsonStr)  // {"name":"Old8","version":1}
```

---

## 并发原语

Old8Lang 提供内置的并发原语,支持多线程和并发编程。所有并发原语都是全局函数,无需导入库即可使用。

### 互斥锁 (Mutex)

互斥锁用于保护共享资源,确保同一时刻只有一个线程可以访问。

#### MutexCreate
**签名**: `MutexCreate() -> int`

**描述**: 创建一个新的互斥锁。

**返回值**: 互斥锁 ID

**示例**:
```old8
mutex <- MutexCreate()
```

#### MutexLock
**签名**: `MutexLock(mutexId: int) -> void`

**描述**: 获取互斥锁,如果锁已被占用则阻塞等待。

**参数**:
- `mutexId`: 互斥锁 ID

**示例**:
```old8
MutexLock(mutex)
// 临界区代码
MutexUnlock(mutex)
```

#### MutexTryLock
**签名**: `MutexTryLock(mutexId: int, timeoutMs: int) -> bool`

**描述**: 尝试获取互斥锁,可设置超时时间。

**参数**:
- `mutexId`: 互斥锁 ID
- `timeoutMs`: 超时时间（毫秒）

**返回值**: 是否成功获取锁

**示例**:
```old8
if MutexTryLock(mutex, 1000) {
    // 成功获取锁
    MutexUnlock(mutex)
} else {
    PrintLine("获取锁超时")
}
```

#### MutexUnlock
**签名**: `MutexUnlock(mutexId: int) -> void`

**描述**: 释放互斥锁。

#### MutexDispose
**签名**: `MutexDispose(mutexId: int) -> void`

**描述**: 释放互斥锁资源。

**示例（使用 using 语句自动释放）**:
```old8
using mutex <- MutexCreate() {
    MutexLock(mutex)
    counter <- counter + 1
    MutexUnlock(mutex)
}  // 自动调用 MutexDispose
```

---

### 信号量 (Semaphore)

信号量用于限制同时访问某资源的线程数量。

#### SemaphoreCreate
**签名**: `SemaphoreCreate(initialCount: int, maxCount: int) -> int`

**描述**: 创建信号量。

**参数**:
- `initialCount`: 初始计数
- `maxCount`: 最大计数

**返回值**: 信号量 ID

**示例**:
```old8
// 限制最多3个线程同时访问
sem <- SemaphoreCreate(3, 3)
```

#### SemaphoreAcquire
**签名**: `SemaphoreAcquire(semaphoreId: int) -> void`

**描述**: 获取信号量,计数减1,如果计数为0则阻塞等待。

#### SemaphoreTryAcquire
**签名**: `SemaphoreTryAcquire(semaphoreId: int, timeoutMs: int) -> bool`

**描述**: 尝试获取信号量,可设置超时。

**返回值**: 是否成功获取

#### SemaphoreRelease
**签名**: `SemaphoreRelease(semaphoreId: int) -> void`

**描述**: 释放信号量,计数加1。

#### SemaphoreDispose
**签名**: `SemaphoreDispose(semaphoreId: int) -> void`

**描述**: 释放信号量资源。

**完整示例**:
```old8
using sem <- SemaphoreCreate(2, 2) {
    SemaphoreAcquire(sem)
    PrintLine("执行任务...")
    Sleep(1000)
    SemaphoreRelease(sem)
}
```

---

### 原子整数 (AtomicInt)

原子整数提供线程安全的整数操作,无需加锁。

#### AtomicIntCreate
**签名**: `AtomicIntCreate(initialValue: int) -> int`

**描述**: 创建原子整数。

**返回值**: 原子整数 ID

#### AtomicIntGet
**签名**: `AtomicIntGet(atomicId: int) -> int`

**描述**: 获取当前值。

#### AtomicIntSet
**签名**: `AtomicIntSet(atomicId: int, newValue: int) -> void`

**描述**: 设置新值。

#### AtomicIntIncrement
**签名**: `AtomicIntIncrement(atomicId: int) -> int`

**描述**: 原子递增,返回新值。

**示例**:
```old8
counter <- AtomicIntCreate(0)
newValue <- AtomicIntIncrement(counter)
PrintLine(newValue)  // 1
```

#### AtomicIntDecrement
**签名**: `AtomicIntDecrement(atomicId: int) -> int`

**描述**: 原子递减,返回新值。

#### AtomicIntAdd
**签名**: `AtomicIntAdd(atomicId: int, delta: int) -> int`

**描述**: 原子加法,返回新值。

**示例**:
```old8
newValue <- AtomicIntAdd(counter, 5)
```

#### AtomicIntCompareAndSet
**签名**: `AtomicIntCompareAndSet(atomicId: int, expectedValue: int, newValue: int) -> bool`

**描述**: 比较并交换（CAS操作）。

**返回值**: 是否成功设置

**示例**:
```old8
// 只有当前值为10时才设置为20
if AtomicIntCompareAndSet(counter, 10, 20) {
    PrintLine("CAS成功")
}
```

#### AtomicIntDispose
**签名**: `AtomicIntDispose(atomicId: int) -> void`

**描述**: 释放原子整数资源。

---

### 通道 (Channel)

通道用于线程间通信,支持发送和接收消息。

#### ChannelCreate
**签名**: `ChannelCreate() -> int`

**描述**: 创建无界通道。

**返回值**: 通道 ID

#### ChannelCreateBounded
**签名**: `ChannelCreateBounded(capacity: int) -> int`

**描述**: 创建有界通道,指定容量。

**示例**:
```old8
ch <- ChannelCreateBounded(10)  // 最多缓冲10个消息
```

#### ChannelSend
**签名**: `ChannelSend(channelId: int, value: object) -> void`

**描述**: 向通道发送消息,如果通道已满则阻塞等待。

**示例**:
```old8
ChannelSend(ch, "Hello")
ChannelSend(ch, 123)
```

#### ChannelTrySend
**签名**: `ChannelTrySend(channelId: int, value: object, timeoutMs: int) -> bool`

**描述**: 尝试发送消息,可设置超时。

**返回值**: 是否成功发送

#### ChannelReceive
**签名**: `ChannelReceive(channelId: int) -> object`

**描述**: 从通道接收消息,如果通道为空则阻塞等待。

**示例**:
```old8
msg <- ChannelReceive(ch)
PrintLine(msg)
```

#### ChannelTryReceive
**签名**: `ChannelTryReceive(channelId: int, timeoutMs: int) -> object?`

**描述**: 尝试接收消息,可设置超时。

**返回值**: 接收到的消息,超时则返回 null

#### ChannelClose
**签名**: `ChannelClose(channelId: int) -> void`

**描述**: 关闭通道,不再接受新消息。

#### ChannelDispose
**签名**: `ChannelDispose(channelId: int) -> void`

**描述**: 释放通道资源。

**完整示例**:
```old8
using ch <- ChannelCreate() {
    // 生产者
    async func producer() -> void {
        for i in 0..10 {
            ChannelSend(ch, i)
        }
        ChannelClose(ch)
    }

    // 消费者
    async func consumer() -> void {
        while true {
            msg <- ChannelTryReceive(ch, 100)
            if msg == null {
                break
            }
            PrintLine(msg)
        }
    }

    producer()
    consumer()
}
```

**Select 语句（通道多路复用）**:
```old8
ch1 <- ChannelCreate()
ch2 <- ChannelCreate()

select {
    case ch1 <- 100 -> {
        PrintLine("发送到 ch1")
    }
    case val from ch2 -> {
        PrintLine("从 ch2 接收: " + val.ToStr())
    }
    default -> {
        PrintLine("没有通道就绪")
    }
}
```

---

### 读写锁 (ReadWriteLock)

读写锁允许多个读者同时访问,但写者独占访问。

#### ReadWriteLockCreate
**签名**: `ReadWriteLockCreate() -> int`

**描述**: 创建读写锁。

**返回值**: 读写锁 ID

#### ReadLockAcquire
**签名**: `ReadLockAcquire(lockId: int) -> void`

**描述**: 获取读锁。

#### ReadLockRelease
**签名**: `ReadLockRelease(lockId: int) -> void`

**描述**: 释放读锁。

#### WriteLockAcquire
**签名**: `WriteLockAcquire(lockId: int) -> void`

**描述**: 获取写锁（独占）。

#### WriteLockRelease
**签名**: `WriteLockRelease(lockId: int) -> void`

**描述**: 释放写锁。

#### ReadLockTryAcquire
**签名**: `ReadLockTryAcquire(lockId: int, timeoutMs: int) -> bool`

**描述**: 尝试获取读锁。

#### WriteLockTryAcquire
**签名**: `WriteLockTryAcquire(lockId: int, timeoutMs: int) -> bool`

**描述**: 尝试获取写锁。

#### ReadWriteLockDispose
**签名**: `ReadWriteLockDispose(lockId: int) -> void`

**描述**: 释放读写锁资源。

**示例**:
```old8
using rwLock <- ReadWriteLockCreate() {
    // 多个读者
    ReadLockAcquire(rwLock)
    data <- ReadData()
    ReadLockRelease(rwLock)

    // 单个写者
    WriteLockAcquire(rwLock)
    WriteData(newData)
    WriteLockRelease(rwLock)
}
```

---

### 倒计时门闩 (CountDownLatch)

倒计时门闩用于等待多个线程完成任务。

#### CountDownLatchCreate
**签名**: `CountDownLatchCreate(count: int) -> int`

**描述**: 创建倒计时门闩。

**参数**:
- `count`: 初始计数

**返回值**: 门闩 ID

#### CountDownLatchCountDown
**签名**: `CountDownLatchCountDown(latchId: int) -> void`

**描述**: 计数减1。

#### CountDownLatchWait
**签名**: `CountDownLatchWait(latchId: int) -> void`

**描述**: 等待计数归零。

#### CountDownLatchWaitTimeout
**签名**: `CountDownLatchWaitTimeout(latchId: int, timeoutMs: int) -> bool`

**描述**: 等待计数归零,可设置超时。

**返回值**: 是否成功等待到归零

#### CountDownLatchGetCount
**签名**: `CountDownLatchGetCount(latchId: int) -> int`

**描述**: 获取当前计数。

#### CountDownLatchDispose
**签名**: `CountDownLatchDispose(latchId: int) -> void`

**描述**: 释放门闩资源。

**示例**:
```old8
using latch <- CountDownLatchCreate(3) {
    async func worker() -> void {
        PrintLine("工作完成")
        CountDownLatchCountDown(latch)
    }

    worker()
    worker()
    worker()

    CountDownLatchWait(latch)
    PrintLine("所有工作完成")
}
```

---

### 循环栅栏 (CyclicBarrier)

循环栅栏用于让多个线程在某个点上相互等待。

#### CyclicBarrierCreate
**签名**: `CyclicBarrierCreate(participantCount: int) -> int`

**描述**: 创建循环栅栏。

**参数**:
- `participantCount`: 参与者数量

**返回值**: 栅栏 ID

#### CyclicBarrierAwait
**签名**: `CyclicBarrierAwait(barrierId: int) -> void`

**描述**: 等待所有参与者到达栅栏。

#### CyclicBarrierAwaitTimeout
**签名**: `CyclicBarrierAwaitTimeout(barrierId: int, timeoutMs: int) -> bool`

**描述**: 等待所有参与者到达,可设置超时。

**返回值**: 是否成功等待

#### CyclicBarrierGetParticipantCount
**签名**: `CyclicBarrierGetParticipantCount(barrierId: int) -> int`

**描述**: 获取参与者总数。

#### CyclicBarrierGetWaitingCount
**签名**: `CyclicBarrierGetWaitingCount(barrierId: int) -> int`

**描述**: 获取当前等待的参与者数量。

#### CyclicBarrierDispose
**签名**: `CyclicBarrierDispose(barrierId: int) -> void`

**描述**: 释放栅栏资源。

**示例**:
```old8
using barrier <- CyclicBarrierCreate(3) {
    async func worker(id: int) -> void {
        PrintLine("线程 " + id.ToStr() + " 到达栅栏")
        CyclicBarrierAwait(barrier)
        PrintLine("线程 " + id.ToStr() + " 继续执行")
    }

    worker(1)
    worker(2)
    worker(3)
}
```

---

### 取消令牌源 (CancellationTokenSource)

取消令牌源用于协调取消异步操作。

#### CreateCancellationTokenSource
**签名**: `CreateCancellationTokenSource() -> int`

**描述**: 创建取消令牌源。

**返回值**: 令牌源 ID

#### Cancel
**签名**: `Cancel(ctsId: int) -> void`

**描述**: 立即取消操作。

#### CancelAfter
**签名**: `CancelAfter(ctsId: int, delayMs: int) -> void`

**描述**: 延迟指定时间后取消。

**参数**:
- `ctsId`: 令牌源 ID
- `delayMs`: 延迟时间（毫秒）

#### DisposeCancellationTokenSource
**签名**: `DisposeCancellationTokenSource(ctsId: int) -> void`

**描述**: 释放令牌源资源。

**示例**:
```old8
using cts <- CreateCancellationTokenSource() {
    CancelAfter(cts, 5000)  // 5秒后自动取消

    // 执行可取消的长时间操作
    // ...
}
```

---

## 实用工具函数

### Sleep
**签名**: `Sleep(milliseconds: int) -> void`

**描述**: 暂停当前线程指定毫秒数。

**示例**:
```old8
PrintLine("开始")
Sleep(1000)  // 暂停1秒
PrintLine("结束")
```

### GetCurrentThreadId
**签名**: `GetCurrentThreadId() -> int`

**描述**: 获取当前线程的 ID。

**返回值**: 线程 ID

**示例**:
```old8
threadId <- GetCurrentThreadId()
PrintLine("当前线程ID: " + threadId.ToStr())
```

### GetProcessorCount
**签名**: `GetProcessorCount() -> int`

**描述**: 获取系统处理器核心数量。

**返回值**: CPU 核心数

**示例**:
```old8
cores <- GetProcessorCount()
PrintLine("系统有 " + cores.ToStr() + " 个核心")
```

---

## 资源管理

### Using 语句

`using` 语句提供自动资源管理,确保资源在使用完毕后自动释放。

**语法**:
```old8
// 形式1: 带变量声明
using resource <- CreateResource() {
    // 使用资源
}  // 自动调用 Dispose

// 形式2: 使用已有变量
res <- CreateResource()
using res {
    // 使用资源
}  // 自动调用 Dispose
```

**适用资源**: 所有返回 ID（int）的并发原语都支持自动释放:
- Mutex → MutexDispose
- Semaphore → SemaphoreDispose
- AtomicInt → AtomicIntDispose
- Channel → ChannelDispose
- ReadWriteLock → ReadWriteLockDispose
- CountDownLatch → CountDownLatchDispose
- CyclicBarrier → CyclicBarrierDispose
- CancellationTokenSource → DisposeCancellationTokenSource

**示例**:
```old8
using mutex <- MutexCreate() {
    MutexLock(mutex)
    counter <- counter + 1
    MutexUnlock(mutex)
}  // MutexDispose 自动调用
```

---

## 总结

本 API 参考文档涵盖了 Old8Lang 的核心标准库函数。更多高级功能和库请参考:
- [Old8LangLib](../Old8LangLib/README.md) - 操作系统、文件、网络功能
- [Old8Lang.NetLib](../Old8Lang.NetLib/README.md) - MQTT 和高级网络功能
- [语法参考](Old8Lang_Grammar.md) - 完整语法说明

