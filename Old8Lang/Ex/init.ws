import Terminal
import other
a <- fib(20)
PrintLine(a)
b <- {12 123 1231 123 1}
b.Add(1)
PrintLine(b.ToStr())
class Test
   a <- 1
   b <- 2
   func init(c d)
      a <- c
      b <- d
   func Add()
      return a+b
pass
test <- Test(2 3)
PrintLine(test.Add())