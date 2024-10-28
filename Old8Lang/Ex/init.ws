PrintLine("引用模块\n")
import Terminal
import other

// 测试Json
PrintLine("测试Json")
test <- Test(2,3)
PrintLine(Json(test))
a <- ToObj("{\"a\" : 1,\"b\" : {\"c\" : 3}}")
PrintLine(a.a)

err <- ${a.a}{1}{2}
PrintLine(err)

//测试函数 测试For in
PrintLine("测试函数")
a <- fib2(20)
a <- fibIter(35)
for i in a {
    Print(i+" ")
}
b <- {12,123,1231,123,1}
b.Add(2)
PrintLine("\n"+b.ToStr())

// 测试类型转换
PrintLine("测试类型转换")
a <- {"a":1,"b":3}
a <- a as Test
PrintLine(a.Add())

// 测试匿名函数
PrintLine("测试lambda")
a <- (c,b) => c + b
PrintLine(a(5,2))

// 测试数组
PrintLine("测试数组")
a <- [1,2,3]
PrintLine(a[0:2].ToStr())

// 测试switch
//a <- ReadLine()
PrintLine("测试switch")
switch "2" {
    case "1"
        PrintLine("1")
    case "2"
        PrintLine("2")
    default
        PrintLine("default")
}