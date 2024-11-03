// 数组操作 , 类型 , 引用

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