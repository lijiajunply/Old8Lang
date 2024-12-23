// 测试类
class Test{
   a <- 2
   b <- 2
   func Add:int (c:int,d:int){
      a <- c+d
      return a
   }
   func init(a){
       PrintLine(a)
   }
}

arr <- [1,2,3,4]
arr[0] <- 21
PrintLine(arr[0])

a <- Test("asdf")
PrintLine(a.Add(1,2))
a.a <- 23
PrintLine(a.a)