// 测试类
/*
class Test{
   a <- 1
   b <- 2
   func init(c:int){
       PrintLine(c)
   }
}

a <- Test(1)
*/

Compiler("fib:int (a:int) ->
               {
                  if a == 1
                     return 1
                  elif a == 2
                     return 1
                  else
                     return fib(a-1)+fib(a-2)
               }")

PrintLine(fib(30))