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

请查看 [CHANGELOG.md](./CHANGELOG.md) 文件获取详细的更新记录。

## 开发人员
1. LuckyFish

本项目归LuckyFish和西安建筑科技大学iOS众创空间俱乐部所有。