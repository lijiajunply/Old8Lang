# 更新记录

## Old8Lang 0.8.0 版本

1. 修复以往Bug
2. 加入Json操作和基本方法
3. 使用反射来支持自定义方法
4. 加入类型转换
5. 将缩进解析转变为大括号块

完成时间：2024年10月4日

这个项目从22年立项以来，已经快2年了。

这两年的时间我逐步完善了Old8Lang，修了很多的Bug，添加了很多的功能。
但是一直停留在解释器和csly这里。
所以在未来的一段时间里，我可能会先完成自己的前端（即代码文本解析）。
然后就是对于递归的优化。

## Old8Lang 0.2.0 0.3.0版本

我们现在可以使用字典，列表，数组，元组（现在只支持而二元数组）。0.3.0版本则是对项目进行优化

```
a <- {1 2 3 4}//列表
b <- [1 2 3 4]//数组
c <- {(1:"1232") (2:"12345")}//字典
d <- (1 "asdf")
```

## Old8Lang 0.1.0 版本

在0.1.0版本中，可以使用原生函数和引用语句：

```
import os
import console
import net
import math

[import "console.dll" console Write print]
[import "console.dll" console WriteLine printline]
```

引用语句会引用相关内容，使其类和方法加载到该文件上：

import `<context>`

原生函数需要使用到C#的dll，该语法需要3~4个参数：

[import `<dllname> <classname> <methodname> <nativemethodname>`]

## 2022.12.30 12h

现在已经基本上写完了，但是只是一小部分，因为个人能力有限，现在先写成这个样子

已实现的：赋值语句，指向语句，if语句，for语句，while语句，func语句（还没有实现传参和返回功能），类实现（目前类里面方法功能还不太行）

未实现的：方法传参返回，继承，泛型，原生函数（也就是说只能通过变量储存器去观看变量）

未来还要写虚拟机但我已经忙好几天了，好累，等明年再说吧，现在连测试都还没开始，但应该可以使用。

## 2022.11.22 晚

下个学期再写吧，这个学期先写一下Old8Down（类markdown,想用这个专门写文章）

链接：

[Old8Down 西建大专用标记语言](https://gitee.com/luckyfishisdashen/Old8Down)

这个标记语言我目前还没想好具体的语法，可能要寒假的时候才能写完。

现在的想法就是可以专门用来写文章，语法可能要改一下，毕竟我想让markdown不那么难用，或者说想让markdown小白一点

## 2022.11.22 建库

我一直想写一门编程语言，然后最近看到了一个C#写编译器的教程：https://www.bilibili.com/video/BV15v41147Zg （国内）/ https://www.youtube.com/watch?v=wgHIkdUQbp0&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y (国外)

然后我就想自己也写一个。
