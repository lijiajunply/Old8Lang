using SkiaSharp;

namespace Old8LangLib;

/// <summary>
/// 图像处理库，提供丰富的图像加载、保存、变换、调整、滤镜和绘制功能
/// 基于 SkiaSharp 实现，支持跨平台
/// </summary>
public static class ImageLib
{
    // 辅助方法：验证颜色值范围（0-255）
    private static void ValidateColorValue(int value, string colorName)
    {
        if (value < 0 || value > 255)
        {
            throw new ArgumentOutOfRangeException(colorName, $"{colorName} 必须在 0-255 范围内");
        }
    }

    // 辅助方法：从 HSB 转换为 RGB
    private static SKColor FromHsb(float hue, float saturation, float brightness)
    {
        // 将色调归一化到 0-360 范围
        hue %= 360;
        if (hue < 0) hue += 360;

        // 饱和度和亮度限制在 0-1 范围
        saturation = Math.Max(0, Math.Min(1, saturation));
        brightness = Math.Max(0, Math.Min(1, brightness));

        if (saturation == 0)
        {
            // 灰度
            byte gray = (byte)(brightness * 255);
            return new SKColor(gray, gray, gray);
        }

        float c = brightness * saturation;
        float x = c * (1 - Math.Abs((hue / 60) % 2 - 1));
        float m = brightness - c;

        float r, g, b;
        if (hue < 60)
        {
            r = c;
            g = x;
            b = 0;
        }
        else if (hue < 120)
        {
            r = x;
            g = c;
            b = 0;
        }
        else if (hue < 180)
        {
            r = 0;
            g = c;
            b = x;
        }
        else if (hue < 240)
        {
            r = 0;
            g = x;
            b = c;
        }
        else if (hue < 300)
        {
            r = x;
            g = 0;
            b = c;
        }
        else
        {
            r = c;
            g = 0;
            b = x;
        }

        return new SKColor(
            (byte)((r + m) * 255),
            (byte)((g + m) * 255),
            (byte)((b + m) * 255)
        );
    }

    // ===== 基础图像操作 =====

    /// <summary>
    /// 从文件加载图像
    /// </summary>
    /// <param name="path">图像文件路径</param>
    /// <returns>加载的图像对象</returns>
    /// <exception cref="FileNotFoundException">当文件不存在时抛出</exception>
    /// <exception cref="InvalidOperationException">当图像加载失败时抛出</exception>
    public static SKBitmap LoadImage(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"图像文件不存在: '{path}'", path);
        }

        var bitmap = SKBitmap.Decode(path);
        if (bitmap == null)
        {
            throw new InvalidOperationException($"无法加载图像: '{path}'");
        }

        return bitmap;
    }

    /// <summary>
    /// 保存图像到文件
    /// </summary>
    /// <param name="image">要保存的图像</param>
    /// <param name="path">保存路径</param>
    /// <param name="format">图像格式（png, jpg, bmp, gif）</param>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    /// <exception cref="InvalidOperationException">当保存失败时抛出</exception>
    public static void SaveImage(SKBitmap image, string path, string format)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        SKEncodedImageFormat encodedFormat = format.ToLower() switch
        {
            "png" => SKEncodedImageFormat.Png,
            "jpg" or "jpeg" => SKEncodedImageFormat.Jpeg,
            "bmp" => SKEncodedImageFormat.Bmp,
            "gif" => SKEncodedImageFormat.Gif,
            _ => throw new ArgumentException($"不支持的图像格式: '{format}'", nameof(format))
        };

        using var skImage = SKImage.FromBitmap(image);
        using var data = skImage.Encode(encodedFormat, 90);

        if (data == null)
        {
            throw new InvalidOperationException("图像编码失败");
        }

        using var stream = File.OpenWrite(path);
        data.SaveTo(stream);
    }

    /// <summary>
    /// 创建指定大小和颜色的空白图像
    /// </summary>
    /// <param name="width">图像宽度</param>
    /// <param name="height">图像高度</param>
    /// <param name="r">红色分量（0-255）</param>
    /// <param name="g">绿色分量（0-255）</param>
    /// <param name="b">蓝色分量（0-255）</param>
    /// <returns>新创建的图像</returns>
    public static SKBitmap CreateImage(int width, int height, int r, int g, int b)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentException("图像宽度和高度必须大于 0");
        }

        ValidateColorValue(r, "r");
        ValidateColorValue(g, "g");
        ValidateColorValue(b, "b");

        var bitmap = new SKBitmap(width, height);
        var color = new SKColor((byte)r, (byte)g, (byte)b);

        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(color);

        return bitmap;
    }

    /// <summary>
    /// 克隆图像
    /// </summary>
    /// <param name="image">要克隆的图像</param>
    /// <returns>克隆后的图像</returns>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    public static SKBitmap CloneImage(SKBitmap image)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        return image.Copy();
    }

    /// <summary>
    /// 释放图像资源
    /// </summary>
    /// <param name="image">要释放的图像</param>
    public static void DisposeImage(SKBitmap? image)
    {
        image?.Dispose();
    }

    // ===== 图像信息获取 =====

    /// <summary>
    /// 获取图像宽度
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <returns>图像宽度（像素）</returns>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    public static int GetWidth(SKBitmap image)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        return image.Width;
    }

    /// <summary>
    /// 获取图像高度
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <returns>图像高度（像素）</returns>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    public static int GetHeight(SKBitmap image)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        return image.Height;
    }

    /// <summary>
    /// 获取图像尺寸
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <returns>图像尺寸数组 [width, height]</returns>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    public static int[] GetSize(SKBitmap image)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        return [image.Width, image.Height];
    }

    /// <summary>
    /// 获取指定位置的像素颜色
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <returns>像素颜色数组 [R, G, B, A]</returns>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当坐标超出图像范围时抛出</exception>
    public static int[] GetPixel(SKBitmap image, int x, int y)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        if (x < 0 || x >= image.Width || y < 0 || y >= image.Height)
        {
            throw new ArgumentOutOfRangeException($"坐标 ({x}, {y}) 超出图像范围");
        }

        var color = image.GetPixel(x, y);
        return [color.Red, color.Green, color.Blue, color.Alpha];
    }

    /// <summary>
    /// 设置指定位置的像素颜色
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <param name="r">红色分量（0-255）</param>
    /// <param name="g">绿色分量（0-255）</param>
    /// <param name="b">蓝色分量（0-255）</param>
    /// <param name="a">透明度分量（0-255，默认为255）</param>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当坐标超出图像范围时抛出</exception>
    public static void SetPixel(SKBitmap image, int x, int y, int r, int g, int b, int a = 255)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        if (x < 0 || x >= image.Width || y < 0 || y >= image.Height)
        {
            throw new ArgumentOutOfRangeException($"坐标 ({x}, {y}) 超出图像范围");
        }

        ValidateColorValue(r, "r");
        ValidateColorValue(g, "g");
        ValidateColorValue(b, "b");
        ValidateColorValue(a, "a");

        var color = new SKColor((byte)r, (byte)g, (byte)b, (byte)a);
        image.SetPixel(x, y, color);
    }

    /// <summary>
    /// 获取图像的像素格式信息
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <returns>像素格式字符串</returns>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    public static string GetPixelFormat(SKBitmap image)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        return image.ColorType.ToString();
    }

    /// <summary>
    /// 获取图像的主要颜色
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <param name="sampleRate">采样率（1-10，数值越大采样越稀疏）</param>
    /// <returns>主要颜色数组 [R, G, B]</returns>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    public static int[] GetDominantColor(SKBitmap image, int sampleRate = 5)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        if (sampleRate < 1 || sampleRate > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "采样率必须在 1-10 范围内");
        }

        long totalR = 0, totalG = 0, totalB = 0;
        int count = 0;

        for (int y = 0; y < image.Height; y += sampleRate)
        {
            for (int x = 0; x < image.Width; x += sampleRate)
            {
                var color = image.GetPixel(x, y);
                totalR += color.Red;
                totalG += color.Green;
                totalB += color.Blue;
                count++;
            }
        }

        if (count == 0)
        {
            return [0, 0, 0];
        }

        return
        [
            (int)(totalR / count),
            (int)(totalG / count),
            (int)(totalB / count)
        ];
    }

    // ===== 图像变换 =====

    /// <summary>
    /// 调整图像大小
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <param name="width">新的宽度</param>
    /// <param name="height">新的高度</param>
    /// <param name="highQuality">是否使用高质量缩放</param>
    /// <returns>调整大小后的新图像</returns>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    public static SKBitmap ResizeImage(SKBitmap image, int width, int height, bool highQuality = true)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        if (width <= 0 || height <= 0)
        {
            throw new ArgumentException("宽度和高度必须大于 0");
        }

        var resized = new SKBitmap(width, height);
        using var canvas = new SKCanvas(resized);

        using var paint = new SKPaint();
        paint.IsAntialias = true;

        var srcRect = new SKRect(0, 0, image.Width, image.Height);
        var destRect = new SKRect(0, 0, width, height);

        canvas.DrawBitmap(image, srcRect, destRect, paint);
        return resized;
    }

    /// <summary>
    /// 按比例缩放图像
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <param name="scale">缩放比例</param>
    /// <param name="highQuality">是否使用高质量缩放</param>
    /// <returns>缩放后的新图像</returns>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    public static SKBitmap ScaleImage(SKBitmap image, double scale, bool highQuality = true)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        if (scale <= 0)
        {
            throw new ArgumentException("缩放比例必须大于 0");
        }

        int newWidth = (int)(image.Width * scale);
        int newHeight = (int)(image.Height * scale);

        return ResizeImage(image, newWidth, newHeight, highQuality);
    }

    /// <summary>
    /// 旋转图像
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <param name="angle">旋转角度（正值为顺时针）</param>
    /// <returns>旋转后的新图像</returns>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    public static SKBitmap RotateImage(SKBitmap image, double angle)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        // 计算旋转后的边界大小
        double radians = angle * Math.PI / 180;
        double cos = Math.Abs(Math.Cos(radians));
        double sin = Math.Abs(Math.Sin(radians));
        int newWidth = (int)(image.Width * cos + image.Height * sin);
        int newHeight = (int)(image.Width * sin + image.Height * cos);

        var rotated = new SKBitmap(newWidth, newHeight);
        using var canvas = new SKCanvas(rotated);

        // 透明背景
        canvas.Clear(SKColors.Transparent);

        // 移动到中心点并旋转
        canvas.Translate(newWidth / 2f, newHeight / 2f);
        canvas.RotateDegrees((float)angle);
        canvas.Translate(-image.Width / 2f, -image.Height / 2f);

        // 绘制图像
        canvas.DrawBitmap(image, 0, 0);

        return rotated;
    }

    /// <summary>
    /// 裁剪图像
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <param name="x">裁剪区域左上角X坐标</param>
    /// <param name="y">裁剪区域左上角Y坐标</param>
    /// <param name="width">裁剪区域宽度</param>
    /// <param name="height">裁剪区域高度</param>
    /// <returns>裁剪后的新图像</returns>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    /// <exception cref="ArgumentException">当裁剪区域无效时抛出</exception>
    public static SKBitmap CropImage(SKBitmap image, int x, int y, int width, int height)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        if (x < 0 || y < 0 || width <= 0 || height <= 0)
        {
            throw new ArgumentException("裁剪区域参数无效");
        }

        if (x + width > image.Width || y + height > image.Height)
        {
            throw new ArgumentException("裁剪区域超出图像范围");
        }

        var cropped = new SKBitmap(width, height);
        using var canvas = new SKCanvas(cropped);

        var srcRect = new SKRect(x, y, x + width, y + height);
        var destRect = new SKRect(0, 0, width, height);

        canvas.DrawBitmap(image, srcRect, destRect);

        return cropped;
    }

    /// <summary>
    /// 水平翻转图像
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <returns>翻转后的新图像</returns>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    public static SKBitmap FlipHorizontal(SKBitmap image)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        var flipped = new SKBitmap(image.Width, image.Height);
        using var canvas = new SKCanvas(flipped);

        canvas.Scale(-1, 1, image.Width / 2f, 0);
        canvas.DrawBitmap(image, 0, 0);

        return flipped;
    }

    /// <summary>
    /// 垂直翻转图像
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <returns>翻转后的新图像</returns>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    public static SKBitmap FlipVertical(SKBitmap image)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        var flipped = new SKBitmap(image.Width, image.Height);
        using var canvas = new SKCanvas(flipped);

        canvas.Scale(1, -1, 0, image.Height / 2f);
        canvas.DrawBitmap(image, 0, 0);

        return flipped;
    }

    // ===== 图像调整 =====

    /// <summary>
    /// 调整图像亮度
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <param name="brightness">亮度调整值（-255 到 255）</param>
    /// <returns>调整后的新图像</returns>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    public static SKBitmap AdjustBrightness(SKBitmap image, int brightness)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        if (brightness < -255 || brightness > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(brightness), "亮度值必须在 -255 到 255 范围内");
        }

        var result = new SKBitmap(image.Width, image.Height);

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                var color = image.GetPixel(x, y);
                int r = Math.Clamp(color.Red + brightness, 0, 255);
                int g = Math.Clamp(color.Green + brightness, 0, 255);
                int b = Math.Clamp(color.Blue + brightness, 0, 255);

                result.SetPixel(x, y, new SKColor((byte)r, (byte)g, (byte)b, color.Alpha));
            }
        }

        return result;
    }

    /// <summary>
    /// 调整图像对比度
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <param name="contrast">对比度调整值（-100 到 100）</param>
    /// <returns>调整后的新图像</returns>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    public static SKBitmap AdjustContrast(SKBitmap image, double contrast)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        if (contrast < -100 || contrast > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(contrast), "对比度值必须在 -100 到 100 范围内");
        }

        var result = new SKBitmap(image.Width, image.Height);
        double factor = (100.0 + contrast) / 100.0;
        factor *= factor;

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                var color = image.GetPixel(x, y);

                int r = (int)Math.Clamp(((color.Red / 255.0 - 0.5) * factor + 0.5) * 255, 0, 255);
                int g = (int)Math.Clamp(((color.Green / 255.0 - 0.5) * factor + 0.5) * 255, 0, 255);
                int b = (int)Math.Clamp(((color.Blue / 255.0 - 0.5) * factor + 0.5) * 255, 0, 255);

                result.SetPixel(x, y, new SKColor((byte)r, (byte)g, (byte)b, color.Alpha));
            }
        }

        return result;
    }

    /// <summary>
    /// 调整图像饱和度
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <param name="saturation">饱和度调整值（-100 到 100）</param>
    /// <returns>调整后的新图像</returns>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    public static SKBitmap AdjustSaturation(SKBitmap image, double saturation)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        if (saturation < -100 || saturation > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(saturation), "饱和度值必须在 -100 到 100 范围内");
        }

        var result = new SKBitmap(image.Width, image.Height);
        double saturationFactor = 1.0 + saturation / 100.0;

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                var color = image.GetPixel(x, y);

                // 转换到 HSL
                color.ToHsl(out var h, out var s, out var l);

                // 调整饱和度
                s = (float)Math.Clamp(s * saturationFactor, 0, 1);

                // 转换回 RGB
                var newColor = SKColor.FromHsl(h, s * 100, l * 100, color.Alpha);

                result.SetPixel(x, y, newColor);
            }
        }

        return result;
    }

    /// <summary>
    /// 调整图像色调
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <param name="hueShift">色调偏移值（-180 到 180 度）</param>
    /// <returns>调整后的新图像</returns>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    public static SKBitmap AdjustHue(SKBitmap image, double hueShift)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        if (hueShift < -180 || hueShift > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(hueShift), "色调偏移值必须在 -180 到 180 范围内");
        }

        var result = new SKBitmap(image.Width, image.Height);

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                var color = image.GetPixel(x, y);

                // 转换到 HSL
                color.ToHsl(out var h, out var s, out var l);

                // 调整色调
                h = (float)((h + hueShift + 360) % 360);

                // 转换回 RGB
                var newColor = SKColor.FromHsl(h, s * 100, l * 100, color.Alpha);

                result.SetPixel(x, y, newColor);
            }
        }

        return result;
    }

    // ===== 图像滤镜 =====

    /// <summary>
    /// 转换为灰度图像
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <returns>灰度图像</returns>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    public static SKBitmap ToGrayscale(SKBitmap image)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        var result = new SKBitmap(image.Width, image.Height);

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                var color = image.GetPixel(x, y);
                int gray = (int)(color.Red * 0.299 + color.Green * 0.587 + color.Blue * 0.114);

                result.SetPixel(x, y, new SKColor((byte)gray, (byte)gray, (byte)gray, color.Alpha));
            }
        }

        return result;
    }

    /// <summary>
    /// 反转颜色（负片效果）
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <returns>反转后的图像</returns>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    public static SKBitmap InvertColors(SKBitmap image)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        var result = new SKBitmap(image.Width, image.Height);

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                var color = image.GetPixel(x, y);
                int r = 255 - color.Red;
                int g = 255 - color.Green;
                int b = 255 - color.Blue;

                result.SetPixel(x, y, new SKColor((byte)r, (byte)g, (byte)b, color.Alpha));
            }
        }

        return result;
    }

    /// <summary>
    /// 应用模糊滤镜（高斯模糊）
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <param name="radius">模糊半径（1-10）</param>
    /// <returns>模糊后的图像</returns>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    public static SKBitmap ApplyBlur(SKBitmap image, int radius)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        if (radius < 1 || radius > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "模糊半径必须在 1-10 范围内");
        }

        var result = new SKBitmap(image.Width, image.Height);
        using var canvas = new SKCanvas(result);

        using var paint = new SKPaint();
        using var filter = SKImageFilter.CreateBlur(radius, radius);
        paint.ImageFilter = filter;

        canvas.DrawBitmap(image, 0, 0, paint);

        return result;
    }

    /// <summary>
    /// 应用锐化滤镜
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <param name="strength">锐化强度（1-10）</param>
    /// <returns>锐化后的图像</returns>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    public static SKBitmap ApplySharpen(SKBitmap image, int strength)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        if (strength < 1 || strength > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(strength), "锐化强度必须在 1-10 范围内");
        }

        var result = new SKBitmap(image.Width, image.Height);
        float factor = strength / 2.0f;

        // 锐化卷积核
        float[,] kernel =
        {
            { 0, -factor, 0 },
            { -factor, 1 + 4 * factor, -factor },
            { 0, -factor, 0 }
        };

        for (int y = 1; y < image.Height - 1; y++)
        {
            for (int x = 1; x < image.Width - 1; x++)
            {
                float r = 0, g = 0, b = 0;

                for (int ky = 0; ky < 3; ky++)
                {
                    for (int kx = 0; kx < 3; kx++)
                    {
                        var pixel = image.GetPixel(x + kx - 1, y + ky - 1);
                        float k = kernel[ky, kx];
                        r += pixel.Red * k;
                        g += pixel.Green * k;
                        b += pixel.Blue * k;
                    }
                }

                var alpha = image.GetPixel(x, y).Alpha;
                result.SetPixel(x, y, new SKColor(
                    (byte)Math.Clamp(r, 0, 255),
                    (byte)Math.Clamp(g, 0, 255),
                    (byte)Math.Clamp(b, 0, 255),
                    alpha
                ));
            }
        }

        // 处理边缘像素
        for (int y = 0; y < image.Height; y++)
        {
            result.SetPixel(0, y, image.GetPixel(0, y));
            result.SetPixel(image.Width - 1, y, image.GetPixel(image.Width - 1, y));
        }

        for (int x = 0; x < image.Width; x++)
        {
            result.SetPixel(x, 0, image.GetPixel(x, 0));
            result.SetPixel(x, image.Height - 1, image.GetPixel(x, image.Height - 1));
        }

        return result;
    }

    /// <summary>
    /// 边缘检测（Sobel算子）
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <param name="threshold">边缘阈值（0-255）</param>
    /// <returns>边缘检测后的图像</returns>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    public static SKBitmap DetectEdges(SKBitmap image, int threshold)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        ValidateColorValue(threshold, "threshold");

        // 先转换为灰度图
        var gray = ToGrayscale(image);
        var result = new SKBitmap(image.Width, image.Height);

        // Sobel算子
        int[,] sobelX = { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
        int[,] sobelY = { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };

        for (int y = 1; y < image.Height - 1; y++)
        {
            for (int x = 1; x < image.Width - 1; x++)
            {
                int gx = 0, gy = 0;

                for (int ky = 0; ky < 3; ky++)
                {
                    for (int kx = 0; kx < 3; kx++)
                    {
                        int pixel = gray.GetPixel(x + kx - 1, y + ky - 1).Red;
                        gx += pixel * sobelX[ky, kx];
                        gy += pixel * sobelY[ky, kx];
                    }
                }

                int magnitude = (int)Math.Sqrt(gx * gx + gy * gy);
                byte value = magnitude > threshold ? (byte)255 : (byte)0;

                result.SetPixel(x, y, new SKColor(value, value, value));
            }
        }

        gray.Dispose();
        return result;
    }

    // ===== 图像绘制 =====

    /// <summary>
    /// 在图像上绘制矩形
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <param name="x">矩形左上角X坐标</param>
    /// <param name="y">矩形左上角Y坐标</param>
    /// <param name="width">矩形宽度</param>
    /// <param name="height">矩形高度</param>
    /// <param name="r">红色分量（0-255）</param>
    /// <param name="g">绿色分量（0-255）</param>
    /// <param name="b">蓝色分量（0-255）</param>
    /// <param name="lineWidth">线条宽度</param>
    /// <param name="fill">是否填充</param>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    public static void DrawRectangle(SKBitmap image, int x, int y, int width, int height,
        int r, int g, int b, int lineWidth = 1, bool fill = false)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        ValidateColorValue(r, "r");
        ValidateColorValue(g, "g");
        ValidateColorValue(b, "b");

        using var canvas = new SKCanvas(image);
        using var paint = new SKPaint();
        paint.Color = new SKColor((byte)r, (byte)g, (byte)b);
        paint.Style = fill ? SKPaintStyle.Fill : SKPaintStyle.Stroke;
        paint.StrokeWidth = lineWidth;
        paint.IsAntialias = true;

        var rect = new SKRect(x, y, x + width, y + height);
        canvas.DrawRect(rect, paint);
    }

    /// <summary>
    /// 在图像上绘制圆形
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <param name="centerX">圆心X坐标</param>
    /// <param name="centerY">圆心Y坐标</param>
    /// <param name="radius">半径</param>
    /// <param name="r">红色分量（0-255）</param>
    /// <param name="g">绿色分量（0-255）</param>
    /// <param name="b">蓝色分量（0-255）</param>
    /// <param name="lineWidth">线条宽度</param>
    /// <param name="fill">是否填充</param>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    public static void DrawCircle(SKBitmap image, int centerX, int centerY, int radius,
        int r, int g, int b, int lineWidth = 1, bool fill = false)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        ValidateColorValue(r, "r");
        ValidateColorValue(g, "g");
        ValidateColorValue(b, "b");

        using var canvas = new SKCanvas(image);
        using var paint = new SKPaint();
        paint.Color = new SKColor((byte)r, (byte)g, (byte)b);
        paint.Style = fill ? SKPaintStyle.Fill : SKPaintStyle.Stroke;
        paint.StrokeWidth = lineWidth;
        paint.IsAntialias = true;

        canvas.DrawCircle(centerX, centerY, radius, paint);
    }

    /// <summary>
    /// 在图像上绘制直线
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <param name="x1">起点X坐标</param>
    /// <param name="y1">起点Y坐标</param>
    /// <param name="x2">终点X坐标</param>
    /// <param name="y2">终点Y坐标</param>
    /// <param name="r">红色分量（0-255）</param>
    /// <param name="g">绿色分量（0-255）</param>
    /// <param name="b">蓝色分量（0-255）</param>
    /// <param name="lineWidth">线条宽度</param>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    public static void DrawLine(SKBitmap image, int x1, int y1, int x2, int y2,
        int r, int g, int b, int lineWidth = 1)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        ValidateColorValue(r, "r");
        ValidateColorValue(g, "g");
        ValidateColorValue(b, "b");

        using var canvas = new SKCanvas(image);
        using var paint = new SKPaint();
        paint.Color = new SKColor((byte)r, (byte)g, (byte)b);
        paint.StrokeWidth = lineWidth;
        paint.IsAntialias = true;

        canvas.DrawLine(x1, y1, x2, y2, paint);
    }

    /// <summary>
    /// 在图像上绘制文本
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <param name="text">要绘制的文本</param>
    /// <param name="x">文本起始X坐标</param>
    /// <param name="y">文本基线Y坐标</param>
    /// <param name="fontSize">字体大小</param>
    /// <param name="r">红色分量（0-255）</param>
    /// <param name="g">绿色分量（0-255）</param>
    /// <param name="b">蓝色分量（0-255）</param>
    /// <param name="fontFamily">字体名称</param>
    /// <exception cref="ArgumentNullException">当图像或文本为null时抛出</exception>
    public static void DrawText(SKBitmap image, string text, int x, int y, int fontSize,
        int r, int g, int b, string fontFamily = "Arial")
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        if (string.IsNullOrEmpty(text))
        {
            throw new ArgumentNullException(nameof(text), "文本不能为空");
        }

        ValidateColorValue(r, "r");
        ValidateColorValue(g, "g");
        ValidateColorValue(b, "b");

        using var canvas = new SKCanvas(image);
        using var font = new SKFont();
        font.Size = fontSize;
        font.Typeface = SKTypeface.FromFamilyName(fontFamily);
        using var paint = new SKPaint();
        paint.Color = new SKColor((byte)r, (byte)g, (byte)b);
        paint.IsAntialias = true;

        canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
    }

    /// <summary>
    /// 在图像上绘制多边形
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <param name="points">顶点坐标数组（交替存储x, y坐标）</param>
    /// <param name="r">红色分量（0-255）</param>
    /// <param name="g">绿色分量（0-255）</param>
    /// <param name="b">蓝色分量（0-255）</param>
    /// <param name="lineWidth">线条宽度</param>
    /// <param name="fill">是否填充</param>
    /// <exception cref="ArgumentNullException">当图像或点数组为null时抛出</exception>
    /// <exception cref="ArgumentException">当点数组长度不是偶数或少于6个元素时抛出</exception>
    public static void DrawPolygon(SKBitmap image, int[] points, int r, int g, int b,
        int lineWidth = 1, bool fill = false)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        if (points == null || points.Length < 6 || points.Length % 2 != 0)
        {
            throw new ArgumentException("点数组必须包含至少3个点（6个坐标值），且长度必须是偶数", nameof(points));
        }

        ValidateColorValue(r, "r");
        ValidateColorValue(g, "g");
        ValidateColorValue(b, "b");

        using var canvas = new SKCanvas(image);
        using var paint = new SKPaint();
        paint.Color = new SKColor((byte)r, (byte)g, (byte)b);
        paint.Style = fill ? SKPaintStyle.Fill : SKPaintStyle.Stroke;
        paint.StrokeWidth = lineWidth;
        paint.IsAntialias = true;

        using var path = new SKPath();
        path.MoveTo(points[0], points[1]);

        for (int i = 2; i < points.Length; i += 2)
        {
            path.LineTo(points[i], points[i + 1]);
        }

        path.Close();
        canvas.DrawPath(path, paint);
    }

    // ===== 高级图像操作 =====

    /// <summary>
    /// 转换图像格式
    /// </summary>
    /// <param name="sourcePath">源图像文件路径</param>
    /// <param name="targetPath">目标图像文件路径</param>
    /// <param name="targetFormat">目标格式（png, jpg, bmp, gif）</param>
    /// <exception cref="FileNotFoundException">当源文件不存在时抛出</exception>
    public static void ConvertFormat(string sourcePath, string targetPath, string targetFormat)
    {
        var image = LoadImage(sourcePath);
        SaveImage(image, targetPath, targetFormat);
        image.Dispose();
    }

    /// <summary>
    /// 合并两个图像
    /// </summary>
    /// <param name="baseImage">基础图像</param>
    /// <param name="overlayImage">覆盖图像</param>
    /// <param name="x">覆盖图像的X坐标</param>
    /// <param name="y">覆盖图像的Y坐标</param>
    /// <param name="alpha">覆盖图像的透明度（0-255）</param>
    /// <returns>合并后的新图像</returns>
    /// <exception cref="ArgumentNullException">当任一图像为null时抛出</exception>
    public static SKBitmap MergeImages(SKBitmap baseImage, SKBitmap overlayImage, int x, int y, int alpha = 255)
    {
        if (baseImage == null)
        {
            throw new ArgumentNullException(nameof(baseImage), "基础图像不能为 null");
        }

        if (overlayImage == null)
        {
            throw new ArgumentNullException(nameof(overlayImage), "覆盖图像不能为 null");
        }

        ValidateColorValue(alpha, "alpha");

        var result = baseImage.Copy();
        using var canvas = new SKCanvas(result);
        using var paint = new SKPaint();
        paint.Color = new SKColor(255, 255, 255, (byte)alpha);
        paint.ColorFilter = SKColorFilter.CreateBlendMode(
            new SKColor(255, 255, 255, (byte)alpha),
            SKBlendMode.DstIn
        );

        canvas.DrawBitmap(overlayImage, x, y, paint);

        return result;
    }

    /// <summary>
    /// 水平拼接多个图像
    /// </summary>
    /// <param name="images">图像数组</param>
    /// <param name="spacing">图像间距</param>
    /// <returns>拼接后的新图像</returns>
    /// <exception cref="ArgumentNullException">当图像数组为null时抛出</exception>
    /// <exception cref="ArgumentException">当图像数组为空时抛出</exception>
    public static SKBitmap ConcatHorizontal(SKBitmap[] images, int spacing = 0)
    {
        if (images == null)
        {
            throw new ArgumentNullException(nameof(images), "图像数组不能为 null");
        }

        if (images.Length == 0)
        {
            throw new ArgumentException("图像数组不能为空", nameof(images));
        }

        int totalWidth = images.Sum(img => img.Width) + spacing * (images.Length - 1);
        int maxHeight = images.Max(img => img.Height);

        var result = new SKBitmap(totalWidth, maxHeight);
        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.White);

        int currentX = 0;
        foreach (var img in images)
        {
            canvas.DrawBitmap(img, currentX, 0);
            currentX += img.Width + spacing;
        }

        return result;
    }

    /// <summary>
    /// 垂直拼接多个图像
    /// </summary>
    /// <param name="images">图像数组</param>
    /// <param name="spacing">图像间距</param>
    /// <returns>拼接后的新图像</returns>
    /// <exception cref="ArgumentNullException">当图像数组为null时抛出</exception>
    /// <exception cref="ArgumentException">当图像数组为空时抛出</exception>
    public static SKBitmap ConcatVertical(SKBitmap[] images, int spacing = 0)
    {
        if (images == null)
        {
            throw new ArgumentNullException(nameof(images), "图像数组不能为 null");
        }

        if (images.Length == 0)
        {
            throw new ArgumentException("图像数组不能为空", nameof(images));
        }

        int maxWidth = images.Max(img => img.Width);
        int totalHeight = images.Sum(img => img.Height) + spacing * (images.Length - 1);

        var result = new SKBitmap(maxWidth, totalHeight);
        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.White);

        int currentY = 0;
        foreach (var img in images)
        {
            canvas.DrawBitmap(img, 0, currentY);
            currentY += img.Height + spacing;
        }

        return result;
    }

    /// <summary>
    /// 为图像添加边框
    /// </summary>
    /// <param name="image">图像对象</param>
    /// <param name="borderWidth">边框宽度</param>
    /// <param name="r">红色分量（0-255）</param>
    /// <param name="g">绿色分量（0-255）</param>
    /// <param name="b">蓝色分量（0-255）</param>
    /// <returns>添加边框后的新图像</returns>
    /// <exception cref="ArgumentNullException">当图像为null时抛出</exception>
    public static SKBitmap AddBorder(SKBitmap image, int borderWidth, int r, int g, int b)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "图像不能为 null");
        }

        if (borderWidth < 0)
        {
            throw new ArgumentException("边框宽度不能为负数", nameof(borderWidth));
        }

        ValidateColorValue(r, "r");
        ValidateColorValue(g, "g");
        ValidateColorValue(b, "b");

        int newWidth = image.Width + borderWidth * 2;
        int newHeight = image.Height + borderWidth * 2;

        var result = new SKBitmap(newWidth, newHeight);
        using var canvas = new SKCanvas(result);

        // 填充边框颜色
        canvas.Clear(new SKColor((byte)r, (byte)g, (byte)b));

        // 绘制原图像
        canvas.DrawBitmap(image, borderWidth, borderWidth);

        return result;
    }
}