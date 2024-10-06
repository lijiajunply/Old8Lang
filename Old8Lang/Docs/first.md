# Old8Lang 示范

## 变量和赋值

``` old8lang
a <- 1 // 我们可以使用 <- 来赋值
a <- "1" // 这个是String
a <- 1.0 // 这个是Double
a <- 'c' // 这个是Char
a <- true // 这个是Bool
a <- [1,2,3,4] // 这个是Array
a <- (1,2) // 这个是Tuple
a <- {1,2,32,3} // 这个是List
a <- {1:2,3:4} // 这个是字典
```

这里我们使用`<-`来进行赋值。就相当于其他语言中的`=`。

## 打印变量

```old8lang
PrintLine(a) // 打印a
PrintLine("Hello ","World") // Hello World
Print(a) //不换行
PrintLine() //换行
```

我们在Old8Lang中内置了`PrintLine`和`Print`两个函数，用来打印变量。