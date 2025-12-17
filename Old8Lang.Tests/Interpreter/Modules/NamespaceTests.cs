using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Old8Lang;

namespace Old8Lang.Tests.Interpreter.Modules;

/// <summary>
/// 命名空间测试
/// 测试模块命名空间、作用域隔离、命名冲突解决等
/// </summary>
[Trait("Category", "Interpreter")]
[Trait("Category", "Interpreter-Modules")]
[Trait("Category", "Interpreter-Namespace")]
public class NamespaceTests : InterpreterTestBase
{
    public NamespaceTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void Namespace_BasicNamespace_SimpleNamespaceDeclaration()
    {
        var code = @"
            namespace MyModule {
                module_variable <- ""module_value""
                module_function <- func() {
                    return ""module_function_result""
                }
            }

            // 访问命名空间成员
            accessed_value <- MyModule.module_variable
            accessed_function_result <- MyModule.module_function()
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("accessed_value"));
        Assert.True(result.ContainsKey("accessed_function_result"));
        Assert.Equal("module_value", result["accessed_value"]);
        Assert.Equal("module_function_result", result["accessed_function_result"]);
    }

    [Fact]
    public void Namespace_NestedNamespace_NestedNamespaceDeclaration()
    {
        var code = @"
            namespace Outer {
                outer_variable <- ""outer_value""

                namespace Inner {
                    inner_variable <- ""inner_value""
                    inner_function <- func() {
                        return ""inner_function_result""
                    }
                }

                outer_function <- func() {
                    return ""outer_function_result""
                }
            }

            // 访问嵌套命名空间成员
            outer_value <- Outer.outer_variable
            inner_value <- Outer.Inner.inner_variable
            inner_function_result <- Outer.Inner.inner_function()
            outer_function_result <- Outer.outer_function()
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("outer_value"));
        Assert.True(result.ContainsKey("inner_value"));
        Assert.True(result.ContainsKey("inner_function_result"));
        Assert.True(result.ContainsKey("outer_function_result"));

        Assert.Equal("outer_value", result["outer_value"]);
        Assert.Equal("inner_value", result["inner_value"]);
        Assert.Equal("inner_function_result", result["inner_function_result"]);
        Assert.Equal("outer_function_result", result["outer_function_result"]);
    }

    [Fact]
    public void Namespace_MultipleNamespaces_MultipleNamespaceDeclarations()
    {
        var code = @"
            namespace MathUtils {
                add <- func(a, b) { return a + b }
                multiply <- func(a, b) { return a * b }
                constant_pi <- 3.14159
            }

            namespace StringUtils {
                reverse <- func(s) {
                    result <- """"
                    i <- s.length - 1
                    while i >= 0 {
                        result <- result + s[i]
                        i <- i - 1
                    }
                    return result
                }
                to_upper <- func(s) { return s.toUpperCase() }
            }

            namespace ArrayUtils {
                sum <- func(arr) {
                    total <- 0
                    i <- 0
                    while i < arr.length {
                        total <- total + arr[i]
                        i <- i + 1
                    }
                    return total
                }
                max <- func(arr) {
                    maximum <- arr[0]
                    i <- 1
                    while i < arr.length {
                        if arr[i] > maximum {
                            maximum <- arr[i]
                        }
                        i <- i + 1
                    }
                    return maximum
                }
            }

            // 测试各个命名空间的功能
            math_result1 <- MathUtils.add(5, 3)
            math_result2 <- MathUtils.multiply(4, 7)
            pi_value <- MathUtils.constant_pi

            string_result1 <- StringUtils.reverse(""hello"")
            string_result2 <- StringUtils.to_upper(""world"")

            array_result1 <- ArrayUtils.sum({1, 2, 3, 4, 5})
            array_result2 <- ArrayUtils.max({10, 5, 8, 12, 3})
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("math_result1"));
        Assert.True(result.ContainsKey("math_result2"));
        Assert.True(result.ContainsKey("pi_value"));
        Assert.True(result.ContainsKey("string_result1"));
        Assert.True(result.ContainsKey("string_result2"));
        Assert.True(result.ContainsKey("array_result1"));
        Assert.True(result.ContainsKey("array_result2"));

        Assert.Equal(8, Convert.ToInt32(result["math_result1"]));
        Assert.Equal(28, Convert.ToInt32(result["math_result2"]));
        Assert.Equal(3.14159, Convert.ToDouble(result["pi_value"]));
        Assert.Equal("olleh", result["string_result1"]);
        Assert.Equal("WORLD", result["string_result2"]);
        Assert.Equal(15, Convert.ToInt32(result["array_result1"]));
        Assert.Equal(12, Convert.ToInt32(result["array_result2"]));
    }

    [Fact]
    public void Namespace_NamespaceImport_ImportSpecificMembers()
    {
        var code = @"
            namespace DataStructures {
                Stack <- func() {
                    return {
                        items: {},
                        push: func(self, item) {
                            self.items <- self.items.concat({item})
                        },
                        pop: func(self) {
                            if self.items.length > 0 {
                                last <- self.items[self.items.length - 1]
                                self.items <- self.items.slice(0, self.items.length - 1)
                                return last
                            } else {
                                return null
                            }
                        },
                        size: func(self) {
                            return self.items.length
                        }
                    }
                }

                Queue <- func() {
                    return {
                        items: {},
                        enqueue: func(self, item) {
                            self.items <- self.items.concat({item})
                        },
                        dequeue: func(self) {
                            if self.items.length > 0 {
                                first <- self.items[0]
                                self.items <- self.items.slice(1)
                                return first
                            } else {
                                return null
                            }
                        },
                        size: func(self) {
                            return self.items.length
                        }
                    }
                }
            }

            namespace Algorithms {
                bubble_sort <- func(arr) {
                    n <- arr.length
                    i <- 0
                    while i < n - 1 {
                        j <- 0
                        while j < n - i - 1 {
                            if arr[j] > arr[j + 1] {
                                temp <- arr[j]
                                arr[j] <- arr[j + 1]
                                arr[j + 1] <- temp
                            }
                            j <- j + 1
                        }
                        i <- i + 1
                    }
                    return arr
                }
            }

            // 导入并使用命名空间成员
            Stack <- DataStructures.Stack
            Queue <- DataStructures.Queue
            bubble_sort <- Algorithms.bubble_sort

            // 使用导入的功能
            stack_instance <- Stack()
            stack_instance.push(stack_instance, ""first"")
            stack_instance.push(stack_instance, ""second"")
            stack_size <- stack_instance.size(stack_instance)
            popped_item <- stack_instance.pop(stack_instance)

            queue_instance <- Queue()
            queue_instance.enqueue(queue_instance, ""item1"")
            queue_instance.enqueue(queue_instance, ""item2"")
            queue_size <- queue_instance.size(queue_instance)
            dequeued_item <- queue_instance.dequeue(queue_instance)

            unsorted_array <- {5, 2, 8, 1, 9}
            sorted_array <- bubble_sort(unsorted_array)
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("stack_size"));
        Assert.True(result.ContainsKey("popped_item"));
        Assert.True(result.ContainsKey("queue_size"));
        Assert.True(result.ContainsKey("dequeued_item"));
        Assert.True(result.ContainsKey("sorted_array"));

        Assert.Equal(2, Convert.ToInt32(result["stack_size"]));
        Assert.Equal("second", result["popped_item"]);
        Assert.Equal(2, Convert.ToInt32(result["queue_size"]));
        Assert.Equal("item1", result["dequeued_item"]);
    }

    [Fact]
    public void Namespace_NameConflictResolution_SameNamesDifferentNamespaces()
    {
        var code = @"
            namespace ModuleA {
                process <- func(data) {
                    return ""ModuleA processed: "" + data
                }
                version <- ""1.0""
            }

            namespace ModuleB {
                process <- func(data) {
                    return ""ModuleB processed: "" + data
                }
                version <- ""2.0""
            }

            namespace ModuleC {
                process <- func(data) {
                    return ""ModuleC processed: "" + data
                }
                version <- ""3.0""
            }

            // 使用完全限定名解决冲突
            result_a <- ModuleA.process(""test"")
            result_b <- ModuleB.process(""test"")
            result_c <- ModuleC.process(""test"")

            version_a <- ModuleA.version
            version_b <- ModuleB.version
            version_c <- ModuleC.version

            // 创建别名解决冲突
            process_a <- ModuleA.process
            process_b <- ModuleB.process
            process_c <- ModuleC.process

            alias_result_a <- process_a(""alias_test"")
            alias_result_b <- process_b(""alias_test"")
            alias_result_c <- process_c(""alias_test"")
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("result_a"));
        Assert.True(result.ContainsKey("result_b"));
        Assert.True(result.ContainsKey("result_c"));
        Assert.True(result.ContainsKey("version_a"));
        Assert.True(result.ContainsKey("version_b"));
        Assert.True(result.ContainsKey("version_c"));
        Assert.True(result.ContainsKey("alias_result_a"));
        Assert.True(result.ContainsKey("alias_result_b"));
        Assert.True(result.ContainsKey("alias_result_c"));

        Assert.Equal("ModuleA processed: test", result["result_a"]);
        Assert.Equal("ModuleB processed: test", result["result_b"]);
        Assert.Equal("ModuleC processed: test", result["result_c"]);
        Assert.Equal("1.0", result["version_a"]);
        Assert.Equal("2.0", result["version_b"]);
        Assert.Equal("3.0", result["version_c"]);
    }

    [Fact]
    public void Namespace_GlobalVsLocalScope_ScopeIsolation()
    {
        var code = @"
            // 全局变量
            global_variable <- ""global_value""
            global_function <- func() {
                return ""global_function_result""
            }

            namespace TestNamespace {
                // 命名空间内的同名变量
                global_variable <- ""namespace_value""
                global_function <- func() {
                    return ""namespace_function_result""
                }

                namespace_local_function <- func() {
                    // 在命名空间内部访问全局变量（如果语言支持）
                    namespace_result <- global_function()
                    return namespace_result
                }
            }

            // 在全局作用域访问
            global_result <- global_function()
            namespace_result <- TestNamespace.global_function()

            // 测试命名空间内部函数
            internal_result <- TestNamespace.namespace_local_function()
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("global_result"));
        Assert.True(result.ContainsKey("namespace_result"));
        Assert.True(result.ContainsKey("internal_result"));

        Assert.Equal("global_function_result", result["global_result"]);
        Assert.Equal("namespace_function_result", result["namespace_result"]);
        Assert.Equal("namespace_function_result", result["internal_result"]);
    }

    [Fact]
    public void Namespace_ModuleHierarchy_ModuleNestingAndInheritance()
    {
        var code = @"
            namespace BaseModule {
                base_constant <- ""base_value""
                base_function <- func() {
                    return ""base_function_result""
                }

                base_helper <- func(value) {
                    return ""base_helper: "" + value
                }
            }

            namespace ExtendedModule {
                // 引用基础模块
                base <- BaseModule

                extended_function <- func() {
                    base_result <- base.base_function()
                    return ""extended_"" + base_result
                }

                override_function <- func() {
                    return ""override_result""
                }

                combined_function <- func(data) {
                    base_helped <- base.base_helper(data)
                    extended <- extended_function()
                    return base_helped + "" | "" + extended
                }
            }

            namespace UtilityModule {
                // 工具函数
                format_result <- func(prefix, value) {
                    return prefix + "": "" + value
                }

                create_logger <- func(module_name) {
                    return {
                        log: func(self, message) {
                            return ""["" + module_name + ""] "" + message
                        }
                    }
                }
            }

            // 测试模块层次结构
            base_const <- BaseModule.base_constant
            base_func_result <- BaseModule.base_function()

            extended_result <- ExtendedModule.extended_function()
            override_result <- ExtendedModule.override_function()
            combined_result <- ExtendedModule.combined_function(""test_data"")

            // 测试工具模块
            utility_logger <- UtilityModule.create_logger(""MyModule"")
            log_message <- utility_logger.log(utility_logger, ""Test message"")
            formatted_result <- UtilityModule.format_result(""Result"", extended_result)
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("base_const"));
        Assert.True(result.ContainsKey("base_func_result"));
        Assert.True(result.ContainsKey("extended_result"));
        Assert.True(result.ContainsKey("override_result"));
        Assert.True(result.ContainsKey("combined_result"));
        Assert.True(result.ContainsKey("log_message"));
        Assert.True(result.ContainsKey("formatted_result"));

        Assert.Equal("base_value", result["base_const"]);
        Assert.Equal("base_function_result", result["base_func_result"]);
        Assert.Equal("extended_base_function_result", result["extended_result"]);
        Assert.Equal("override_result", result["override_result"]);
    }

    [Fact]
    public void Namespace_DynamicNamespaceAccess_DynamicMemberAccess()
    {
        var code = @"
            namespace DynamicTest {
                member1 <- ""value1""
                member2 <- ""value2""
                member3 <- ""value3""

                dynamic_function <- func(name) {
                    return ""dynamic_"" + name
                }

                get_member <- func(member_name) {
                    if member_name == ""member1"" {
                        return member1
                    } else if member_name == ""member2"" {
                        return member2
                    } else if member_name == ""member3"" {
                        return member3
                    } else {
                        return null
                    }
                }
            }

            // 动态访问测试
            static_member1 <- DynamicTest.member1
            static_member2 <- DynamicTest.member2

            // 通过函数动态访问
            dynamic_member1 <- DynamicTest.get_member(""member1"")
            dynamic_member3 <- DynamicTest.get_member(""member3"")
            non_existent <- DynamicTest.get_member(""member4"")

            // 动态函数调用
            dynamic_func_result1 <- DynamicTest.dynamic_function(""test1"")
            dynamic_func_result2 <- DynamicTest.dynamic_function(""test2"")
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("static_member1"));
        Assert.True(result.ContainsKey("static_member2"));
        Assert.True(result.ContainsKey("dynamic_member1"));
        Assert.True(result.ContainsKey("dynamic_member3"));
        Assert.True(result.ContainsKey("non_existent"));
        Assert.True(result.ContainsKey("dynamic_func_result1"));
        Assert.True(result.ContainsKey("dynamic_func_result2"));

        Assert.Equal("value1", result["static_member1"]);
        Assert.Equal("value2", result["static_member2"]);
        Assert.Equal("value1", result["dynamic_member1"]);
        Assert.Equal("value3", result["dynamic_member3"]);
        Assert.Equal("dynamic_test1", result["dynamic_func_result1"]);
        Assert.Equal("dynamic_test2", result["dynamic_func_result2"]);
    }

    [Fact]
    public void Namespace_NamespaceChaining_ChainedNamespaceAccess()
    {
        var code = @"
            namespace Level1 {
                namespace Level2 {
                    namespace Level3 {
                        deep_value <- ""deep_nested_value""
                        deep_function <- func() {
                            return ""deep_function_result""
                        }

                        namespace Level4 {
                            deepest_value <- ""deepest_value""
                            deepest_function <- func() {
                                return ""deepest_function_result""
                            }
                        }
                    }

                    level2_value <- ""level2_value""
                    level2_function <- func() {
                        return ""level2_function_result""
                    }
                }

                level1_value <- ""level1_value""
                level1_function <- func() {
                    return ""level1_function_result""
                }
            }

            // 测试命名空间链式访问
            deep_value <- Level1.Level2.Level3.deep_value
            deep_func_result <- Level1.Level2.Level3.deep_function()
            deepest_value <- Level1.Level2.Level3.Level4.deepest_value
            deepest_func_result <- Level1.Level2.Level3.Level4.deepest_function()

            level1_val <- Level1.level1_value
            level2_val <- Level1.Level2.level2_value
            level1_func <- Level1.level1_function()
            level2_func <- Level1.Level2.level2_function()
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("deep_value"));
        Assert.True(result.ContainsKey("deep_func_result"));
        Assert.True(result.ContainsKey("deepest_value"));
        Assert.True(result.ContainsKey("deepest_func_result"));
        Assert.True(result.ContainsKey("level1_val"));
        Assert.True(result.ContainsKey("level2_val"));
        Assert.True(result.ContainsKey("level1_func"));
        Assert.True(result.ContainsKey("level2_func"));

        Assert.Equal("deep_nested_value", result["deep_value"]);
        Assert.Equal("deep_function_result", result["deep_func_result"]);
        Assert.Equal("deepest_value", result["deepest_value"]);
        Assert.Equal("deepest_function_result", result["deepest_func_result"]);
        Assert.Equal("level1_value", result["level1_val"]);
        Assert.Equal("level2_value", result["level2_val"]);
        Assert.Equal("level1_function_result", result["level1_func"]);
        Assert.Equal("level2_function_result", result["level2_func"]);
    }

    [Fact]
    public void Namespace_NamespaceConstants_ImmutableNamespaceMembers()
    {
        var code = @"
            namespace Constants {
                PI <- 3.14159265359
                E <- 2.71828182846
                GRAVITY <- 9.81
                LIGHT_SPEED <- 299792458
                ABSOLUTE_ZERO <- -273.15

                // 常量函数
                to_radians <- func(degrees) {
                    return degrees * PI / 180
                }

                circle_area <- func(radius) {
                    return PI * radius * radius
                }
            }

            namespace StatusCodes {
                OK <- 200
                NOT_FOUND <- 404
                SERVER_ERROR <- 500
                UNAUTHORIZED <- 401

                is_success <- func(code) {
                    return code >= 200 && code < 300
                }

                is_client_error <- func(code) {
                    return code >= 400 && code < 500
                }

                is_server_error <- func(code) {
                    return code >= 500
                }
            }

            // 使用常量命名空间
            pi_value <- Constants.PI
            e_value <- Constants.E
            gravity_value <- Constants.GRAVITY

            // 使用常量函数
            radians_90 <- Constants.to_radians(90)
            area_circle_5 <- Constants.circle_area(5)

            // 使用状态码常量
            status_ok <- StatusCodes.OK
            status_not_found <- StatusCodes.NOT_FOUND

            // 使用状态码函数
            is_200_success <- StatusCodes.is_success(200)
            is_404_success <- StatusCodes.is_success(404)
            is_500_client_error <- StatusCodes.is_client_error(500)
            is_500_server_error <- StatusCodes.is_server_error(500)
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("pi_value"));
        Assert.True(result.ContainsKey("e_value"));
        Assert.True(result.ContainsKey("gravity_value"));
        Assert.True(result.ContainsKey("radians_90"));
        Assert.True(result.ContainsKey("area_circle_5"));
        Assert.True(result.ContainsKey("status_ok"));
        Assert.True(result.ContainsKey("status_not_found"));
        Assert.True(result.ContainsKey("is_200_success"));
        Assert.True(result.ContainsKey("is_404_success"));
        Assert.True(result.ContainsKey("is_500_server_error"));

        Assert.Equal(3.14159265359, Convert.ToDouble(result["pi_value"]));
        Assert.Equal(2.71828182846, Convert.ToDouble(result["e_value"]));
        Assert.Equal(9.81, Convert.ToDouble(result["gravity_value"]));
        Assert.Equal(200, Convert.ToInt32(result["status_ok"]));
        Assert.Equal(404, Convert.ToInt32(result["status_not_found"]));
        Assert.Equal(true, result["is_200_success"]);
        Assert.Equal(false, result["is_404_success"]);
        Assert.Equal(true, result["is_500_server_error"]);
    }

    [Fact]
    public void Namespace_NamespaceFactory_FactoryPatternInNamespaces()
    {
        var code = @"
            namespace Factories {
                AnimalFactory <- func() {
                    return {
                        create_dog <- func(name) {
                            return {
                                type: ""dog"",
                                name: name,
                                sound: func(self) {
                                    return self.name + "" says: Woof!""
                                }
                            }
                        },

                        create_cat <- func(name) {
                            return {
                                type: ""cat"",
                                name: name,
                                sound: func(self) {
                                    return self.name + "" says: Meow!""
                                }
                            }
                        },

                        create_bird <- func(name) {
                            return {
                                type: ""bird"",
                                name: name,
                                sound: func(self) {
                                    return self.name + "" says: Tweet!""
                                }
                            }
                        }
                    }
                }

                VehicleFactory <- func() {
                    return {
                        create_car <- func(make, model) {
                            return {
                                type: ""car"",
                                make: make,
                                model: model,
                                info: func(self) {
                                    return self.make + "" "" + self.model + "" car""
                                }
                            }
                        },

                        create_bike <- func(brand) {
                            return {
                                type: ""bike"",
                                brand: brand,
                                info: func(self) {
                                    return self.brand + "" bicycle""
                                }
                            }
                        }
                    }
                }
            }

            namespace AnimalRegistry {
                registry <- {}

                register_animal <- func(animal) {
                    registry[animal.name] <- animal
                }

                get_animal <- func(name) {
                    return registry[name]
                }

                list_animals <- func() {
                    names <- {}
                    for name, animal in registry {
                        names <- names.concat({name})
                    }
                    return names
                }
            }

            // 使用工厂创建对象
            animal_factory <- Factories.AnimalFactory()
            vehicle_factory <- Factories.VehicleFactory()

            // 创建动物
            dog <- animal_factory.create_dog(""Buddy"")
            cat <- animal_factory.create_cat(""Whiskers"")
            bird <- animal_factory.create_bird(""Tweety"")

            dog_sound <- dog.sound(dog)
            cat_sound <- cat.sound(cat)
            bird_sound <- bird.sound(bird)

            // 创建交通工具
            car <- vehicle_factory.create_car(""Toyota"", ""Camry"")
            bike <- vehicle_factory.create_bike(""Giant"")

            car_info <- car.info(car)
            bike_info <- bike.info(bike)

            // 注册动物
            AnimalRegistry.register_animal(dog)
            AnimalRegistry.register_animal(cat)
            AnimalRegistry.register_animal(bird)

            registered_animals <- AnimalRegistry.list_animals()
            retrieved_dog <- AnimalRegistry.get_animal(""Buddy"")
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("dog_sound"));
        Assert.True(result.ContainsKey("cat_sound"));
        Assert.True(result.ContainsKey("bird_sound"));
        Assert.True(result.ContainsKey("car_info"));
        Assert.True(result.ContainsKey("bike_info"));
        Assert.True(result.ContainsKey("registered_animals"));

        Assert.Equal("Buddy says: Woof!", result["dog_sound"]);
        Assert.Equal("Whiskers says: Meow!", result["cat_sound"]);
        Assert.Equal("Tweety says: Tweet!", result["bird_sound"]);
        Assert.Equal("Toyota Camry car", result["car_info"]);
        Assert.Equal("Giant bicycle", result["bike_info"]);
    }

    [Fact]
    public void Namespace_NamespacePlugins_PluginArchitecture()
    {
        var code = @"
            namespace PluginSystem {
                plugins <- {}
                plugin_order <- {}

                register_plugin <- func(name, plugin_instance) {
                    plugins[name] <- plugin_instance
                    plugin_order <- plugin_order.concat({name})
                }

                get_plugin <- func(name) {
                    return plugins[name]
                }

                execute_all_plugins <- func(data) {
                    results <- {}
                    i <- 0
                    while i < plugin_order.length {
                        plugin_name <- plugin_order[i]
                        plugin <- plugins[plugin_name]
                        results[plugin_name] <- plugin.process(plugin, data)
                        i <- i + 1
                    }
                    return results
                }
            }

            namespace PluginImplementations {
                // 日志插件
                LoggerPlugin <- func() {
                    return {
                        name: ""Logger"",
                        process: func(self, data) {
                            return ""Logged: "" + data
                        }
                    }
                }

                // 验证插件
                ValidatorPlugin <- func() {
                    return {
                        name: ""Validator"",
                        process: func(self, data) {
                            if data.length > 0 {
                                return ""Validated: "" + data
                            } else {
                                return ""Validation failed: empty data""
                            }
                        }
                    }
                }

                // 转换插件
                TransformerPlugin <- func() {
                    return {
                        name: ""Transformer"",
                        process: func(self, data) {
                            return ""Transformed: "" + data.toUpperCase()
                        }
                    }
                }

                // 缓存插件
                CachePlugin <- func() {
                    cache <- {}
                    return {
                        name: ""Cache"",
                        process: func(self, data) {
                            if cache[data] == null {
                                cache[data] <- ""Cached: "" + data
                                return ""Cached new: "" + data
                            } else {
                                return ""From cache: "" + cache[data]
                            }
                        }
                    }
                }
            }

            // 创建插件实例
            logger_plugin <- PluginImplementations.LoggerPlugin()
            validator_plugin <- PluginImplementations.ValidatorPlugin()
            transformer_plugin <- PluginImplementations.TransformerPlugin()
            cache_plugin <- PluginImplementations.CachePlugin()

            // 注册插件
            PluginSystem.register_plugin(""logger"", logger_plugin)
            PluginSystem.register_plugin(""validator"", validator_plugin)
            PluginSystem.register_plugin(""transformer"", transformer_plugin)
            PluginSystem.register_plugin(""cache"", cache_plugin)

            // 执行所有插件
            test_data <- ""sample_data""
            plugin_results <- PluginSystem.execute_all_plugins(test_data)

            // 再次执行以测试缓存
            cached_results <- PluginSystem.execute_all_plugins(test_data)

            // 获取特定插件
            logger_instance <- PluginSystem.get_plugin(""logger"")
            logger_result <- logger_instance.process(logger_instance, ""direct_call"")
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("plugin_results"));
        Assert.True(result.ContainsKey("cached_results"));
        Assert.True(result.ContainsKey("logger_result"));

        var results = result["plugin_results"] as dynamic;
        var cached = result["cached_results"] as dynamic;

        Assert.NotNull(results);
        Assert.NotNull(cached);
        Assert.Equal("Logged: direct_call", result["logger_result"]);
    }

    [Fact]
    public void Namespace_NamespaceConfiguration_ConfigurationNamespace()
    {
        var code = @"
            namespace Config {
                // 数据库配置
                database <- {
                    host: ""localhost"",
                    port: 5432,
                    name: ""myapp"",
                    username: ""admin"",
                    password: ""secret""
                }

                // 应用配置
                application <- {
                    name: ""MyApplication"",
                    version: ""1.0.0"",
                    debug: true,
                    log_level: ""INFO""
                }

                // 服务器配置
                server <- {
                    host: ""0.0.0.0"",
                    port: 8080,
                    ssl_enabled: false,
                    max_connections: 100
                }

                // 配置辅助函数
                get_database_url <- func() {
                    return ""postgresql://"" + database.username + "":"" + database.password +
                           ""@"" + database.host + "":"" + database.port + ""/"" + database.name
                }

                get_server_address <- func() {
                    protocol <- ""http""
                    if server.ssl_enabled {
                        protocol <- ""https""
                    }
                    return protocol + "://"" + server.host + "":"" + server.port
                }

                is_debug_mode <- func() {
                    return application.debug == true
                }
            }

            // 使用配置命名空间
            db_url <- Config.get_database_url()
            server_address <- Config.get_server_address()
            debug_mode <- Config.is_debug_mode()

            // 直接访问配置值
            app_name <- Config.application.name
            app_version <- Config.application.version
            server_port <- Config.server.port
            db_host <- Config.database.host
        ";

        var result = TestInterpreter(code);

        Assert.True(result.ContainsKey("db_url"));
        Assert.True(result.ContainsKey("server_address"));
        Assert.True(result.ContainsKey("debug_mode"));
        Assert.True(result.ContainsKey("app_name"));
        Assert.True(result.ContainsKey("app_version"));
        Assert.True(result.ContainsKey("server_port"));
        Assert.True(result.ContainsKey("db_host"));

        Assert.Equal("MyApplication", result["app_name"]);
        Assert.Equal("1.0.0", result["app_version"]);
        Assert.Equal(8080, Convert.ToInt32(result["server_port"]));
        Assert.Equal("localhost", result["db_host"]);
        Assert.Equal(true, result["debug_mode"]);
    }
}