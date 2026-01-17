using Old8Lang.LanguageServer.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Services;

public partial class SymbolInfoTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void TestSymbolInfo_BasicProperties()
    {
        // Arrange
        var location = new SourceLocation
        {
            Uri = "file:///test.old8",
            Line = 10,
            Column = 5,
            EndLine = 10,
            EndColumn = 15
        };

        var symbol = new SymbolInfo
        {
            Name = "testSymbol",
            Kind = SymbolKind.Function,
            Type = "string",
            Location = location,
            AccessModifier = AccessModifier.Public,
            IsStatic = false,
            References = [],
            Members = new Dictionary<string, SymbolInfo>()
        };

        // Assert
        Assert.Equal("testSymbol", symbol.Name);
        Assert.Equal(SymbolKind.Function, symbol.Kind);
        Assert.Equal("string", symbol.Type);
        Assert.NotNull(symbol.Location);
        Assert.Equal("file:///test.old8", symbol.Location.Uri);
        Assert.Equal(10, symbol.Location.Line);
        Assert.Equal(5, symbol.Location.Column);
        Assert.Equal(10, symbol.Location.EndLine);
        Assert.Equal(15, symbol.Location.EndColumn);
        Assert.Equal(AccessModifier.Public, symbol.AccessModifier);
        Assert.False(symbol.IsStatic);
        Assert.NotNull(symbol.References);
        Assert.Empty(symbol.References);
        Assert.Empty(symbol.Members);
    }

    [Fact]
    public void TestSymbolInfo_StaticProperty()
    {
        // Arrange
        var location = new SourceLocation
        {
            Uri = "file:///test.old8",
            Line = 1,
            Column = 1,
            EndLine = 1,
            EndColumn = 20
        };

        var symbol = new SymbolInfo
        {
            Name = "staticProperty",
            Kind = SymbolKind.Property,
            Type = "int",
            Location = location,
            AccessModifier = AccessModifier.Public,
            IsStatic = true
        };
        symbol.Location = location;

        // Assert
        Assert.Equal("staticProperty", symbol.Name);
        Assert.Equal(SymbolKind.Property, symbol.Kind);
        Assert.Equal("int", symbol.Type);
        Assert.True(symbol.IsStatic);
        Assert.Equal(AccessModifier.Public, symbol.AccessModifier);
        Assert.NotNull(symbol.Location);
    }

    [Fact]
    public void TestSymbolInfo_PrivateMethod()
    {
        // Arrange
        var location = new SourceLocation
        {
            Uri = "file:///test.old8",
            Line = 5,
            Column = 10,
            EndLine = 8,
            EndColumn = 20
        };

        var symbol = new SymbolInfo
        {
            Name = "privateMethod",
            Kind = SymbolKind.Method,
            Type = "func privateMethod() -> void",
            Location = location,
            AccessModifier = AccessModifier.Private,
            IsStatic = false
        };
        symbol.Location = location;

        // Assert
        Assert.Equal("privateMethod", symbol.Name);
        Assert.Equal(SymbolKind.Method, symbol.Kind);
        Assert.Equal("func privateMethod() -> void", symbol.Type);
        Assert.Equal(AccessModifier.Private, symbol.AccessModifier);
        Assert.False(symbol.IsStatic);
        Assert.NotNull(symbol.Location);
    }

    [Fact]
    public void TestSourceLocation_Properties()
    {
        // Arrange
        var location = new SourceLocation
        {
            Uri = "file:///test.old8",
            Line = 10,
            Column = 15,
            EndLine = 10,
            EndColumn = 25
        };

        // Assert
        Assert.Equal("file:///test.old8", location.Uri);
        Assert.Equal(10, location.Line);
        Assert.Equal(15, location.Column);
        Assert.Equal(10, location.EndLine);
        Assert.Equal(25, location.EndColumn);
    }

    [Fact]
    public void TestSourceLocation_SingleLine()
    {
        // Arrange
        var location = new SourceLocation
        {
            Uri = "file:///test.old8",
            Line = 10,
            Column = 5,
            EndLine = 10,
            EndColumn = 15
        };

        // Assert
        Assert.Equal(10, location.Line);
        Assert.Equal(5, location.Column);
        Assert.Equal(10, location.EndLine);
        Assert.Equal(15, location.EndColumn);
    }

    [Fact]
    public void TestAccessModifier_AllValues()
    {
        // Test all enum values
        var allValues = Enum.GetValues<AccessModifier>();
        
        foreach (var modifier in allValues)
        {
            Assert.True(Enum.IsDefined(typeof(AccessModifier), modifier));
        }
    }

    [Fact]
    public void TestSymbolKind_AllValues()
    {
        // Test all enum values
        var allKinds = Enum.GetValues<SymbolKind>();
        
        foreach (var kind in allKinds)
        {
            Assert.True(Enum.IsDefined(typeof(SymbolKind), kind));
        }
    }

    [Fact]
    public void TestSymbolInfo_Equality()
    {
        // Arrange
        var symbol1 = new SymbolInfo
        {
            Name = "test",
            Kind = SymbolKind.Function,
            Location = new SourceLocation
            {
                Uri = "file:///test.old8",
                Line = 1,
                Column = 1
            }
        };

        var symbol2 = new SymbolInfo
        {
            Name = "test",
            Kind = SymbolKind.Function,
            Location = new SourceLocation
            {
                Uri = "file:///test.old8",
                Line = 1,
                Column = 1
            }
        };

        var symbol3 = new SymbolInfo
        {
            Name = "different",
            Kind = SymbolKind.Variable,
            Location = new SourceLocation
            {
                Uri = "file:///test.old8",
                Line = 1,
                Column = 1
            }
        };

        // Since SymbolInfo doesn't override Equals, we test reference equality
        Assert.NotSame(symbol1, symbol2);
        Assert.NotSame(symbol1, symbol3);
        Assert.NotSame(symbol2, symbol3);

        // Test property equality
        Assert.Equal(symbol1.Name, symbol2.Name);
        Assert.Equal(symbol1.Kind, symbol2.Kind);
        Assert.NotEqual(symbol1.Name, symbol3.Name);
        Assert.NotEqual(symbol1.Kind, symbol3.Kind);
    }
}