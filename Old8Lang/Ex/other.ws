fib(a) ->
{
   if a == 1
      return 1
   elif a == 2
      return 1
   else
      return fib(a-1)+fib(a-2)
}
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