using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Old8Lang;
namespace Old8Lang.Tests.Interpreter.Modules;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Intermediates;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 原生库导入测试
/// 测试native语句、C#方法绑定、外部库调用等原生功能
/// </summary>
[Trait("Category", "Interpreter")]
[Trait("Category", "Interpreter-Modules")]
[Trait("Category", "Interpreter-NativeImport")]
public class NativeImportTests
{
    private readonly ITestOutputHelper _output;

    public NativeImportTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private Dictionary<string, object> TestInterpreter(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = new Dictionary<string, object>();
        // 这里需要根据实际情况提取变量值
        // 暂时返回空字典，让测试能够编译
        return result;
    }

    [Fact]
    public void NativeImport_BasicNativeStatement_SimpleNativeMethod()
    {
        var code = @"
            // 基础native语句测试
            native result <- Math.Abs(-5)
            final_result <- result
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("final_result"));
        Assert.Equal(5, Convert.ToInt32(result["final_result"]));
    }

    [Fact]
    public void NativeImport_NativeMathFunctions_MathOperations()
    {
        var code = @"
            // 原生数学函数测试
            native abs_result <- Math.Abs(-10)
            native sqrt_result <- Math.Sqrt(16)
            native pow_result <- Math.Pow(2, 3)
            native max_result <- Math.Max(5, 8)
            native min_result <- Math.Min(3, 7)
            math_results <- {
                abs: abs_result,
                sqrt: sqrt_result,
                power: pow_result,
                max: max_result,
                min: min_result
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("math_results"));
        var results = result["math_results"] as dynamic;
        Assert.Equal(10, Convert.ToInt32(results.abs));
        Assert.Equal(4, Convert.ToInt32(results.sqrt));
        Assert.Equal(8, Convert.ToInt32(results.power));
        Assert.Equal(8, Convert.ToInt32(results.max));
        Assert.Equal(3, Convert.ToInt32(results.min));
    }

    [Fact]
    public void NativeImport_NativeStringFunctions_StringOperations()
    {
        var code = @"
            // 原生字符串函数测试
            native upper_result <- String.ToUpper(""hello world"")
            native lower_result <- String.ToLower(""HELLO WORLD"")
            native length_result <- String.Length(""test string"")
            native trim_result <- String.Trim(""  spaced text  "")
            native contains_result <- String.Contains(""hello world"", ""world"")
            string_results <- {
                upper: upper_result,
                lower: lower_result,
                length: length_result,
                trimmed: trim_result,
                contains: contains_result
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("string_results"));
        var results = result["string_results"] as dynamic;
        Assert.Equal("HELLO WORLD", results.upper);
        Assert.Equal("hello world", results.lower);
        Assert.Equal(11, Convert.ToInt32(results.length));
        Assert.Equal("spaced text", results.trimmed);
        Assert.Equal(true, results.contains);
    }

    [Fact]
    public void NativeImport_NativeDateTimeFunctions_DateTimeOperations()
    {
        var code = @"
            // 原生日期时间函数测试
            native current_time <- DateTime.Now
            native today <- DateTime.Today
            native specific_date <- new DateTime(2023, 12, 25)
            native year <- specific_date.Year
            native month <- specific_date.Month
            native day <- specific_date.Day
            datetime_results <- {
                current: current_time,
                today: today,
                specific: specific_date,
                year: year,
                month: month,
                day: day
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("datetime_results"));
        var results = result["datetime_results"] as dynamic;
        Assert.Equal(2023, Convert.ToInt32(results.year));
        Assert.Equal(12, Convert.ToInt32(results.month));
        Assert.Equal(25, Convert.ToInt32(results.day));
    }

    [Fact]
    public void NativeImport_NativeConsoleFunctions_ConsoleOperations()
    {
        var code = @"
            // 原生控制台函数测试
            native title <- Console.Title
            native window_width <- Console.WindowWidth
            native window_height <- Console.WindowHeight
            console_info <- {
                title: title,
                width: window_width,
                height: window_height
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("console_info"));
        var info = result["console_info"] as dynamic;
        Assert.True(info.width > 0);
        Assert.True(info.height > 0);
    }

    [Fact]
    public void NativeImport_NativeFileOperations_FileOperations()
    {
        var code = @"
            // 原生文件操作测试
            native current_dir <- Environment.CurrentDirectory
            native temp_path <- Path.GetTempPath()
            native user_name <- Environment.UserName
            file_info <- {
                current_directory: current_dir,
                temp_path: temp_path,
                user_name: user_name
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("file_info"));
        var info = result["file_info"] as dynamic;
        Assert.NotNull(info.current_directory);
        Assert.NotNull(info.temp_path);
        Assert.NotNull(info.user_name);
    }

    [Fact]
    public void NativeImport_NativeEnvironmentVariables_EnvironmentAccess()
    {
        var code = @"
            // 原生环境变量测试
            native path_var <- Environment.GetEnvironmentVariable(""PATH"")
            native machine_name <- Environment.MachineName
            native os_version <- Environment.OSVersion
            native processor_count <- Environment.ProcessorCount
            env_info <- {
                path: path_var,
                machine_name: machine_name,
                os_version: os_version,
                processor_count: processor_count
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("env_info"));
        var info = result["env_info"] as dynamic;
        Assert.NotNull(info.machine_name);
        Assert.True(Convert.ToInt32(info.processor_count) > 0);
    }

    [Fact]
    public void NativeImport_NativeTypeConversion_TypeOperations()
    {
        var code = @"
            // 原生类型转换测试
            string_num <- ""123""
            native int_result <- Convert.ToInt32(string_num)
            native double_result <- Convert.ToDouble(string_num)
            native bool_result <- Convert.ToBoolean(1)
            native string_result <- Convert.ToString(456)
            conversion_results <- {
                int_value: int_result,
                double_value: double_result,
                bool_value: bool_result,
                string_value: string_result
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("conversion_results"));
        var results = result["conversion_results"] as dynamic;
        Assert.Equal(123, Convert.ToInt32(results.int_value));
        Assert.Equal(123.0, Convert.ToDouble(results.double_value));
        Assert.Equal(true, results.bool_value);
        Assert.Equal("456", results.string_value);
    }

    [Fact]
    public void NativeImport_NativeArrayOperations_ArrayManipulations()
    {
        var code = @"
            // 原生数组操作测试
            native array_length <- [1, 2, 3, 4, 5].Length
            native array_sort <- Array.Sort([3, 1, 4, 1, 5])
            native array_reverse <- Array.Reverse([1, 2, 3])
            array_results <- {
                original_length: array_length,
                sorted_length: array_sort,
                reversed_length: array_reverse
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("array_results"));
        var results = result["array_results"] as dynamic;
        Assert.Equal(5, Convert.ToInt32(results.original_length));
    }

    [Fact]
    public void NativeImport_NativeListOperations_ListManipulations()
    {
        var code = @"
            // 原生列表操作测试
            native list_count <- List({1, 2, 3}).Count
            native list_contains <- List({1, 2, 3}).Contains(2)
            native list_index <- List({""a"", ""b"", ""c""}).IndexOf(""b"")
            list_results <- {
                count: list_count,
                contains: list_contains,
                index: list_index
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("list_results"));
        var results = result["list_results"] as dynamic;
        Assert.Equal(3, Convert.ToInt32(results.count));
        Assert.Equal(true, results.contains);
        Assert.Equal(1, Convert.ToInt32(results.index));
    }

    [Fact]
    public void NativeImport_NativeDictionaryOperations_DictionaryManipulations()
    {
        var code = @"
            // 原生字典操作测试
            dict_var <- {""key1"": ""value1"", ""key2"": ""value2""}
            native dict_count <- dict_var.Count
            native contains_key <- dict_var.ContainsKey(""key1"")
            native contains_value <- dict_var.ContainsValue(""value2"")
            dict_results <- {
                count: dict_count,
                has_key: contains_key,
                has_value: contains_value
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("dict_results"));
        var results = result["dict_results"] as dynamic;
        Assert.Equal(2, Convert.ToInt32(results.count));
        Assert.Equal(true, results.has_key);
        Assert.Equal(true, results.has_value);
    }

    [Fact]
    public void NativeImport_NativeGuidOperations_GuidManipulations()
    {
        var code = @"
            // 原生GUID操作测试
            native new_guid <- Guid.NewGuid()
            native guid_string <- new_guid.ToString()
            native is_empty <- Guid.NewGuid() != Guid.Empty
            guid_results <- {
                new_id: new_guid,
                string_representation: guid_string,
                not_empty: is_empty
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("guid_results"));
        var results = result["guid_results"] as dynamic;
        Assert.NotNull(results.string_representation);
        Assert.Equal(true, results.not_empty);
    }

    [Fact]
    public void NativeImport_NativeRandomOperations_RandomNumbers()
    {
        var code = @"
            // 原生随机数操作测试
            native random_obj <- new Random()
            native random_int <- random_obj.Next(1, 100)
            native random_double <- random_obj.NextDouble()
            random_results <- {
                integer_value: random_int,
                double_value: random_double
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("random_results"));
        var results = result["random_results"] as dynamic;
        Assert.True(Convert.ToInt32(results.integer_value) >= 1);
        Assert.True(Convert.ToInt32(results.integer_value) <= 100);
        Assert.True(Convert.ToDouble(results.double_value) >= 0.0);
        Assert.True(Convert.ToDouble(results.double_value) < 1.0);
    }

    [Fact]
    public void NativeImport_NativeJsonOperations_JsonManipulations()
    {
        var code = @"
            // 原生JSON操作测试
            json_string -> ""{\""name\"": \""test\"", \""value\"": 123}""
            native json_parse <- Json.Parse(json_string)
            native json_stringify <- Json.Stringify({name: ""test"", value: 123})
            json_results <- {
                parsed: json_parse,
                stringified: json_stringify
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("json_results"));
        var results = result["json_results"] as dynamic;
        Assert.NotNull(results.stringified);
    }

    [Fact]
    public void NativeImport_NativeRegexOperations_RegexPattern()
    {
        var code = @"
            // 原生正则表达式操作测试
            test_text <- ""Hello World 123""
            native is_match <- Regex.IsMatch(test_text, ""\d+"")
            native match_result <- Regex.Match(test_text, ""\d+"")
            native replace_result <- Regex.Replace(test_text, ""\d+"", ""999"")
            regex_results <- {
                is_number_match: is_match,
                match_value: match_result,
                replaced_text: replace_result
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("regex_results"));
        var results = result["regex_results"] as dynamic;
        Assert.Equal(true, results.is_number_match);
        Assert.Equal("Hello World 999", results.replaced_text);
    }

    [Fact]
    public void NativeImport_NativeWebOperations_UriParsing()
    {
        var code = @"
            // 原生Web操作测试
            test_url <- ""https://www.example.com:8080/path?query=value""
            native uri_obj <- new Uri(test_url)
            native scheme <- uri_obj.Scheme
            native host <- uri_obj.Host
            native port <- uri_obj.Port
            native path <- uri_obj.AbsolutePath
            uri_results <- {
                protocol: scheme,
                hostname: host,
                port_number: port,
                file_path: path
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("uri_results"));
        var results = result["uri_results"] as dynamic;
        Assert.Equal("https", results.protocol);
        Assert.Equal("www.example.com", results.hostname);
        Assert.Equal(8080, Convert.ToInt32(results.port_number));
        Assert.Equal("/path", results.file_path);
    }

    [Fact]
    public void NativeImport_CustomNativeMethod_UserDefined()
    {
        var code = @"
            // 自定义原生方法测试
            custom_add <- func(a, b) -> a + b
            result <- custom_add(3, 4)
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("result"));
        Assert.Equal(7, Convert.ToInt32(result["result"]));
    }

    [Fact]
    public void NativeImport_NativeMethodParameters_ParameterPassing()
    {
        var code = @"
            // 原生方法参数传递测试
            param1 <- 10
            param2 <- 20
            native max_result <- Math.Max(param1, param2)
            native min_result <- Math.Min(param1, param2)
            param_results <- {
                maximum: max_result,
                minimum: min_result
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("param_results"));
        var results = result["param_results"] as dynamic;
        Assert.Equal(20, Convert.ToInt32(results.maximum));
        Assert.Equal(10, Convert.ToInt32(results.minimum));
    }

    [Fact]
    public void NativeImport_NativeMethodChaining_MethodChaining()
    {
        var code = @"
            // 原生方法链式调用测试
            test_string <- ""  Hello World  ""
            native chained_result <- test_string.Trim().ToLower()
            native length_result <- test_string.Trim().ToLower().Length
            chaining_results <- {
                processed_string: chained_result,
                final_length: length_result
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("chaining_results"));
        var results = result["chaining_results"] as dynamic;
        Assert.Equal("hello world", results.processed_string);
        Assert.Equal(11, Convert.ToInt32(results.final_length));
    }

    [Fact]
    public void NativeImport_NativeExceptionHandling_ErrorHandling()
    {
        var code = @"
            // 原生异常处理测试
            test_value <- 0
            try {
                native div_result <- 10 / test_value
                error_occurred <- false
            } catch {
                error_occurred <- true
            }
            exception_result <- {
                has_error: error_occurred
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("exception_result"));
        var exceptionResult = result["exception_result"] as dynamic;
        Assert.True(exceptionResult.has_error);
    }

    [Fact]
    public void NativeImport_NativeStaticProperties_StaticAccess()
    {
        var code = @"
            // 原生静态属性访问测试
            native math_pi <- Math.PI
            native math_e <- Math.E
            native today <- DateTime.Today
            static_results <- {
                pi_value: math_pi,
                e_value: math_e,
                current_date: today
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("static_results"));
        var results = result["static_results"] as dynamic;
        Assert.True(Convert.ToDouble(results.pi_value) > 3.14);
        Assert.True(Convert.ToDouble(results.e_value) > 2.71);
        Assert.NotNull(results.current_date);
    }

    [Fact]
    public void NativeImport_NativeComplexObjects_ObjectCreation()
    {
        var code = @"
            // 原生复杂对象创建测试
            native list_obj <- new List<string>()
            native dict_obj <- new Dictionary<string, int>()
            native array_obj <- new int[5]
            object_results <- {
                list_created: list_obj,
                dict_created: dict_obj,
                array_created: array_obj
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("object_results"));
        var results = result["object_results"] as dynamic;
        Assert.NotNull(results.list_created);
        Assert.NotNull(results.dict_created);
        Assert.NotNull(results.array_created);
    }

    [Fact]
    public void NativeImport_NativeMethodOverloading_OverloadedMethods()
    {
        var code = @"
            // 原生方法重载测试
            native abs_int <- Math.Abs(-5)
            native abs_double <- Math.Abs(-5.5)
            native round_default <- Math.Round(3.7)
            native round_digits <- Math.Round(3.14159, 2)
            overload_results <- {
                int_absolute: abs_int,
                double_absolute: abs_double,
                rounded_default: round_default,
                rounded_precise: round_digits
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("overload_results"));
        var results = result["overload_results"] as dynamic;
        Assert.Equal(5, Convert.ToInt32(results.int_absolute));
        Assert.Equal(5.5, Convert.ToDouble(results.double_absolute));
        Assert.Equal(4, Convert.ToInt32(results.rounded_default));
        Assert.Equal(3.14, Convert.ToDouble(results.rounded_precise));
    }

    [Fact]
    public void NativeImport_NativeGenericMethods_GenericUsage()
    {
        var code = @"
            // 原生泛型方法测试
            native array_result <- Array.Find([1, 2, 3, 4, 5], func(x) -> x == 3)
            native list_result <- List({1, 2, 3}).Find(func(x) -> x == 2)
            generic_results <- {
                found_in_array: array_result,
                found_in_list: list_result
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("generic_results"));
        var results = result["generic_results"] as dynamic;
        Assert.Equal(3, Convert.ToInt32(results.found_in_array));
        Assert.Equal(2, Convert.ToInt32(results.found_in_list));
    }

    [Fact]
    public void NativeImport_NativeExtensionMethods_ExtensionUsage()
    {
        var code = @"
            // 原生扩展方法测试
            test_string <- ""hello""
            native starts_with <- test_string.StartsWith(""h"")
            native ends_with <- test_string.EndsWith(""o"")
            native is_null_or_empty <- String.IsNullOrEmpty("""")
            extension_results <- {
                starts_correctly: starts_with,
                ends_correctly: ends_with,
                empty_check: is_null_or_empty
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("extension_results"));
        var results = result["extension_results"] as dynamic;
        Assert.Equal(true, results.starts_correctly);
        Assert.Equal(true, results.ends_correctly);
        Assert.Equal(true, results.empty_check);
    }

    [Fact]
    public void NativeImport_NativeAsyncOperations_AsyncHandling()
    {
        var code = @"
            // 原生异步操作测试
            native delay_result <- Task.Delay(100)
            async_results <- {
                delay_task: delay_result
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("async_results"));
        var results = result["async_results"] as dynamic;
        Assert.NotNull(results.delay_task);
    }

    [Fact]
    public void NativeImport_NativeReflectionOperations_TypeInformation()
    {
        var code = @"
            // 原生反射操作测试
            test_var <- ""Hello World""
            native type_info <- test_var.GetType()
            native type_name <- type_info.Name
            native assembly_info <- type_info.Assembly
            reflection_results <- {
                object_type: type_name,
                assembly_name: assembly_info
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("reflection_results"));
        var results = result["reflection_results"] as dynamic;
        Assert.NotNull(results.object_type);
        Assert.NotNull(results.assembly_name);
    }

    [Fact]
    public void NativeImport_NativeCollectionsCollections_SpecializedCollections()
    {
        var code = @"
            // 原生集合操作测试
            native hashset_obj <- new HashSet<int>()
            native queue_obj <- new Queue<string>()
            native stack_obj <- new Stack<int>()
            collection_results <- {
                hashset_created: hashset_obj,
                queue_created: queue_obj,
                stack_created: stack_obj
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("collection_results"));
        var results = result["collection_results"] as dynamic;
        Assert.NotNull(results.hashset_created);
        Assert.NotNull(results.queue_created);
        Assert.NotNull(results.stack_created);
    }

    [Fact]
    public void NativeImport_NativeEnumOperations_EnumHandling()
    {
        var code = @"
            // 原生枚举操作测试
            native day_of_week <- DayOfWeek.Monday
            native day_name <- day_of_week.ToString()
            native day_value <- Convert.ToInt32(day_of_week)
            enum_results <- {
                enum_value: day_name,
                integer_value: day_value
            }
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("enum_results"));
        var results = result["enum_results"] as dynamic;
        Assert.Equal("Monday", results.enum_value);
        Assert.Equal(1, Convert.ToInt32(results.integer_value));
    }
}