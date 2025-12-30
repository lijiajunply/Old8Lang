using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Classes;

/// <summary>
/// 成员访问解释模式测试
/// </summary>
[Collection("Sequential")]
public class MemberAccessTests
{
    [Fact]
    public void MemberAccess_PublicField_ReadsValueCorrectly()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- """"
                public age <- 0

                func init(name:string, age:int) {
                    this.name <- name
                    this.age <- age
                }
            }

            person <- Person(""Alice"", 25)
            resultName <- person.name
            resultAge <- person.age
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var resultName = interpreter.Manager.GetValue(new LangId("resultName"));
        var resultAge = interpreter.Manager.GetValue(new LangId("resultAge"));

        Assert.NotNull(resultName);
        Assert.IsType<StringLangValue>(resultName);
        Assert.Equal("Alice", ((StringLangValue)resultName).Value);

        Assert.NotNull(resultAge);
        Assert.IsType<IntLangValue>(resultAge);
        Assert.Equal(25, ((IntLangValue)resultAge).Value);
    }

    [Fact]
    public void MemberAccess_PublicField_WritesValueCorrectly()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- """"
                public age <- 0
            }

            person <- Person()
            person.name <- ""Bob""
            person.age <- 30
            resultName <- person.name
            resultAge <- person.age
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var resultName = interpreter.Manager.GetValue(new LangId("resultName"));
        var resultAge = interpreter.Manager.GetValue(new LangId("resultAge"));

        Assert.NotNull(resultName);
        Assert.IsType<StringLangValue>(resultName);
        Assert.Equal("Bob", ((StringLangValue)resultName).Value);

        Assert.NotNull(resultAge);
        Assert.IsType<IntLangValue>(resultAge);
        Assert.Equal(30, ((IntLangValue)resultAge).Value);
    }

    [Fact]
    public void MemberAccess_PublicMethod_CallsMethodCorrectly()
    {
        // Arrange
        var code = @"
            class Calculator {
                public value <- 0

                func init(initialValue:int) {
                    value <- initialValue
                }

                func Add(number:int) -> int {
                    value <- value + number
                    return value
                }

                func Multiply(factor:int) -> int {
                    value <- value * factor
                    return value
                }
            }

            calc <- Calculator(10)
            result1 <- calc.Add(5)
            result2 <- calc.Multiply(2)
            finalValue <- calc.value
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var finalValue = interpreter.Manager.GetValue(new LangId("finalValue"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(15, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(30, ((IntLangValue)result2).Value);

        Assert.NotNull(finalValue);
        Assert.IsType<IntLangValue>(finalValue);
        Assert.Equal(30, ((IntLangValue)finalValue).Value);
    }

    [Fact]
    public void MemberAccess_ChainedAccess_AccessesNestedMembers()
    {
        // Arrange
        var code = @"
            class Address {
                public street <- """"
                public city <- """"

                func init(street:string, city:string) {
                    this.street <- street
                    this.city <- city
                }

                func GetFullAddress() -> string {
                    return street + "", "" + city
                }
            }

            class Person {
                public name <- """"
                public address <- null

                func init(name:string, address:Address) {
                    this.name <- name
                    this.address <- address
                }

                func GetInfo() -> string {
                    return name + "" lives at "" + address.GetFullAddress()
                }
            }

            addr <- Address(""123 Main St"", ""New York"")
            person <- Person(""Alice"", addr)
            resultStreet <- person.address.street
            resultCity <- person.address.city
            resultFullAddress <- person.address.GetFullAddress()
            resultInfo <- person.GetInfo()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var resultStreet = interpreter.Manager.GetValue(new LangId("resultStreet"));
        var resultCity = interpreter.Manager.GetValue(new LangId("resultCity"));
        var resultFullAddress = interpreter.Manager.GetValue(new LangId("resultFullAddress"));
        var resultInfo = interpreter.Manager.GetValue(new LangId("resultInfo"));

        Assert.NotNull(resultStreet);
        Assert.IsType<StringLangValue>(resultStreet);
        Assert.Equal("123 Main St", ((StringLangValue)resultStreet).Value);

        Assert.NotNull(resultCity);
        Assert.IsType<StringLangValue>(resultCity);
        Assert.Equal("New York", ((StringLangValue)resultCity).Value);

        Assert.NotNull(resultFullAddress);
        Assert.IsType<StringLangValue>(resultFullAddress);
        Assert.Equal("123 Main St, New York", ((StringLangValue)resultFullAddress).Value);

        Assert.NotNull(resultInfo);
        Assert.IsType<StringLangValue>(resultInfo);
        Assert.Equal("Alice lives at 123 Main St, New York", ((StringLangValue)resultInfo).Value);
    }

    [Fact]
    public void MemberAccess_WithParameters_PassesArgumentsCorrectly()
    {
        // Arrange
        var code = @"
            class MathOperations {
                func Power(base:double, exponent:int) -> double {
                    result <- 1.0
                    for i <- 1, i <= exponent, i++ {
                        result <- result * base
                    }
                    return result
                }

                func Factorial(n:int) -> int {
                    if n <= 1 {
                        return 1
                    }
                    return n * Factorial(n - 1)
                }
            }

            math <- MathOperations()
            result1 <- math.Power(2.0, 8)
            result2 <- math.Power(3.0, 3)
            result3 <- math.Factorial(5)
            result4 <- math.Factorial(0)
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
        Assert.Equal(256.0, ((DoubleLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<DoubleLangValue>(result2);
        Assert.Equal(27.0, ((DoubleLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(120, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<IntLangValue>(result4);
        Assert.Equal(1, ((IntLangValue)result4).Value);
    }

    [Fact]
    public void MemberAccess_StaticMember_AccessesClassLevelMember()
    {
        // Arrange
        var code = @"
            class Counter {
                static count <- 0
                public instanceId <- 0

                func init() {
                    Counter.count <- Counter.count + 1
                    this.instanceId <- count
                }

                static func GetCount() -> int {
                    return Counter.count
                }

                static func Reset() {
                    Counter.count <- 0
                }
            }

            counter1 <- Counter()
            counter2 <- Counter()

            totalCount1 <- Counter.GetCount()
            id1 <- counter1.instanceId
            id2 <- counter2.instanceId

            Counter.Reset()
            totalCount2 <- Counter.GetCount()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var totalCount1 = interpreter.Manager.GetValue(new LangId("totalCount1"));
        var id1 = interpreter.Manager.GetValue(new LangId("id1"));
        var id2 = interpreter.Manager.GetValue(new LangId("id2"));
        var totalCount2 = interpreter.Manager.GetValue(new LangId("totalCount2"));

        Assert.NotNull(totalCount1);
        Assert.IsType<IntLangValue>(totalCount1);
        Assert.Equal(2, ((IntLangValue)totalCount1).Value);

        Assert.NotNull(id1);
        Assert.IsType<IntLangValue>(id1);
        Assert.Equal(1, ((IntLangValue)id1).Value);

        Assert.NotNull(id2);
        Assert.IsType<IntLangValue>(id2);
        Assert.Equal(2, ((IntLangValue)id2).Value);

        Assert.NotNull(totalCount2);
        Assert.IsType<IntLangValue>(totalCount2);
        Assert.Equal(0, ((IntLangValue)totalCount2).Value);
    }

    [Fact]
    public void MemberAccess_WithArrayMembers_AccessesArrayElements()
    {
        // Arrange
        var code = @"
            class DataContainer {
                public data <- {}
                public metadata <- {}

                func init() {
                    this.data <- {10, 20, 30, 40, 50}
                    this.metadata <- {""count"": 5, ""source"": ""test""}
                }

                func GetData(index:int) -> int {
                    return this.data[index]
                }

                func GetMetadata(key:string) -> string {
                    return this.metadata[key]
                }

                func AddData(value:int) {
                    this.data.Add(value)
                    this.metadata[""count""] <- len(this.data)
                }
            }

            container <- DataContainer()
            result1 <- container.GetData(0)
            result2 <- container.GetData(2)
            result3 <- container.GetMetadata(""source"")
            result4 <- container.GetMetadata(""count"")

            container.AddData(60)
            result5 <- len(container.data)
            result6 <- container.GetMetadata(""count"")
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
        var result5 = interpreter.Manager.GetValue(new LangId("result5"));
        var result6 = interpreter.Manager.GetValue(new LangId("result6"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(10, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(30, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<StringLangValue>(result3);
        Assert.Equal("test", ((StringLangValue)result3).Value);

        Assert.NotNull(result5);
        Assert.IsType<IntLangValue>(result5);
        Assert.Equal(6, ((IntLangValue)result5).Value);
    }

    [Fact]
    public void MemberAccess_PropertyAccess_ModifiesAndReadsProperties()
    {
        // Arrange
        var code = @"
            class Circle {
                public radius <- 0
                private pi <- 3.14159

                func init(radius:double) {
                    this.radius <- radius
                }

                func GetArea() -> double {
                    return this.pi * this.radius * this.radius
                }

                func GetCircumference() -> double {
                    return 2 * this.pi * this.radius
                }

                func SetRadius(newRadius:double) {
                    if newRadius > 0 {
                        this.radius <- newRadius
                    }
                }
            }

            circle <- Circle(5.0)
            area1 <- circle.GetArea()
            circumference1 <- circle.GetCircumference()

            circle.SetRadius(10.0)
            area2 <- circle.GetArea()
            circumference2 <- circle.GetCircumference()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var area1 = interpreter.Manager.GetValue(new LangId("area1"));
        var circumference1 = interpreter.Manager.GetValue(new LangId("circumference1"));
        var area2 = interpreter.Manager.GetValue(new LangId("area2"));
        var circumference2 = interpreter.Manager.GetValue(new LangId("circumference2"));

        Assert.NotNull(area1);
        Assert.IsType<DoubleLangValue>(area1);
        Assert.Equal(78.53975, ((DoubleLangValue)area1).Value, 5);

        Assert.NotNull(circumference1);
        Assert.IsType<DoubleLangValue>(circumference1);
        Assert.Equal(31.4159, ((DoubleLangValue)circumference1).Value, 5);

        Assert.NotNull(area2);
        Assert.IsType<DoubleLangValue>(area2);
        Assert.Equal(314.159, ((DoubleLangValue)area2).Value, 5);
    }

    [Fact]
    public void MemberAccess_WithComplexTypes_HandlesObjectsAsMembers()
    {
        // Arrange
        var code = @"
            class Point {
                public x <- 0
                public y <- 0

                func init(x:double, y:double) {
                    this.x <- x
                    this.y <- y
                }

                func DistanceTo(other:Point) -> double {
                    dx <- this.x - other.x
                    dy <- this.y - other.y
                    return (dx * dx + dy * dy) ^ 0.5
                }
            }

            class Line {
                public start <- null
                public end <- null

                func init(start:Point, end:Point) {
                    this.start <- start
                    this.end <- end
                }

                func GetLength() -> double {
                    return this.start.DistanceTo(this.end)
                }
            }

            p1 <- Point(0.0, 0.0)
            p2 <- Point(3.0, 4.0)
            line <- Line(p1, p2)
            length <- line.GetLength()
            startX <- line.start.x
            endY <- line.end.y
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var length = interpreter.Manager.GetValue(new LangId("length"));
        var startX = interpreter.Manager.GetValue(new LangId("startX"));
        var endY = interpreter.Manager.GetValue(new LangId("endY"));

        Assert.NotNull(length);
        Assert.IsType<DoubleLangValue>(length);
        Assert.Equal(5.0, ((DoubleLangValue)length).Value, 5);

        Assert.NotNull(startX);
        Assert.IsType<DoubleLangValue>(startX);
        Assert.Equal(0.0, ((DoubleLangValue)startX).Value, 5);

        Assert.NotNull(endY);
        Assert.IsType<DoubleLangValue>(endY);
        Assert.Equal(4.0, ((DoubleLangValue)endY).Value, 5);
    }

    [Fact]
    public void MemberAccess_MethodChaining_CallsMultipleMethods()
    {
        // Arrange
        var code = """

                               class StringBuilder {
                                   private content <- ""

                                   func StringBuilder() {
                                       content <- ""
                                   }

                                   func Append(text:string) -> StringBuilder {
                                       content <- content + text
                                       return this
                                   }

                                   func AppendLine(text:string) -> StringBuilder {
                                       content <- content + text + "\n"
                                       return this
                                   }

                                   func Clear() -> StringBuilder {
                                       content <- ""
                                       return this
                                   }

                                   func ToString() -> string {
                                       return content
                                   }
                               }

                               builder <- StringBuilder()
                               result <- builder.Append("Hello").AppendLine(" World").Append("!").ToString()
                               length <- len(result)
                           
                   """;
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var length = interpreter.Manager.GetValue(new LangId("length"));

        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Hello World\n!", ((StringLangValue)result).Value);

        Assert.NotNull(length);
        Assert.IsType<IntLangValue>(length);
        Assert.Equal(13, ((IntLangValue)length).Value);
    }

    [Fact]
    public void MemberAccess_WithConditionals_SelectsBasedOnMemberValues()
    {
        // Arrange
        var code = @"
            class Student {
                public name <- """"
                public grade <- 0
                public attendance <- 0

                func init(name:string, grade:int, attendance:double) {
                    this.name <- name
                    this.grade <- grade
                    this.attendance <- attendance
                }

                func IsPassing() -> bool {
                    return grade >= 60 and attendance >= 0.75
                }

                func GetPerformance() -> string {
                    if grade >= 90 {
                        return ""Excellent""
                    } else if grade >= 80 {
                        return ""Good""
                    } else if grade >= 70 {
                        return ""Satisfactory""
                    } else if grade >= 60 {
                        return ""Needs Improvement""
                    } else {
                        return ""Failing""
                    }
                }
            }

            student1 <- Student(""Alice"", 95, 0.95)
            student2 <- Student(""Bob"", 75, 0.80)
            student3 <- Student(""Charlie"", 55, 0.90)

            passing1 <- student1.IsPassing()
            passing2 <- student2.IsPassing()
            passing3 <- student3.IsPassing()

            perf1 <- student1.GetPerformance()
            perf2 <- student2.GetPerformance()
            perf3 <- student3.GetPerformance()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var passing1 = interpreter.Manager.GetValue(new LangId("passing1"));
        var passing2 = interpreter.Manager.GetValue(new LangId("passing2"));
        var passing3 = interpreter.Manager.GetValue(new LangId("passing3"));

        var perf1 = interpreter.Manager.GetValue(new LangId("perf1"));
        var perf2 = interpreter.Manager.GetValue(new LangId("perf2"));
        var perf3 = interpreter.Manager.GetValue(new LangId("perf3"));

        Assert.NotNull(passing1);
        Assert.IsType<BoolLangValue>(passing1);
        Assert.True(((BoolLangValue)passing1).Value);

        Assert.NotNull(passing2);
        Assert.IsType<BoolLangValue>(passing2);
        Assert.True(((BoolLangValue)passing2).Value);

        Assert.NotNull(passing3);
        Assert.IsType<BoolLangValue>(passing3);
        Assert.False(((BoolLangValue)passing3).Value);

        Assert.NotNull(perf1);
        Assert.IsType<StringLangValue>(perf1);
        Assert.Equal("Excellent", ((StringLangValue)perf1).Value);

        Assert.NotNull(perf2);
        Assert.IsType<StringLangValue>(perf2);
        Assert.Equal("Satisfactory", ((StringLangValue)perf2).Value);

        Assert.NotNull(perf3);
        Assert.IsType<StringLangValue>(perf3);
        Assert.Equal("Failing", ((StringLangValue)perf3).Value);
    }

    [Fact]
    public void MemberAccess_WithCollections_AccessesCollectionMembers()
    {
        // Arrange
        var code = """

                               class Library {
                                   public books <- {}
                                   public members <- []

                                   func init() {
                                       this.books <- {
                                           {"title": "1984", "author": "Orwell", "year": 1949},
                                           {"title": "Brave New World", "author": "Huxley", "year": 1932},
                                           {"title": "Fahrenheit 451", "author": "Bradbury", "year": 1953}
                                       }
                                       this.members <- ["Alice", "Bob", "Charlie"]
                                   }

                                   func GetBook(index:int) -> any {
                                       return this.books[index]
                                   }

                                   func AddBook(title:string, author:string, year:int) {
                                       this.books.Add({"title": title, "author": author, "year": year})
                                   }

                                   func GetMemberCount() -> int {
                                       return len(this.members)
                                   }

                                   func GetBookTitles() -> list {
                                       titles <- {}
                                       for book in this.books {
                                           titles.Add(book["title"])
                                       }
                                       return titles
                                   }
                               }

                               library <- Library()
                               book1 <- library.GetBook(0)
                               title1 <- book1["title"]
                               author1 <- book1["author"]

                               library.AddBook("Animal Farm", "Orwell", 1945)
                               totalBooks <- len(library.books)

                               memberCount <- library.GetMemberCount()
                               allTitles <- library.GetBookTitles()
                           
                   """;
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var title1 = interpreter.Manager.GetValue(new LangId("title1"));
        var author1 = interpreter.Manager.GetValue(new LangId("author1"));
        var totalBooks = interpreter.Manager.GetValue(new LangId("totalBooks"));
        var memberCount = interpreter.Manager.GetValue(new LangId("memberCount"));

        Assert.NotNull(title1);
        Assert.IsType<StringLangValue>(title1);
        Assert.Equal("1984", ((StringLangValue)title1).Value);

        Assert.NotNull(author1);
        Assert.IsType<StringLangValue>(author1);
        Assert.Equal("Orwell", ((StringLangValue)author1).Value);

        Assert.NotNull(totalBooks);
        Assert.IsType<IntLangValue>(totalBooks);
        Assert.Equal(4, ((IntLangValue)totalBooks).Value);

        Assert.NotNull(memberCount);
        Assert.IsType<IntLangValue>(memberCount);
        Assert.Equal(3, ((IntLangValue)memberCount).Value);
    }

    [Fact]
    public void MemberAccess_WithInheritance_AccessesInheritedMembers()
    {
        // Arrange
        var code = @"
            class Vehicle {
                public brand <- """"
                public model <- """"
                public year <- 0

                func init(brand:string, model:string, year:int) {
                    this.brand <- brand
                    this.model <- model
                    this.year <- year
                }

                func GetInfo() -> string {
                    return year.ToStr() + "" "" + brand + "" "" + model
                }
            }

            class Car extends Vehicle {
                public doors <- 0
                public fuelType <- """"

                func init(brand:string, model:string, year:int, doors:int, fuelType:string) {
                    super.init(brand, model, year)
                    this.doors <- doors
                    this.fuelType <- fuelType
                }

                func GetCarInfo() -> string {
                    return GetInfo() + "" ("" + doors.ToStr() + "" doors, "" + fuelType + "")""
                }
            }

            car <- Car(""Toyota"", ""Camry"", 2022, 4, ""gasoline"")
            vehicleInfo <- car.GetInfo()
            carInfo <- car.GetCarInfo()
            brand <- car.brand
            doors <- car.doors
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var vehicleInfo = interpreter.Manager.GetValue(new LangId("vehicleInfo"));
        var carInfo = interpreter.Manager.GetValue(new LangId("carInfo"));
        var brand = interpreter.Manager.GetValue(new LangId("brand"));
        var doors = interpreter.Manager.GetValue(new LangId("doors"));

        Assert.NotNull(vehicleInfo);
        Assert.IsType<StringLangValue>(vehicleInfo);
        Assert.Equal("2022 Toyota Camry", ((StringLangValue)vehicleInfo).Value);

        Assert.NotNull(carInfo);
        Assert.IsType<StringLangValue>(carInfo);
        Assert.Equal("2022 Toyota Camry (4 doors, gasoline)", ((StringLangValue)carInfo).Value);

        Assert.NotNull(brand);
        Assert.IsType<StringLangValue>(brand);
        Assert.Equal("Toyota", ((StringLangValue)brand).Value);

        Assert.NotNull(doors);
        Assert.IsType<IntLangValue>(doors);
        Assert.Equal(4, ((IntLangValue)doors).Value);
    }

    [Fact]
    public void MemberAccess_WithErrorHandling_HandlesInvalidAccess()
    {
        // Arrange
        var code = @"
            class SafeAccess {
                public data <- {}
                public isValid <- true

                func init() {
                    this.data <- {""name"": ""Alice"", ""age"": 25}
                    this.isValid <- true
                }

                func GetSafe(key:string) -> any {
                    if this.data.ContainsKey(key) {
                        return this.data[key]
                    } else {
                        return null
                    }
                }

                func SetSafe(key:string, value:any) -> bool {
                    if this.isValid {
                        this.data[key] <- value
                        return true
                    }
                    return false
                }

                func Invalidate() {
                    isValid <- false
                }
            }

            safe <- SafeAccess()
            result1 <- safe.GetSafe(""name"")
            result2 <- safe.GetSafe(""nonexistent"")
            setResult1 <- safe.SetSafe(""city"", ""New York"")

            safe.Invalidate()
            setResult2 <- safe.SetSafe(""country"", ""USA"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var setResult1 = interpreter.Manager.GetValue(new LangId("setResult1"));
        var setResult2 = interpreter.Manager.GetValue(new LangId("setResult2"));

        Assert.NotNull(result1);
        Assert.IsType<StringLangValue>(result1);
        Assert.Equal("Alice", ((StringLangValue)result1).Value);

        Assert.NotNull(setResult1);
        Assert.IsType<BoolLangValue>(setResult1);
        Assert.True(((BoolLangValue)setResult1).Value);

        Assert.NotNull(setResult2);
        Assert.IsType<BoolLangValue>(setResult2);
        Assert.False(((BoolLangValue)setResult2).Value);
    }

    [Fact]
    public void MemberAccess_WithDynamicProperties_HandlesRuntimePropertyAccess()
    {
        // Arrange
        var code = @"
            class DynamicObject {
                public properties

                func init() {
                    this.properties <- dict()
                }

                func SetProperty(name:string, value:any) {
                    properties[name] <- value
                }

                func GetProperty(name:string) -> any {
                    return properties[name]
                }

                func HasProperty(name:string) -> bool {
                    return properties.ContainsKey(name)
                }

                func GetAllProperties() -> any {
                    return properties
                }
            }

            dynamicObj <- DynamicObject()
            dynamicObj.SetProperty(""name"", ""Dynamic"")
            dynamicObj.SetProperty(""value"", 42)
            dynamicObj.SetProperty(""isActive"", true)

            hasName <- dynamicObj.HasProperty(""name"")
            hasMissing <- dynamicObj.HasProperty(""missing"")

            nameValue <- dynamicObj.GetProperty(""name"")
            valueValue <- dynamicObj.GetProperty(""value"")
            isActiveValue <- dynamicObj.GetProperty(""isActive"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var hasName = interpreter.Manager.GetValue(new LangId("hasName"));
        var hasMissing = interpreter.Manager.GetValue(new LangId("hasMissing"));
        var nameValue = interpreter.Manager.GetValue(new LangId("nameValue"));
        var valueValue = interpreter.Manager.GetValue(new LangId("valueValue"));
        var isActiveValue = interpreter.Manager.GetValue(new LangId("isActiveValue"));

        Assert.NotNull(hasName);
        Assert.IsType<BoolLangValue>(hasName);
        Assert.True(((BoolLangValue)hasName).Value);

        Assert.NotNull(hasMissing);
        Assert.IsType<BoolLangValue>(hasMissing);
        Assert.False(((BoolLangValue)hasMissing).Value);

        Assert.NotNull(nameValue);
        Assert.IsType<StringLangValue>(nameValue);
        Assert.Equal("Dynamic", ((StringLangValue)nameValue).Value);

        Assert.NotNull(valueValue);
        Assert.IsType<IntLangValue>(valueValue);
        Assert.Equal(42, ((IntLangValue)valueValue).Value);

        Assert.NotNull(isActiveValue);
        Assert.IsType<BoolLangValue>(isActiveValue);
        Assert.True(((BoolLangValue)isActiveValue).Value);
    }
}