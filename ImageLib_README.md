# Old8Lang 图像处理库 (ImageLib)

## 概述

ImageLib 是 Old8Lang 的图像处理标准库，提供了丰富的图像加载、保存、变换、调整、滤镜和绘制功能。

## 功能特性

### 1. 基础图像操作
- **LoadImage(path)** - 从文件加载图像
- **SaveImage(image, path, format)** - 保存图像到文件（支持 png、jpg、bmp、gif）
- **CreateImage(width, height, r, g, b)** - 创建空白图像
- **CloneImage(image)** - 克隆图像
- **DisposeImage(image)** - 释放图像资源

### 2. 图像信息获取
- **GetWidth(image)** - 获取图像宽度
- **GetHeight(image)** - 获取图像高度
- **GetSize(image)** - 获取图像尺寸 [width, height]
- **GetPixel(image, x, y)** - 获取像素颜色 [R, G, B, A]
- **SetPixel(image, x, y, r, g, b, a)** - 设置像素颜色
- **GetPixelFormat(image)** - 获取像素格式
- **GetDominantColor(image, sampleRate)** - 获取主要颜色

### 3. 图像变换
- **ResizeImage(image, width, height, highQuality)** - 调整图像大小
- **ScaleImage(image, scale, highQuality)** - 按比例缩放图像
- **RotateImage(image, angle)** - 旋转图像（角度，正值为顺时针）
- **CropImage(image, x, y, width, height)** - 裁剪图像
- **FlipHorizontal(image)** - 水平翻转
- **FlipVertical(image)** - 垂直翻转

### 4. 图像调整
- **AdjustBrightness(image, brightness)** - 调整亮度（-255 到 255）
- **AdjustContrast(image, contrast)** - 调整对比度（-100 到 100）
- **AdjustSaturation(image, saturation)** - 调整饱和度（-100 到 100）
- **AdjustHue(image, hueShift)** - 调整色调（-180 到 180 度）

### 5. 图像滤镜
- **ToGrayscale(image)** - 转换为灰度图像
- **InvertColors(image)** - 反转颜色（负片效果）
- **ApplyBlur(image, radius)** - 模糊滤镜（半径 1-10）
- **ApplySharpen(image, strength)** - 锐化滤镜（强度 1-10）
- **DetectEdges(image, threshold)** - 边缘检测（Sobel 算子，阈值 0-255）

### 6. 图像绘制
- **DrawRectangle(image, x, y, width, height, r, g, b, lineWidth, fill)** - 绘制矩形
- **DrawCircle(image, centerX, centerY, radius, r, g, b, lineWidth, fill)** - 绘制圆形
- **DrawLine(image, x1, y1, x2, y2, r, g, b, lineWidth)** - 绘制直线
- **DrawText(image, text, x, y, fontSize, r, g, b, fontFamily)** - 绘制文本
- **DrawPolygon(image, points, r, g, b, lineWidth, fill)** - 绘制多边形

### 7. 高级操作
- **ConvertFormat(sourcePath, targetPath, targetFormat)** - 转换图像格式
- **MergeImages(baseImage, overlayImage, x, y, alpha)** - 合并图像
- **ConcatHorizontal(images, spacing)** - 水平拼接图像
- **ConcatVertical(images, spacing)** - 垂直拼接图像
- **AddBorder(image, borderWidth, r, g, b)** - 添加边框

## 使用示例

### 创建和保存图像

```old8
// 创建一个 800x600 的白色图像
img <- ImageLib.CreateImage(800, 600, 255, 255, 255)

// 保存为 PNG 格式
ImageLib.SaveImage(img, "output.png", "png")

// 释放资源
ImageLib.DisposeImage(img)
```

### 加载和处理图像

```old8
// 加载图像
img <- ImageLib.LoadImage("input.png")

// 获取图像信息
width <- ImageLib.GetWidth(img)
height <- ImageLib.GetHeight(img)
PrintLine($"尺寸: {width}x{height}")

// 转换为灰度
gray <- ImageLib.ToGrayscale(img)
ImageLib.SaveImage(gray, "gray.png", "png")

// 释放资源
ImageLib.DisposeImage(img)
ImageLib.DisposeImage(gray)
```

### 图像变换

```old8
img <- ImageLib.LoadImage("photo.jpg")

// 缩小到原来的 50%
small <- ImageLib.ScaleImage(img, 0.5, true)

// 旋转 45 度
rotated <- ImageLib.RotateImage(img, 45.0)

// 裁剪区域
cropped <- ImageLib.CropImage(img, 100, 100, 300, 200)

// 保存结果
ImageLib.SaveImage(small, "small.png", "png")
ImageLib.SaveImage(rotated, "rotated.png", "png")
ImageLib.SaveImage(cropped, "cropped.png", "png")
```

### 图像调整

```old8
img <- ImageLib.LoadImage("photo.jpg")

// 增加亮度
bright <- ImageLib.AdjustBrightness(img, 50)

// 增加对比度
contrast <- ImageLib.AdjustContrast(img, 30.0)

// 增加饱和度
saturated <- ImageLib.AdjustSaturation(img, 50.0)

// 改变色调
hue <- ImageLib.AdjustHue(img, 60.0)

// 保存结果
ImageLib.SaveImage(bright, "bright.png", "png")
```

### 图像滤镜

```old8
img <- ImageLib.LoadImage("photo.jpg")

// 模糊效果
blurred <- ImageLib.ApplyBlur(img, 5)

// 锐化效果
sharpened <- ImageLib.ApplySharpen(img, 7)

// 边缘检测
edges <- ImageLib.DetectEdges(img, 100)

ImageLib.SaveImage(edges, "edges.png", "png")
```

### 图像绘制

```old8
// 创建画布
canvas <- ImageLib.CreateImage(800, 600, 255, 255, 255)

// 绘制红色矩形边框
ImageLib.DrawRectangle(canvas, 50, 50, 200, 150, 255, 0, 0, 3, false)

// 绘制蓝色填充圆形
ImageLib.DrawCircle(canvas, 400, 300, 80, 0, 0, 255, 1, true)

// 绘制绿色直线
ImageLib.DrawLine(canvas, 50, 400, 750, 400, 0, 255, 0, 2)

// 绘制文本
ImageLib.DrawText(canvas, "Hello, Old8Lang!", 300, 500, 24, 0, 0, 0, "Arial")

// 绘制三角形
points <- [400, 50, 500, 200, 300, 200]
ImageLib.DrawPolygon(canvas, points, 255, 128, 0, 2, false)

ImageLib.SaveImage(canvas, "drawing.png", "png")
```

### 图像拼接

```old8
img1 <- ImageLib.LoadImage("photo1.jpg")
img2 <- ImageLib.LoadImage("photo2.jpg")
img3 <- ImageLib.LoadImage("photo3.jpg")

images <- [img1, img2, img3]

// 水平拼接，间隔 10 像素
hconcat <- ImageLib.ConcatHorizontal(images, 10)
ImageLib.SaveImage(hconcat, "horizontal.png", "png")

// 垂直拼接，间隔 10 像素
vconcat <- ImageLib.ConcatVertical(images, 10)
ImageLib.SaveImage(vconcat, "vertical.png", "png")
```

### 添加边框

```old8
img <- ImageLib.LoadImage("photo.jpg")

// 添加 20 像素宽的金色边框
bordered <- ImageLib.AddBorder(img, 20, 255, 215, 0)
ImageLib.SaveImage(bordered, "bordered.png", "png")
```

## 颜色说明

所有颜色参数使用 RGB 格式，每个分量的范围是 0-255：
- **r** - 红色分量（0-255）
- **g** - 绿色分量（0-255）
- **b** - 蓝色分量（0-255）
- **a** - 透明度分量（0-255，可选）

常见颜色示例：
- 红色: (255, 0, 0)
- 绿色: (0, 255, 0)
- 蓝色: (0, 0, 255)
- 白色: (255, 255, 255)
- 黑色: (0, 0, 0)
- 黄色: (255, 255, 0)
- 品红: (255, 0, 255)
- 青色: (0, 255, 255)
- 金色: (255, 215, 0)

## 支持的图像格式

- **PNG** (.png) - 支持透明度
- **JPEG** (.jpg, .jpeg) - 有损压缩
- **BMP** (.bmp) - 位图格式
- **GIF** (.gif) - 支持动画（仅读取第一帧）

## 注意事项

1. **资源管理**: 使用完图像后应调用 `DisposeImage()` 释放资源
2. **平台兼容性**: 图像处理功能在 Windows 上开箱即用，在 Linux/macOS 上需要安装 libgdiplus
3. **性能**: 某些操作（如模糊、锐化）对大图像可能较慢
4. **内存使用**: 处理大图像时注意内存占用

## Linux/macOS 安装依赖

### Ubuntu/Debian
```bash
sudo apt-get install libgdiplus
```

### macOS
```bash
brew install mono-libgdiplus
```

### Arch Linux
```bash
sudo pacman -S libgdiplus
```

## 完整示例

请参考 `ImageLibExample.old8` 文件查看更多使用示例。

## 相关文档

- [Old8Lang 语法文档](Old8Lang_Grammar.md)
- [Old8Lang EBNF](Old8Lang/Old8Lang.ebnf)
- [标准库文档](README.md)
