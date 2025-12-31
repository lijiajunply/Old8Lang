using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.AST;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Types;

/// <summary>
/// 编译器模式下的类型系统测试 - 枚举
/// </summary>
    public class EnumTests
    {
        private readonly ITestOutputHelper _output;

        public EnumTests(ITestOutputHelper output)
        {
            _output = output;
        }

    [Fact]
    public void BasicEnum_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            enum Color {
                Red,    // 0
                Green,  // 1
                Blue    // 2
            }
            
            redValue <- Color.Red
            greenValue <- Color.Green
            blueValue <- Color.Blue
            
            Assert.Equal(0, redValue)
            Assert.Equal(1, greenValue)
            Assert.Equal(2, blueValue)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EnumWithExplicitValues_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            enum HttpStatus {
                OK <- 200,
                Created <- 201,
                BadRequest <- 400,
                NotFound <- 404,
                InternalServerError <- 500
            }
            
            okCode <- HttpStatus.OK
            createdCode <- HttpStatus.Created
            notFoundCode <- HttpStatus.NotFound
            
            Assert.Equal(200, okCode)
            Assert.Equal(201, createdCode)
            Assert.Equal(404, notFoundCode)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EnumWithMixedValues_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            enum Priority {
                Low,        // 0
                Medium <- 5,  // 5
                High,       // 6
                Critical <- 10 // 10
            }
            
            lowValue <- Priority.Low
            mediumValue <- Priority.Medium
            highValue <- Priority.High
            criticalValue <- Priority.Critical
            
            Assert.Equal(0, lowValue)
            Assert.Equal(5, mediumValue)
            Assert.Equal(6, highValue)
            Assert.Equal(10, criticalValue)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EmptyEnum_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            enum Empty {
            }
            
            // 空枚举没有成员，访问应该通过其他方式处理
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void SingleMemberEnum_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            enum SingleValue {
                OnlyOne  // 0
            }
            
            singleValue <- SingleValue.OnlyOne
            Assert.Equal(0, singleValue)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EnumWithAccessModifiers_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            public enum PublicEnum {
                Value1,  // 0
                Value2   // 1
            }
            
            private enum PrivateEnum {
                ValueA,  // 0
                ValueB   // 1
            }
            
            publicValue1 <- PublicEnum.Value1
            publicValue2 <- PublicEnum.Value2
            
            // 私有枚举可能需要在类内部访问
            Assert.Equal(0, publicValue1)
            Assert.Equal(1, publicValue2)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EnumInSwitchStatement_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            enum Direction {
                North,  // 0
                East,   // 1
                South,  // 2
                West    // 3
            }
            
            direction <- Direction.East
            result <- """"
            
            switch direction {
                case 0 {
                    result <- ""Going North""
                }
                case 1 {
                    result <- ""Going East""
                }
                case 2 {
                    result <- ""Going South""
                }
                case 3 {
                    result <- ""Going West""
                }
            }
            
            Assert.Equal(""Going East"", result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EnumComparison_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            enum Status {
                InProgress <- 1,
                Completed <- 2,
                Failed <- 3
            }
            
            inProgress <- Status.InProgress
            completed <- Status.Completed
            failed <- Status.Failed
            
            // 测试枚举值的比较
            isCompleted <- completed > inProgress
            isFailed <- failed > completed
            isNotFailed <- inProgress < failed
            
            Assert.Equal(true, isCompleted)
            Assert.Equal(true, isFailed)
            Assert.Equal(true, isNotFailed)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EnumInFunctionParameters_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            enum LogLevel {
                Debug <- 0,
                Info <- 1,
                Warning <- 2,
                Error <- 3
            }
            
            func logMessage(level:LogLevel, message:string) -> void {
                levelValue <- level
                if levelValue == 0 {
                    // Debug 级别
                    Assert.Equal(""Debug: "" + message)
                } else if levelValue == 1 {
                    // Info 级别
                    Assert.Equal(""Info: "" + message)
                } else if levelValue == 2 {
                    // Warning 级别
                    Assert.Equal(""Warning: "" + message)
                } else if levelValue == 3 {
                    // Error 级别
                    Assert.Equal(""Error: "" + message)
                }
            }
            
            // 测试不同的日志级别
            logMessage(LogLevel.Debug, ""Debug message"")
            logMessage(LogLevel.Info, ""Info message"")
            logMessage(LogLevel.Warning, ""Warning message"")
            logMessage(LogLevel.Error, ""Error message"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EnumInReturnTypes_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            enum ResultType {
                Success <- 1,
                Error <- 2
            }
            
            func processOperation(input:int) -> ResultType {
                if input > 0 {
                    return ResultType.Success
                } else {
                    return ResultType.Error
                }
            }
            
            // 测试返回类型
            result1 <- processOperation(10)  // Success = 1
            result2 <- processOperation(-5)  // Error = 2
            
            Assert.Equal(1, result1)
            Assert.Equal(2, result2)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EnumToStringConversion_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            enum DayOfWeek {
                Monday,    // 0
                Tuesday,   // 1
                Wednesday, // 2
                Thursday,  // 3
                Friday,    // 4
                Saturday, // 5
                Sunday     // 6
            }
            
            func getDayName(day:DayOfWeek) -> string {
                dayValue <- day
                if dayValue == 0 {
                    return ""Monday""
                } else if dayValue == 1 {
                    return ""Tuesday""
                } else if dayValue == 2 {
                    return ""Wednesday""
                } else if dayValue == 3 {
                    return ""Thursday""
                } else if dayValue == 4 {
                    return ""Friday""
                } else if dayValue == 5 {
                    return ""Saturday""
                } else if dayValue == 6 {
                    return ""Sunday""
                } else {
                    return ""Unknown""
                }
            }
            
            // 测试星期名称转换
            mondayName <- getDayName(DayOfWeek.Monday)
            wednesdayName <- getDayName(DayOfWeek.Wednesday)
            saturdayName <- getDayName(DayOfWeek.Saturday)
            
            Assert.Equal(""Monday"", mondayName)
            Assert.Equal(""Wednesday"", wednesdayName)
            Assert.Equal(""Saturday"", saturdayName)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EnumRangeValidation_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            enum SmallRange {
                Min,    // 0
                Middle,  // 1
                Max     // 2
            }
            
            enum LargeRange {
                Start <- 1000,
                Middle <- 5000,
                End <- 10000
            }
            
            // 测试小范围
            smallMin <- SmallRange.Min
            smallMiddle <- SmallRange.Middle
            smallMax <- SmallRange.Max
            
            // 测试大范围
            largeStart <- LargeRange.Start
            largeMiddle <- LargeRange.Middle
            largeEnd <- LargeRange.End
            
            Assert.Equal(0, smallMin)
            Assert.Equal(1, smallMiddle)
            Assert.Equal(2, smallMax)
            
            Assert.Equal(1000, largeStart)
            Assert.Equal(5000, largeMiddle)
            Assert.Equal(10000, largeEnd)
            
            // 验证范围关系
            Assert.True(smallMin <= smallMiddle && smallMiddle <= smallMax)
            Assert.True(largeStart <= largeMiddle && largeMiddle <= largeEnd)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EnumWithComplexLogic_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            enum Season {
                Spring,  // 0
                Summer,  // 1
                Fall,    // 2
                Winter   // 3
            }
            
            enum Weather {
                Sunny,    // 0
                Cloudy,   // 1
                Rainy,    // 2
                Snowy     // 3
            }
            
            func getSeasonWeather(season:Season, weather:Weather) -> string {
                seasonValue <- season
                weatherValue <- weather
                
                // 春天和晴天
                if seasonValue == 0 && weatherValue == 0 {
                    return ""Perfect spring day""
                }
                // 夏天和下雨
                if seasonValue == 1 && weatherValue == 2 {
                    return ""Summer rain""
                }
                // 秋天和多云
                if seasonValue == 2 && weatherValue == 1 {
                    return ""Overcast autumn""
                }
                // 冬天和下雪
                if seasonValue == 3 && weatherValue == 3 {
                    return ""Winter wonderland""
                }
                
                // 其他组合
                return ""Season: "" + (seasonValue as string) + "", Weather: "" + (weatherValue as string)
            }
            
            // 测试不同组合
            result1 <- getSeasonWeather(Season.Spring, Weather.Sunny)
            result2 <- getSeasonWeather(Season.Summer, Weather.Rainy)
            result3 <- getSeasonWeather(Season.Fall, Weather.Cloudy)
            result4 <- getSeasonWeather(Season.Winter, Weather.Snowy)
            result5 <- getSeasonWeather(Season.Spring, Weather.Cloudy)  // 混合情况
            
            Assert.Equal(""Perfect spring day"", result1)
            Assert.Equal(""Summer rain"", result2)
            Assert.Equal(""Overcast autumn"", result3)
            Assert.Equal(""Winter wonderland"", result4)
            Assert.Equal(""Season: 0, Weather: 1"", result5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}