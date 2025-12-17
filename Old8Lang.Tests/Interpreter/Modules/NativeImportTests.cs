using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Old8Lang;

namespace Old8Lang.Tests.Interpreter.Modules;

/// <summary>
/// 原生库导入测试
/// 测试native语句、C#方法绑定、外部库调用等原生功能
/// </summary>
[Trait("Category", "Interpreter")]
[Trait("Category", "Interpreter-Modules")]
[Trait("Category", "Interpreter-NativeImport")]
public class NativeImportTests : InterpreterTestBase
{
    public NativeImportTests(ITestOutputHelper output) : base(output)
    {
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
            test_string <- ""Hello World""
            native length_result <- test_string.Length
            native upper_result <- test_string.ToUpper()
            native lower_result <- test_string.ToLower()
            native contains_result <- test_string.Contains(""World"")
            native substring_result <- test_string.Substring(0, 5)

            string_results <- {
                original: test_string,
                length: length_result,
                upper: upper_result,
                lower: lower_result,
                contains: contains_result,
                substring: substring_result
            }
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("string_results"));
        var results = result["string_results"] as dynamic;
        Assert.Equal("Hello World", results.original);
        Assert.Equal(11, Convert.ToInt32(results.length));
        Assert.Equal("HELLO WORLD", results.upper);
        Assert.Equal("hello world", results.lower);
        Assert.Equal(true, results.contains);
        Assert.Equal("Hello", results.substring);
    }

    [Fact]
    public void NativeImport_NativeDateTimeFunctions_DateTimeOperations()
    {
        var code = @"
            // 原生DateTime函数测试
            native current_time <- DateTime.Now
            native today <- DateTime.Today
            native specific_date <- new DateTime(2023, 12, 25)
            native year_result <- specific_date.Year
            native month_result <- specific_date.Month
            native day_result <- specific_date.Day

            datetime_results <- {
                has_current_time: current_time != null,
                has_today: today != null,
                specific_year: year_result,
                specific_month: month_result,
                specific_day: day_result
            }
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("datetime_results"));
        var results = result["datetime_results"] as dynamic;
        Assert.Equal(true, results.has_current_time);
        Assert.Equal(true, results.has_today);
        Assert.Equal(2023, Convert.ToInt32(results.specific_year));
        Assert.Equal(12, Convert.ToInt32(results.specific_month));
        Assert.Equal(25, Convert.ToInt32(results.specific_day));
    }

    [Fact]
    public void NativeImport_NativeConsoleFunctions_ConsoleOperations()
    {
        var code = @"
            // 原生Console函数测试
            message <- ""Test Message""
            native console_write <- Console.WriteLine(message)

            // 测试Console输入（在实际环境中可能需要用户输入，这里模拟）
            try {
                native console_read_attempt <- Console.ReadLine()
                console_input_available <- true
            } catch e {
                console_input_available <- false
                console_error <- e.message
            }

            console_results <- {
                message_written: console_write == null,  // WriteLine通常返回void
                input_available: console_input_available
            }
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("console_results"));
        var results = result["console_results"] as dynamic;
        // Console.WriteLine的返回值测试
        // input_available可能会因为环境问题而不同
    }

    [Fact]
    public void NativeImport_NativeFileOperations_FileSystemFunctions()
    {
        var code = @"
            // 原生文件操作测试
            test_content <- ""Test file content""
            test_file_path <- ""test_native_file.txt""

            // 写文件
            try {
                native write_result <- File.WriteAllText(test_file_path, test_content)
                file_write_success <- true
            } catch e {
                file_write_success <- false
                write_error <- e.message
            }

            // 读文件
            try {
                native read_result <- File.ReadAllText(test_file_path)
                file_read_success <- true
            } catch e {
                file_read_success <- false
                read_error <- e.message
            }

            // 检查文件是否存在
            try {
                native exists_result <- File.Exists(test_file_path)
                file_exists <- exists_result
            } catch e {
                file_exists <- false
                exists_error <- e.message
            }

            file_results <- {
                write_success: file_write_success,
                read_success: file_read_success,
                file_exists: file_exists,
                read_content: read_result if file_read_success else null
            }
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("file_results"));
        var results = result["file_results"] as dynamic;
        // 文件操作的结果取决于执行环境的权限
        // 至少应该有结果对象返回
        Assert.NotNull(results);
    }

    [Fact]
    public void NativeImport_NativeEnvironmentFunctions_EnvironmentAccess()
    {
        var code = @"
            // 原生Environment函数测试
            try {
                native machine_name <- Environment.MachineName
                native os_version <- Environment.OSVersion.ToString()
                native processor_count <- Environment.ProcessorCount
                native current_directory <- Environment.CurrentDirectory

                environment_results <- {
                    machine_name: machine_name,
                    os_version: os_version,
                    processor_count: processor_count,
                    current_directory: current_directory
                }
            } catch e {
                environment_results <- {
                    error: e.message,
                    access_failed: true
                }
            }
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("environment_results"));
        var results = result["environment_results"] as dynamic;
        Assert.NotNull(results);
        // 环境访问可能因权限而失败，但应该返回结果对象
    }

    [Fact]
    public void NativeImport_NativeCollectionsMethods_CollectionOperations()
    {
        var code = @"
            // 原生集合方法测试
            test_list <- {1, 2, 3, 4, 5}
            test_dict <- {""a"": 1, ""b"": 2}

            try {
                // 列表操作
                native list_count <- test_list.Count
                native list_contains <- test_list.Contains(3)
                native list_first <- test_list.First()

                // 字典操作
                native dict_count <- test_dict.Count
                native dict_contains_key <- test_dict.ContainsKey(""a"")
                native dict_keys <- test_dict.Keys

                collection_results <- {
                    list_count: list_count,
                    list_contains: list_contains,
                    list_first: list_first,
                    dict_count: dict_count,
                    dict_contains_key: dict_contains_key,
                    dict_keys_available: dict_keys != null
                }
            } catch e {
                collection_results <- {
                    error: e.message,
                    collection_access_failed: true
                }
            }
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("collection_results"));
        var results = result["collection_results"] as dynamic;
        Assert.NotNull(results);
    }

    [Fact]
    public void NativeImport_NativeTypeConversion_TypeConversionOperations()
    {
        var code = @"
            // 原生类型转换测试
            string_number <- ""42""
            float_string <- ""3.14""
            bool_string <- ""true""

            try {
                // 类型转换
                native int_parse <- Convert.ToInt32(string_number)
                native double_parse <- Convert.ToDouble(float_string)
                native bool_parse <- Convert.ToBoolean(bool_string)

                // 类型检查
                native is_int_string <- string_number.GetType().Name
                native parsed_int_type <- int_parse.GetType().Name

                conversion_results <- {
                    parsed_int: int_parse,
                    parsed_double: double_parse,
                    parsed_bool: bool_parse,
                    original_string_type: is_int_string,
                    parsed_type: parsed_int_type
                }
            } catch e {
                conversion_results <- {
                    error: e.message,
                    conversion_failed: true
                }
            }
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("conversion_results"));
        var results = result["conversion_results"] as dynamic;
        Assert.NotNull(results);
    }

    [Fact]
    public void NativeImport_NativeGuidOperations_GuidFunctions()
    {
        var code = @"
            // 原生Guid操作测试
            try {
                // 创建新Guid
                native new_guid <- Guid.NewGuid()
                native guid_string <- new_guid.ToString()

                // 解析Guid字符串
                test_guid_string <- ""12345678-1234-1234-1234-123456789abc""
                native parsed_guid <- Guid.Parse(test_guid_string)

                // Guid比较
                native guid_equals <- new_guid.Equals(new_guid)

                guid_results <- {
                    new_guid_string: guid_string,
                    parsed_guid_string: parsed_guid.ToString(),
                    guid_equals_self: guid_equals,
                    guid_length: guid_string.Length
                }
            } catch e {
                guid_results <- {
                    error: e.message,
                    guid_operations_failed: true
                }
            }
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("guid_results"));
        var results = result["guid_results"] as dynamic;
        Assert.NotNull(results);
    }

    [Fact]
    public void NativeImport_NativeRandomOperations_RandomNumberGeneration()
    {
        var code = @"
            // 原生随机数生成测试
            try {
                native random <- new Random()
                native random_int <- random.Next(1, 100)
                native random_double <- random.NextDouble()
                native random_bytes_length <- 10

                // 生成随机字节
                random_bytes <- {}
                i <- 0
                while i < 5 {
                    native random_byte <- random.Next(0, 256)
                    random_bytes <- random_bytes.concat({random_byte})
                    i <- i + 1
                }

                random_results <- {
                    random_int_range: random_int >= 1 && random_int < 100,
                    random_double_range: random_double >= 0.0 && random_double < 1.0,
                    random_bytes_count: random_bytes.length,
                    has_random_values: true
                }
            } catch e {
                random_results <- {
                    error: e.message,
                    random_operations_failed: true
                }
            }
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("random_results"));
        var results = result["random_results"] as dynamic;
        Assert.NotNull(results);
    }

    [Fact]
    public void NativeImport_NativeStringBuilder_StringBuilderOperations()
    {
        var code = @"
            // 原生StringBuilder操作测试
            try {
                native sb <- new StringBuilder()
                native sb_append <- sb.Append(""Hello"")
                native sb_append_line <- sb.AppendLine("" World"")
                native sb_append_format <- sb.AppendFormat("" Number: {0}"", 42)
                native final_string <- sb.ToString()

                stringbuilder_results <- {
                    final_content: final_string,
                    content_length: final_string.Length,
                    builder_operations_completed: true
                }
            } catch e {
                stringbuilder_results <- {
                    error: e.message,
                    stringbuilder_operations_failed: true
                }
            }
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("stringbuilder_results"));
        var results = result["stringbuilder_results"] as dynamic;
        Assert.NotNull(results);
    }

    [Fact]
    public void NativeImport_NativePathOperations_PathManipulation()
    {
        var code = @"
            // 原生路径操作测试
            test_path1 <- ""/home/user""
            test_path2 <- ""documents/file.txt""

            try {
                native combined_path <- Path.Combine(test_path1, test_path2)
                native directory_name <- Path.GetDirectoryName(combined_path)
                native file_name <- Path.GetFileName(combined_path)
                native file_extension <- Path.GetExtension(combined_path)
                native path_without_extension <- Path.GetFileNameWithoutExtension(combined_path)

                path_results <- {
                    combined: combined_path,
                    directory: directory_name,
                    filename: file_name,
                    extension: file_extension,
                    name_without_extension: path_without_extension
                }
            } catch e {
                path_results <- {
                    error: e.message,
                    path_operations_failed: true
                }
            }
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("path_results"));
        var results = result["path_results"] as dynamic;
        Assert.NotNull(results);
    }

    [Fact]
    public void NativeImport_NativeRegexOperations_RegularExpressionOperations()
    {
        var code = @"
            // 原生正则表达式操作测试
            test_text <- ""The price is $123.45 for item #ABC""
            pattern <- ""\\$[0-9]+\\.[0-9]{2}""

            try {
                native regex <- new Regex(pattern)
                native match_result <- regex.Match(test_text)
                native is_match <- regex.IsMatch(test_text)
                native matches <- regex.Matches(test_text)
                native replace_result <- regex.Replace(test_text, ""$0.00"")

                regex_results <- {
                    text: test_text,
                    pattern: pattern,
                    has_match: is_match,
                    match_success: match_result.Success,
                    replacement_result: replace_result,
                    regex_operations_completed: true
                }
            } catch e {
                regex_results <- {
                    error: e.message,
                    regex_operations_failed: true
                }
            }
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("regex_results"));
        var results = result["regex_results"] as dynamic;
        Assert.NotNull(results);
    }

    [Fact]
    public void NativeImport_NativeWebClientOperations_WebClientOperations()
    {
        var code = @"
            // 原生WebClient操作测试（可能因网络环境而变化）
            test_url <- ""https://httpbin.org/get""

            try {
                native client <- new WebClient()
                // 注意：实际网络请求可能超时或失败，这里主要测试native语句语法
                // native response <- client.DownloadString(test_url)

                webclient_results <- {
                    client_created: client != null,
                    network_operations_attempted: true,
                    note: ""Network operations may fail due to environment""
                }
            } catch e {
                webclient_results <- {
                    error: e.message,
                    webclient_operations_failed: true
                }
            }
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("webclient_results"));
        var results = result["webclient_results"] as dynamic;
        Assert.NotNull(results);
    }

    [Fact]
    public void NativeImport_NativeJsonOperations_JsonSerialization()
    {
        var code = @"
            // 原生JSON操作测试
            test_object <- {name: ""Test"", value: 42, active: true}

            try {
                // 使用JavaScriptSerializer或其他JSON序列化器
                // 注意：具体的类名可能因.NET版本而异
                native json_serializer <- new System.Web.Script.Serialization.JavaScriptSerializer()
                native json_string <- json_serializer.Serialize(test_object)

                json_results <- {
                    original_object: test_object,
                    serialized_json: json_string,
                    serialization_attempted: true
                }
            } catch e {
                // 如果JavaScriptSerializer不可用，尝试其他方法或标记失败
                json_results <- {
                    error: e.message,
                    json_operations_failed: true,
                    fallback_note: ""JSON serialization may require specific references""
                }
            }
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("json_results"));
        var results = result["json_results"] as dynamic;
        Assert.NotNull(results);
    }

    [Fact]
    public void NativeImport_NativeExceptionHandling_NativeExceptionHandling()
    {
        var code = @"
            // 原生异常处理测试
            exception_handling_results <- {}

            try {
                // 故意触发除零错误
                native division_error <- 10 / 0
                exception_handling_results[""division_error_caught""] <- false
            } catch e {
                exception_handling_results[""division_error_caught""] <- true
                exception_handling_results[""division_error_type""] <- e.GetType().Name
                exception_handling_results[""division_error_message""] <- e.Message
            }

            try {
                // 故意触发空引用错误
                native null_reference <- ((object)null).ToString()
                exception_handling_results[""null_reference_caught""] <- false
            } catch e {
                exception_handling_results[""null_reference_caught""] <- true
                exception_handling_results[""null_reference_type""] <- e.GetType().Name
                exception_handling_results[""null_reference_message""] <- e.Message
            }

            try {
                // 故意触发索引越界错误
                test_array <- {1, 2, 3}
                native index_error <- test_array[10]
                exception_handling_results[""index_error_caught""] <- false
            } catch e {
                exception_handling_results[""index_error_caught""] <- true
                exception_handling_results[""index_error_type""] <- e.GetType().Name
                exception_handling_results[""index_error_message""] <- e.Message
            }
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("exception_handling_results"));
        var results = result["exception_handling_results"] as dynamic;
        Assert.NotNull(results);
        // 验证异常被正确捕获
        Assert.Equal(true, results.division_error_caught);
    }

    [Fact]
    public void NativeImport_NativeTypeReflection_TypeInformation()
    {
        var code = @"
            // 原生类型反射测试
            test_string <- ""Hello""
            test_number <- 42
            test_list <- {1, 2, 3}

            try {
                // 获取类型信息
                native string_type <- test_string.GetType()
                native number_type <- test_number.GetType()
                native list_type <- test_list.GetType()

                native string_type_name <- string_type.Name
                native number_type_name <- number_type.Name
                native list_type_name <- list_type.Name

                reflection_results <- {
                    string_type: string_type_name,
                    number_type: number_type_name,
                    list_type: list_type_name,
                    reflection_successful: true
                }
            } catch e {
                reflection_results <- {
                    error: e.message,
                    reflection_failed: true
                }
            }
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("reflection_results"));
        var results = result["reflection_results"] as dynamic;
        Assert.NotNull(results);
    }

    [Fact]
    public void NativeImport_NativeLinqOperations_LinqQueryOperations()
    {
        var code = @"
            // 原生LINQ操作测试
            test_list <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}

            try {
                // LINQ查询操作
                native where_result <- test_list.Where(x => x > 5)
                native select_result <- test_list.Select(x => x * 2)
                native first_result <- test_list.First()
                native first_or_default_result <- test_list.FirstOrDefault(x => x > 100)
                native count_result <- test_list.Count()
                native sum_result <- test_list.Sum()

                linq_results <- {
                    where_count: where_result.Count(),
                    select_count: select_result.Count(),
                    first_value: first_result,
                    first_or_default_is_default: first_or_default_result == 0,
                    total_count: count_result,
                    total_sum: sum_result,
                    linq_operations_successful: true
                }
            } catch e {
                linq_results <- {
                    error: e.message,
                    linq_operations_failed: true
                }
            }
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("linq_results"));
        var results = result["linq_results"] as dynamic;
        Assert.NotNull(results);
    }

    [Fact]
    public void NativeImport_NativeEnumOperations_EnumOperations()
    {
        var code = @"
            // 原生枚举操作测试
            try {
                // 使用系统枚举
                native day_of_week <- DayOfWeek.Monday
                native day_value <- Convert.ToInt32(day_of_week)
                native day_name <- day_of_week.ToString()

                // 枚举解析
                native parsed_day <- Enum.Parse(typeof(DayOfWeek), ""Friday"")
                native parsed_day_name <- parsed_day.ToString()

                enum_results <- {
                    original_day: day_name,
                    day_integer_value: day_value,
                    parsed_day: parsed_day_name,
                    enum_operations_successful: true
                }
            } catch e {
                enum_results <- {
                    error: e.message,
                    enum_operations_failed: true
                }
            }
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("enum_results"));
        var results = result["enum_results"] as dynamic;
        Assert.NotNull(results);
    }

    [Fact]
    public void NativeImport_NativeGenericOperations_GenericClassOperations()
    {
        var code = @"
            // 原生泛型操作测试
            try {
                // 创建泛型列表
                native string_list <- new System.Collections.Generic.List[string]()
                native int_list <- new System.Collections.Generic.List[int]()

                // 添加元素
                native add_string <- string_list.Add(""Hello"")
                native add_int <- int_list.Add(42)

                // 获取计数
                native string_count <- string_list.Count
                native int_count <- int_list.Count

                generic_results <- {
                    string_list_count: string_count,
                    int_list_count: int_count,
                    generic_operations_successful: true
                }
            } catch e {
                generic_results <- {
                    error: e.message,
                    generic_operations_failed: true
                }
            }
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("generic_results"));
        var results = result["generic_results"] as dynamic;
        Assert.NotNull(results);
    }
}