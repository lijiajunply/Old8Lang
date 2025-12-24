using Old8LangLib;

namespace Old8Lang.Tests.Library;

public class TemplateEngineTests
{
    [Fact]
    public void Render_WithSimpleVariables_ReturnsCorrectResult()
    {
        // Arrange
        var template = "Hello {{name}}, welcome to {{site}}!";
        var engine = new TemplateEngine();
        engine.LoadTemplate(template);

        var variables = new Dictionary<string, object>
        {
            { "name", "John" },
            { "site", "MyWebsite" }
        };

        // Act
        var result = engine.Render(variables);

        // Assert
        Assert.Equal("Hello John, welcome to MyWebsite!", result);
    }

    [Fact]
    public void Render_WithGlobalVariables_ReturnsCorrectResult()
    {
        // Arrange
        var template = "Hello {{name}}, welcome to {{site}}!";
        var engine = new TemplateEngine();
        engine.AddGlobalVariable("site", "GlobalSite");
        engine.LoadTemplate(template);

        var variables = new Dictionary<string, object>
        {
            { "name", "John" }
        };

        // Act
        var result = engine.Render(variables);

        // Assert
        Assert.Equal("Hello John, welcome to GlobalSite!", result);
    }

    [Fact]
    public void Render_WithIfStatement_ReturnsCorrectResult()
    {
        // Arrange
        var template = "Hello {% if name %}{{name}}{% endif %}{% if !name %}Anonymous{% endif %}!";
        var engine = new TemplateEngine();
        engine.LoadTemplate(template);

        var variables = new Dictionary<string, object>
        {
            { "name", "John" }
        };

        // Act
        var result = engine.Render(variables);

        // Assert
        Assert.Equal("Hello John!", result);
    }

    [Fact]
    public void Render_WithIfStatement_EmptyVariable_ReturnsCorrectResult()
    {
        // Arrange
        var template = "Hello {% if name %}{{name}}{% endif %}{% if !name %}Anonymous{% endif %}!";
        var engine = new TemplateEngine();
        engine.LoadTemplate(template);

        var variables = new Dictionary<string, object>
        {
            { "name", "" }
        };

        // Act
        var result = engine.Render(variables);

        // Assert
        Assert.Equal("Hello Anonymous!", result);
    }

    [Fact]
    public void Render_WithForLoop_ReturnsCorrectResult()
    {
        // Arrange
        var template = "<ul>{% for item in items %}<li>{{item}}</li>{% endfor %}</ul>";
        var engine = new TemplateEngine();
        engine.LoadTemplate(template);

        var items = new List<object> { "Apple", "Banana", "Cherry" };
        var variables = new Dictionary<string, object>
        {
            { "items", items }
        };

        // Act
        var result = engine.Render(variables);

        // Assert
        var expected = "<ul><li>Apple</li><li>Banana</li><li>Cherry</li></ul>";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Render_WithNestedIfInForLoop_ReturnsCorrectResult()
    {
        // Arrange
        var template =
            "<ul>{% for item in items %}{% if item %}<li>{{item}}</li>{% endif %}{% if !item %}<li>Empty</li>{% endif %}{% endfor %}</ul>";
        var engine = new TemplateEngine();
        engine.LoadTemplate(template);

        var items = new List<object> { "Apple", "", "Cherry" };
        var variables = new Dictionary<string, object>
        {
            { "items", items }
        };

        // Act
        var result = engine.Render(variables);

        // Assert
        var expected = "<ul><li>Apple</li><li>Empty</li><li>Cherry</li></ul>";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Render_WithComments_IgnoresComments()
    {
        // Arrange
        var template = "Hello {# This is a comment #}{{name}}!";
        var engine = new TemplateEngine();
        engine.LoadTemplate(template);

        var variables = new Dictionary<string, object>
        {
            { "name", "John" }
        };

        // Act
        var result = engine.Render(variables);

        // Assert
        Assert.Equal("Hello John!", result);
    }

    [Fact]
    public void RenderHtml_StaticMethod_ReturnsCorrectResult()
    {
        // Arrange
        var template = "<h1>Hello {{title}}!</h1><p>{{content}}</p>";
        var variables = new Dictionary<string, object>
        {
            { "title", "World" },
            { "content", "This is a test." }
        };

        // Act
        var result = TemplateEngine.RenderHtml(template, variables);

        // Assert
        Assert.Equal("<h1>Hello World!</h1><p>This is a test.</p>", result);
    }

    [Fact]
    public void RenderConfig_StaticMethod_ReturnsCorrectResult()
    {
        // Arrange
        var template = "[Database]\nHost = {{host}}\nPort = {{port}}\nName = {{name}}";
        var variables = new Dictionary<string, object>
        {
            { "host", "localhost" },
            { "port", 5432 },
            { "name", "mydb" }
        };

        // Act
        var result = TemplateEngine.RenderConfig(template, variables);

        // Assert
        Assert.Equal("[Database]\nHost = localhost\nPort = 5432\nName = mydb", result);
    }

    [Fact]
    public void LoadTemplateFromFile_WithValidFile_ReturnsCorrectResult()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var templateContent = "Hello {{name}} from file!";
        File.WriteAllText(tempFile, templateContent);

        var engine = new TemplateEngine();
        var variables = new Dictionary<string, object>
        {
            { "name", "FileUser" }
        };

        try
        {
            // Act
            engine.LoadTemplateFromFile(tempFile);
            var result = engine.Render(variables);

            // Assert
            Assert.Equal("Hello FileUser from file!", result);
        }
        finally
        {
            // Cleanup
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Render_WithObjectProperties_ReturnsCorrectResult()
    {
        // Arrange
        var template = "User: {{user.name}}, Age: {{user.age}}";
        var engine = new TemplateEngine();
        engine.LoadTemplate(template);

        var user = new { name = "Alice", age = 30 };
        var variables = new Dictionary<string, object>
        {
            { "user", user }
        };

        // Act
        var result = engine.Render(variables);

        // Assert
        Assert.Equal("User: Alice, Age: 30", result);
    }

    [Fact]
    public void Render_WithComplexTemplate_ReturnsCorrectResult()
    {
        // Arrange
        var template = @"
<!DOCTYPE html>
<html>
<head>
    <title>{{title}}</title>
</head>
<body>
    <h1>{{header}}</h1>
    {% if showWelcome %}
    <p>Welcome, {{username}}!</p>
    {% endif %}
    <ul>
    {% for item in items %}
        <li>{{item}}</li>
    {% endfor %}
    </ul>
    <footer>
        {# This is a comment #}
        &copy; {{year}} {{company}}
    </footer>
</body>
</html>";

        var engine = new TemplateEngine();
        engine.LoadTemplate(template);

        var variables = new Dictionary<string, object>
        {
            { "title", "My Page" },
            { "header", "Welcome" },
            { "showWelcome", true },
            { "username", "John Doe" },
            { "items", new List<object> { "Item 1", "Item 2", "Item 3" } },
            { "year", 2025 },
            { "company", "My Company" }
        };

        // Act
        var result = engine.Render(variables);

        // Assert
        Assert.Contains("<title>My Page</title>", result);
        Assert.Contains("<h1>Welcome</h1>", result);
        Assert.Contains("<p>Welcome, John Doe!</p>", result);
        Assert.Contains("<li>Item 1</li>", result);
        Assert.Contains("<li>Item 2</li>", result);
        Assert.Contains("<li>Item 3</li>", result);
        Assert.Contains("&copy; 2025 My Company", result);
        Assert.DoesNotContain("{#", result); // Comments should be removed
        Assert.DoesNotContain("#}", result); // Comments should be removed
    }
}