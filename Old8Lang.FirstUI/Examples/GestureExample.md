# Old8Lang.FirstUI 手势系统示例

> 完整的手势检测和拖放操作支持

## 概述

Old8Lang.FirstUI 提供了强大的手势系统，支持：
- **GestureDetector**: 检测各种手势（单击、双击、长按、拖动、滑动）
- **Draggable**: 使组件可拖动
- **DropTarget**: 接收拖放操作

---

## GestureDetector - 手势检测器

### 基本用法

```csharp
using Old8Lang.FirstUI.Gesture;
using Old8Lang.FirstUI.Basic;

// 创建一个可检测手势的文本
var gestureDetector = new GestureDetector
{
    Child = new Text("点击我!") { FontSize = 24 },
    OnTap = (data) =>
    {
        Console.WriteLine($"单击位置: {data.Position}");
    },
    OnDoubleTap = (data) =>
    {
        Console.WriteLine($"双击位置: {data.Position}");
    },
    OnLongPress = (data) =>
    {
        Console.WriteLine($"长按位置: {data.Position}");
    }
};
```

### 支持的手势类型

| 手势 | 事件名称 | 说明 |
|------|---------|------|
| 单击 | `OnTap` | 快速按下并释放 |
| 双击 | `OnDoubleTap` | 连续两次单击（300ms 内） |
| 长按 | `OnLongPress` | 按下并保持（默认 500ms） |
| 拖动开始 | `OnDragStart` | 开始拖动（超过阈值） |
| 拖动中 | `OnDrag` | 拖动过程中持续触发 |
| 拖动结束 | `OnDragEnd` | 释放完成拖动 |
| 滑动 | `OnSwipe` | 快速拖动并释放 |

### 拖动检测示例

```csharp
var gestureDetector = new GestureDetector
{
    Child = new Container
    {
        Width = 200,
        Height = 200,
        BackgroundColor = "#FF5722",
        Child = new Text("拖动我") { Color = "#FFFFFF" }
    },

    OnDragStart = (data) =>
    {
        Console.WriteLine("开始拖动");
    },

    OnDrag = (data) =>
    {
        Console.WriteLine($"拖动中，偏移: {data.Delta}, 总偏移: {data.TotalDelta}");
    },

    OnDragEnd = (data) =>
    {
        Console.WriteLine($"拖动结束，总偏移: {data.TotalDelta}");
    }
};
```

### 滑动检测示例

```csharp
var gestureDetector = new GestureDetector
{
    Child = new Text("快速滑动!"),

    OnSwipe = (data) =>
    {
        var direction = data.SwipeDirection switch
        {
            SwipeDirection.Up => "向上",
            SwipeDirection.Down => "向下",
            SwipeDirection.Left => "向左",
            SwipeDirection.Right => "向右",
            _ => "未知"
        };

        Console.WriteLine($"滑动方向: {direction}, 速度: {data.Velocity:F2} px/s");
    },

    // 可自定义滑动速度阈值
    SwipeVelocityThreshold = 300
};
```

### 自定义参数

```csharp
var gestureDetector = new GestureDetector
{
    Child = myWidget,

    // 长按延迟（毫秒，默认 500ms）
    LongPressDelay = 800,

    // 拖动阈值（像素，默认 10px）
    DragThreshold = 15,

    // 滑动速度阈值（像素/秒，默认 500）
    SwipeVelocityThreshold = 600,

    // 双击时间间隔（毫秒，默认 300ms）
    DoubleTapDelay = 250
};
```

### 链式调用

```csharp
var gestureDetector = new GestureDetector()
    .SetChild(myWidget)
    .SetOnTap((data) => Console.WriteLine("Tap!"))
    .SetOnDoubleTap((data) => Console.WriteLine("Double Tap!"))
    .SetLongPressDelay(600)
    .SetDragThreshold(20);
```

---

## Draggable - 可拖动组件

### 基本用法

```csharp
using Old8Lang.FirstUI.Gesture;

// 创建一个可拖动的盒子
var draggable = new Draggable
{
    Child = new Container
    {
        Width = 100,
        Height = 100,
        BackgroundColor = "#4CAF50",
        Child = new Text("拖动我") { Color = "#FFFFFF" }
    },

    OnDragStart = (data) =>
    {
        Console.WriteLine("开始拖动");
    },

    OnDragging = (data) =>
    {
        Console.WriteLine($"拖动中: {data.CurrentPosition}");
    },

    OnDragEnd = (data) =>
    {
        Console.WriteLine($"拖动结束: {data.CurrentPosition}");
    }
};
```

### 轴限制拖动

```csharp
// 仅允许水平拖动
var horizontalDraggable = new Draggable
{
    Child = myWidget,
    Axis = DragAxis.Horizontal
};

// 仅允许垂直拖动
var verticalDraggable = new Draggable
{
    Child = myWidget,
    Axis = DragAxis.Vertical
};

// 不限制（默认）
var freeDraggable = new Draggable
{
    Child = myWidget,
    Axis = DragAxis.None
};
```

### 拖动反馈效果

```csharp
// 半透明效果
var draggable = new Draggable
{
    Child = myWidget,
    Feedback = DragFeedback.Opacity,
    DragOpacity = 0.5  // 拖动时不透明度
};

// 阴影效果（待实现）
var draggable2 = new Draggable
{
    Child = myWidget,
    Feedback = DragFeedback.Shadow
};

// 无效果（默认）
var draggable3 = new Draggable
{
    Child = myWidget,
    Feedback = DragFeedback.Default
};
```

### 拖放数据传递

```csharp
var draggable = new Draggable
{
    Child = new Text("文件.txt"),
    Data = new { FileName = "文件.txt", Path = "/path/to/file.txt" },
    DataType = "file",

    OnDragEnd = (data) =>
    {
        Console.WriteLine($"拖动数据: {data.Data}");
        Console.WriteLine($"数据类型: {data.DataType}");
    }
};
```

### 链式调用

```csharp
var draggable = new Draggable()
    .SetChild(myWidget)
    .SetAxis(DragAxis.Horizontal)
    .SetFeedback(DragFeedback.Opacity)
    .SetDragOpacity(0.6)
    .SetData(myData)
    .SetDataType("custom")
    .SetOnDragStart((data) => Console.WriteLine("Start"))
    .SetOnDragEnd((data) => Console.WriteLine("End"));
```

---

## DropTarget - 拖放目标

### 基本用法

```csharp
using Old8Lang.FirstUI.Gesture;

// 创建一个接收拖放的区域
var dropTarget = new DropTarget
{
    Child = new Container
    {
        Width = 300,
        Height = 300,
        BackgroundColor = "#E3F2FD",
        Child = new Text("拖放到这里")
    },

    OnDragEnter = (data) =>
    {
        Console.WriteLine("拖动进入目标区域");
    },

    OnDragOver = (data) =>
    {
        Console.WriteLine("拖动悬停在目标区域");
    },

    OnDragLeave = () =>
    {
        Console.WriteLine("拖动离开目标区域");
    },

    OnDrop = (data) =>
    {
        Console.WriteLine($"放置成功: {data.Data}");
    }
};
```

### 限制接受的数据类型

```csharp
var dropTarget = new DropTarget
{
    Child = myWidget,

    // 只接受特定类型的数据
    AcceptedDataTypes = new[] { "file", "image", "text" },

    OnDrop = (data) =>
    {
        Console.WriteLine($"接收到类型: {data.DataType}");
    }
};
```

### 悬停高亮效果

```csharp
var dropTarget = new DropTarget
{
    Child = myWidget,

    // 悬停时的背景色
    HoverColor = "#BBDEFB",

    // 悬停时的边框颜色
    HoverBorderColor = "#2196F3",

    // 悬停时的边框宽度
    HoverBorderWidth = 3,

    OnDrop = (data) =>
    {
        Console.WriteLine("放置成功!");
    }
};
```

### 链式调用

```csharp
var dropTarget = new DropTarget()
    .SetChild(myWidget)
    .SetAcceptedDataTypes("file", "image")
    .SetHoverColor("#FFEB3B")
    .SetHoverBorderColor("#FFC107")
    .SetHoverBorderWidth(2)
    .SetOnDragEnter((data) => Console.WriteLine("Enter"))
    .SetOnDrop((data) => Console.WriteLine("Drop"));
```

---

## 完整拖放示例

```csharp
using Old8Lang.FirstUI.Gesture;
using Old8Lang.FirstUI.Layout;
using Old8Lang.FirstUI.Basic;

// 创建可拖动的项目
var draggableItem = new Draggable
{
    Child = new Container
    {
        Width = 100,
        Height = 100,
        BackgroundColor = "#4CAF50",
        BorderRadius = 8,
        Child = new Text("拖动我") { Color = "#FFFFFF", FontSize = 16 }
    },
    Data = new { ItemName = "项目 1", ItemId = 1 },
    DataType = "item",
    Feedback = DragFeedback.Opacity,
    DragOpacity = 0.7,

    OnDragStart = (data) =>
    {
        Console.WriteLine("开始拖动项目");
    }
};

// 创建放置目标
var dropTarget = new DropTarget
{
    Child = new Container
    {
        Width = 300,
        Height = 300,
        BackgroundColor = "#F5F5F5",
        BorderRadius = 8,
        Child = new Text("拖放到这里") { FontSize = 18, Color = "#757575" }
    },
    AcceptedDataTypes = new[] { "item" },
    HoverColor = "#E8F5E9",
    HoverBorderColor = "#4CAF50",
    HoverBorderWidth = 3,

    OnDragEnter = (data) =>
    {
        Console.WriteLine("项目进入放置区");
    },

    OnDrop = (data) =>
    {
        Console.WriteLine($"成功放置: {data.Data}");
        // 处理放置逻辑
    }
};

// 布局
var layout = new Row
{
    Children = new List<WidgetBase> { draggableItem, dropTarget },
    Spacing = 20
};
```

---

## GestureEventData 事件数据

所有手势事件都会提供 `GestureEventData` 对象，包含以下信息：

```csharp
public class GestureEventData
{
    public GestureType Type { get; set; }           // 手势类型
    public Point Position { get; set; }             // 相对位置
    public Point ScreenPosition { get; set; }       // 屏幕坐标
    public Vector Delta { get; set; }               // 拖动偏移
    public Vector TotalDelta { get; set; }          // 总偏移
    public SwipeDirection? SwipeDirection { get; set; } // 滑动方向
    public double Velocity { get; set; }            // 滑动速度
    public double Scale { get; set; }               // 缩放比例（缩放手势）
    public double Angle { get; set; }               // 旋转角度（旋转手势）
    public DateTime Timestamp { get; set; }         // 时间戳
    public bool Handled { get; set; }               // 是否已处理
}
```

---

## DragDropData 拖放数据

拖放事件会提供 `DragDropData` 对象：

```csharp
public class DragDropData
{
    public object? Data { get; set; }              // 拖动的数据对象
    public string? DataType { get; set; }          // 数据类型标识符
    public Point StartPosition { get; set; }       // 拖动开始位置
    public Point CurrentPosition { get; set; }     // 当前位置
    public bool AllowDrop { get; set; }            // 是否允许放置
}
```

---

## 最佳实践

### 1. 手势冲突避免

当一个组件同时监听多个手势时，注意手势优先级：

```csharp
var detector = new GestureDetector
{
    Child = myWidget,

    // 拖动会取消长按
    OnLongPress = (data) => Console.WriteLine("Long Press"),
    OnDragStart = (data) => Console.WriteLine("Drag Start - Long Press Cancelled"),

    // 双击会重置单击计数
    OnTap = (data) => Console.WriteLine("Tap"),
    OnDoubleTap = (data) => Console.WriteLine("Double Tap - Single Tap Skipped")
};
```

### 2. 性能优化

避免在高频事件（如 `OnDrag`）中执行耗时操作：

```csharp
var draggable = new Draggable
{
    Child = myWidget,

    // ❌ 不推荐：每次拖动都更新 UI
    OnDragging = (data) =>
    {
        UpdateComplexUI();  // 耗时操作
    },

    // ✅ 推荐：仅在拖动结束时更新
    OnDragEnd = (data) =>
    {
        UpdateComplexUI();
    }
};
```

### 3. 数据验证

在 DropTarget 中验证拖放数据：

```csharp
var dropTarget = new DropTarget
{
    Child = myWidget,
    AcceptedDataTypes = new[] { "file" },

    OnDrop = (data) =>
    {
        if (data.Data is FileInfo file)
        {
            // 验证文件类型
            if (file.Extension == ".txt")
            {
                ProcessFile(file);
            }
            else
            {
                Console.WriteLine("不支持的文件类型");
            }
        }
    }
};
```

---

## FirstUIBinding API

在 Old8Lang 中使用手势 API：

```old8
import "firstui" as ui

// 创建手势检测器
gestureDetector <- ui.CreateGestureDetector(myWidget)

// 创建可拖动组件
draggable <- ui.CreateDraggable(myWidget)

// 创建拖放目标
dropTarget <- ui.CreateDropTarget(myWidget)

// 包装回调函数
onTapCallback <- ui.WrapGestureCallback(() -> {
    PrintLine("Tapped!")
})

gestureDetector.SetOnTap(onTapCallback)
```

---

*最后更新: 2026-01-01*
