# Old8Lang
为西安建筑科技大学专门写的一个编程语言(bushi
    
西安建筑科技大学：https://baike.baidu.com/item/西安建筑科技大学/345895
  
西建大又被誉为“老八校”
  
## 2022.11.22 建库
  
我一直想写一门编程语言，然后最近看到了一个C#写编译器的教程：https://www.bilibili.com/video/BV15v41147Zg （国内）/ https://www.youtube.com/watch?v=wgHIkdUQbp0&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y (国外)
  
然后我就想自己也写一个。

## 2022.11.22 晚
  
下个学期再写吧，这个学期先写一下Old8Down（类markdown,想用这个专门写文章）
  
链接： 
  
[Old8Down 西建大专用标记语言](https://gitee.com/luckyfishisdashen/Old8Down)
  
这个标记语言我目前还没想好具体的语法，可能要寒假的时候才能写完。
  
现在的想法就是可以专门用来写文章，语法可能要改一下，毕竟我想让markdown不那么难用，或者说想让markdown小白一点

## Old8Lang示例：
```cs
old a <- 1 //将1赋值给a
a -> old b //将a赋值给b
//在Old8Lang，变量更像是C中的指针，为了好看，这里使用 -> 或 <-

if(a == 1) {
    b <- 2 
} 

for(old c <- 1;c<=2:c++){ 
    Console.WriteLine(b)
} 

while(a<=2){
    Console.WriteLine(b)
} 

eight Old8LangClass{ 
    old _a , _b
    Old8LangClass(old a , old b){ 
        a -> XATAT._a 
        b -> XATAT._b 
    } 
    ADD(){
        old c <- _a + _b
        return c
    }
    ADD_1() => _a + _b
}  

//类的实例：
a <- new Old8LangClass(1,2)                       
a.ADD() == a.ADD_1() // ture
a <- a.ADD
a.ADD() // 3 
```


## Old8Lang关键字：
- old => 声明变量
- eight => 类声明
- XAUAT => 类里的this
- null  => C# : null
- if for while => C# : if for while