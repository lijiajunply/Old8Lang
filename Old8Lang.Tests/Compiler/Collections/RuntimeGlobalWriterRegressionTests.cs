using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Collections;

public class RuntimeGlobalWriterRegressionTests
{
    [Fact]
    public void Write_CyclicCollectionsAndAliases_PreservesReferences()
    {
        var list = new List<object>();
        list.Add(list);
        var dictionary = new Dictionary<string, object> { ["self"] = null!, ["list"] = list };
        dictionary["self"] = dictionary;

        var manager = new VariateManager();
        RuntimeGlobalWriter.SetManager(manager);
        RuntimeGlobalWriter.Write("list", list);
        RuntimeGlobalWriter.Write("map", dictionary);
        RuntimeGlobalWriter.Write("alias", list);

        var wrappedList = Assert.IsType<ListLangValue>(manager.GetValue(new LangId("list")));
        var wrappedMap = Assert.IsType<DictionaryLangValue>(manager.GetValue(new LangId("map")));
        Assert.Same(wrappedList, wrappedList.Values[0]);
        Assert.Same(wrappedList, manager.GetValue(new LangId("alias")));
        Assert.Same(wrappedList, wrappedMap.Get(new StringLangValue("list")));
        Assert.Same(wrappedMap, wrappedMap.Get(new StringLangValue("self")));
    }
}
