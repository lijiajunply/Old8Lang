using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Old8Lang.FirstUI.Core;
using Old8Lang.FirstUI.Utils;

namespace Old8Lang.FirstUI.Basic;

/// <summary>
/// Image 图片显示组件
/// </summary>
public class Image : WidgetBase
{
    /// <summary>
    /// 图片源（本地路径或 URL）
    /// </summary>
    public string Source { get; set; } = "";

    /// <summary>
    /// 图片适应模式
    /// </summary>
    public ImageFit Fit { get; set; } = ImageFit.Contain;

    /// <summary>
    /// 图片拉伸方式
    /// </summary>
    public ImageStretch Stretch { get; set; } = ImageStretch.Uniform;

    public Image(string source)
    {
        Source = source;
    }

    public override object Build(BuildContext context)
    {
        var image = new Avalonia.Controls.Image
        {
            Stretch = Stretch switch
            {
                ImageStretch.None => Avalonia.Media.Stretch.None,
                ImageStretch.Fill => Avalonia.Media.Stretch.Fill,
                ImageStretch.Uniform => Avalonia.Media.Stretch.Uniform,
                ImageStretch.UniformToFill => Avalonia.Media.Stretch.UniformToFill,
                _ => Avalonia.Media.Stretch.Uniform
            }
        };

        // 应用基础样式
        LayoutHelper.ApplyBaseStyles(image, this);

        // 加载图片
        LoadImage(image);

        return image;
    }

    /// <summary>
    /// 加载图片
    /// </summary>
    private void LoadImage(Avalonia.Controls.Image image)
    {
        if (string.IsNullOrEmpty(Source))
            return;

        try
        {
            // 判断是本地文件还是 URL
            if (Source.StartsWith("http://") || Source.StartsWith("https://"))
            {
                // URL 图片
                image.Source = new Bitmap(Source);
            }
            else
            {
                // 本地文件
                if (File.Exists(Source))
                {
                    image.Source = new Bitmap(Source);
                }
                else
                {
                    Console.Error.WriteLine($"[Image] File not found: {Source}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Image] Error loading image: {ex.Message}");
        }
    }

    /// <summary>
    /// 链式调用：设置图片适应模式
    /// </summary>
    public Image SetFit(ImageFit fit)
    {
        Fit = fit;
        Stretch = fit switch
        {
            ImageFit.Contain => ImageStretch.Uniform,
            ImageFit.Cover => ImageStretch.UniformToFill,
            ImageFit.Fill => ImageStretch.Fill,
            ImageFit.None => ImageStretch.None,
            _ => ImageStretch.Uniform
        };
        return this;
    }

    /// <summary>
    /// 链式调用：设置图片拉伸方式
    /// </summary>
    public Image SetStretch(ImageStretch stretch)
    {
        Stretch = stretch;
        return this;
    }
}

/// <summary>
/// 图片适应模式
/// </summary>
public enum ImageFit
{
    /// <summary>包含：保持宽高比，完整显示图片</summary>
    Contain,
    /// <summary>覆盖：保持宽高比，填充容器（可能裁剪）</summary>
    Cover,
    /// <summary>填充：拉伸填充容器（可能变形）</summary>
    Fill,
    /// <summary>无：原始大小</summary>
    None
}

/// <summary>
/// 图片拉伸方式
/// </summary>
public enum ImageStretch
{
    /// <summary>无拉伸</summary>
    None,
    /// <summary>填充容器（可能变形）</summary>
    Fill,
    /// <summary>等比例缩放，完整显示</summary>
    Uniform,
    /// <summary>等比例缩放，填充容器</summary>
    UniformToFill
}
