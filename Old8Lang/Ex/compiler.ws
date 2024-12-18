// 基本计算和赋值

a <- 123
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

Last <- 1
Second <- 1
Now <- 1
for i <- 2 , i < 35 , i++{
    Now <- Last + Second
    Second <- Last
    Last <- Now
}
PrintLine(Now)