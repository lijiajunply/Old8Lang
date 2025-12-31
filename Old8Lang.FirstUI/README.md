# Old8Lang.FirstUI

> Old8Lang 的现代化跨平台 GUI 标准库

## 简介

Old8Lang.FirstUI 是为 Old8Lang 语言设计的声明式 GUI 框架，基于 Avalonia UI，借鉴了 Flutter 和 SwiftUI 的设计理念。它让你能够用简洁直观的代码构建跨平台桌面应用程序。

## 特性

- 🎨 **声明式 UI**: 用数据驱动 UI，代码简洁易懂
- 🔄 **响应式**: 状态变化自动触发界面更新
- 🧩 **组件化**: 通过组合小部件构建复杂界面
- 🌐 **跨平台**: 支持 Windows、macOS、Linux
- ⚡ **高性能**: 基于 Avalonia UI 的高效渲染引擎
- 🎭 **主题系统**: 内置浅色/深色主题，支持自定义

## 快速开始

### Hello World

```old8
import "firstui" as ui

app <- ui.App()

MainView() -> {
    return ui.Column({
        children: {
            ui.Text("Hello, Old8Lang FirstUI!", {
                fontSize: 28,
                fontWeight: "bold"
            }),
            ui.Button("点击我", {
                onClick: () -> {
                    ui.ShowToast("你好，世界！")
                }
            })
        }
    })
}

app.Run(() -> MainView())
```

### 计数器应用

```old8
import "firstui" as ui

counter <- 0

CounterView() -> {
    return ui.VStack({
        ui.Text("计数器")
            .fontSize(24)
            .fontWeight("bold"),

        ui.Text(counter.ToStr())
            .fontSize(48)
            .color("#007AFF"),

        ui.HStack({
            ui.Button("-")
                .onClick(() -> { counter <- counter - 1 }),
            ui.Button("+")
                .onClick(() -> { counter <- counter + 1 })
        })
        .spacing(20)
    })
    .padding(40)
}

app <- ui.App()
app.Run(() -> CounterView())
```

## 核心概念

### 组件 (Widget)

FirstUI 的界面由组件构建。组件可以是简单的文本、按钮，也可以是复杂的布局容器。

**布局组件**:
- `Container`: 容器组件（支持内边距、外边距、背景色、边框、圆角）
- `Column` / `VStack`: 垂直布局
- `Row` / `HStack`: 水平布局
- `Stack` / `ZStack`: 层叠布局
- `ScrollView`: 滚动容器

**基础组件**:
- `Text`: 文本显示
- `Button`: 按钮（多种样式变体）
- `Image`: 图片（支持本地文件和URL）
- `TextInput`: 文本输入框（支持密码模式、多行）
- `Checkbox`: 复选框（支持三态）
- `RadioButton` & `RadioGroup`: 单选按钮

**高级组件**:
- `Card`: 卡片组件（Material Design 风格）
- `Dialog`: 对话框（支持多种类型）
- `Toast`: 消息提示
- `Tooltip`: 工具提示
- `TabView`: 选项卡视图
- `ListView`: 列表视图（支持虚拟化）
- `GridView`: 网格视图
- `Menu`: 菜单（支持子菜单）
- `Pagination`: 分页组件
- `Popover`: 弹出框
- `Breadcrumb`: 面包屑导航

### 状态管理

使用响应式状态管理，当状态变化时自动更新 UI：

```old8
// 局部状态
state <- ui.State(0)

// 更新状态
state.Set(state.Get() + 1)

// 可观察状态（自动触发 UI 更新）
observableState <- ui.ObservableState(0)
observableState.BindTo(myWidget)  // 绑定到组件

// 全局状态
globalCounter <- ui.GlobalState("counter", 0)
// 在任何地方访问
counter <- ui.GlobalState.Get("counter")

// 计算属性（依赖其他状态）
firstName <- ui.State("张")
lastName <- ui.State("三")
fullName <- ui.Computed(() -> {
    return firstName.Get() + lastName.Get()
}, firstName, lastName)
```

### 样式与主题

支持灵活的样式定制：

```old8
// 链式调用设置样式
ui.Text("标题")
    .fontSize(24)
    .fontWeight("bold")
    .color("#333333")
    .align("center")

// 使用配置字典
ui.Button("按钮", {
    backgroundColor: "#007AFF",
    textColor: "#FFFFFF",
    borderRadius: 8,
    padding: {top: 10, bottom: 10, left: 20, right: 20}
})

// 切换主题
ui.SetTheme("dark")  // 或 "light"
```

## 设计理念

### 声明式 vs 命令式

**传统命令式** (❌ 不推荐):
```csharp
var button = new Button();
button.Content = "点击我";
button.Click += (s, e) => { ShowMessage("被点击了"); };
panel.Children.Add(button);
```

**FirstUI 声明式** (✅ 推荐):
```old8
ui.Button("点击我", {
    onClick: () -> { ui.ShowToast("被点击了") }
})
```

### 组合优于继承

通过组合小组件构建复杂界面，而不是继承大型基类：

```old8
// 自定义组合组件
UserCard(name, avatar) -> {
    return ui.Card({
        child: ui.Row({
            children: {
                ui.Image(avatar, {width: 50, height: 50}),
                ui.Column({
                    children: {
                        ui.Text(name).fontWeight("bold"),
                        ui.Text("在线").color("#00AA00")
                    }
                })
            }
        })
    })
}

// 使用自定义组件
ui.Column({
    children: {
        UserCard("张三", "avatar1.png"),
        UserCard("李四", "avatar2.png")
    }
})
```

## 示例程序

查看 `Examples/` 目录了解更多示例：

- `HelloWorld.old8` - 最简单的入门示例
- `Counter.old8` - 计数器应用（状态管理）
- `TodoList.old8` - 待办事项列表（列表渲染）
- `Form.old8` - 表单验证示例
- `Theme.old8` - 主题切换示例
- `Navigation.old8` - 多页面导航

## 开发状态

🚀 **此项目目前处于活跃开发阶段**

完整的开发计划请查看 [TODO.md](./TODO.md)

### 已完成的功能

✅ **阶段一：项目搭建与基础架构**
- ✅ 项目结构创建（基于 Avalonia UI 11.2.2）
- ✅ 核心抽象层（WidgetBase、BuildContext）
- ✅ 工具类库（LayoutHelper、TypeConverter 等）

✅ **阶段二：基础组件库**
- ✅ 布局组件：Container, Row/HStack, Column/VStack, Stack/ZStack
- ✅ 基础控件：Text, Button, Image, TextInput
- ✅ 表单组件：Checkbox, RadioButton/RadioGroup

✅ **阶段三：高级组件（已完成）**
- ✅ 容器组件：ScrollView, Card, ListView, GridView
- ✅ 导航组件：TabView, Breadcrumb, Pagination, Menu
- ✅ 反馈组件：Dialog, Toast, Tooltip, Popover

✅ **阶段四：状态管理系统（已完成）**
- ✅ State 类：局部状态管理
- ✅ ObservableState 类：响应式状态（自动触发 UI 更新）
- ✅ GlobalState 类：全局状态管理
- ✅ Computed 类：计算属性（依赖派生）

### 开发中

🔨 **阶段四：动画系统**
- ⏳ 动画基类和过渡效果
- ⏳ 手势与交互

🔨 **阶段五：主题与样式系统**
- ⏳ 主题系统（Light/Dark）
- ⏳ 自定义样式支持

预计开发周期：**13周**
- 阶段一 (第1-2周): 项目搭建与基础架构 ✅
- 阶段二 (第3-4周): 基础组件库 ✅
- 阶段三 (第5-6周): 高级组件与交互 ✅
- 阶段四 (第7-8周): 状态管理与动画 ✅ (状态管理完成，动画待实现)
- 阶段五 (第9周): 主题与样式系统 ⏳
- 阶段六 (第10周): Old8Lang API 设计 ⏳
- 阶段七 (第11周): 示例与文档 ⏳
- 阶段八 (第12周): 测试与优化 ⏳
- 阶段九 (第13周): 发布准备 ⏳

## 技术栈

- **基础框架**: [Avalonia UI](https://avaloniaui.net/) 11.x
- **目标平台**: Windows 10/11, macOS 10.15+, Linux
- **语言支持**: Old8Lang (通过 C# 绑定层)
- **.NET 版本**: .NET 10.0

## 贡献

欢迎参与 Old8Lang.FirstUI 的开发！

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'feat: 添加某个很棒的特性'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 提交 Pull Request

## 参考资源

- [Flutter 文档](https://flutter.dev/docs) - 组件设计灵感来源
- [SwiftUI 文档](https://developer.apple.com/documentation/swiftui) - 声明式 API 设计参考
- [Avalonia 文档](https://docs.avaloniaui.net/) - 底层 UI 框架文档
- [Old8Lang 文档](../README.md) - Old8Lang 语言文档

## 许可证

与 Old8Lang 项目保持一致

---

*最后更新: 2026-01-01*
