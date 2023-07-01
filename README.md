# Old8Lang

为西安建筑科技大学专门写的一个编程语言(bushi

西安建筑科技大学：https://baike.baidu.com/item/西安建筑科技大学/345895

西建大又被誉为“老八校”

本语言有两个实现，纯手撸的和使用csly框架实现的。

未来可能还会使用ply来实现python版本的解释器和编译器（2022.12.30 12h）

## Old8Lang示例：

```
a <- 1 //将1赋值给a
a -> b //将a赋值给b
//在Old8Lang，变量更像是C中的指针，为了好看，这里使用 -> 或 <-

// if,for,while
if a == 1
   b <- 2 
for c <- 1,c<=2,c++
   PrintLine(b)
while(a<=2)
   PrintLine(b)
//

//类
class Old8LangClass
   _a <- 1
   _b <- 2
   init(old a , old b) ->
      a -> _a 
      b -> _b 
   ADD() ->
      return _a + _b
   ADD_1() => _a + _b


//类的实例：
a <- Old8LangClass(1,2) // 运行init函数          
_ <- a.ADD() == a.ADD_1() // ture
a.ADD() // 3 

//彩蛋函数：
a.XAUAT
//输出：西建大还我血汗钱我要回家

//数组：
a <- []
a <- [1 2 1]
PrintLine(a[0]) //为a数组的第一个
PrintLine(a[-1]) //为最后一个
b <- a[0:1] //取 a[n](0<=n<1)

//type:
typeof(a) // a : Func
a <- 5
typeof(a) // a : int

//使用原生函数：
[import "Old8LangLib" File FileRead]

[import:"Old8LangLib" File FileRead]
FileRead(file) ->
   base <- base + "\n" // base 就是原函数结果
   return base
a <- FileRead
  
```

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


## Old8Lang关键字：

- if elif else => C# : if else if else
- for while => C# : for while
