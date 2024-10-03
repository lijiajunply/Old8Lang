// 函数：斐波那契
fib(a) ->
{
   if a == 1
      return 1
   elif a == 2
      return 1
   else
      return fib(a-1)+fib(a-2)
}

fibIter(a) -> {
    list <- {1,1}
    for i <- 2 , i < a , i++
    {
        b <- list[i-1]+list[i-2]
        list.Add(b)
    }
    return list
}

fib2(a) -> {
    Last <- 1
    Second <- 1
    Now <- 1
    for i <- 2 , i < a , i++{
        Now <- Last + Second
        Second <- Last
        Last <- Now
    }
    return Now
}

// 测试类
class Test{
   a <- 1
   b <- 2
   func init(c,d)
   {
      a <- c
      b <- d
   }
   func Add()
      return a+b
}