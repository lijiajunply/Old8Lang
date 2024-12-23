// 测试类
class Test{
   a <- 2
   b <- 2
   func Add:int (c:int,d:int){
      this.a <- c+d
      return this.a
   }
   func init(a){
       PrintLine(a)
   }
}

/*
arr <- {12,31,2}
PrintLine(Len(arr))
//arr.Add(1)
*/

/*
arr[0] <- 21
PrintLine(arr[0])
*/

a <- Test("asdf")
PrintLine(a.Add(1,2))
PrintLine(a.a)

/*

*/