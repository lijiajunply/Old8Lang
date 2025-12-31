# Old8Lang.FirstUI 架构设计文档

## 目录
- [概述](#概述)
- [整体架构](#整体架构)
- [核心层](#核心层)
- [组件层](#组件层)
- [绑定层](#绑定层)
- [状态管理](#状态管理)
- [事件处理](#事件处理)
- [主题系统](#主题系统)
- [设计模式](#设计模式)

---

## 概述

Old8Lang.FirstUI 是一个基于 **Avalonia UI** 的声明式 GUI 框架，专为 Old8Lang 语言设计。它的架构分为三个主要层次：

```
┌─────────────────────────────────────────┐
│        Old8Lang 应用代码层              │
│  (使用 firstui.old8 库编写 GUI 代码)     │
└────────────────┬────────────────────────┘
                 │
┌────────────────▼────────────────────────┐
│        Old8Lang 绑定层                   │
│  (FirstUIBinding.cs - 类型转换/桥接)     │
└────────────────┬────────────────────────┘
                 │
┌────────────────▼────────────────────────┐
│        FirstUI 核心层                    │
│  (WidgetBase, StateManager, Context)    │
└────────────────┬────────────────────────┘
                 │
┌────────────────▼────────────────────────┐
│        Avalonia UI 框架层                │
│  (底层 GUI 框架 - Control, Visual)       │
└─────────────────────────────────────────┘
```

---

## 整体架构

### 设计原则

1. **分层隔离**: 每层职责清晰，上层依赖下层，下层不感知上层
2. **声明式优先**: UI 由数据定义，而非命令式构建
3. **类型安全**: 尽可能在 C# 层保证类型安全，减少运行时错误
4. **性能优化**: 最小化跨语言边界调用，延迟加载组件

### 架构图

```
┌──────────────────────────────────────────────────────────┐
│                   Old8Lang 应用层                         │
│  ┌─────────────┐  ┌──────────────┐  ┌─────────────┐     │
│  │  Counter    │  │   TodoList   │  │   Form      │     │
│  │  .old8      │  │   .old8      │  │   .old8     │     │
│  └─────────────┘  └──────────────┘  └─────────────┘     │
└────────────────────────────┬─────────────────────────────┘
                             │ 调用
┌────────────────────────────▼─────────────────────────────┐
│                   FirstUI 绑定层                          │
│  ┌─────────────────────────────────────────────────┐     │
│  │  FirstUIBinding.cs                              │     │
│  │  • CreateApp()     - 创建应用                    │     │
│  │  • CreateWidget()  - 创建组件                    │     │
│  │  • ShowToast()     - 显示消息                    │     │
│  │  • SetTheme()      - 切换主题                    │     │
│  └─────────────────────────────────────────────────┘     │
│  ┌─────────────────────────────────────────────────┐     │
│  │  Utils/TypeConverter.cs                         │     │
│  │  • Old8Lang Dict → C# Config                    │     │
│  │  • Old8Lang Func → C# Delegate                  │     │
│  └─────────────────────────────────────────────────┘     │
└────────────────────────────┬─────────────────────────────┘
                             │ 构建
┌────────────────────────────▼─────────────────────────────┐
│                   FirstUI 核心层                          │
│  ┌───────────────────┐  ┌────────────────────┐          │
│  │  Core/            │  │  State/            │          │
│  │  • WidgetBase     │  │  • StateManager    │          │
│  │  • BuildContext   │  │  • ObservableState │          │
│  │  • WidgetTree     │  │  • GlobalState     │          │
│  └───────────────────┘  └────────────────────┘          │
│  ┌───────────────────┐  ┌────────────────────┐          │
│  │  Theme/           │  │  Animation/        │          │
│  │  • Theme          │  │  • Animation       │          │
│  │  • ColorScheme    │  │  • Transition      │          │
│  └───────────────────┘  └────────────────────┘          │
└────────────────────────────┬─────────────────────────────┘
                             │ 使用
┌────────────────────────────▼─────────────────────────────┐
│                   组件层                                  │
│  ┌───────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │  Layout/      │  │  Basic/      │  │  Input/      │  │
│  │  • Container  │  │  • Text      │  │  • TextInput │  │
│  │  • Row        │  │  • Button    │  │  • Checkbox  │  │
│  │  • Column     │  │  • Image     │  │  • Select    │  │
│  │  • Stack      │  │  • Icon      │  │  ...         │  │
│  │  • Grid       │  │  ...         │  │              │  │
│  └───────────────┘  └──────────────┘  └──────────────┘  │
│  ┌───────────────────────────────────────────────────┐  │
│  │  Advanced/                                         │  │
│  │  • Dialog   • ListView   • TabView   • Menu      │  │
│  └───────────────────────────────────────────────────┘  │
└────────────────────────────┬─────────────────────────────┘
                             │ 渲染
┌────────────────────────────▼─────────────────────────────┐
│                   Avalonia UI 框架层                      │
│  • Control   • Visual   • Layout   • Rendering          │
└──────────────────────────────────────────────────────────┘
```

---

## 核心层

### WidgetBase

所有组件的抽象基类，定义了组件的通用属性和行为。

**职责**:
- 维护组件基本属性（宽高、边距、颜色等）
- 提供 `Build()` 抽象方法供子类实现
- 支持链式调用（Fluent API）

**关键方法**:
```csharp
public abstract class WidgetBase
{
    public abstract object Build(BuildContext context);
    public virtual void Update();

    // 链式调用
    public WidgetBase SetWidth(double width);
    public WidgetBase SetHeight(double height);
    public WidgetBase SetPadding(double padding);
    // ...
}
```

**设计考量**:
- 返回 `object` 而非 `Control` 是为了支持未来可能的多后端（Avalonia、WinUI 等）
- 链式调用模式方便 Old8Lang 代码编写（类似 SwiftUI）

### BuildContext

构建上下文，携带构建时的环境信息。

**职责**:
- 提供父组件引用（用于继承样式）
- 管理主题配置
- 提供状态管理器访问
- 存储全局状态

**使用场景**:
```csharp
public object Build(BuildContext context)
{
    var theme = context.Theme;
    var stateManager = context.StateManager;
    // ...
}
```

### WidgetTree

组件树管理器（待实现）。

**职责**:
- 维护组件的层次结构
- 处理组件的更新和重建
- 优化渲染性能（diff 算法）

---

## 组件层

组件分为四类：

### 1. Layout 组件（布局）

**Container**: 通用容器，支持边距、内边距、背景、边框等。

```csharp
public class Container : WidgetBase
{
    public WidgetBase? Child { get; set; }
    public double BorderRadius { get; set; }
    public string? BorderColor { get; set; }
    // ...
}
```

**Row / Column**: 水平/垂直线性布局。

```csharp
public class Row : WidgetBase
{
    public List<WidgetBase> Children { get; set; }
    public HorizontalAlignment MainAxisAlignment { get; set; }
    public VerticalAlignment CrossAxisAlignment { get; set; }
    public double Spacing { get; set; }
}
```

### 2. Basic 组件（基础）

**Text**: 文本显示。

```csharp
public class Text : WidgetBase
{
    public string Content { get; set; }
    public double FontSize { get; set; }
    public string FontWeight { get; set; }
    public string Color { get; set; }
    // ...
}
```

**Button**: 按钮。

```csharp
public class Button : WidgetBase
{
    public string Label { get; set; }
    public Action? OnClick { get; set; }
    public string? Icon { get; set; }
    // ...
}
```

### 3. Input 组件（输入）

**TextInput**: 文本输入框。

```csharp
public class TextInput : WidgetBase
{
    public string Placeholder { get; set; }
    public string Value { get; set; }
    public Action<string>? OnChanged { get; set; }
    public bool IsPassword { get; set; }
    // ...
}
```

### 4. Advanced 组件（高级）

**Dialog**: 对话框。
**ListView**: 列表视图（虚拟化）。
**TabView**: 选项卡视图。

---

## 绑定层

### FirstUIBinding

提供给 Old8Lang 调用的入口点。

**核心方法**:

```csharp
public static class FirstUIBinding
{
    // 创建应用
    public static object CreateApp();

    // 创建组件（从 Old8Lang 字典配置）
    public static object CreateWidget(string widgetType, object? config);

    // 显示 Toast
    public static void ShowToast(string message, int duration);

    // 显示对话框
    public static object ShowDialog(string title, string content);

    // 切换主题
    public static void SetTheme(string themeName);
}
```

### TypeConverter

类型转换工具。

**职责**:
- Old8Lang 字典 → C# 配置对象
- Old8Lang 函数 → C# 委托
- Old8Lang 列表 → C# List

**示例**:
```csharp
public static class TypeConverter
{
    // 将 Old8Lang 字典转换为 C# Dictionary
    public static Dictionary<string, object> ToDictionary(object old8Dict);

    // 将 Old8Lang 函数包装为 C# Action
    public static Action WrapAction(object old8Func);

    // 将 Old8Lang 函数包装为 C# Action<T>
    public static Action<T> WrapAction<T>(object old8Func);
}
```

### CallbackBridge

回调函数桥接器。

**问题**: Old8Lang lambda 传递给 C# 后的生命周期管理。

**解决方案**:
- 维护回调函数的引用表
- 在组件销毁时自动清理
- 使用弱引用避免内存泄漏

---

## 状态管理

### StateManager

简单的键值对状态管理器。

**特性**:
- 支持状态监听（Watch）
- 状态变化自动通知
- 线程安全（TODO）

**使用示例**:
```csharp
var manager = new StateManager();

// 设置状态
manager.SetState("counter", 0);

// 监听状态变化
manager.Listen("counter", () =>
{
    Console.WriteLine("Counter changed!");
});

// 获取状态
var counter = manager.GetState<int>("counter");
```

### ObservableState\<T>

泛型响应式状态。

**特性**:
- 类型安全
- 自动通知订阅者
- 支持多个订阅者

**使用示例**:
```csharp
var counter = new ObservableState<int>(0);

counter.Subscribe(value =>
{
    Console.WriteLine($"Counter: {value}");
});

counter.Value = 10; // 自动触发订阅者
```

### GlobalState

全局状态（待实现）。

**设计思路**: 类似 Flutter Provider 或 SwiftUI EnvironmentObject。

---

## 事件处理

### 事件流程

```
Old8Lang 代码定义回调
       ↓
通过 FirstUIBinding 传递给 C#
       ↓
TypeConverter 包装为 C# Action/Func
       ↓
注册到 Avalonia Control 事件
       ↓
事件触发时调用 Action
       ↓
在 UI 线程执行回调
```

### 线程安全

**问题**: Old8Lang 可能在非 UI 线程调用 API。

**解决方案**:
```csharp
public static void ShowToast(string message, int duration)
{
    Dispatcher.UIThread.Post(() =>
    {
        // 在 UI 线程执行
        InternalShowToast(message, duration);
    });
}
```

---

## 主题系统

### 主题结构

```csharp
public class ThemeData
{
    public ColorScheme Colors { get; set; }
    public TypographyScheme Typography { get; set; }
    public SpacingScheme Spacing { get; set; }
}
```

### 主题切换

```csharp
// 内置主题
var lightTheme = Theme.Light();
var darkTheme = Theme.Dark();

// 应用主题
App.SetTheme(darkTheme);

// 组件访问主题
var primaryColor = context.Theme.Colors.Primary;
```

### 响应式主题

当主题切换时，所有组件自动重建以应用新样式。

---

## 设计模式

### 1. 构建者模式（Builder Pattern）

用于组件的链式构建：

```old8
ui.Text("标题")
    .fontSize(24)
    .fontWeight("bold")
    .color("#333")
```

### 2. 观察者模式（Observer Pattern）

用于状态管理和事件处理：

```csharp
state.Subscribe(value => UpdateUI());
```

### 3. 工厂模式（Factory Pattern）

用于组件创建：

```csharp
FirstUIBinding.CreateWidget("Text", config);
```

### 4. 组合模式（Composite Pattern）

用于组件树的构建：

```old8
ui.Column({
    children: {
        ui.Text("标题"),
        ui.Row({
            children: { ... }
        })
    }
})
```

### 5. 桥接模式（Bridge Pattern）

用于 Old8Lang 和 C# 之间的桥接：

```
Old8Lang ←→ TypeConverter ←→ C# 对象
```

---

## 性能优化策略

### 1. 延迟加载

组件在首次访问时才创建 Avalonia Control。

### 2. 虚拟化

长列表使用虚拟化技术，只渲染可见项。

### 3. Diff 算法

组件更新时，对比新旧组件树，只更新变化的部分。

### 4. 缓存

缓存已创建的组件实例，避免重复构建。

### 5. 批量更新

多个状态变化合并为一次 UI 更新。

---

## 扩展性设计

### 自定义组件

开发者可以继承 `WidgetBase` 创建自定义组件：

```csharp
public class MyCustomWidget : WidgetBase
{
    public override object Build(BuildContext context)
    {
        // 返回 Avalonia Control
        return new MyAvaloniaControl();
    }
}
```

### 插件系统（未来）

支持动态加载第三方组件库。

### 多后端支持（未来）

抽象出 UI 后端接口，支持 Avalonia、WinUI、GTK 等。

---

## 技术债务与改进方向

### 当前限制

1. **性能**: 跨语言调用有开销
2. **类型安全**: Old8Lang 动态类型可能导致运行时错误
3. **调试困难**: 跨语言调试不便
4. **文档不足**: 需要更多示例和教程

### 改进方向

1. **代码生成**: 自动生成 Old8Lang 绑定代码
2. **类型推断**: 在 Old8Lang 层添加类型检查
3. **Hot Reload**: 支持代码热更新
4. **可视化工具**: 提供 GUI 设计器

---

## 参考资源

- [Avalonia 架构文档](https://docs.avaloniaui.net/docs/concepts)
- [Flutter 架构](https://flutter.dev/docs/resources/architectural-overview)
- [React 架构](https://reactjs.org/docs/design-principles.html)
- [SwiftUI 架构](https://developer.apple.com/documentation/swiftui)

---

*最后更新: 2025-12-31*
