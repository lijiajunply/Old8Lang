# IL 装饰器：包装闭包改为运行期转发 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「IL 装饰器无法调用被装饰函数」。

## 1. 现象

```old8
func decorator(f) {
    wrapper <- () -> {
        result <- f()
        return result + 10
    }
    return wrapper
}

@decorator
func getValue() -> int { return 32 }

PrintLine(getValue())
```

| 模式 | 结果 |
|---|---|
| 解释器 | 42 |
| IL | `NAME_ERROR 名称 'f' 未定义` |
| 虚拟机 | 42 |

`Compiler/Functions/DecoratorCompilerTests` 与 `DecoratorInterpreterTests`（二者实际都是 IL 编译路径）
大面积失败。

## 2. 根因

IL 的 `VisitFuncInit` 在应用装饰器后，拿到的是解释器构造的 wrapper **闭包**
（`f` 存在于解释器闭包环境里，不在 IL 当前作用域）。直接编译 wrapper 的函数体时，
`f` 既不在 LocalVar、ArgumentIndices，也不在 IL 闭包捕获里，于是落到未知名字兜底 →
`InvokeInstance("f")` → 解释器里没有名为 f 的全局函数 → NAME_ERROR。

## 3. 修复

装饰器函数的函数体不再直接编译，而是生成一个**转发委托**：

- 签名沿用原函数的参数类型与返回类型；
- 委托体把参数打包成 `object[]`，调用 `IlRuntimeFunctions.InvokeByName(methodName, args)`；
- 解释器在编译前已执行声明（含装饰器应用），管理器里的同名函数就是装饰后的 wrapper，
  因此运行期调用与解释器完全一致；
- 按原有的 `name$参数类型` 规则注册到 `DelegateVar` / `FuncParameters`，调用点解析不变。

同时给 `IlRuntimeFunctions` 增加 `InvokeByName`（按名字 + 参数个数在 `ImportInfos`
里查找并执行，找不到抛 `NAME_ERROR`）。

## 4. 验证

### 4.1 CLI 对比

| 表达式 | 解释器 | IL | 虚拟机 |
|---|---|---|---|
| 上面的 `getValue()` 装饰器例子 | 42 | 42（修复前 NAME_ERROR） | 42 |

### 4.2 分组变化

`Compiler.Functions` 45 → 35 失败（无参装饰器、装饰器改结果/缓存/多函数等一批转通过）。
剩余装饰器失败集中在两类：wrapper 的 lambda 参数未写类型注解（IL 要求完整注解）、
以及旧 lambda/箭头语法，属用例本身需要迁移。

## 5. 遗留

- 装饰器转发委托只支持同步函数；异步函数装饰器仍不可用（MODE_SUPPORT 已记录）。
- 多装饰器/带参装饰器用例里仍有未标注 lambda 参数，需补 `:类型` 后才能验证 IL 语义。
