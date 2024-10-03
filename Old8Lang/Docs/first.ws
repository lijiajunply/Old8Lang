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