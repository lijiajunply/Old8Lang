// 基本计算和赋值

a <- 123+1
a <- 1233
b <- a >= 1233 
PrintLine(b)

// if语句
if a == 123
  PrintLine("123")
elif a == 1234
  PrintLine("1234")
elif a == 1233
  PrintLine("1233")
else
  PrintLine("false")

// for语句


for a <- 1,a <= 5,a++{
    PrintLine(a)
}


// switch语句

switch a {
    case 123
        PrintLine("1")
    case 1233
        PrintLine("2")
    default
        PrintLine("default")
}

for i in [1~3] {
    PrintLine(i)
}

func main(a) {
    PrintLine(a)
}

main("asdf")