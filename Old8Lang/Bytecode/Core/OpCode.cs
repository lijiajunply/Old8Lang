namespace Old8Lang.Bytecode.Core;

/// <summary>
/// 字节码操作码枚举
/// </summary>
public enum OpCode : byte
{
    // ===== 栈操作 (0x00-0x0F) =====
    /// <summary>无操作</summary>
    Nop = 0x00,

    /// <summary>从常量池加载常量到栈 (操作数: constantIndex)</summary>
    LoadConst = 0x01,

    /// <summary>加载局部变量到栈 (操作数: localIndex)</summary>
    LoadLocal = 0x02,

    /// <summary>存储栈顶值到局部变量 (操作数: localIndex)</summary>
    StoreLocal = 0x03,

    /// <summary>加载全局变量到栈 (操作数: nameIndex)</summary>
    LoadGlobal = 0x04,

    /// <summary>存储栈顶值到全局变量 (操作数: nameIndex)</summary>
    StoreGlobal = 0x05,

    /// <summary>弹出栈顶元素</summary>
    Pop = 0x06,

    /// <summary>复制栈顶元素</summary>
    Dup = 0x07,

    /// <summary>加载null值</summary>
    LoadNull = 0x08,

    /// <summary>加载true值</summary>
    LoadTrue = 0x09,

    /// <summary>加载false值</summary>
    LoadFalse = 0x0A,

    /// <summary>交换栈顶两个元素</summary>
    Swap = 0x0B,

    // ===== 算术运算 (0x10-0x1F) =====
    /// <summary>加法: b, a → (a + b)</summary>
    Add = 0x10,

    /// <summary>减法: b, a → (a - b)</summary>
    Sub = 0x11,

    /// <summary>乘法: b, a → (a * b)</summary>
    Mul = 0x12,

    /// <summary>除法: b, a → (a / b)</summary>
    Div = 0x13,

    /// <summary>取模: b, a → (a % b)</summary>
    Mod = 0x14,

    /// <summary>取反: a → (-a)</summary>
    Neg = 0x15,

    /// <summary>幂运算: b, a → (a ** b)</summary>
    Pow = 0x16,

    // ===== 比较运算 (0x20-0x2F) =====
    /// <summary>等于: b, a → (a == b)</summary>
    Equal = 0x20,

    /// <summary>不等于: b, a → (a != b)</summary>
    NotEqual = 0x21,

    /// <summary>大于: b, a → (a > b)</summary>
    Greater = 0x22,

    /// <summary>小于: b, a → (a &lt; b)</summary>
    Less = 0x23,

    /// <summary>大于等于: b, a → (a >= b)</summary>
    GreaterEqual = 0x24,

    /// <summary>小于等于: b, a → (a &lt;= b)</summary>
    LessEqual = 0x25,

    // ===== 逻辑运算 (0x30-0x3F) =====
    /// <summary>逻辑与: b, a → (a && b)</summary>
    And = 0x30,

    /// <summary>逻辑或: b, a → (a || b)</summary>
    Or = 0x31,

    /// <summary>逻辑非: a → (!a)</summary>
    Not = 0x32,

    // ===== 控制流 (0x40-0x4F) =====
    /// <summary>无条件跳转 (操作数: offset)</summary>
    Jump = 0x40,

    /// <summary>条件跳转(false) (操作数: offset)</summary>
    JumpIfFalse = 0x41,

    /// <summary>条件跳转(true) (操作数: offset)</summary>
    JumpIfTrue = 0x42,

    /// <summary>函数调用 (操作数: argCount, funcNameIndex)</summary>
    Call = 0x43,

    /// <summary>原生函数调用 (操作数: argCount, nativeFuncNameIndex)</summary>
    CallNative = 0x44,

    /// <summary>返回 (返回栈顶值)</summary>
    Return = 0x45,

    /// <summary>返回void</summary>
    ReturnVoid = 0x46,

    /// <summary>跳出循环 (break语句)</summary>
    Break = 0x47,

    /// <summary>继续下一次循环 (continue语句)</summary>
    Continue = 0x48,

    /// <summary>创建函数 (操作数: funcIndex)</summary>
    MakeFunction = 0x49,

    /// <summary>调用栈顶函数 (操作数: argCount)</summary>
    CallDynamic = 0x4A,

    /// <summary>创建闭包 (操作数: funcIndex, capturedVarCount, [varNames...])</summary>
    MakeClosure = 0x4B,

    // ===== 对象操作 (0x50-0x5F) =====
    /// <summary>创建新对象 (操作数: classNameIndex)</summary>
    NewObject = 0x50,

    /// <summary>获取字段 (操作数: fieldNameIndex)</summary>
    GetField = 0x51,

    /// <summary>设置字段 (操作数: fieldNameIndex)</summary>
    SetField = 0x52,

    /// <summary>获取索引元素 (array[index])</summary>
    GetIndex = 0x53,

    /// <summary>设置索引元素 (array[index] = value)</summary>
    SetIndex = 0x54,

    /// <summary>调用方法 (操作数: argCount, methodNameIndex)</summary>
    CallMethod = 0x55,

    /// <summary>加载super引用 (将当前实例作为super上下文压栈)</summary>
    LoadSuper = 0x56,

    /// <summary>加载this引用 (将当前实例压栈)</summary>
    LoadThis = 0x5A,

    /// <summary>调用父类方法 (操作数: argCount, methodNameIndex)</summary>
    CallSuperMethod = 0x57,

    /// <summary>获取父类字段 (操作数: fieldNameIndex)</summary>
    GetSuperField = 0x58,

    /// <summary>设置父类字段 (操作数: fieldNameIndex)</summary>
    SetSuperField = 0x59,

    // ===== 容器操作 (0x60-0x6F) =====
    /// <summary>创建数组 (操作数: elementCount)</summary>
    NewArray = 0x60,

    /// <summary>创建列表 (操作数: elementCount)</summary>
    NewList = 0x61,

    /// <summary>创建字典 (操作数: pairCount)</summary>
    NewDict = 0x62,

    /// <summary>获取数组长度</summary>
    ArrayLength = 0x63,

    /// <summary>创建元组 (操作数: elementCount)</summary>
    NewTuple = 0x64,

    /// <summary>创建范围 (start, end, step)</summary>
    NewRange = 0x65,

    /// <summary>获取迭代器 (从集合获取迭代器)</summary>
    GetIterator = 0x66,

    /// <summary>迭代器MoveNext (返回bool表示是否有下一个元素)</summary>
    IteratorMoveNext = 0x67,

    /// <summary>获取迭代器当前元素</summary>
    IteratorCurrent = 0x68,

    /// <summary>切片操作 (collection[start:end:step])</summary>
    Slice = 0x69,

    /// <summary>创建分组字典 (用于 GroupBy 操作)</summary>
    NewGroupDict = 0x6A,

    /// <summary>添加元素到分组 (操作数: groupDict, key, element)</summary>
    AddToGroup = 0x6B,

    /// <summary>将分组字典转换为分组列表</summary>
    GroupDictToList = 0x6C,

    // ===== 类型操作 (0x70-0x7F) =====
    /// <summary>类型转换 (操作数: targetTypeIndex)</summary>
    Cast = 0x70,

    /// <summary>类型检查 (操作数: typeNameIndex)</summary>
    IsType = 0x71,

    /// <summary>获取类型</summary>
    TypeOf = 0x72,

    /// <summary>定义枚举 (操作数: enumNameIndex, memberCount, [memberName, memberValue]...)</summary>
    DefineEnum = 0x73,

    /// <summary>定义接口 (操作数: interfaceNameIndex, methodCount)</summary>
    DefineInterface = 0x74,

    /// <summary>定义Mixin (操作数: mixinNameIndex, methodCount)</summary>
    DefineMixin = 0x75,

    /// <summary>应用Mixin到类 (操作数: mixinNameIndex)</summary>
    ApplyMixin = 0x76,

    /// <summary>检查接口实现 (操作数: interfaceNameIndex)</summary>
    CheckInterface = 0x77,

    // ===== 并发原语 (0x80-0x9F) =====
    /// <summary>创建互斥锁</summary>
    MutexCreate = 0x80,

    /// <summary>锁定互斥锁 (操作数: mutexId)</summary>
    MutexLock = 0x81,

    /// <summary>解锁互斥锁 (操作数: mutexId)</summary>
    MutexUnlock = 0x82,

    /// <summary>释放互斥锁 (操作数: mutexId)</summary>
    MutexDispose = 0x83,

    /// <summary>创建通道</summary>
    ChannelCreate = 0x84,

    /// <summary>通道发送 (操作数: channelId, value)</summary>
    ChannelSend = 0x85,

    /// <summary>通道接收 (操作数: channelId)</summary>
    ChannelReceive = 0x86,

    /// <summary>关闭通道 (操作数: channelId)</summary>
    ChannelClose = 0x87,

    /// <summary>创建信号量 (操作数: initialCount, maxCount)</summary>
    SemaphoreCreate = 0x88,

    /// <summary>获取信号量 (操作数: semaphoreId)</summary>
    SemaphoreAcquire = 0x89,

    /// <summary>释放信号量 (操作数: semaphoreId)</summary>
    SemaphoreRelease = 0x8A,

    /// <summary>尝试非阻塞发送到通道 (操作数: timeoutMs, value, channelId) 返回bool</summary>
    ChannelTrySend = 0x8B,

    /// <summary>尝试非阻塞接收通道 (操作数: timeoutMs, channelId) 返回ChannelReceiveResult</summary>
    ChannelTryReceive = 0x8C,

    /// <summary>创建线程 (操作数: funcIndex, argCount) 返回threadId</summary>
    ThreadCreate = 0x8D,

    /// <summary>启动线程 (操作数: threadId)</summary>
    ThreadStart = 0x8E,

    /// <summary>等待线程完成 (操作数: threadId) 返回线程结果</summary>
    ThreadJoin = 0x8F,

    /// <summary>检查线程是否存活 (操作数: threadId) 返回bool</summary>
    ThreadIsAlive = 0x90,

    /// <summary>释放线程资源 (操作数: threadId)</summary>
    ThreadDispose = 0x91,

    // ===== 异步支持 (0xA0-0xAF) =====
    /// <summary>等待异步操作</summary>
    Await = 0xA0,

    /// <summary>生成器yield</summary>
    Yield = 0xA1,

    /// <summary>创建Task</summary>
    NewTask = 0xA2,

    /// <summary>调用异步函数 (操作数: argCount, funcName)</summary>
    CallAsync = 0xA3,

    /// <summary>异步生成器yield (在异步生成器中yield值)</summary>
    AwaitYield = 0xA4,

    /// <summary>创建异步生成器 (操作数: funcIndex)</summary>
    NewAsyncGenerator = 0xA5,

    /// <summary>调用异步生成器函数 (操作数: argCount, funcName)</summary>
    CallAsyncGenerator = 0xA6,

    // ===== 异常处理 (0xB0-0xBF) =====
    /// <summary>抛出异常</summary>
    Throw = 0xB0,

    /// <summary>开始try块 (操作数: catchOffset, finallyOffset)</summary>
    TryBegin = 0xB1,

    /// <summary>结束try块</summary>
    TryEnd = 0xB2,

    /// <summary>开始catch块</summary>
    CatchBegin = 0xB3,

    /// <summary>结束catch块</summary>
    CatchEnd = 0xB4,

    /// <summary>开始finally块</summary>
    FinallyBegin = 0xB5,

    /// <summary>结束finally块</summary>
    FinallyEnd = 0xB6,

    // ===== 特殊指令 (0xC0-0xFF) =====
    /// <summary>defer语句 (延迟执行)</summary>
    Defer = 0xC0,

    /// <summary>执行所有defer</summary>
    ExecuteDefers = 0xC1,

    /// <summary>加载 extern 函数 (操作数: dllNameIndex, funcNameIndex, externTypeIndex)</summary>
    LoadExtern = 0xC2,

    /// <summary>调用 extern 函数 (操作数: argCount, funcNameIndex)</summary>
    CallExtern = 0xC3,

    /// <summary>开始using块 (操作数: finallyOffset) - 类似TryBegin但专门用于using</summary>
    UsingBegin = 0xC4,

    /// <summary>结束using块</summary>
    UsingEnd = 0xC5,

    /// <summary>释放using资源 (从栈顶弹出资源并调用Dispose)</summary>
    DisposeResource = 0xC6,

    /// <summary>导入原生资源 (操作数: dllNameIndex, classNameIndex, mode, p1, p2)</summary>
    ImportNative = 0xC7,

    // ===== 模块操作 (0xD0-0xDF) =====
    /// <summary>加载模块 (操作数: moduleNameIndex)</summary>
    LoadModule = 0xD0,

    /// <summary>导入符号 (操作数: moduleNameIndex, symbolNameIndex)</summary>
    ImportSymbol = 0xD1,

    /// <summary>导入符号并重命名 (操作数: moduleNameIndex, symbolNameIndex, aliasIndex)</summary>
    ImportSymbolAs = 0xD2,

    /// <summary>导入所有符号 (操作数: moduleNameIndex)</summary>
    ImportAll = 0xD3,

    /// <summary>导出符号 (操作数: symbolNameIndex)</summary>
    ExportSymbol = 0xD4,

    /// <summary>获取模块符号 (操作数: moduleNameIndex, symbolNameIndex)</summary>
    GetModuleSymbol = 0xD5,

    /// <summary>打印调试信息 (操作数: messageIndex)</summary>
    DebugPrint = 0xF0,

    /// <summary>断点</summary>
    Breakpoint = 0xF1,
}
