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

## 标准库 (Standard Libraries)

Old8Lang 提供了丰富的标准库，涵盖核心功能、网络、数据库、序列化和机器学习等领域。

### 模式支持说明

每个 API 都标注了支持的执行模式：
- ✅ **解释模式** (`-f`): 完全支持
- ✅ **编译模式** (`-c`): 完全支持
- ✅ **VM 模式** (`-vm`): 完全支持
- ❌ 不支持该模式

---

## 核心标准库 (Old8LangLib)

**位置**: `Old8LangLib/`

核心标准库提供基础功能，包括数学运算、文件操作、加密、图像处理等。

### Math 模块

**模式支持**: ✅ 解释模式 | ✅ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "Math"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `Math.Sin` | `(x: double) -> double` | 正弦函数 |
| `Math.Cos` | `(x: double) -> double` | 余弦函数 |
| `Math.Tan` | `(x: double) -> double` | 正切函数 |
| `Math.Sqrt` | `(x: double) -> double` | 平方根 |
| `Math.Pow` | `(base: double, exp: double) -> double` | 幂运算 |
| `Math.Abs` | `(x: double) -> double` | 绝对值 |
| `Math.Floor` | `(x: double) -> double` | 向下取整 |
| `Math.Ceil` | `(x: double) -> double` | 向上取整 |
| `Math.Round` | `(x: double) -> double` | 四舍五入 |
| `Math.Max` | `(a: double, b: double) -> double` | 最大值 |
| `Math.Min` | `(a: double, b: double) -> double` | 最小值 |
| `Math.Log` | `(x: double) -> double` | 自然对数 |
| `Math.Log10` | `(x: double) -> double` | 以10为底的对数 |
| `Math.Exp` | `(x: double) -> double` | e的x次方 |

**示例**:
```old8lang
import "Math"

// 计算圆的面积
radius <- 5.0
area <- Math.PI * Math.Pow(radius, 2)
PrintLine("Area: " + area.ToStr())

// 三角函数
angle <- Math.PI / 4  // 45度
sin_value <- Math.Sin(angle)
cos_value <- Math.Cos(angle)
PrintLine("sin(45°) = " + sin_value.ToStr())
PrintLine("cos(45°) = " + cos_value.ToStr())
```

---

### File 模块

**模式支持**: ✅ 解释模式 | ✅ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "File"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `File.Read` | `(path: string) -> string` | 读取文件全部内容 |
| `File.Write` | `(path: string, content: string) -> void` | 写入文件 |
| `File.Append` | `(path: string, content: string) -> void` | 追加内容到文件 |
| `File.Exists` | `(path: string) -> bool` | 检查文件是否存在 |
| `File.Delete` | `(path: string) -> void` | 删除文件 |
| `File.Copy` | `(source: string, dest: string) -> void` | 复制文件 |
| `File.Move` | `(source: string, dest: string) -> void` | 移动文件 |
| `File.GetSize` | `(path: string) -> int` | 获取文件大小 |
| `File.ReadLines` | `(path: string) -> list<string>` | 按行读取文件 |
| `File.WriteLines` | `(path: string, lines: list<string>) -> void` | 按行写入文件 |

**示例**:
```old8lang
import "File"

// 写入文件
File.Write("test.txt", "Hello, Old8Lang!")

// 读取文件
content <- File.Read("test.txt")
PrintLine(content)

// 检查文件是否存在
if File.Exists("test.txt") {
    PrintLine("File exists")
    size <- File.GetSize("test.txt")
    PrintLine("Size: " + size.ToStr() + " bytes")
}

// 按行读取
lines <- File.ReadLines("test.txt")
for line <- lines {
    PrintLine("Line: " + line)
}
```

---

### Crypto 模块

**模式支持**: ✅ 解释模式 | ✅ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "Crypto"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `Crypto.MD5` | `(data: string) -> string` | MD5 哈希 |
| `Crypto.SHA1` | `(data: string) -> string` | SHA1 哈希 |
| `Crypto.SHA256` | `(data: string) -> string` | SHA256 哈希 |
| `Crypto.SHA512` | `(data: string) -> string` | SHA512 哈希 |
| `Crypto.AESEncrypt` | `(data: string, key: string) -> string` | AES 加密 |
| `Crypto.AESDecrypt` | `(encrypted: string, key: string) -> string` | AES 解密 |
| `Crypto.Base64Encode` | `(data: string) -> string` | Base64 编码 |
| `Crypto.Base64Decode` | `(encoded: string) -> string` | Base64 解码 |

**示例**:
```old8lang
import "Crypto"

// 哈希函数
text <- "Hello, World!"
md5_hash <- Crypto.MD5(text)
sha256_hash <- Crypto.SHA256(text)
PrintLine("MD5: " + md5_hash)
PrintLine("SHA256: " + sha256_hash)

// AES 加密/解密
key <- "my-secret-key-32-characters-long"
encrypted <- Crypto.AESEncrypt("Secret Message", key)
decrypted <- Crypto.AESDecrypt(encrypted, key)
PrintLine("Encrypted: " + encrypted)
PrintLine("Decrypted: " + decrypted)

// Base64 编码
encoded <- Crypto.Base64Encode("Hello")
decoded <- Crypto.Base64Decode(encoded)
PrintLine("Encoded: " + encoded)
PrintLine("Decoded: " + decoded)
```

---

### Image 模块

**模式支持**: ✅ 解释模式 | ✅ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "Image"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `Image.Load` | `(path: string) -> object` | 加载图像 |
| `Image.Save` | `(image: object, path: string) -> void` | 保存图像 |
| `Image.Resize` | `(image: object, width: int, height: int) -> object` | 调整大小 |
| `Image.Crop` | `(image: object, x: int, y: int, w: int, h: int) -> object` | 裁剪图像 |
| `Image.Rotate` | `(image: object, angle: double) -> object` | 旋转图像 |
| `Image.Flip` | `(image: object, mode: string) -> object` | 翻转图像 |
| `Image.GetWidth` | `(image: object) -> int` | 获取宽度 |
| `Image.GetHeight` | `(image: object) -> int` | 获取高度 |

**示例**:
```old8lang
import "Image"

// 加载图像
img <- Image.Load("photo.jpg")
width <- Image.GetWidth(img)
height <- Image.GetHeight(img)
PrintLine("Size: " + width.ToStr() + "x" + height.ToStr())

// 调整大小
resized <- Image.Resize(img, 800, 600)
Image.Save(resized, "photo_resized.jpg")

// 裁剪
cropped <- Image.Crop(img, 100, 100, 400, 300)
Image.Save(cropped, "photo_cropped.jpg")

// 旋转
rotated <- Image.Rotate(img, 90)
Image.Save(rotated, "photo_rotated.jpg")
```

---

### Regex 模块

**模式支持**: ✅ 解释模式 | ✅ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "Regex"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `Regex.Match` | `(pattern: string, text: string) -> bool` | 匹配模式 |
| `Regex.Find` | `(pattern: string, text: string) -> string` | 查找第一个匹配 |
| `Regex.FindAll` | `(pattern: string, text: string) -> list<string>` | 查找所有匹配 |
| `Regex.Replace` | `(pattern: string, text: string, replacement: string) -> string` | 替换匹配 |
| `Regex.Split` | `(pattern: string, text: string) -> list<string>` | 按模式分割 |

**示例**:
```old8lang
import "Regex"

// 匹配邮箱
email <- "user@example.com"
pattern <- "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"
if Regex.Match(pattern, email) {
    PrintLine("Valid email")
}

// 查找所有数字
text <- "Price: $123.45, Quantity: 10"
numbers <- Regex.FindAll("\\d+", text)
for num <- numbers {
    PrintLine("Found: " + num)
}

// 替换
result <- Regex.Replace("\\d+", "The year is 2024", "XXXX")
PrintLine(result)  // "The year is XXXX"
```

---

### Terminal 模块

**模式支持**: ✅ 解释模式 | ✅ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "Terminal"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `Terminal.Clear` | `() -> void` | 清空终端 |
| `Terminal.SetCursorPosition` | `(x: int, y: int) -> void` | 设置光标位置 |
| `Terminal.GetCursorPosition` | `() -> object` | 获取光标位置 |
| `Terminal.SetTitle` | `(title: string) -> void` | 设置终端标题 |
| `Terminal.Beep` | `() -> void` | 发出蜂鸣声 |
| `Terminal.GetWidth` | `() -> int` | 获取终端宽度 |
| `Terminal.GetHeight` | `() -> int` | 获取终端高度 |

**示例**:
```old8lang
import "Terminal"

// 清空终端
Terminal.Clear()

// 设置标题
Terminal.SetTitle("Old8Lang Application")

// 获取终端大小
width <- Terminal.GetWidth()
height <- Terminal.GetHeight()
PrintLine("Terminal size: " + width.ToStr() + "x" + height.ToStr())

// 设置光标位置并输出
Terminal.SetCursorPosition(10, 5)
PrintLine("Hello at (10, 5)")
```

---

### ColorfulTerminal 模块

**模式支持**: ✅ 解释模式 | ✅ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "ColorfulTerminal"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `ColorfulTerminal.Print` | `(text: string, color: string) -> void` | 彩色输出 |
| `ColorfulTerminal.PrintLine` | `(text: string, color: string) -> void` | 彩色输出并换行 |
| `ColorfulTerminal.SetForeground` | `(color: string) -> void` | 设置前景色 |
| `ColorfulTerminal.SetBackground` | `(color: string) -> void` | 设置背景色 |
| `ColorfulTerminal.Reset` | `() -> void` | 重置颜色 |

**支持的颜色**: `"Red"`, `"Green"`, `"Blue"`, `"Yellow"`, `"Cyan"`, `"Magenta"`, `"White"`, `"Black"`

**示例**:
```old8lang
import "ColorfulTerminal"

// 彩色输出
ColorfulTerminal.PrintLine("Success!", "Green")
ColorfulTerminal.PrintLine("Warning!", "Yellow")
ColorfulTerminal.PrintLine("Error!", "Red")

// 设置颜色
ColorfulTerminal.SetForeground("Cyan")
PrintLine("This is cyan text")
ColorfulTerminal.Reset()
```

---

### Time 模块

**模式支持**: ✅ 解释模式 | ✅ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "Time"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `Time.Now` | `() -> object` | 获取当前时间 |
| `Time.Format` | `(time: object, format: string) -> string` | 格式化时间 |
| `Time.Parse` | `(timeStr: string, format: string) -> object` | 解析时间字符串 |
| `Time.AddDays` | `(time: object, days: int) -> object` | 添加天数 |
| `Time.AddHours` | `(time: object, hours: int) -> object` | 添加小时 |
| `Time.AddMinutes` | `(time: object, minutes: int) -> object` | 添加分钟 |
| `Time.Diff` | `(time1: object, time2: object) -> int` | 计算时间差（秒） |
| `Time.Sleep` | `(milliseconds: int) -> void` | 休眠 |

**示例**:
```old8lang
import "Time"

// 获取当前时间
now <- Time.Now()
formatted <- Time.Format(now, "yyyy-MM-dd HH:mm:ss")
PrintLine("Current time: " + formatted)

// 时间运算
tomorrow <- Time.AddDays(now, 1)
PrintLine("Tomorrow: " + Time.Format(tomorrow, "yyyy-MM-dd"))

// 解析时间
parsed <- Time.Parse("2024-01-01 12:00:00", "yyyy-MM-dd HH:mm:ss")

// 计算时间差
diff <- Time.Diff(tomorrow, now)
PrintLine("Difference: " + diff.ToStr() + " seconds")

// 休眠
PrintLine("Sleeping for 1 second...")
Time.Sleep(1000)
PrintLine("Done!")
```

---

### OS 模块

**模式支持**: ✅ 解释模式 | ✅ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "OS"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `OS.GetEnv` | `(name: string) -> string` | 获取环境变量 |
| `OS.SetEnv` | `(name: string, value: string) -> void` | 设置环境变量 |
| `OS.GetPlatform` | `() -> string` | 获取平台名称 |
| `OS.GetArch` | `() -> string` | 获取架构 |
| `OS.Exec` | `(command: string) -> string` | 执行系统命令 |
| `OS.GetCurrentDir` | `() -> string` | 获取当前目录 |
| `OS.SetCurrentDir` | `(path: string) -> void` | 设置当前目录 |
| `OS.Exit` | `(code: int) -> void` | 退出程序 |

**示例**:
```old8lang
import "OS"

// 获取平台信息
platform <- OS.GetPlatform()
arch <- OS.GetArch()
PrintLine("Platform: " + platform + " (" + arch + ")")

// 环境变量
path <- OS.GetEnv("PATH")
PrintLine("PATH: " + path)

// 执行命令
result <- OS.Exec("echo Hello from shell")
PrintLine(result)

// 目录操作
current <- OS.GetCurrentDir()
PrintLine("Current directory: " + current)
```

---

### CSV 模块

**模式支持**: ✅ 解释模式 | ✅ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "CSV"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `CSV.Read` | `(path: string) -> list<list<string>>` | 读取 CSV 文件 |
| `CSV.Write` | `(path: string, data: list<list<string>>) -> void` | 写入 CSV 文件 |
| `CSV.Parse` | `(content: string) -> list<list<string>>` | 解析 CSV 字符串 |
| `CSV.Stringify` | `(data: list<list<string>>) -> string` | 转换为 CSV 字符串 |

**示例**:
```old8lang
import "CSV"

// 写入 CSV
data <- [
    ["Name", "Age", "City"],
    ["Alice", "30", "New York"],
    ["Bob", "25", "London"]
]
CSV.Write("data.csv", data)

// 读取 CSV
rows <- CSV.Read("data.csv")
for row <- rows {
    PrintLine(row[0] + ", " + row[1] + ", " + row[2])
}
```

---

### Template 模块

**模式支持**: ✅ 解释模式 | ✅ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "Template"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `Template.Render` | `(template: string, data: dict) -> string` | 渲染模板 |
| `Template.RenderFile` | `(path: string, data: dict) -> string` | 渲染模板文件 |

**模板语法**:
- `{{variable}}` - 变量替换
- `{{#if condition}}...{{/if}}` - 条件
- `{{#each items}}...{{/each}}` - 循环

**示例**:
```old8lang
import "Template"

// 简单模板
template <- "Hello, {{name}}! You are {{age}} years old."
data <- {"name": "Alice", "age": 30}
result <- Template.Render(template, data)
PrintLine(result)  // "Hello, Alice! You are 30 years old."

// 条件和循环
template2 <- "{{#if show}}Items: {{#each items}}{{this}}, {{/each}}{{/if}}"
data2 <- {"show": true, "items": [1, 2, 3]}
result2 <- Template.Render(template2, data2)
PrintLine(result2)
```

---

### Vector 模块

**模式支持**: ✅ 解释模式 | ✅ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "Vector"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `Vector.Create` | `(values: list<double>) -> object` | 创建向量 |
| `Vector.Add` | `(v1: object, v2: object) -> object` | 向量加法 |
| `Vector.Subtract` | `(v1: object, v2: object) -> object` | 向量减法 |
| `Vector.Multiply` | `(v: object, scalar: double) -> object` | 标量乘法 |
| `Vector.Dot` | `(v1: object, v2: object) -> double` | 点积 |
| `Vector.Cross` | `(v1: object, v2: object) -> object` | 叉积（3D） |
| `Vector.Magnitude` | `(v: object) -> double` | 向量长度 |
| `Vector.Normalize` | `(v: object) -> object` | 归一化 |

**示例**:
```old8lang
import "Vector"

// 创建向量
v1 <- Vector.Create([1.0, 2.0, 3.0])
v2 <- Vector.Create([4.0, 5.0, 6.0])

// 向量运算
sum <- Vector.Add(v1, v2)
diff <- Vector.Subtract(v1, v2)
scaled <- Vector.Multiply(v1, 2.0)

// 点积和叉积
dot <- Vector.Dot(v1, v2)
cross <- Vector.Cross(v1, v2)

// 长度和归一化
length <- Vector.Magnitude(v1)
normalized <- Vector.Normalize(v1)

PrintLine("Dot product: " + dot.ToStr())
PrintLine("Length: " + length.ToStr())
```

## 网络库 (Old8Lang.NetLib)

**位置**: `Old8Lang.NetLib/`

网络库提供 HTTP、WebSocket、MQTT、Socket 等网络功能。

### HTTP 模块

**模式支持**: ✅ 解释模式 | ✅ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "HTTP"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `HTTP.Get` | `(url: string) -> string` | GET 请求 |
| `HTTP.Post` | `(url: string, data: string) -> string` | POST 请求 |
| `HTTP.Put` | `(url: string, data: string) -> string` | PUT 请求 |
| `HTTP.Delete` | `(url: string) -> string` | DELETE 请求 |
| `HTTP.SetHeader` | `(name: string, value: string) -> void` | 设置请求头 |
| `HTTP.SetTimeout` | `(milliseconds: int) -> void` | 设置超时 |

**示例**:
```old8lang
import "HTTP"

// GET 请求
response <- HTTP.Get("https://api.example.com/users")
PrintLine(response)

// POST 请求
HTTP.SetHeader("Content-Type", "application/json")
data <- "{\"name\": \"Alice\", \"age\": 30}"
result <- HTTP.Post("https://api.example.com/users", data)
PrintLine(result)

// 设置超时
HTTP.SetTimeout(5000)  // 5秒
```

---

### WebSocket 模块

**模式支持**: ✅ 解释模式 | ❌ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "WebSocket"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `WebSocket.Connect` | `(url: string) -> object` | 连接 WebSocket |
| `WebSocket.Send` | `(ws: object, message: string) -> void` | 发送消息 |
| `WebSocket.Receive` | `(ws: object) -> string` | 接收消息 |
| `WebSocket.Close` | `(ws: object) -> void` | 关闭连接 |
| `WebSocket.IsConnected` | `(ws: object) -> bool` | 检查连接状态 |

**示例**:
```old8lang
import "WebSocket"

// 连接 WebSocket
ws <- WebSocket.Connect("wss://echo.websocket.org")

// 发送消息
WebSocket.Send(ws, "Hello, WebSocket!")

// 接收消息
message <- WebSocket.Receive(ws)
PrintLine("Received: " + message)

// 关闭连接
WebSocket.Close(ws)
```

---

### MQTT 模块

**模式支持**: ✅ 解释模式 | ❌ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "MQTT"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `MQTT.Connect` | `(broker: string, port: int) -> object` | 连接 MQTT 代理 |
| `MQTT.Publish` | `(client: object, topic: string, message: string) -> void` | 发布消息 |
| `MQTT.Subscribe` | `(client: object, topic: string) -> void` | 订阅主题 |
| `MQTT.Receive` | `(client: object) -> string` | 接收消息 |
| `MQTT.Disconnect` | `(client: object) -> void` | 断开连接 |

**示例**:
```old8lang
import "MQTT"

// 连接 MQTT 代理
client <- MQTT.Connect("broker.hivemq.com", 1883)

// 订阅主题
MQTT.Subscribe(client, "test/topic")

// 发布消息
MQTT.Publish(client, "test/topic", "Hello, MQTT!")

// 接收消息
message <- MQTT.Receive(client)
PrintLine("Received: " + message)

// 断开连接
MQTT.Disconnect(client)
```

---

### Socket 模块

**模式支持**: ✅ 解释模式 | ✅ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "Socket"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `Socket.Create` | `(type: string) -> object` | 创建 Socket |
| `Socket.Connect` | `(socket: object, host: string, port: int) -> void` | 连接服务器 |
| `Socket.Send` | `(socket: object, data: string) -> void` | 发送数据 |
| `Socket.Receive` | `(socket: object, size: int) -> string` | 接收数据 |
| `Socket.Close` | `(socket: object) -> void` | 关闭 Socket |
| `Socket.Bind` | `(socket: object, port: int) -> void` | 绑定端口 |
| `Socket.Listen` | `(socket: object, backlog: int) -> void` | 监听连接 |
| `Socket.Accept` | `(socket: object) -> object` | 接受连接 |

**示例**:
```old8lang
import "Socket"

// TCP 客户端
socket <- Socket.Create("TCP")
Socket.Connect(socket, "example.com", 80)
Socket.Send(socket, "GET / HTTP/1.1\r\nHost: example.com\r\n\r\n")
response <- Socket.Receive(socket, 1024)
PrintLine(response)
Socket.Close(socket)

// TCP 服务器
server <- Socket.Create("TCP")
Socket.Bind(server, 8080)
Socket.Listen(server, 5)
PrintLine("Server listening on port 8080")
client <- Socket.Accept(server)
data <- Socket.Receive(client, 1024)
Socket.Send(client, "Hello, Client!")
Socket.Close(client)
Socket.Close(server)
```

---

### WebAPI 模块

**模式支持**: ✅ 解释模式 | ✅ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "WebAPI"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `WebAPI.Request` | `(method: string, url: string, data: string) -> string` | 通用请求 |
| `WebAPI.SetAuth` | `(type: string, token: string) -> void` | 设置认证 |
| `WebAPI.ParseJSON` | `(json: string) -> object` | 解析 JSON |
| `WebAPI.ToJSON` | `(obj: object) -> string` | 转换为 JSON |

**示例**:
```old8lang
import "WebAPI"

// 设置认证
WebAPI.SetAuth("Bearer", "your-api-token")

// 发送请求
response <- WebAPI.Request("GET", "https://api.example.com/data", "")
data <- WebAPI.ParseJSON(response)

// 处理数据
PrintLine("Data: " + data.ToStr())
```

---

## 数据库库 (Old8Lang.DatabaseLib)

**位置**: `Old8Lang.DatabaseLib/`

数据库库提供 MySQL、PostgreSQL、SQLite 等数据库连接和操作。

### MySQL 模块

**模式支持**: ✅ 解释模式 | ✅ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "MySQL"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `MySQL.Connect` | `(host: string, user: string, password: string, database: string) -> object` | 连接数据库 |
| `MySQL.Query` | `(conn: object, sql: string) -> list<dict>` | 执行查询 |
| `MySQL.Execute` | `(conn: object, sql: string) -> int` | 执行命令 |
| `MySQL.Close` | `(conn: object) -> void` | 关闭连接 |

**示例**:
```old8lang
import "MySQL"

// 连接数据库
conn <- MySQL.Connect("localhost", "root", "password", "testdb")

// 查询数据
results <- MySQL.Query(conn, "SELECT * FROM users")
for row <- results {
    PrintLine("User: " + row["name"])
}

// 插入数据
affected <- MySQL.Execute(conn, "INSERT INTO users (name, age) VALUES ('Alice', 30)")
PrintLine("Inserted " + affected.ToStr() + " rows")

// 关闭连接
MySQL.Close(conn)
```

---

### PostgreSQL 模块

**模式支持**: ✅ 解释模式 | ✅ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "PostgreSQL"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `PostgreSQL.Connect` | `(connectionString: string) -> object` | 连接数据库 |
| `PostgreSQL.Query` | `(conn: object, sql: string) -> list<dict>` | 执行查询 |
| `PostgreSQL.Execute` | `(conn: object, sql: string) -> int` | 执行命令 |
| `PostgreSQL.Close` | `(conn: object) -> void` | 关闭连接 |

**示例**:
```old8lang
import "PostgreSQL"

// 连接数据库
connStr <- "Host=localhost;Username=postgres;Password=password;Database=testdb"
conn <- PostgreSQL.Connect(connStr)

// 查询数据
results <- PostgreSQL.Query(conn, "SELECT * FROM users")
for row <- results {
    PrintLine("User: " + row["name"])
}

// 关闭连接
PostgreSQL.Close(conn)
```

---

### SQLite 模块

**模式支持**: ✅ 解释模式 | ✅ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "SQLite"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `SQLite.Connect` | `(path: string) -> object` | 连接数据库 |
| `SQLite.Query` | `(conn: object, sql: string) -> list<dict>` | 执行查询 |
| `SQLite.Execute` | `(conn: object, sql: string) -> int` | 执行命令 |
| `SQLite.Close` | `(conn: object) -> void` | 关闭连接 |

**示例**:
```old8lang
import "SQLite"

// 连接数据库（自动创建）
conn <- SQLite.Connect("test.db")

// 创建表
SQLite.Execute(conn, "CREATE TABLE IF NOT EXISTS users (id INTEGER PRIMARY KEY, name TEXT, age INTEGER)")

// 插入数据
SQLite.Execute(conn, "INSERT INTO users (name, age) VALUES ('Alice', 30)")

// 查询数据
results <- SQLite.Query(conn, "SELECT * FROM users")
for row <- results {
    PrintLine("User: " + row["name"] + ", Age: " + row["age"].ToStr())
}

// 关闭连接
SQLite.Close(conn)
```

---

### InMemory 模块

**模式支持**: ✅ 解释模式 | ✅ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "InMemory"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `InMemory.Create` | `() -> object` | 创建内存数据库 |
| `InMemory.Set` | `(db: object, key: string, value: object) -> void` | 设置值 |
| `InMemory.Get` | `(db: object, key: string) -> object` | 获取值 |
| `InMemory.Delete` | `(db: object, key: string) -> void` | 删除值 |
| `InMemory.Exists` | `(db: object, key: string) -> bool` | 检查键是否存在 |
| `InMemory.Clear` | `(db: object) -> void` | 清空数据库 |

**示例**:
```old8lang
import "InMemory"

// 创建内存数据库
db <- InMemory.Create()

// 存储数据
InMemory.Set(db, "user:1", {"name": "Alice", "age": 30})
InMemory.Set(db, "user:2", {"name": "Bob", "age": 25})

// 读取数据
user <- InMemory.Get(db, "user:1")
PrintLine("User: " + user["name"])

// 检查存在
if InMemory.Exists(db, "user:1") {
    PrintLine("User 1 exists")
}

// 删除数据
InMemory.Delete(db, "user:2")
```

---

### ORM 模块

**模式支持**: ✅ 解释模式 | ❌ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "ORM"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `ORM.Define` | `(name: string, schema: dict) -> object` | 定义模型 |
| `ORM.Create` | `(model: object, data: dict) -> object` | 创建记录 |
| `ORM.Find` | `(model: object, id: int) -> object` | 查找记录 |
| `ORM.FindAll` | `(model: object, filter: dict) -> list<object>` | 查找所有记录 |
| `ORM.Update` | `(record: object, data: dict) -> void` | 更新记录 |
| `ORM.Delete` | `(record: object) -> void` | 删除记录 |

**示例**:
```old8lang
import "ORM"

// 定义模型
User <- ORM.Define("User", {
    "name": "string",
    "age": "int",
    "email": "string"
})

// 创建记录
user <- ORM.Create(User, {"name": "Alice", "age": 30, "email": "alice@example.com"})

// 查找记录
found <- ORM.Find(User, 1)
PrintLine("Found: " + found["name"])

// 更新记录
ORM.Update(user, {"age": 31})

// 删除记录
ORM.Delete(user)
```

---

## 序列化库 (Old8Lang.SerializationLib)

**位置**: `Old8Lang.SerializationLib/`

序列化库提供 MessagePack、Protobuf 等序列化格式支持。

### MessagePack 模块

**模式支持**: ✅ 解释模式 | ✅ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "MessagePack"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `MessagePack.Serialize` | `(obj: object) -> string` | 序列化对象 |
| `MessagePack.Deserialize` | `(data: string) -> object` | 反序列化 |

**示例**:
```old8lang
import "MessagePack"

// 序列化
data <- {"name": "Alice", "age": 30, "items": [1, 2, 3]}
serialized <- MessagePack.Serialize(data)
PrintLine("Serialized: " + serialized)

// 反序列化
deserialized <- MessagePack.Deserialize(serialized)
PrintLine("Name: " + deserialized["name"])
PrintLine("Age: " + deserialized["age"].ToStr())
```

---

### Protobuf 模块

**模式支持**: ✅ 解释模式 | ✅ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "Protobuf"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `Protobuf.Serialize` | `(obj: object, schema: string) -> string` | 序列化对象 |
| `Protobuf.Deserialize` | `(data: string, schema: string) -> object` | 反序列化 |
| `Protobuf.LoadSchema` | `(path: string) -> string` | 加载 .proto 文件 |

**示例**:
```old8lang
import "Protobuf"

// 加载 schema
schema <- Protobuf.LoadSchema("user.proto")

// 序列化
data <- {"name": "Alice", "age": 30}
serialized <- Protobuf.Serialize(data, schema)

// 反序列化
deserialized <- Protobuf.Deserialize(serialized, schema)
PrintLine("Name: " + deserialized["name"])
```

---

### Factory 模块

**模式支持**: ✅ 解释模式 | ✅ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "SerializerFactory"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `SerializerFactory.Create` | `(type: string) -> object` | 创建序列化器 |
| `SerializerFactory.Serialize` | `(serializer: object, obj: object) -> string` | 序列化 |
| `SerializerFactory.Deserialize` | `(serializer: object, data: string) -> object` | 反序列化 |

**支持的类型**: `"JSON"`, `"MessagePack"`, `"Protobuf"`, `"XML"`

**示例**:
```old8lang
import "SerializerFactory"

// 创建序列化器
serializer <- SerializerFactory.Create("JSON")

// 序列化
data <- {"name": "Alice", "age": 30}
serialized <- SerializerFactory.Serialize(serializer, data)
PrintLine(serialized)

// 反序列化
deserialized <- SerializerFactory.Deserialize(serializer, serialized)
PrintLine("Name: " + deserialized["name"])
```

---

## 机器学习库 (Old8Lang.MachineLearningLib)

**位置**: `Old8Lang.MachineLearningLib/`

机器学习库提供分类、回归、聚类等机器学习功能。

### Classification 模块

**模式支持**: ✅ 解释模式 | ❌ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "Classification"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `Classification.Train` | `(data: list<list<double>>, labels: list<int>, algorithm: string) -> object` | 训练分类模型 |
| `Classification.Predict` | `(model: object, input: list<double>) -> int` | 预测类别 |
| `Classification.Evaluate` | `(model: object, testData: list<list<double>>, testLabels: list<int>) -> double` | 评估模型 |

**支持的算法**: `"LogisticRegression"`, `"DecisionTree"`, `"RandomForest"`, `"SVM"`, `"NaiveBayes"`

**示例**:
```old8lang
import "Classification"

// 训练数据
data <- [[1.0, 2.0], [2.0, 3.0], [3.0, 4.0], [4.0, 5.0]]
labels <- [0, 0, 1, 1]

// 训练模型
model <- Classification.Train(data, labels, "LogisticRegression")

// 预测
prediction <- Classification.Predict(model, [2.5, 3.5])
PrintLine("Prediction: " + prediction.ToStr())

// 评估
accuracy <- Classification.Evaluate(model, data, labels)
PrintLine("Accuracy: " + accuracy.ToStr())
```

---

### Regression 模块

**模式支持**: ✅ 解释模式 | ❌ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "Regression"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `Regression.Train` | `(data: list<list<double>>, targets: list<double>, algorithm: string) -> object` | 训练回归模型 |
| `Regression.Predict` | `(model: object, input: list<double>) -> double` | 预测值 |
| `Regression.Evaluate` | `(model: object, testData: list<list<double>>, testTargets: list<double>) -> double` | 评估模型 |

**支持的算法**: `"LinearRegression"`, `"PolynomialRegression"`, `"Ridge"`, `"Lasso"`

**示例**:
```old8lang
import "Regression"

// 训练数据
data <- [[1.0], [2.0], [3.0], [4.0]]
targets <- [2.0, 4.0, 6.0, 8.0]

// 训练模型
model <- Regression.Train(data, targets, "LinearRegression")

// 预测
prediction <- Regression.Predict(model, [5.0])
PrintLine("Prediction: " + prediction.ToStr())

// 评估（R² 分数）
score <- Regression.Evaluate(model, data, targets)
PrintLine("R² Score: " + score.ToStr())
```

---

### Clustering 模块

**模式支持**: ✅ 解释模式 | ❌ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "Clustering"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `Clustering.Train` | `(data: list<list<double>>, numClusters: int, algorithm: string) -> object` | 训练聚类模型 |
| `Clustering.Predict` | `(model: object, input: list<double>) -> int` | 预测簇 |
| `Clustering.GetCenters` | `(model: object) -> list<list<double>>` | 获取簇中心 |

**支持的算法**: `"KMeans"`, `"DBSCAN"`, `"HierarchicalClustering"`

**示例**:
```old8lang
import "Clustering"

// 训练数据
data <- [[1.0, 2.0], [1.5, 1.8], [5.0, 8.0], [8.0, 8.0], [1.0, 0.6], [9.0, 11.0]]

// 训练模型（3个簇）
model <- Clustering.Train(data, 3, "KMeans")

// 预测
cluster <- Clustering.Predict(model, [2.0, 2.0])
PrintLine("Cluster: " + cluster.ToStr())

// 获取簇中心
centers <- Clustering.GetCenters(model)
for i <- 0, i < centers.Length(), i <- i + 1 {
    PrintLine("Center " + i.ToStr() + ": " + centers[i].ToStr())
}
```

---

### DataLoader 模块

**模式支持**: ✅ 解释模式 | ✅ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "DataLoader"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `DataLoader.LoadCSV` | `(path: string) -> list<list<double>>` | 加载 CSV 数据 |
| `DataLoader.LoadJSON` | `(path: string) -> object` | 加载 JSON 数据 |
| `DataLoader.Normalize` | `(data: list<list<double>>) -> list<list<double>>` | 归一化数据 |
| `DataLoader.Split` | `(data: list<list<double>>, ratio: double) -> object` | 分割训练/测试集 |

**示例**:
```old8lang
import "DataLoader"

// 加载数据
data <- DataLoader.LoadCSV("data.csv")

// 归一化
normalized <- DataLoader.Normalize(data)

// 分割数据（80% 训练，20% 测试）
split <- DataLoader.Split(normalized, 0.8)
trainData <- split["train"]
testData <- split["test"]

PrintLine("Train size: " + trainData.Length().ToStr())
PrintLine("Test size: " + testData.Length().ToStr())
```

---

### Predictor 模块

**模式支持**: ✅ 解释模式 | ✅ 编译模式 | ✅ VM 模式

**导入方式**:
```old8lang
import "Predictor"
```

**主要函数**:

| 函数 | 签名 | 描述 |
|------|------|------|
| `Predictor.LoadModel` | `(path: string) -> object` | 加载模型 |
| `Predictor.SaveModel` | `(model: object, path: string) -> void` | 保存模型 |
| `Predictor.Predict` | `(model: object, input: object) -> object` | 预测 |
| `Predictor.BatchPredict` | `(model: object, inputs: list<object>) -> list<object>` | 批量预测 |

**示例**:
```old8lang
import "Predictor"
import "Classification"

// 训练模型
data <- [[1.0, 2.0], [2.0, 3.0], [3.0, 4.0]]
labels <- [0, 0, 1]
model <- Classification.Train(data, labels, "LogisticRegression")

// 保存模型
Predictor.SaveModel(model, "model.pkl")

// 加载模型
loadedModel <- Predictor.LoadModel("model.pkl")

// 预测
prediction <- Predictor.Predict(loadedModel, [2.5, 3.5])
PrintLine("Prediction: " + prediction.ToStr())

// 批量预测
inputs <- [[1.5, 2.5], [3.5, 4.5]]
predictions <- Predictor.BatchPredict(loadedModel, inputs)
for pred <- predictions {
    PrintLine("Prediction: " + pred.ToStr())
}
```

---

## 总结

本 API 参考文档涵盖了 Old8Lang 的完整标准库，包括：

- **核心标准库 (Old8LangLib)**: 12个模块 - Math, File, Crypto, Image, Regex, Terminal, ColorfulTerminal, Time, OS, CSV, Template, Vector
- **网络库 (Old8Lang.NetLib)**: 5个模块 - HTTP, WebSocket, MQTT, Socket, WebAPI
- **数据库库 (Old8Lang.DatabaseLib)**: 5个模块 - MySQL, PostgreSQL, SQLite, InMemory, ORM
- **序列化库 (Old8Lang.SerializationLib)**: 3个模块 - MessagePack, Protobuf, Factory
- **机器学习库 (Old8Lang.MachineLearningLib)**: 5个模块 - Classification, Regression, Clustering, DataLoader, Predictor

每个模块都标注了支持的执行模式（解释模式、编译模式、VM 模式），并提供了完整的函数签名和可运行的代码示例。

更多信息请参考:
- [ARCHITECTURE.md](ARCHITECTURE.md) - 架构文档
- [LANGUAGE_FEATURES.md](LANGUAGE_FEATURES.md) - 语言特性文档
- [CLI_GUIDE.md](CLI_GUIDE.md) - CLI 命令参考
- [Old8Lang_Grammar.md](Old8Lang_Grammar.md) - 完整语法说明

