using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Old8Lang.Tests.LanguageServer.Handlers.Navigation;

/// <summary>
/// TypeHierarchyHandler 单元测试：覆盖类继承（extends）、接口实现（implements）、
/// Mixin 混入（with）以及跨文件 supertypes / subtypes 查询。
/// </summary>
public class TypeHierarchyHandlerTests
{
    [Fact]
    public async Task PrepareSupertypesAndSubtypes_HandlesInheritanceInterfacesAndMixins()
    {
        var baseCode =
            "interface ISpeakable {\n" +
            "    func speak() -> string\n" +
            "}\n" +
            "mixin Timestamped {\n" +
            "    public createdAt: int <- 0\n" +
            "}\n" +
            "class Animal implements ISpeakable {\n" +
            "    public func speak() -> string { return \"...\" }\n" +
            "}\n";

        var derivedCode =
            "class Dog extends Animal with Timestamped {\n" +
            "    public func bark() -> string { return \"woof\" }\n" +
            "}\n" +
            "class Cat extends Animal {\n" +
            "    public func meow() -> string { return \"meow\" }\n" +
            "}\n" +
            "pet <- Dog()\n";

        var documentManager = new DocumentManager();
        var workspaceIndex = new WorkspaceIndex(documentManager);
        documentManager.AttachWorkspaceIndex(workspaceIndex);

        const string baseUri = "file:///type-base.old8";
        const string derivedUri = "file:///type-derived.old8";
        documentManager.UpdateDocument(baseUri, baseCode);
        documentManager.UpdateDocument(derivedUri, derivedCode);

        var handler = new TypeHierarchyHandler(documentManager, workspaceIndex);

        // Prepare on `Dog` (line 0, col 6)
        var dogPrepared = await handler.Handle(new TypeHierarchyPrepareParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.From(derivedUri)),
            Position = new Position(0, 7)
        }, CancellationToken.None);

        Assert.NotNull(dogPrepared);
        var dogItem = Assert.Single(dogPrepared);
        Assert.Equal("Dog", dogItem.Name);
        Assert.Equal(SymbolKind.Class, dogItem.Kind);

        // Supertypes of `Dog`: Animal + Timestamped (cross-file)
        var dogSupers = await handler.Handle(new TypeHierarchySupertypesParams
        {
            Item = dogItem
        }, CancellationToken.None);

        Assert.NotNull(dogSupers);
        Assert.Contains(dogSupers, s => s.Name == "Animal" && s.Uri.ToString() == baseUri);
        Assert.Contains(dogSupers, s => s.Name == "Timestamped" && s.Uri.ToString() == baseUri);

        // Supertypes of `Animal`: ISpeakable
        var animalItem = dogSupers.First(s => s.Name == "Animal");
        var animalSupers = await handler.Handle(new TypeHierarchySupertypesParams
        {
            Item = animalItem
        }, CancellationToken.None);

        Assert.NotNull(animalSupers);
        var ifaceItem = Assert.Single(animalSupers);
        Assert.Equal("ISpeakable", ifaceItem.Name);
        Assert.Equal(SymbolKind.Interface, ifaceItem.Kind);

        // Subtypes of `Animal`: Dog and Cat (cross-file)
        var animalSubs = await handler.Handle(new TypeHierarchySubtypesParams
        {
            Item = animalItem
        }, CancellationToken.None);

        Assert.NotNull(animalSubs);
        Assert.Contains(animalSubs, s => s.Name == "Dog" && s.Uri.ToString() == derivedUri);
        Assert.Contains(animalSubs, s => s.Name == "Cat" && s.Uri.ToString() == derivedUri);

        // Subtypes of `ISpeakable`: Animal
        var ifaceSubs = await handler.Handle(new TypeHierarchySubtypesParams
        {
            Item = ifaceItem
        }, CancellationToken.None);
        Assert.Contains(ifaceSubs!, s => s.Name == "Animal");

        // Prepare on variable `pet` (line 6, col 1) resolves to its class `Dog`
        var petPrepared = await handler.Handle(new TypeHierarchyPrepareParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.From(derivedUri)),
            Position = new Position(6, 1)
        }, CancellationToken.None);
        Assert.Equal("Dog", Assert.Single(petPrepared!).Name);
    }
}
