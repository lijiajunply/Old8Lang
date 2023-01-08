# Interpreter

在上一章我们已经写好了Paeser，将语句通过EBNF转成了OldLangTree这一基础类，现在我们要使用csly来获取OldLangTree结果来运行我们的程序了。

```csharp
ParserBuilder<OldTokenGeneric, OldLangTree> Parser = new ParserBuilder<OldTokenGeneric, OldLangTree>();
OldParser oldParser = new OldParser();
```

我们先定义一个ParserBuilder实例和一个Parser实例。

```csharp
 var buildResult = Parser.BuildParser(oldParser,ParserType.EBNF_LL_RECURSIVE_DESCENT).Result;
```

这里我们定义了一个变量buildResult，重点讲一下后面这个函数该如何使用：

`<Parser实例>,<parser类型：bnf和ebnf>,<开始语句，一般可以通过在Parser.cs中定义一下>`

```csharp
 var r = buildResult.Parse(Code);
 var RUN = r.Result;
```

这里先使用Parse函数获取一个包装类，该函数传入我们的代码（string类型），然后在使用Result来获取我们的OldLangTree实例

现在，我们拿到了OldLangTree实例，然后就可以做我们想做的事情了。

```csharp
//全部代码：
ParserBuilder<OldTokenGeneric, OldLangTree> Parser = new ParserBuilder<OldTokenGeneric, OldLangTree>();
OldParser oldParser = new OldParser();
var buildResult = Parser.BuildParser(oldParser,ParserType.EBNF_LL_RECURSIVE_DESCENT).Result;
var r = buildResult.Parse(Code);
var RUN = r.Result;
if (r.Errors !=null && r.Errors.Any())
{
    // display errors
    r.Errors.ForEach(error => Error.Add(error.ErrorMessage)); ;
}
else
{
    var run = RUN as OldStatement;
    run.Run(ref Manager);
}
```
