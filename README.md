# Old8Lang

为西安建筑科技大学专门写的一个编程语言(bushi

西安建筑科技大学：https://baike.baidu.com/item/西安建筑科技大学/345895

西建大又被誉为“老八校”

本语言有两个实现，纯手撸的和使用csly框架实现的。

未来可能还会使用ply来实现python版本的解释器和编译器（2022.12.30 12h）

## 2022.11.22 建库

我一直想写一门编程语言，然后最近看到了一个C#写编译器的教程：https://www.bilibili.com/video/BV15v41147Zg （国内）/ https://www.youtube.com/watch?v=wgHIkdUQbp0&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y (国外)

然后我就想自己也写一个。

## 2022.11.22 晚

下个学期再写吧，这个学期先写一下Old8Down（类markdown,想用这个专门写文章）

链接：

[Old8Down 西建大专用标记语言](https://gitee.com/luckyfishisdashen/Old8Down)

这个标记语言我目前还没想好具体的语法，可能要寒假的时候才能写完。

现在的想法就是可以专门用来写文章，语法可能要改一下，毕竟我想让markdown不那么难用，或者说想让markdown小白一点

## Old8Lang示例：

```
a -> 1 // 赋值语句
1 <- b // 与 b -> 1是一样的
b -* a // b指向a , 即a,b共用一个a的Value
a *- b // 相反也是一样的
if a == b
   a <- 2
for a <- 1 , a < 3 ,a <- a + 1
   b <- b + 1
while not (a == 4)
   a <- a + 1
func c()
   d <- 4
class d
   d1 <- a
  
```

## Old8Lang关键字：

- if elif else => C# : if else if else
- for while => C# : for while

## 2022.12.30 12h

现在已经基本上写完了，但是只是一小部分，因为个人能力有限，现在先写成这个样子

已实现的：赋值语句，指向语句，if语句，for语句，while语句，func语句（还没有实现传参和返回功能），类实现（目前类里面方法功能还不太行）

未实现的：方法传参返回，继承，泛型，原生函数（也就是说只能通过变量储存器去观看变量）

未来还要写虚拟机但我已经忙好几天了，好累，等明年再说吧，现在连测试都还没开始，但应该可以使用。
