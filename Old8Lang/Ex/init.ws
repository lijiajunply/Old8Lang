import Terminal
import other

//测试函数
a <- fib(20)
PrintLine(a)
b <- {12,123,1231,123,1}
b.Add(1)
PrintLine(b.ToStr())

// Json 测试
test <- Test(2,3)
PrintLine(Json(test))
a <- ToObj("{\"a\":1, \"b\":2}")

// 测试类型转换
a <- a as Test
PrintLine(a.Add())
a <- {"a":1 "b":3}
a <- a as Test

// 测试匿名函数
a <- (c,b) => c + b
PrintLine(a(5,2))