// See https://aka.ms/new-console-template for more information

using Old8Lang.FirstUI;
using Old8Lang.FirstUI.Basic;
using Old8Lang.FirstUI.Core;
using Old8Lang.FirstUI.Layout;

var app = FirstUIBinding.CreateApp();
int counter = 0;
app.Run(new Column([
    new Text("计数器应用")
        .SetFontSize(28)
        .SetFontWeight("bold")
        .SetColor("#000000"),
    new Text(content: counter.ToString())
        .SetFontSize(72)
        .SetFontWeight("bold")
        .SetColor("#007AFF")
        .SetMargin(20, 0, 30, 0),
    new Row(
        children:
        [
            // 减少按钮
            new Button(
                label: "➖ 减少",
                onClick: () => { counter -= 1; }
            ),

            // 重置按钮
            new Button(
                label: "🔄 重置",
                onClick: () =>
                {
                    counter = 0;
                    FirstUIBinding.ShowToast("已重置计数器");
                }
            ).SetMargin(20, 0, 0, 0),

            // 增加按钮
            new Button(
                label: "➕ 增加",
                onClick: () => { counter += 1; }
            ).SetMargin(20, 0, 0, 0)
        ]
    ).SetSpacing(20),
    new Text("点击按钮改变计数值")
        .SetFontSize(14)
        .SetColor("#8E8E93")
        .SetMargin(40, 0, 0, 0)
]).SetPadding(40), "计数器应用");