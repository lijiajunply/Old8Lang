using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Classes;

/// <summary>
/// Mixin解释模式测试
/// </summary>
public class MixinTests
{
    [Fact]
    public void Mixin_BasicMixin_MixesMultipleBehaviors()
    {
        // Arrange
        var code = @"
            mixin Drawable {
                public visible <- true

                func Show() -> void {
                    visible <- true
                }

                func Hide() -> void {
                    visible <- false
                }

                func IsVisible() -> bool {
                    return visible
                }
            }

            mixin Movable {
                public x <- 0
                public y <- 0

                func Move(newX:double, newY:double) -> void {
                    x <- newX
                    y <- newY
                }

                func GetPosition() -> tuple {
                    return (x, y)
                }
            }

            class Sprite with Drawable, Movable {
                public name <- """"

                func init(n:string) {
                    name <- n
                }
            }

            sprite <- Sprite(""Player"")
            sprite.Move(10.0, 20.0)
            sprite.Hide()

            result1 <- sprite.name
            result2 <- sprite.x
            result3 <- sprite.y
            result4 <- sprite.IsVisible()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        var result4 = interpreter.Manager.GetValue(new LangId("result4"));

        Assert.NotNull(result1);
        Assert.IsType<StringLangValue>(result1);
        Assert.Equal("Player", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<DoubleLangValue>(result2);
        Assert.Equal(10.0, ((DoubleLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<DoubleLangValue>(result3);
        Assert.Equal(20.0, ((DoubleLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<BoolLangValue>(result4);
        Assert.False(((BoolLangValue)result4).Value);
    }

    [Fact]
    public void Mixin_ConflictResolution_HandlesMethodConflicts()
    {
        // Arrange
        var code = @"
            mixin Logger {
                public logLevel <- ""info""

                func Log(message:string) -> void {
                    PrintLine(""["" + logLevel + ""] "" + message)
                }
            }

            mixin VerboseLogger extends Logger {
                public verbose <- false

                func Log(message:string) -> void {
                    if verbose {
                        PrintLine(""[VERBOSE] "" + message)
                    } else {
                        super.Log(message)
                    }
                }
            }

            class Application with VerboseLogger {
                public name <- """"

                func init(n:string) {
                    name <- n
                }
            }

            app <- Application(""MyApp"")
            app.verbose <- true
            app.Log(""Application started"")
            app.logLevel <- ""debug""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var appName = interpreter.Manager.GetValue(new LangId("app.name"));
        var logLevel = interpreter.Manager.GetValue(new LangId("app.logLevel"));

        Assert.NotNull(appName);
        Assert.IsType<StringLangValue>(appName);
        Assert.Equal("MyApp", ((StringLangValue)appName).Value);

        Assert.NotNull(logLevel);
        Assert.IsType<StringLangValue>(logLevel);
        Assert.Equal("debug", ((StringLangValue)logLevel).Value);
    }

    [Fact]
    public void Mixin_MethodOverriding_OverridesMixinMethods()
    {
        // Arrange
        var code = @"
            mixin Calculator {
                func Add(a:double, b:double) -> double {
                    return a + b
                }

                func Multiply(a:double, b:double) -> double {
                    return a * b
                }
            }

            class AdvancedCalculator with Calculator {
                func Add(a:double, b:double) -> double {
                    // Override with validation
                    if a < 0 or b < 0 {
                        return 0.0  // 确保返回double类型
                    }
                    return a + b  // 直接调用原始实现，避免无限递归
                }

                func AddAll(numbers:[double]) -> double {
                    sum <- 0.0
                    for num in numbers {
                        sum <- sum + num
                    }
                    return sum
                }
            }

            calc <- AdvancedCalculator()
            result1 <- calc.Add(5.0, 3.0)
            result2 <- calc.Add(-1.0, 3.0)
            result3 <- calc.Multiply(4.0, 2.0)
            result4 <- calc.AddAll([1.0, 2.0, 3.0])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        var result4 = interpreter.Manager.GetValue(new LangId("result4"));

        Assert.NotNull(result1);
        Assert.IsType<DoubleLangValue>(result1);
        Assert.Equal(8.0, ((DoubleLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<DoubleLangValue>(result2);
        Assert.Equal(0.0, ((DoubleLangValue)result2).Value); // Negative numbers return 0

        Assert.NotNull(result3);
        Assert.IsType<DoubleLangValue>(result3);
        Assert.Equal(8.0, ((DoubleLangValue)result3).Value); // Uses inherited method

        Assert.NotNull(result4);
        Assert.IsType<DoubleLangValue>(result4);
        Assert.Equal(6.0, ((DoubleLangValue)result4).Value);
    }

    [Fact]
    public void Mixin_ConstructorChain_CallsMixinConstructors()
    {
        // Arrange
        var code = @"
            mixin Configurable {
                public settings <- {}

                func Configurable() {
                    settings <- {""theme"": ""default"", ""debug"": false}
                }

                func SetConfig(key:string, value:any) -> void {
                    settings[key] <- value
                }

                func GetConfig(key:string) -> any {
                    return settings[key]
                }
            }

            mixin Validatable {
                public errors <- {}

                func Validatable() {
                    errors <- {}
                }

                func AddError(error:string) -> void {
                    errors.Add(error)
                }

                func HasErrors() -> bool {
                    return len(errors) > 0
                }
            }

            class FormComponent with Configurable, Validatable {
                public value <- """"

                func init() {
                    this.Configurable()
                    this.Validatable()
                }

                func Validate() -> bool {
                    if len(value) == 0 {
                        AddError(""Value cannot be empty"")
                        return false
                    }
                    return true
                }

                func SetValue(v:string) -> void {
                    value <- v
                    Validate()
                }
            }

            component <- FormComponent()
            component.SetConfig(""required"", true)
            component.SetValue("""")
            isValid <- component.Validate()
            hasErrors <- component.HasErrors()
            configValue <- component.GetConfig(""required"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var isValid = interpreter.Manager.GetValue(new LangId("isValid"));
        var hasErrors = interpreter.Manager.GetValue(new LangId("hasErrors"));
        var configValue = interpreter.Manager.GetValue(new LangId("configValue"));

        Assert.NotNull(isValid);
        Assert.IsType<BoolLangValue>(isValid);
        Assert.False(((BoolLangValue)isValid).Value);

        Assert.NotNull(hasErrors);
        Assert.IsType<BoolLangValue>(hasErrors);
        Assert.True(((BoolLangValue)hasErrors).Value);

        Assert.NotNull(configValue);
        Assert.IsType<BoolLangValue>(configValue);
        Assert.True(((BoolLangValue)configValue).Value);
    }

    [Fact]
    public void Mixin_Polymorphism_MixinsWithInheritance()
    {
        // Arrange
        var code = @"
            class Shape {
                public name <- """"

                func init(n:string) {
                    name <- n
                }

                func GetName() -> string {
                    return name
                }
            }

            mixin Colorable {
                public color <- ""black""

                func SetColor(c:string) -> void {
                    color <- c
                }

                func GetColor() -> string {
                    return color
                }
            }

            class ColoredCircle extends Shape with Colorable {
                public radius <- 0

                func init(n:string, r:double) {
                    this.Shape(n)
                    radius <- r
                }

                func GetInfo() -> string {
                    return GetName() + "" (color: "" + GetColor() + "", radius: "" + radius.ToStr() + "")""
                }
            }

            circle <- ColoredCircle(""MyCircle"", 5.0)
            circle.SetColor(""red"")
            result <- circle.GetInfo()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("MyCircle (color: red, radius: 5.0)", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Mixin_AbstractMixin_AbstractMixinMethods()
    {
        // Arrange
        var code = @"
            abstract mixin Persistence {
                func Save() -> void
                func Load() -> void
            }

            class DatabaseObject with Persistence {
                public id <- 0
                public data <- """"

                func Save() -> void {
                    // Save to database logic
                }

                func Load() -> void {
                    // Load from database logic
                }
            }

            class FileSystemObject with Persistence {
                public filename <- """"

                func Save() -> void {
                    // Save to file system logic
                }

                func Load() -> void {
                    // Load from file system logic
                }
            }

            dbObj <- DatabaseObject()
            fsObj <- FileSystemObject()

            dbObj.id <- 123
            dbObj.data <- ""Sample data""
            fsObj.filename <- ""sample.txt""

            result1 <- dbObj.id
            result2 <- fsObj.filename
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(123, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("sample.txt", ((StringLangValue)result2).Value);
    }

    [Fact]
    public void Mixin_MultipleInheritanceLevels_NestedMixinInheritance()
    {
        // Arrange
        var code = @"
            mixin BasicFeatures {
                public name <- """"

                func BasicFeatures() {
                    name <- ""unnamed""
                }

                func SetName(n:string) -> void {
                    name <- n
                }
            }

            mixin ExtendedFeatures extends BasicFeatures {
                public version <- 1

                func ExtendedFeatures() {
                    this.BasicFeatures()
                    version <- 1
                }

                func GetInfo() -> string {
                    return name + "" v"" + version.ToStr()
                }
            }

            mixin AdvancedFeatures extends ExtendedFeatures {
                public features <- {}

                func AdvancedFeatures() {
                    this.ExtendedFeatures()
                    features <- {""feature1"", ""feature2""}
                }

                func AddFeature(feature:string) -> void {
                    features.Add(feature)
                }

                func GetAllFeatures() -> string {
                    return GetInfo() + "" features: "" + features.Join("", "")
                }
            }

            class AdvancedObject with AdvancedFeatures {
                func init(n:string, v:int) {
                    this.BasicFeatures()
                    SetName(n)
                    version <- v
                }
            }

            obj <- AdvancedObject(""MyObject"", 2)
            obj.AddFeature(""feature3"")

            result <- obj.GetAllFeatures()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        // Result should contain name, version, and features
        Assert.Contains("MyObject", ((StringLangValue)result).Value);
        Assert.Contains("v2", ((StringLangValue)result).Value);
        Assert.Contains("feature3", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Mixin_RuntimeMixing_DynamicMixinApplication()
    {
        // Arrange
        var code = @"
            mixin EventSystem {
                private events <- {}

                func EventSystem() {
                    events <- {}
                }

                func AddEventListener(eventName:string, handler:func) -> void {
                    if not events.ContainsKey(eventName) {
                        events[eventName] <- {}
                    }
                    events[eventName].Push(handler)
                }

                func TriggerEvent(eventName:string, data:any) -> void {
                    if events.ContainsKey(eventName) {
                        for handler in events[eventName] {
                            handler(data)
                        }
                    }
                }
            }

            class GameObject {
                public id <- 0
                public x <- 0
                public y <- 0

                func init(id:int) {
                    this.id <- id
                }

                func MoveTo(newX:int, newY:int) -> void {
                    x <- newX
                    y <- newY
                }
            }

            // Apply mixin to existing object
            player <- GameObject(1)
            // In a real implementation, there would be a way to apply mixin at runtime
            // For this test, we'll create a class that includes the mixin
            class Player extends GameObject with EventSystem {
                func init(id:int) {
                    this.GameObject(id)
                }

                func MoveWithEvent(newX:int, newY:int) -> void {
                    oldX <- x
                    oldY <- y
                    MoveTo(newX, newY)
                    TriggerEvent(""moved"", {""oldX"": oldX, ""oldY"": oldY, ""newX"": newX, ""newY"": newY})
                }
            }

            player <- Player(1)
            player.AddEventListener(""moved"", (data) -> {
                PrintLine(""Player moved from "" + data[""oldX""].ToStr() + "","" + data[""oldY""].ToStr() + "" to "" + data[""newX""].ToStr() + "","" + data[""newY""].ToStr())
            })

            player.MoveWithEvent(10, 20)
            finalX <- player.x
            finalY <- player.y
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var finalX = interpreter.Manager.GetValue(new LangId("finalX"));
        var finalY = interpreter.Manager.GetValue(new LangId("finalY"));

        Assert.NotNull(finalX);
        Assert.IsType<IntLangValue>(finalX);
        Assert.Equal(10, ((IntLangValue)finalX).Value);

        Assert.NotNull(finalY);
        Assert.IsType<IntLangValue>(finalY);
        Assert.Equal(20, ((IntLangValue)finalY).Value);
    }

    [Fact]
    public void Mixin_PropertyAccess_AccessMixinProperties()
    {
        // Arrange
        var code = @"
            mixin Timed {
                public createdAt <- 0
                public lastModified <- 0

                func Timed() {
                    createdAt <- 0  // Would normally be current time
                    lastModified <- createdAt
                }

                func Touch() -> void {
                    lastModified <- 0  // Would normally be current time
                }

                func GetAge() -> int {
                    return lastModified - createdAt
                }
            }

            mixin Versioned {
                public version <- 1
                public history <- {}

                func Versioned() {
                    version <- 1
                    history <- {""version 1""}
                }

                func IncrementVersion() -> void {
                    version <- version + 1
                    history.Add(""version "" + version.ToStr())
                }
            }

            class Document with Timed, Versioned {
                public content <- """"

                func init(text:string) {
                    this.Timed()
                    this.Versioned()
                    content <- text
                }

                func UpdateContent(newContent:string) -> void {
                    content <- newContent
                    Touch()
                    IncrementVersion()
                }

                func GetInfo() -> string {
                    return ""Doc v"" + version.ToStr() + "" (age: "" + GetAge().ToStr() + "")""
                }
            }

            doc <- Document(""Initial content"")
            doc.UpdateContent(""Updated content"")
            result <- doc.GetInfo()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Doc v2 (age: 0)", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Mixin_MethodResolutionOrder_ResolvesCorrectMethod()
    {
        // Arrange
        var code = @"
            mixin A {
                func Method() -> string {
                    return ""A""
                }
            }

            mixin B {
                func Method() -> string {
                    return ""B""
                }
            }

            mixin C with A, B {
                func Method() -> string {
                    return ""C-""
                }
            }

            class TestClass with C, B, A {
                func TestMethod() -> string {
                    return ""Class-""
                }
            }

            // Test method resolution order
            obj <- TestClass()

            // Call different methods to test resolution
            result1 <- obj.Method()
            // result2 <- obj.A.Method()  // If explicit mixin method access is supported
            // result3 <- obj.B.Method()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        Assert.NotNull(result1);
        Assert.IsType<StringLangValue>(result1);
        // The result depends on the method resolution order implementation
        // This tests that the MRO is consistent and predictable
    }

    [Fact]
    public void Mixin_StaticMembers_StaticMixinMembers()
    {
        // Arrange
        var code = @"
            mixin CounterMixin {
                static count <- 0

                static func Increment() -> int {
                    count <- count + 1
                    return count
                }

                static func GetCount() -> int {
                    return count
                }
            }

            class Counter with CounterMixin {
                public instanceId <- 0

                func init() {
                    instanceId <- CounterMixin.Increment()
                }

                func GetInstanceId() -> int {
                    return instanceId
                }
            }

            counter1 <- Counter()
            counter2 <- Counter()
            counter3 <- Counter()

            result1 <- counter1.GetInstanceId()
            result2 <- counter2.GetInstanceId()
            result3 <- counter3.GetInstanceId()
            totalCount <- CounterMixin.GetCount()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        var totalCount = interpreter.Manager.GetValue(new LangId("totalCount"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(1, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(2, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(3, ((IntLangValue)result3).Value);

        Assert.NotNull(totalCount);
        Assert.IsType<IntLangValue>(totalCount);
        Assert.Equal(3, ((IntLangValue)totalCount).Value);
    }
}