namespace FirstUI.Core;

/// <summary>
/// 组件基类
/// 所有 FirstUI 组件都继承此类
/// </summary>
public abstract class WidgetBase
{
    /// <summary>
    /// 组件唯一标识符
    /// </summary>
    public string Id { get; protected set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 组件宽度
    /// </summary>
    public double? Width { get; set; }

    /// <summary>
    /// 组件高度
    /// </summary>
    public double? Height { get; set; }

    /// <summary>
    /// 内边距
    /// </summary>
    public Thickness Padding { get; set; } = new(0);

    /// <summary>
    /// 外边距
    /// </summary>
    public Thickness Margin { get; set; } = new(0);

    /// <summary>
    /// 背景色
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// 是否可见
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 不透明度 (0.0 - 1.0)
    /// </summary>
    public double Opacity { get; set; } = 1.0;

    /// <summary>
    /// 构建组件对应的 Avalonia 控件
    /// </summary>
    /// <param name="context">构建上下文</param>
    /// <returns>Avalonia 控件实例</returns>
    public abstract object Build(BuildContext context);

    /// <summary>
    /// 更新组件状态
    /// </summary>
    public virtual void Update()
    {
        // 子类可以覆盖此方法实现自定义更新逻辑
    }

    /// <summary>
    /// 重建组件（用于状态变化时触发）
    /// </summary>
    public virtual void Rebuild()
    {
        // 子类可以覆盖此方法实现重建逻辑
        Update();
    }

    /// <summary>
    /// 链式调用：设置宽度
    /// </summary>
    public WidgetBase SetWidth(double width)
    {
        Width = width;
        return this;
    }

    /// <summary>
    /// 链式调用：设置高度
    /// </summary>
    public WidgetBase SetHeight(double height)
    {
        Height = height;
        return this;
    }

    /// <summary>
    /// 链式调用：设置内边距
    /// </summary>
    public WidgetBase SetPadding(double padding)
    {
        Padding = new Thickness(padding);
        return this;
    }

    /// <summary>
    /// 链式调用：设置背景色
    /// </summary>
    public WidgetBase SetBackgroundColor(string color)
    {
        BackgroundColor = color;
        return this;
    }

    /// <summary>
    /// 链式调用：设置外边距
    /// </summary>
    public WidgetBase SetMargin(double margin)
    {
        Margin = new Thickness(margin);
        return this;
    }

    /// <summary>
    /// 链式调用：设置内边距
    /// </summary>
    public WidgetBase SetPadding(double left, double top, double right, double bottom)
    {
        Padding = new Thickness(left, top, right, bottom);
        return this;
    }

    /// <summary>
    /// 链式调用：设置外边距
    /// </summary>
    public WidgetBase SetMargin(double left, double top, double right, double bottom)
    {
        Margin = new Thickness(left, top, right, bottom);
        return this;
    }

    /// <summary>
    /// 链式调用：设置不透明度
    /// </summary>
    public WidgetBase SetOpacity(double opacity)
    {
        Opacity = Math.Clamp(opacity, 0.0, 1.0);
        return this;
    }

    /// <summary>
    /// 链式调用：设置可见性
    /// </summary>
    public WidgetBase SetVisible(bool visible)
    {
        IsVisible = visible;
        return this;
    }
}

/// <summary>
/// 边距/内距结构
/// </summary>
public struct Thickness
{
    public double Left { get; set; }
    public double Top { get; set; }
    public double Right { get; set; }
    public double Bottom { get; set; }

    public Thickness(double uniformSize)
    {
        Left = Top = Right = Bottom = uniformSize;
    }

    public Thickness(double horizontal, double vertical)
    {
        Left = Right = horizontal;
        Top = Bottom = vertical;
    }

    public Thickness(double left, double top, double right, double bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public override string ToString()
    {
        return $"({Left}, {Top}, {Right}, {Bottom})";
    }
}