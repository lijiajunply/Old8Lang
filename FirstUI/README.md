# Old8Lang.FirstUI

> 基于 Avalonia UI 的声明式 GUI 框架，专为 Old8Lang 语言设计

[![Status](https://img.shields.io/badge/Status-Alpha%20Ready-orange)](https://github.com/old8lang/firstui)
[![Framework](https://img.shields.io/badge/Framework-Avalonia%2011.x-blue)](https://avaloniaui.net/)
[![Language](https://img.shields.io/badge/Language-Old8Lang%20%2B%20C%23-purple)](https://old8lang.org/)

## 概述

Old8Lang.FirstUI 是一个现代化的跨平台 GUI 框架，采用声明式设计理念，借鉴 Flutter 和 SwiftUI 的优秀实践，让开发者能够用简洁直观的代码构建美观的用户界面。

### ✨ 核心特性

- 🎨 **声明式 UI** - 通过组合 Widget 构建界面，类似 Flutter/SwiftUI
- 🌈 **主题系统** - 内置 6 种主题，支持浅色/深色模式切换
- 🎭 **动画系统** - 30+ 缓动函数，流畅的过渡动画
- 👆 **手势支持** - 点击、拖动、滑动等自然交互
- 📱 **响应式** - 状态驱动，自动 UI 更新
- 🎯 **类型安全** - C# 层保证类型安全，减少运行时错误
- 🚀 **高性能** - 虚拟化列表，延迟加载，优化的渲染

## 📦 已实现功能

### ✅ 核心架构 (100%)
- [x] WidgetBase 抽象基类
- [x] BuildContext 构建上下文
- [x] StateManager 状态管理器
- [x] FirstUIBinding Old8Lang 绑定层

### ✅ 布局组件 (100%)
- [x] **Container** - 通用容器，支持边距、背景、边框、圆角
- [x] **Row** - 水平布局，支持主轴/交叉轴对齐
- [x] **Column** - 垂直布局，支持主轴/交叉轴对齐
- [x] **Stack** - 层叠布局，支持子组件堆叠定位
- [x] **Grid** - 网格布局，支持行列定义和跨行跨列

### ✅ 基础组件 (85%)
- [x] **Text** - 文本显示，支持字体、大小、颜色、粗细
- [x] **Button** - 按钮，支持文本、图标、点击事件
- [x] **Image** - 图片显示，支持本地路径和 URL
- [x] **Checkbox** - 复选框
- [x] **RadioButton** - 单选按钮
- [ ] **Icon** - 图标 (计划中)
- [ ] **Switch** - 开关 (计划中)
- [ ] **Slider** - 滑块 (计划中)
- [ ] **ProgressBar** - 进度条 (计划中)

### ✅ 输入组件 (33%)
- [x] **TextInput** - 文本输入框，支持占位符、密码模式
- [ ] **TextArea** - 多行文本输入
- [ ] **Select** - 下拉选择框
- [ ] **DatePicker** - 日期选择器
- [ ] **TimePicker** - 时间选择器
- [ ] **FilePicker** - 文件选择器

### ✅ 高级组件 (75%)
- [x] **ScrollView** - 滚动容器
- [x] **ListView** - 列表视图，支持虚拟化
- [x] **GridView** - 网格视图
- [x] **Card** - 卡片组件
- [x] **TabView** - 选项卡视图
- [x] **Dialog** - 对话框
- [x] **Toast** - 消息提示
- [x] **Menu** - 菜单
- [x] **Popover** - 弹出框
- [x] **Tooltip** - 工具提示
- [x] **Breadcrumb** - 面包屑导航
- [x] **Pagination** - 分页组件
- [ ] **Panel** - 面板 (计划中)

### ✅ 状态管理 (100%)
- [x] **State** - 组件局部状态
- [x] **ObservableState** - 响应式状态
- [x] **GlobalState** - 全局状态管理
- [x] **Computed** - 计算属性

### ✅ 动画系统 (100%)
- [x] **Animation** - 动画基类，支持时长、缓动、循环
- [x] **Easing** - 30+ 缓动函数库
- [x] **Transition** - 过渡动画（淡入淡出、滑动、缩放、旋转）
- [x] **AnimatedContainer** - 动画容器

### ✅ 手势交互 (100%)
- [x] **GestureDetector** - 手势检测（点击、双击、长按、拖动、滑动）
- [x] **Draggable** - 可拖动组件
- [x] **DropTarget** - 拖放目标
- [x] 完整的拖放事件回调

### ✅ 主题系统 (100%)
- [x] **ThemeManager** - 单例主题管理器
- [x] **内置主题**：
  - Light/Dark (iOS 风格)
  - Material/Material Dark (Material Design)
  - Fluent/Fluent Dark (Fluent Design)
- [x] **颜色方案** - Primary, Secondary, Background, Surface, Error
- [x] **字体方案** - H1-H6 标题，Body1, Body2, Button, Caption
- [x] **间距方案** - 统一的间距系统
- [x] **阴影方案** - 5 级阴影效果
- [x] **圆角方案** - 统一的圆角规范
- [x] **样式表** - 类似 CSS 的样式定义
- [x] **主题切换** - 运行时动态切换主题

### ⏳ Old8Lang API 设计 (进行中)
- [x] FirstUIBinding 基础方法
- [ ] 链式调用 API
- [ ] 字典参数配置
- [ ] Lambda 回调支持
- [ ] 组件别名 (VStack, HStack)

## 🚀 快速开始

### 环境要求

- .NET 10.0 或更高版本
- Old8Lang 解释器/编译器
- 支持 Windows 10/11, macOS, Linux

### 安装

1. 克隆项目：
```bash
git clone https://github.com/old8lang/firstui.git
cd Old8Lang.FirstUI
```

2. 构建项目：
```bash
dotnet build Old8Lang.FirstUI.csproj
```

3. 确保项目已添加到 Old8Lang.sln 解决方案中

### Hello World

创建 `HelloWorld.old8` 文件：

```old8
// 引入 FirstUI 库
import "firstui" as ui

// 创建应用
app <- ui.CreateApp()

// 定义主界面
MainView() -> {
    return ui.Column({
        children: {
            ui.Text("Hello, Old8Lang FirstUI!", {
                fontSize: 24,
                fontWeight: "bold",
                color: "#333333"
            }),
            
            ui.Container({
                padding: 20,
                margin: 10,
                backgroundColor: "#f0f0f0",
                borderRadius: 10,
                child: ui.Button("点击我", {
                    onClick: () -> {
                        ui.ShowToast("欢迎使用 Old8Lang FirstUI!")
                    }
                })
            })
        }
    })
}

// 运行应用
app.Run(() -> MainView())
```

运行应用：
```bash
old8lang -f HelloWorld.old8
```

### 计数器应用

```old8
import "firstui" as ui

// 创建可观察状态
counter <- ui.CreateObservableState(0)

CounterView() -> {
    return ui.Column({
        children: {
            ui.Text("计数器应用", {
                fontSize: 24,
                fontWeight: "bold"
            }),
            
            ui.Text(counter.Value.ToStr(), {
                fontSize: 48,
                color: "#007AFF"
            }),
            
            ui.Container({
                child: ui.Row({
                    children: {
                        ui.Button("减少", {
                            onClick: () -> {
                                counter.Value <- counter.Value - 1
                            }
                        }),
                        ui.Button("增加", {
                            onClick: () -> {
                                counter.Value <- counter.Value + 1
                            }
                        })
                    },
                    spacing: 20
                })
            })
        },
        spacing: 20
    })
}

app <- ui.CreateApp()
app.Run(() -> CounterView())
```

## 📖 API 文档

### 组件创建

所有组件都通过 `FirstUIBinding.CreateWidget` 方法创建：

```old8
// 基础语法
widget <- ui.CreateWidget("组件类型", 配置字典)

// 示例：创建文本
text <- ui.CreateWidget("Text", {
    content: "Hello World",
    fontSize: 16,
    color: "#333333"
})

// 示例：创建按钮
button <- ui.CreateWidget("Button", {
    label: "Click Me",
    onClick: () -> {
        PrintLine("Button clicked!")
    }
})
```

### 主题切换

```old8
// 切换到深色主题
ui.SetTheme("dark")

// 获取当前主题
current <- ui.GetCurrentTheme()

// 获取所有可用主题
themes <- ui.GetAvailableThemes()

// 快速切换浅色/深色
ui.ToggleTheme()
```

### 状态管理

```old8
// 创建可观察状态
counter <- ui.CreateObservableState(0)

// 监听状态变化
counter.Subscribe((value) -> {
    PrintLine("Counter changed: " + value.ToStr())
})

// 更新状态
counter.Value <- counter.Value + 1
```

### 动画使用

```old8
// 创建动画容器
animatedBox <- ui.CreateWidget("AnimatedContainer", {
    width: 100,
    height: 100,
    backgroundColor: "#007AFF",
    transition: ui.CreateTransition({
        type: "slide",
        direction: "right",
        duration: 300,
        easing: "easeOut"
    })
})

// 更新属性会自动触发动画
animatedBox.width <- 200
```

## 🎨 主题系统

### 内置主题

| 主题名称 | 描述 | 风格 |
|---------|------|------|
| `light` | 浅色主题 | iOS 风格 |
| `dark` | 深色主题 | iOS 风格 |
| `material` | Material Design 浅色 | Material Design |
| `material-dark` | Material Design 深色 | Material Design |
| `fluent` | Fluent Design 浅色 | Windows 11 |
| `fluent-dark` | Fluent Design 深色 | Windows 11 |

### 自定义主题

```old8
// 创建自定义主题
customTheme <- ui.CreateTheme({
    name: "custom",
    isDark: false,
    colors: {
        primary: "#FF6B6B",
        secondary: "#4ECDC4",
        background: "#FFFFFF",
        surface: "#F5F5F5"
    },
    typography: {
        h1: { size: 36, weight: "bold" },
        body: { size: 16, weight: "normal" }
    }
})

// 应用自定义主题
ui.SetTheme(customTheme)
```

## 🎭 动画效果

### 缓动函数

支持 30+ 缓动函数：
- `linear`, `easeIn`, `easeOut`, `easeInOut`
- `easeInQuad`, `easeOutQuad`, `easeInOutQuad`
- `easeInCubic`, `easeOutCubic`, `easeInOutCubic`
- `easeInElastic`, `easeOutElastic`, `easeInOutElastic`
- `easeInBounce`, `easeOutBounce`, `easeInOutBounce`
- `easeInSpring`, `easeOutSpring`, `easeInOutSpring`

### 过渡动画

```old8
// 淡入淡出
fadeTransition <- ui.CreateTransition({
    type: "fade",
    duration: 300,
    easing: "easeInOut"
})

// 滑动动画
slideTransition <- ui.CreateTransition({
    type: "slide",
    direction: "up",
    duration: 400,
    easing: "easeOutCubic"
})

// 缩放动画
scaleTransition <- ui.CreateTransition({
    type: "scale",
    scale: 1.2,
    duration: 250,
    easing: "easeOutBack"
})
```

## 👆 手势交互

### 手势检测

```old8
// 创建手势检测器
gestureArea <- ui.CreateGestureDetector({
    onTap: () -> {
        PrintLine("Tapped!")
    },
    onDoubleTap: () -> {
        PrintLine("Double tapped!")
    },
    onLongPress: () -> {
        PrintLine("Long pressed!")
    },
    onSwipe: (direction) -> {
        if direction == "left" {
            PrintLine("Swiped left!")
        }
    },
    onDrag: (delta) -> {
        PrintLine("Dragging: " + delta.x.ToStr() + ", " + delta.y.ToStr())
    }
})
```

### 拖放功能

```old8
// 可拖动组件
draggableItem <- ui.CreateDraggable({
    data: "item-data",
    axis: "both",  // "x", "y", "both"
    onDragStart: () -> {
        PrintLine("Drag started")
    },
    onDragEnd: () -> {
        PrintLine("Drag ended")
    }
})

// 拖放目标
dropTarget <- ui.CreateDropTarget({
    acceptedTypes: ["item-data"],
    onDrop: (data) -> {
        PrintLine("Dropped: " + data.ToStr())
    },
    onHover: () -> {
        PrintLine("Hovering over drop target")
    }
})
```

## 📁 项目结构

```
Old8Lang.FirstUI/
├── Core/                    # 核心抽象层
│   ├── WidgetBase.cs       # 组件基类
│   ├── BuildContext.cs     # 构建上下文
│   ├── StateManager.cs     # 状态管理器
│   └── GlobalState.cs      # 全局状态
├── Widgets/                # 组件实现
│   ├── Layout/             # 布局组件
│   ├── Basic/              # 基础组件
│   ├── Input/              # 输入组件
│   └── Advanced/           # 高级组件
├── State/                  # 状态管理
├── Animation/              # 动画系统
├── Gesture/                # 手势交互
├── Theme/                  # 主题系统
├── Utils/                  # 工具类
├── Examples/               # 示例代码
├── FirstUIBinding.cs       # Old8Lang 绑定层
└── README.md              # 项目文档
```

## 🧪 示例项目

查看 `Examples/` 目录中的示例：

- **HelloWorld.old8** - 基础应用示例
- **Counter.old8** - 计数器应用，展示状态管理
- **GestureExample.md** - 手势交互示例
- **ThemeExample.md** - 主题切换示例

## 🛠️ 开发指南

### 添加新组件

1. 继承 `WidgetBase` 基类
2. 实现 `Build(BuildContext)` 方法
3. 添加链式调用方法

### 调试技巧

- 使用 Old8Lang 的 `PrintLine()` 函数调试
- 在 C# 代码中使用 `Debug.WriteLine()`
- 检查 `BuildContext` 中的状态信息

## 🤝 贡献指南

我们欢迎所有形式的贡献！

### 如何贡献

1. Fork 项目
2. 创建功能分支 (`git checkout -b feature/amazing-feature`)
3. 提交更改 (`git commit -m 'Add amazing feature'`)
4. 推送到分支 (`git push origin feature/amazing-feature`)
5. 创建 Pull Request

### 开发规范

- 遵循 C# 命名规范
- 每个公共 API 必须有 XML 文档注释
- 使用 nullable reference types
- 编写单元测试

## 📋 路线图

### v0.1.0 (当前 Alpha)
- [x] 核心架构
- [x] 基础组件
- [x] 主题系统
- [x] 动画系统
- [x] 手势交互
- [ ] Old8Lang API 完善

### v0.2.0 (计划中)
- [ ] 更多输入组件 (Select, DatePicker, FilePicker)
- [ ] 图表组件 (集成 LiveCharts2)
- [ ] 热重载支持
- [ ] 可视化设计器

### v1.0.0 (长期)
- [ ] 移动端支持 (iOS/Android)
- [ ] 3D 渲染支持
- [ ] 插件系统
- [ ] WebAssembly 支持

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 🙏 致谢

- [Avalonia UI](https://avaloniaui.net/) - 优秀的跨平台 UI 框架
- [Flutter](https://flutter.dev/) - 声明式 UI 设计灵感
- [SwiftUI](https://developer.apple.com/documentation/swiftui) - 现代 UI 设计理念
- [Material Design](https://material.io/) - 设计规范参考
- [Fluent Design](https://www.microsoft.com/design/fluent/) - 设计规范参考

## 📞 联系我们

- **GitHub Issues**: [提交问题](https://github.com/old8lang/firstui/issues)
- **讨论区**: [GitHub Discussions](https://github.com/old8lang/firstui/discussions)
- **社区论坛**: [Old8Lang 社区](https://old8lang.org/community)

---

*Old8Lang.FirstUI - 让 GUI 开发变得简单而优雅 ✨*
