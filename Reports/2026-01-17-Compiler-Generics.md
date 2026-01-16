# 编译器模式泛型支持测试报告

**测试时间**: 2026-01-17
**测试类型**: 编译器模式泛型支持
**测试结果**: ✅ 通过

## 1. 测试概览

本次测试主要验证编译器模式下对泛型函数和泛型类的支持。

- **泛型函数**: 验证泛型函数的定义、显式类型参数调用。
- **泛型类**: 验证泛型类的定义、实例化、字段访问和方法调用。

## 2. 测试详细情况

### 2.1 泛型函数测试

**测试文件**: `CompilerTests/generic_function.old8` (已删除)

**测试代码**:
```old8
func identity<T>(value:T) -> T {
    return value
}

val:int <- identity<int>(100)
PrintLine(val)

val2:string <- identity<string>("Hello")
PrintLine(val2)
```

**运行结果**:
```
100
Hello
```

**遇到的问题及修复**:
- **标签错误**: `GenericMethodSpecializer` 未正确初始化 `ReturnLabel`。已修复。
- **类型推断**: 编译模式暂不支持泛型函数的隐式类型推断（如 `identity(100)`），需显式指定类型参数。已在文档中说明。

### 2.2 泛型类测试

**测试文件**: `CompilerTests/generic_class.old8` (已删除)

**测试代码**:
```old8
class Box<T> {
    val:T
    func init(v:T) {
        val <- v
    }
    func get() -> T {
        return val
    }
}

b:Box<int> <- Box<int>(123)
PrintLine(b.get())

s:Box<string> <- Box<string>("Hello")
PrintLine(s.get())
```

**运行结果**:
```
123
Hello
```

**遇到的问题及修复**:
- **NullReferenceException**: 由于 `LangId.OutputType` 无法解析自定义泛型类 `Box<int>`，导致类型被推断为 `object`。修复了 `LangId.OutputType` 以支持自定义泛型类查找。
- **字段访问失败**: `LangId.LoadIlValue` 和 `LangExpression.SetValueToIl` 缺少对类字段（`Ldfld`/`Stfld`）的支持，导致字段未正确读写。已添加相关 IL 生成逻辑。
- **语法错误**: 测试代码中使用了 `new` 关键字（如 `new Box<int>`），Old8Lang 不支持此语法。已修正为直接调用构造函数（如 `Box<int>()`）。

## 3. 结论

编译器模式现已支持泛型函数和泛型类的基本功能。
- 泛型函数支持显式类型参数调用。
- 泛型类支持实例化、字段读写和方法调用。
- 文档已更新以反映最新支持情况。
