using Old8Lang.FirstUI;
using Old8Lang.FirstUI.Basic;
using Old8Lang.FirstUI.Core;
using Old8Lang.FirstUI.Layout;
using Old8Lang.FirstUI.State.Reactive;

var app = FirstUIBinding.CreateApp();

// ========== Vue 风格响应式 API 演示 ==========

// 1. ref() - 响应式引用
var count = Ref.Create(0);
var name = Ref.Create("World");

// 2. computed() - 自动依赖追踪的计算属性
var doubled = AutoComputed.Create(() => count.Value * 2);
var greeting = AutoComputed.Create(() => $"Hello, {name.Value}! Count is {count.Value}");

// 3. watch() - 监听特定状态变化
var watchStop = Watch.Create(
    () => count.Value,
    (newVal, oldVal) =>
    {
        Console.WriteLine($"[Watch] count changed: {oldVal} -> {newVal}");
    }
);

// 4. watchEffect() - 自动追踪依赖的副作用
var effectStop = WatchEffectFactory.Create(() =>
{
    Console.WriteLine($"[WatchEffect] Current state: count={count.Value}, doubled={doubled.Value}, greeting={greeting.Value}");
});

// 构建 UI（使用 lambda 函数，每次重建时重新执行）
app.Run(() => new Column([
    // 标题
    new Text("Vue 风格响应式 API 演示")
        .SetFontSize(28)
        .SetFontWeight("bold")
        .SetColor("#000000"),

    // 计数器显示
    new Text(content: count.Value.ToString() ?? "0")
        .SetFontSize(72)
        .SetFontWeight("bold")
        .SetColor("#007AFF")
        .SetMargin(20, 0, 10, 0),

    // 计算属性显示
    new Text(content: $"Doubled: {doubled.Value}")
        .SetFontSize(24)
        .SetColor("#34C759")
        .SetMargin(0, 0, 10, 0),

    // 问候语显示
    new Text(content: greeting.Value ?? "")
        .SetFontSize(18)
        .SetColor("#5856D6")
        .SetMargin(0, 0, 20, 0),

    // 计数器按钮行
    new Row([
        new Button(
            label: "➖ 减少",
            onClick: () => count.Value -= 1
        ),
        new Button(
            label: "🔄 重置",
            onClick: () =>
            {
                count.Value = 0;
                FirstUIBinding.ShowToast("已重置计数器");
            }
        ).SetMargin(20, 0, 0, 0),
        new Button(
            label: "➕ 增加",
            onClick: () => count.Value += 1
        ).SetMargin(20, 0, 0, 0)
    ]).SetSpacing(20),

    // 名称输入
    new Row([
        new Text("名称: ").SetFontSize(16),
        new TextInput(placeholder: "输入名称", initialValue: name.Value ?? "")
        {
            OnChanged = newName => name.Value = newName
        }.SetWidth(200)
    ]).SetMargin(30, 0, 0, 0),

    // 快捷名称按钮
    new Row([
        new Button(
            label: "Alice",
            onClick: () => name.Value = "Alice"
        ),
        new Button(
            label: "Bob",
            onClick: () => name.Value = "Bob"
        ).SetMargin(10, 0, 0, 0),
        new Button(
            label: "World",
            onClick: () => name.Value = "World"
        ).SetMargin(10, 0, 0, 0)
    ]).SetMargin(10, 0, 0, 0),

    // 说明文字
    new Text("修改 count 或 name，观察控制台输出")
        .SetFontSize(14)
        .SetColor("#8E8E93")
        .SetMargin(40, 0, 0, 0),

    new Text("watch() 监听 count 变化，watchEffect() 自动追踪所有依赖")
        .SetFontSize(12)
        .SetColor("#AEAEB2")
        .SetMargin(5, 0, 0, 0)
]).SetPadding(40), "Vue 风格响应式 API 演示");
