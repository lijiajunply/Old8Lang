import Terminal
import other

//测试函数
a <- fib(20)
PrintLine(a)
b <- {12,123,1231,123,1}
b.Add(1)
PrintLine(b.ToStr())

// 测试Json
test <- Test(2,3)
PrintLine(Json(test))
a <- ToObj("{\"a\":1, \"b\":2}")

// 测试类型转换
a <- a as Test
PrintLine(a.Add())
a <- {"a":1,"b":3}
a <- a as Test

// 测试匿名函数
a <- (c,b) => c + b
PrintLine(a(5,2))

// 测试数组
a <- [1,2,3]
PrintLine(a.ToStr())
PrintLine(a[0:2].ToStr())

// 测试switch
//a <- ReadLine()
switch "2" {
    case "1"
        PrintLine("1")
    case "2"
        PrintLine("2")
    default
        PrintLine("default")
}

// 测试For in
for i in [1,2,3] {
    PrintLine(i)
}