// 函数到类

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