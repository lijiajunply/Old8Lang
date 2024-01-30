class a
   l <- 1
   r <- 2
   add() => l+r
fib(a) ->
   if a == 1
      return 1
   elif a == 2
      return 1
   else
      return fib(a-1)+fib(a-2)
b <- 1