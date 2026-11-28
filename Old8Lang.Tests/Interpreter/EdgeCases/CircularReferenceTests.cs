using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.EdgeCases;

/// <summary>
/// 循环引用与递归深度测试 - 覆盖数据结构循环引用、函数递归深度限制与类实例循环引用
/// </summary>
[Collection("Sequential")]
public class CircularReferenceTests
{
    #region 数据结构循环引用

    [Fact]
    public void CircularReference_SelfReferencingList_PreservesIdentityAndNestedAccess()
    {
        var code = @"
items <- {10, 20}
items.Add(items)

v0 <- items[0]
nested0 <- items[2][0]
deep1 <- items[2][2][1]
count <- items.Count()
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal(10, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("v0"))).Value);
        Assert.Equal(10, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("nested0"))).Value);
        Assert.Equal(20, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("deep1"))).Value);
        Assert.Equal(3, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("count"))).Value);
    }

    [Fact]
    public void CircularReference_MutuallyReferencingLists_MaintainsLiveMutations()
    {
        var code = @"
a <- {1}
b <- {2}
a.Add(b)
b.Add(a)

b[0] <- 99
throughCycle <- a[1][1][1][0]
backToA <- b[1][0]
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal(99, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("throughCycle"))).Value);
        Assert.Equal(1, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("backToA"))).Value);
    }

    [Fact]
    public void CircularReference_SelfReferencingDictionary_AllowsTraversalAndUpdates()
    {
        var code = @"
node <- {""name"": ""root"", ""count"": 1}
node[""self""] <- node
node[""self""][""count""] <- 5

rootName <- node[""self""][""self""][""name""]
updatedCount <- node[""count""]
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal("root", Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("rootName"))).Value);
        Assert.Equal(5, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("updatedCount"))).Value);
    }

    #endregion

    #region 函数递归深度

    [Fact]
    public void RecursionDepth_WithinLimit_ExecutesSuccessfully()
    {
        var code = @"
func sumDown(n:int) -> int {
    if n <= 0 {
        return 0
    }
    return n + sumDown(n - 1)
}
result <- sumDown(100)
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal(5050, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("result"))).Value);
    }

    [Fact]
    public void RecursionDepth_DirectInfiniteRecursion_ThrowsRecursionLimitError()
    {
        var code = @"
func recurseForever(n:int) -> int {
    return recurseForever(n + 1)
}
res <- recurseForever(1)
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        var ex = Assert.Throws<RecursionError>(() => ast.Run(interpreter.Manager));
        Assert.NotNull(ex);
    }

    [Fact]
    public void RecursionDepth_MutualRecursion_SucceedsWithinLimitAndThrowsAtLimit()
    {
        var okCode = @"
func isEven(n:int) -> bool {
    if n == 0 { return true }
    return isOdd(n - 1)
}
func isOdd(n:int) -> bool {
    if n == 0 { return false }
    return isEven(n - 1)
}
r1 <- isEven(30)
r2 <- isOdd(31)
";
        var okInterp = new LangInterpreter();
        okInterp.Build(okCode).Run(okInterp.Manager);
        Assert.True(Assert.IsType<BoolLangValue>(okInterp.Manager.GetValue(new LangId("r1"))).Value);
        Assert.True(Assert.IsType<BoolLangValue>(okInterp.Manager.GetValue(new LangId("r2"))).Value);

        var infCode = @"
func ping(n:int) -> int {
    return pong(n + 1)
}
func pong(n:int) -> int {
    return ping(n + 1)
}
res <- ping(1)
";
        var infInterp = new LangInterpreter();
        var ast = infInterp.Build(infCode);

        var ex = Assert.Throws<RecursionError>(() => ast.Run(infInterp.Manager));
        Assert.NotNull(ex);
    }

    #endregion

    #region 类实例循环引用

    [Fact]
    public void CircularReference_SelfReferencingClassInstance_TraversesAndUpdatesFields()
    {
        var code = @"
class Node {
    public value:int <- 0
    public next:object <- null

    func init(v:int) {
        this.value <- v
    }
}

n <- Node(42)
n.next <- n

v1 <- n.next.value
v2 <- n.next.next.value
n.next.value <- 77
updated <- n.value
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal(42, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("v1"))).Value);
        Assert.Equal(42, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("v2"))).Value);
        Assert.Equal(77, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("updated"))).Value);
    }

    [Fact]
    public void CircularReference_TwoNodeCycle_TraversesRingAndBreaksCleanly()
    {
        var code = @"
class RingNode {
    public id:string <- """"
    public peer:object <- null

    func init(id:string) {
        this.id <- id
    }
}

a <- RingNode(""A"")
b <- RingNode(""B"")
a.peer <- b
b.peer <- a

fromA <- a.peer.id
backToA <- a.peer.peer.id

// 解除循环引用
b.peer <- null
afterBreak <- b.peer == null
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal("B", Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("fromA"))).Value);
        Assert.Equal("A", Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("backToA"))).Value);
        Assert.True(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("afterBreak"))).Value);
    }

    #endregion
}
