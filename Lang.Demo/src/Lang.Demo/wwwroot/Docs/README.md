# Old8Lang

为西安建筑科技大学专门写的一个编程语言(bushi

西安建筑科技大学：https://baike.baidu.com/item/西安建筑科技大学/345895

西建大又被誉为“老八校”

## 语言特性

1. 语法简单，不支持oop的高级功能：继承，多态，泛型等
2. 使用变量储存器来储存变量
3. 支持原生Json操作和更灵活的类型转换

## Old8Lang示例：

```py

// 开始到分支

a <- 1 // 我们可以使用 <- 来赋值
a <- "1" // 这个是String
a <- 1.0 // 这个是Double
a <- 'c' // 这个是Char
a <- true // 这个是Bool
a <- [1,2,3,4] // 这个是Array
a <- (1,2) // 这个是Tuple
a <- {1,2,32,3} // 这个是List
a <- {1:2,3:4} // 这个是字典

// 打印变量
PrintLine(a) // 打印a
PrintLine("Hello ","World") // Hello World
Print(a) //不换行
PrintLine() //换行

// 运算
a <- 1 + 2 // 3
a <- 1 - 2 // -1
a <- 1 * 2 // 2
a <- 1 / 2 // 0.5
a <- 1 + "a" // "1a"
a <- 1 + 'a' // b
a <- 1 == 1 // true
a <- 1 != 1 // false
a <- 1 > 1 // false
a <- 1 < 1 // false


// 分支
if 1 == 1 {
    PrintLine("1 == 1")
}else if 1 == 2 {
    PrintLine("1 == 2")
}else{
    PrintLine("else")
}

switch 1 {
    case 1:
        PrintLine("1")
    case 2:
        PrintLine("2")
    default:
        PrintLine("default")
}

// 循环

for i in [1,2,3,4,5] {
    Print(i+" ")
}

for i <- 1, i <= 5, i++{
    Print(i+" ")
}

i <- 1

while i < 5 {
    PrintLine("while")
}

// 函数
func main(){
    Print("Hello ")
}
main()

func m(a){
    PrintLine(a)
}
m("iOS Club")

// 返回值

func add(a, b){
    return a + b
}

PrintLine(add(1,2)) // 3

// lambda写法

min(a,b) -> {
    if a > b 
        return b
    return a
}

// 递归
fib(a) ->
{
   if a == 1
      return 1
   elif a == 2
      return 1
   else
      return fib(a-1)+fib(a-2)
}

PrintLine(fib(10)) // 55

// 类

class Person{
    name <- ""
    age <- 0
    init(Name) -> {
        name <- Name
    }
    print() -> {
        PrintLine("Name is "+name+" and age is "+age)
    }
    setAge(Age) -> {
        age <- Age
    }
}

p <- Person("Jack")
p.setAge(18)
p.print() // Name is Jack and age is 18

// lambda变量

a <- (a,b) => a+b
PrintLine(a(1,2)) // 3

// 类型转化
a <- {"name" : "老八","age" : 2}
a <- a as Person
a.print() // Name is 老八 and age is 2

// Json操作
a <- ToObj("{\"a\" : 1,\"b\" : {\"c\" : 3}}")
PrintLine(a.a) // 1

// 数组操作
a <- [1,2,3]
a[0] <- 2
PrintLine(a[0]) // 2
PrintLine(a[-1]) // 3
PrintLine(a[1:]) // 2,3
PrintLine(a[:2]) // 1,2

// 列表操作
b <- {1,2,3}
b.Add(4)
PrintLine(b) // 1,2,3,4
b.Remove(2) // 1,3,4
b.AddList({5,6,7})
PrintLine(b) // 1,3,4,5,6,7
b <- {2,3,4,5,1}
PrintLine(b.Sort())

// 字典操作
c <- {"a":1,"b":2}
PrintLine(c["a"])
c.Add("c",2)
PrintLine(c) // a:1,b:2,c:2

// 类型
a <- 5
Type(a) // int
// 或者调用Value的Type方法
a.ToType() // int

// 基本类型转化
a.ToInt() // 5
a.ToStr() // "5"

// 引用
import second
// 会调用second.ws的函数和类，但是不会运行second.ws文件的代码

m("iOS Club")
  
```

## 更新记录

### Old8Lang 0.8.0 版本

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

### Old8Lang 0.2.0 0.3.0版本

我们现在可以使用字典，列表，数组，元组（现在只支持而二元数组）。0.3.0版本则是对项目进行优化

```
a <- {1 2 3 4}//列表
b <- [1 2 3 4]//数组
c <- {(1:"1232") (2:"12345")}//字典
d <- (1 "asdf")
```

### Old8Lang 0.1.0 版本

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

### 2022.12.30 12h

现在已经基本上写完了，但是只是一小部分，因为个人能力有限，现在先写成这个样子

已实现的：赋值语句，指向语句，if语句，for语句，while语句，func语句（还没有实现传参和返回功能），类实现（目前类里面方法功能还不太行）

未实现的：方法传参返回，继承，泛型，原生函数（也就是说只能通过变量储存器去观看变量）

未来还要写虚拟机但我已经忙好几天了，好累，等明年再说吧，现在连测试都还没开始，但应该可以使用。

### 2022.11.22 晚

下个学期再写吧，这个学期先写一下Old8Down（类markdown,想用这个专门写文章）

链接：

[Old8Down 西建大专用标记语言](https://gitee.com/luckyfishisdashen/Old8Down)

这个标记语言我目前还没想好具体的语法，可能要寒假的时候才能写完。

现在的想法就是可以专门用来写文章，语法可能要改一下，毕竟我想让markdown不那么难用，或者说想让markdown小白一点

### 2022.11.22 建库

我一直想写一门编程语言，然后最近看到了一个C#写编译器的教程：https://www.bilibili.com/video/BV15v41147Zg （国内）/ https://www.youtube.com/watch?v=wgHIkdUQbp0&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y (国外)

然后我就想自己也写一个。

## 开发人员
1. LuckyFish

本项目归LuckyFish和西安建筑科技大学iOS众创空间俱乐部所有。