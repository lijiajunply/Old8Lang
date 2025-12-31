# Old8Lang.FirstUI 开发计划

> 基于 Avalonia UI 框架，借鉴 Flutter/SwiftUI 的声明式 UI 设计理念

## 项目概述

### 目标
为 Old8Lang 创建一个现代化的跨平台 GUI 标准库，使用声明式 API 设计，让开发者能够用简洁直观的代码构建用户界面。

### 技术选型
- **基础框架**: Avalonia UI 11.x (支持 Windows, macOS, Linux)
- **设计理念**: 借鉴 Flutter 的 Widget 组合模式和 SwiftUI 的声明式语法
- **语言集成**: 通过 Old8Lang 的 Native 绑定机制暴露 C# API

### 核心设计原则
1. **声明式**: UI 由数据状态驱动，而非命令式构建
2. **组合优于继承**: 通过组合小部件构建复杂界面
3. **响应式**: 状态变化自动触发 UI 更新
4. **简洁性**: 提供符合 Old8Lang 语言风格的简洁 API

---

## 阶段一：项目搭建与基础架构 (第1-2周)

### 1.1 创建项目结构
- [ ] 创建 `Old8Lang.FirstUI` C# 类库项目
- [ ] 配置项目文件 (.csproj)
  - 添加 Avalonia.Desktop NuGet 包 (11.x)
  - 添加 Avalonia.Themes.Fluent
  - 引用 Old8Lang 核心项目
- [ ] 更新 Old8Lang.sln 解决方案文件
- [ ] 设置项目输出目录配置

### 1.2 核心抽象层设计
- [ ] 设计 `WidgetBase` 抽象基类
  - 封装 Avalonia Control 的通用属性
  - 提供统一的构建接口
- [ ] 设计 `BuildContext` 上下文类
  - 管理构建时的状态和环境信息
- [ ] 设计 `StateManager` 状态管理器
  - 实现响应式状态绑定机制
  - 提供状态变化通知
- [ ] 设计 `WidgetTree` 组件树管理
  - 管理组件层次结构
  - 处理组件更新和重建

### 1.3 Old8Lang 绑定层
- [ ] 创建 `FirstUIBinding` 静态类
  - 提供给 Old8Lang 调用的公共 API
- [ ] 实现类型转换辅助函数
  - Old8Lang 对象 ↔ C# 对象映射
  - 处理字典、列表等集合类型
- [ ] 设计回调函数桥接机制
  - Old8Lang 函数 → C# 委托转换

---

## 阶段二：基础组件库 (第3-4周)

### 2.1 布局组件
- [x] `Container` - 容器组件
  - 支持: 宽度、高度、内边距、外边距、背景色、边框、圆角
- [x] `Row` - 水平布局
  - 支持: 主轴对齐、交叉轴对齐、间距
- [x] `Column` - 垂直布局
  - 支持: 主轴对齐、交叉轴对齐、间距
- [x] `Stack` - 层叠布局
  - 支持: 子组件堆叠和定位
- [x] `Grid` - 网格布局
  - 支持: 行列定义、跨行跨列
- [ ] `Wrap` - 流式布局
  - 支持: 自动换行/换列

### 2.2 基础控件
- [x] `Text` - 文本显示
  - 支持: 字体、大小、颜色、粗细、对齐、行高
- [x] `Button` - 按钮
  - 支持: 文本、图标、点击事件、样式变体
- [x] `Image` - 图片显示
  - 支持: 本地路径、URL、适应模式
- [ ] `Icon` - 图标
  - 支持: Material Icons / Font Awesome 集成
- [x] `Checkbox` - 复选框
- [x] `RadioButton` - 单选按钮
- [ ] `Switch` - 开关
- [ ] `Slider` - 滑块
- [ ] `ProgressBar` - 进度条

### 2.3 输入组件
- [x] `TextInput` - 文本输入框
  - 支持: 占位符、密码模式、多行、验证
- [ ] `TextArea` - 多行文本输入
- [ ] `Select` - 下拉选择框
- [ ] `DatePicker` - 日期选择器
- [ ] `TimePicker` - 时间选择器
- [ ] `FilePicker` - 文件选择器

---

## 阶段三：高级组件与交互 (第5-6周) ✅

### 3.1 容器组件 ✅
- [x] `ScrollView` - 滚动容器
  - 支持: 垂直/水平滚动、滚动条样式
- [x] `ListView` - 列表视图
  - 支持: 虚拟化、动态数据绑定
- [x] `GridView` - 网格视图
- [x] `Card` - 卡片组件
  - 支持: 阴影、圆角、elevation
- [ ] `Panel` - 面板
  - 支持: 标题栏、边框

### 3.2 导航组件 ✅
- [x] `TabView` - 选项卡视图
- [ ] `NavigationView` - 导航视图 (侧边栏导航)
- [x] `Breadcrumb` - 面包屑导航
- [x] `Pagination` - 分页组件

### 3.3 反馈组件 ✅
- [x] `Dialog` - 对话框
  - 支持: 确认框、提示框、自定义内容
- [x] `Toast` - 消息提示
- [x] `Tooltip` - 工具提示
- [x] `Menu` - 菜单
  - 支持: 上下文菜单、菜单栏
- [x] `Popover` - 弹出框

### 3.4 数据展示 ⏳
- [ ] `Table` - 表格
  - 支持: 排序、筛选、分页
- [ ] `Tree` - 树形控件
- [ ] `Chart` - 图表 (考虑集成 LiveCharts2)
  - 折线图、柱状图、饼图

---

## 阶段四：状态管理与动画 (第7-8周) ✅

### 4.1 状态管理 ✅
- [x] 实现 `State` 类
  - 管理组件局部状态
- [x] 实现 `ObservableState` 可观察状态
  - 自动触发 UI 更新
- [x] 实现 `GlobalState` 全局状态管理
  - 类似 Flutter Provider / SwiftUI EnvironmentObject
- [x] 实现 `Computed` 计算属性
  - 基于其他状态派生新状态

### 4.2 动画系统 ⏳
- [ ] `Animation` 动画基类
  - 支持: 时长、缓动函数、循环
- [ ] `Transition` 过渡动画
  - 淡入淡出、滑动、缩放
- [ ] `AnimatedContainer` 动画容器
  - 属性变化自动过渡
- [ ] 预设动画效果库
  - 常用动画曲线 (easeIn, easeOut, spring 等)

### 4.3 手势与交互 ⏳
- [ ] `GestureDetector` 手势检测
  - 支持: 点击、双击、长按、拖动、滑动
- [ ] `Draggable` 可拖动组件
- [ ] `DropTarget` 拖放目标

---

## 阶段五：主题与样式系统 (第9周)

### 5.1 主题系统
- [ ] `Theme` 主题类
  - 颜色方案、字体、间距规范
- [ ] 内置主题
  - Light Theme (浅色主题)
  - Dark Theme (深色主题)
- [ ] 自定义主题支持
  - 允许用户定义完整主题

### 5.2 样式系统
- [ ] `StyleSheet` 样式表
  - 类似 CSS 的样式定义
- [ ] 样式继承与覆盖
- [ ] 响应式样式
  - 根据窗口大小调整布局

### 5.3 Material Design / Fluent Design
- [ ] 提供 Material Design 风格组件变体
- [ ] 提供 Fluent Design 风格组件变体
- [ ] 样式切换机制

---

## 阶段六：Old8Lang API 设计 (第10周)

### 6.1 声明式语法设计

**目标**: 让 Old8Lang 代码看起来像 Flutter/SwiftUI

#### Flutter 风格示例 (Old8Lang 实现)
```old8
// 引入 FirstUI 库
import "firstui" as ui

// 创建应用
app <- ui.App()

// 定义主界面
MainView() -> {
    return ui.Column({
        children: {
            ui.Text("欢迎使用 Old8Lang FirstUI", {
                fontSize: 24,
                fontWeight: "bold",
                color: "#333333"
            }),

            ui.Container({
                padding: 20,
                margin: 10,
                backgroundColor: "#f0f0f0",
                borderRadius: 10,
                child: ui.Row({
                    children: {
                        ui.Button("点击我", {
                            onClick: () -> {
                                ui.ShowToast("按钮被点击了!")
                            }
                        }),
                        ui.Spacer(width: 10),
                        ui.TextInput({
                            placeholder: "输入内容...",
                            onChanged: (text) -> {
                                PrintLine("输入: " + text)
                            }
                        })
                    }
                })
            })
        }
    })
}

// 运行应用
app.Run(() -> MainView())
```

#### SwiftUI 风格示例 (Old8Lang 实现)
```old8
import "firstui" as ui

app <- ui.App()

// 使用构建器风格
MainView() -> {
    return ui.VStack({
        ui.Text("计数器应用")
            .fontSize(28)
            .fontWeight("bold"),

        ui.Text(counter.ToStr())
            .fontSize(48)
            .color("#007AFF"),

        ui.HStack({
            ui.Button("减少")
                .onClick(() -> { counter <- counter - 1 }),

            ui.Button("增加")
                .onClick(() -> { counter <- counter + 1 })
        })
        .spacing(20)
    })
    .padding(40)
}

counter <- 0
app.Run(() -> MainView())
```

### 6.2 API 设计要点
- [ ] 设计链式调用 API
  - 每个组件方法返回自身，支持 `.method()` 风格
- [ ] 支持字典参数配置
  - 使用 Old8Lang 字典传递配置项
- [ ] 支持 Lambda 回调
  - 事件处理使用 Old8Lang lambda 表达式
- [ ] 设计组件别名
  - `VStack` = `Column`, `HStack` = `Row` (兼容 SwiftUI 命名)

### 6.3 绑定实现
- [ ] 实现 `FirstUIBinding.CreateWidget` 方法
- [ ] 实现配置字典解析
- [ ] 实现 Old8Lang 回调函数包装器
- [ ] 实现组件属性的链式设置方法

---

## 阶段七：示例与文档 (第11周)

### 7.1 示例程序
- [ ] Hello World 示例
- [ ] 计数器应用 (Counter App)
- [ ] 待办事项列表 (Todo List)
- [ ] 表单验证示例
- [ ] 数据表格示例
- [ ] 图表展示示例
- [ ] 主题切换示例
- [ ] 多页面导航示例

### 7.2 文档编写
- [ ] API 参考文档
  - 每个组件的完整 API 说明
  - 参数列表、类型说明
- [ ] 快速入门教程
  - 5分钟搭建第一个 GUI 应用
- [ ] 完整教程
  - 从零构建完整应用
- [ ] 最佳实践指南
  - 性能优化建议
  - 常见问题与解决方案

### 7.3 代码组织
- [ ] 在 Old8LangLib 项目中添加 `OldLib/firstui.old8` 库文件
  - 提供 Old8Lang 层的便捷封装
  - 实现常用组合组件
- [ ] 创建 `Examples/FirstUIExamples/` 目录
  - 存放所有示例代码

---

## 阶段八：测试与优化 (第12周)

### 8.1 单元测试
- [ ] 为核心类编写单元测试
  - WidgetBase, StateManager, BuildContext
- [ ] 为布局算法编写测试
- [ ] 为绑定层编写测试

### 8.2 集成测试
- [ ] 运行示例应用进行集成测试
- [ ] 测试不同平台兼容性
  - Windows 10/11
  - macOS
  - Linux (Ubuntu/Debian)

### 8.3 性能优化
- [ ] 组件渲染性能分析
- [ ] 减少不必要的重建
- [ ] 优化大列表虚拟化
- [ ] 内存占用优化

### 8.4 用户测试
- [ ] 邀请 Old8Lang 用户试用
- [ ] 收集反馈并改进
- [ ] 修复 Bug 和完善功能

---

## 阶段九：发布准备 (第13周)

### 9.1 打包与分发
- [ ] 配置 NuGet 包发布
- [ ] 编写 README.md
- [ ] 编写 CHANGELOG.md
- [ ] 准备发布说明

### 9.2 版本发布
- [ ] 发布 v0.1.0-alpha 版本
- [ ] 在 Old8Lang 社区宣传
- [ ] 收集早期用户反馈

### 9.3 持续改进
- [ ] 根据反馈迭代改进
- [ ] 添加社区需求的新组件
- [ ] 性能和稳定性持续优化

---

## 技术难点与解决方案

### 难点 1: Old8Lang 与 C# 类型系统桥接
**挑战**: Old8Lang 是动态类型语言，C# 是静态类型语言
**解决方案**:
- 使用 `object` 类型作为通用接口
- 实现运行时类型检查与转换
- 提供详细的错误提示

### 难点 2: 回调函数的生命周期管理
**挑战**: Old8Lang lambda 传递给 C# 委托时的生命周期
**解决方案**:
- 使用弱引用避免循环引用
- 在组件销毁时正确清理回调
- 实现引用计数或 GC 协作机制

### 难点 3: 声明式 UI 的状态同步
**挑战**: 状态变化如何触发 UI 更新
**解决方案**:
- 实现观察者模式的状态管理
- 使用 Avalonia 的数据绑定机制
- 提供 `setState()` 类似的 API 触发重建

### 难点 4: 跨线程 UI 更新
**挑战**: Old8Lang 可能在非 UI 线程调用 API
**解决方案**:
- 所有 UI 操作强制分发到 UI 线程
- 使用 `Dispatcher.UIThread.Post()` 包装
- 提供异步 API 变体

---

## 项目文件结构

```
Old8Lang.FirstUI/
├── Old8Lang.FirstUI.csproj       # 项目文件
├── FirstUIBinding.cs              # Old8Lang 绑定入口
├── Core/                          # 核心抽象层
│   ├── WidgetBase.cs
│   ├── BuildContext.cs
│   ├── StateManager.cs
│   └── WidgetTree.cs
├── Widgets/                       # 组件实现
│   ├── Layout/                    # 布局组件
│   │   ├── Container.cs
│   │   ├── Row.cs
│   │   ├── Column.cs
│   │   ├── Stack.cs
│   │   ├── Grid.cs
│   │   └── Wrap.cs
│   ├── Basic/                     # 基础组件
│   │   ├── Text.cs
│   │   ├── Button.cs
│   │   ├── Image.cs
│   │   └── Icon.cs
│   ├── Input/                     # 输入组件
│   │   ├── TextInput.cs
│   │   ├── Checkbox.cs
│   │   └── Select.cs
│   └── Advanced/                  # 高级组件
│       ├── Dialog.cs
│       ├── ListView.cs
│       └── TabView.cs
├── State/                         # 状态管理
│   ├── State.cs
│   ├── ObservableState.cs
│   └── GlobalState.cs
├── Animation/                     # 动画系统
│   ├── Animation.cs
│   ├── Transition.cs
│   └── AnimatedContainer.cs
├── Theme/                         # 主题系统
│   ├── Theme.cs
│   ├── ColorScheme.cs
│   └── StyleSheet.cs
├── Utils/                         # 工具类
│   ├── TypeConverter.cs
│   ├── CallbackBridge.cs
│   └── LayoutHelper.cs
└── Examples/                      # 示例代码
    ├── HelloWorld.old8
    ├── Counter.old8
    └── TodoList.old8
```

---

## 依赖与版本要求

### .NET 依赖
- .NET 10.0
- Avalonia 11.x
- Avalonia.Desktop
- Avalonia.Themes.Fluent

### 可选依赖
- LiveCharts2 (图表支持)
- Avalonia.Controls.DataGrid (高级表格)
- Material.Avalonia (Material Design 主题)

### Old8Lang 依赖
- Old8Lang.csproj (核心语言库)
- Old8LangLib.csproj (标准库，可选集成)

---

## 开发规范

### 代码风格
- 遵循 C# 命名规范 (PascalCase for public, camelCase for private)
- 每个公共 API 必须有 XML 文档注释
- 使用 nullable reference types

### 提交规范
- 使用语义化提交信息 (feat:, fix:, docs:, refactor:)
- 每个功能/修复一个提交
- 更新 CHANGELOG.md

### 测试要求
- 核心功能代码覆盖率 > 80%
- 每个公共 API 至少一个测试用例
- 使用 xUnit 测试框架

---

## 里程碑

- **M1 (2周)**: 完成基础架构和项目搭建 ✅
- **M2 (4周)**: 完成基础组件库 (布局 + 基础控件) ✅
- **M3 (6周)**: 完成高级组件和交互功能 ✅
- **M4 (8周)**: 完成状态管理和动画系统 🔨
- **M5 (10周)**: 完成 Old8Lang API 设计和绑定 ⏳
- **M6 (11周)**: 完成示例和文档 ⏳
- **M7 (13周)**: 发布 v0.1.0-alpha ⏳

---

## 参考资源

### Avalonia 官方文档
- https://docs.avaloniaui.net/

### Flutter 文档 (设计参考)
- https://flutter.dev/docs

### SwiftUI 文档 (设计参考)
- https://developer.apple.com/documentation/swiftui

### 类似项目参考
- Avalonia.FuncUI (F# 声明式 UI)
- Fabulous (F# + Xamarin.Forms 声明式 UI)
- React (虚拟 DOM 和状态管理思想)

---

## 风险与挑战

### 技术风险
1. **性能问题**: 动态语言调用 C# 可能有性能开销
   - 缓解措施: 优化桥接层、减少跨边界调用
2. **内存管理**: Old8Lang GC 与 .NET GC 协作
   - 缓解措施: 正确管理对象生命周期、避免循环引用

### 兼容性风险
1. **跨平台兼容性**: Avalonia 在不同平台表现可能不一致
   - 缓解措施: 多平台测试、使用平台抽象层
2. **Old8Lang 版本兼容**: 语言特性变化可能影响 API
   - 缓解措施: 保持 API 稳定、提供兼容层

### 用户体验风险
1. **学习曲线**: 用户需要学习新的 GUI 编程范式
   - 缓解措施: 提供详细文档和示例、循序渐进的教程

---

## 后续扩展计划

### V0.2 (3-6个月后)
- [ ] 更多高级组件 (图表、地图、富文本编辑器)
- [ ] Hot Reload 支持
- [ ] 可视化设计器

### V1.0 (6-12个月后)
- [ ] 移动端支持 (iOS/Android via Avalonia)
- [ ] 3D 渲染支持
- [ ] 插件系统

---

## 联系与协作

- **项目负责人**: [待定]
- **协作方式**: GitHub Issues / Pull Requests
- **讨论区**: Old8Lang 社区论坛

---

*最后更新: 2026-01-01*
