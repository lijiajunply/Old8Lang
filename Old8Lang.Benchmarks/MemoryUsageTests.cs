using BenchmarkDotNet.Attributes;
using Old8Lang.AST.Statement;
using Old8Lang.Interpreter;
using Old8Lang.LangParser;
using System.Diagnostics;
using System;

namespace Old8Lang.Benchmarks;

/// <summary>
/// 内存使用性能基准测试
/// 监控和测试内存分配、使用模式和优化效果
/// </summary>
[MemoryDiagnoser]
public class MemoryUsageTests
{
    // 内存测试代码
    private string ObjectCreationCode = "";
    private string GarbageCollectionCode = "";
    private string MemoryLeakTestCode = "";
    private string LargeObjectCode = "";
    private string FrequentAllocationCode = "";
    private string StringMemoryCode = "";
    private string CollectionMemoryCode = "";

    /// <summary>
    /// 初始化测试数据
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        // 对象创建内存测试
        ObjectCreationCode = @"
            func create_objects(count) {
                objects <- {}
                i <- 0
                while i < count {
                    objects[i] <- {
                        id: i,
                        name: ""object_"" + i,
                        data: i * 2,
                        active: i % 2 == 0,
                        metadata: {
                            created: i,
                            modified: i + 1,
                            version: 1
                        }
                    }
                    i <- i + 1
                }
                return objects
            }

            func process_objects(objects) {
                total_data <- 0
                active_count <- 0
                i <- 0
                while i < objects.length {
                    obj <- objects[i]
                    total_data <- total_data + obj.data
                    if obj.active {
                        active_count <- active_count + 1
                    }
                    i <- i + 1
                }
                return {total_data: total_data, active_count: active_count}
            }

            // 创建和处理对象
            object_collection <- create_objects(5000)
            processing_result <- process_objects(object_collection)

            result <- processing_result
        ";

        // 垃圾回收压力测试
        GarbageCollectionCode = @"
            func create_temp_objects() {
                temp_objects <- {}
                i <- 0
                while i < 1000 {
                    temp_objects[i] <- {
                        data: new Array(100),
                        string_data: ""temp_string_"" + i,
                        number_data: i * 3.14159,
                        boolean_data: i % 2 == 0
                    }
                    i <- i + 1
                }
                return temp_objects
            }

            func process_and_discard() {
                // 创建临时对象
                temp <- create_temp_objects()

                // 处理数据
                sum <- 0
                i <- 0
                while i < temp.length {
                    sum <- sum + temp[i].number_data
                    i <- i + 1
                }

                // 返回结果，临时对象应该被回收
                return sum
            }

            // 多次创建和丢弃对象
            results <- {}
            i <- 0
            while i < 50 {
                iteration_sum <- process_and_discard()
                results[i] <- iteration_sum
                i <- i + 1
            }

            result <- {iterations: results.length, final_sum: results[results.length - 1]}
        ";

        // 内存泄漏测试（检查是否正确释放）
        MemoryLeakTestCode = @"
            func create_nested_structure(depth) {
                if depth <= 0 {
                    return {value: depth, children: {}}
                }

                children <- {}
                i <- 0
                while i < 3 {
                    children[i] <- create_nested_structure(depth - 1)
                    i <- i + 1
                }

                return {
                    value: depth,
                    children: children,
                    data: new Array(50)
                }
            }

            func build_deep_structure() {
                deep_structure <- create_nested_structure(8)
                return deep_structure
            }

            func traverse_structure(structure) {
                visited_count <- 0

                func traverse_recursive(node) {
                    visited_count <- visited_count + 1
                    i <- 0
                    while i < node.children.length {
                        traverse_recursive(node.children[i])
                        i <- i + 1
                    }
                }

                traverse_recursive(structure)
                return visited_count
            }

            // 创建深层结构
            deep_structure <- build_deep_structure()
            visited_count <- traverse_structure(deep_structure)

            result <- {structure_created: true, visited_nodes: visited_count}
        ";

        // 大对象内存测试
        LargeObjectCode = @"
            func create_large_matrix(rows, cols) {
                matrix <- {}
                i <- 0
                while i < rows {
                    matrix[i] <- {}
                    j <- 0
                    while j < cols {
                        // 创建包含多种数据类型的复杂对象
                        matrix[i][j] <- {
                            value: i * cols + j,
                            square: (i * cols + j) * (i * cols + j),
                            sqrt: Math.sqrt(i * cols + j),
                            is_even: (i * cols + j) % 2 == 0,
                            description: ""Cell at row "" + i + "", col "" + j,
                            metadata: {
                                row: i,
                                col: j,
                                index: i * cols + j,
                                parity: (i + j) % 2,
                                neighbors: {}
                            }
                        }
                        j <- j + 1
                    }
                    i <- i + 1
                }
                return matrix
            }

            func analyze_matrix(matrix) {
                total_value <- 0
                even_count <- 0
                max_value <- -1
                i <- 0
                while i < matrix.length {
                    j <- 0
                    while j < matrix[i].length {
                        cell <- matrix[i][j]
                        total_value <- total_value + cell.value
                        if cell.is_even {
                            even_count <- even_count + 1
                        }
                        if cell.value > max_value {
                            max_value <- cell.value
                        }
                        j <- j + 1
                    }
                    i <- i + 1
                }
                return {total: total_value, even_count: even_count, max: max_value}
            }

            // 创建大矩阵
            large_matrix <- create_large_matrix(100, 100)
            analysis_result <- analyze_matrix(large_matrix)

            result <- {matrix_size: large_matrix.length, analysis: analysis_result}
        ";

        // 频繁分配测试
        FrequentAllocationCode = @"
            func frequent_allocation_test() {
                results <- {}
                i <- 0
                while i < 10000 {
                    // 每次循环都创建新对象
                    temp_data <- {
                        iteration: i,
                        timestamp: i,
                        payload: new Array(20),
                        calculations: {
                            square: i * i,
                            cube: i * i * i,
                            factorial: i <= 1 ? 1 : i * (i - 1)
                        }
                    }

                    // 执行一些计算
                    sum <- temp_data.calculations.square + temp_data.calculations.cube
                    results[i] <- {input: i, sum: sum}

                    i <- i + 1
                }
                return results
            }

            allocation_result <- frequent_allocation_test()
            final_calculation <- allocation_result[allocation_result.length - 1]

            result <- {total_iterations: allocation_result.length, final_sum: final_calculation.sum}
        ";

        // 字符串内存测试
        StringMemoryCode = @"
            func string_concatenation_test() {
                base_string <- ""The quick brown fox jumps over the lazy dog. ""
                large_string <- """"
                i <- 0
                while i < 1000 {
                    large_string <- large_string + base_string
                    i <- i + 1
                }
                return large_string
            }

            func string_operations_test(text) {
                operations <- {}

                // 字符串长度
                operations.length <- text.length

                // 字符串分割
                words <- text.split("" "")
                operations.word_count <- words.length

                // 字符串查找
                operations.fox_count <- 0
                i <- 0
                while i < words.length {
                    if words[i] == ""fox"" {
                        operations.fox_count <- operations.fox_count + 1
                    }
                    i <- i + 1
                }

                // 字符串替换
                replaced_text <- text.replace(""fox"", ""cat"")
                operations.replacement_length <- replaced_text.length

                return operations
            }

            // 字符串内存测试
            large_text <- string_concatenation_test()
            string_analysis <- string_operations_test(large_text)

            result <- {original_length: large_text.length, analysis: string_analysis}
        ";

        // 集合内存测试
        CollectionMemoryCode = @"
            func collection_memory_test() {
                // 创建各种集合类型
                arrays <- {}
                dictionaries <- {}
                mixed_collections <- {}

                // 创建大数组集合
                i <- 0
                while i < 100 {
                    array_instance <- {}
                    j <- 0
                    while j < 500 {
                        array_instance[j] <- j * i + Math.sin(j)
                        j <- j + 1
                    }
                    arrays[i] <- array_instance
                    i <- i + 1
                }

                // 创建大字典集合
                i <- 0
                while i < 50 {
                    dict_instance <- {}
                    j <- 0
                    while j < 200 {
                        key <- ""key_"" + i + ""_"" + j
                        value <- {
                            index: j,
                            data: Math.random() * 1000,
                            processed: false
                        }
                        dict_instance[key] <- value
                        j <- j + 1
                    }
                    dictionaries[i] <- dict_instance
                    i <- i + 1
                }

                // 创建混合集合
                i <- 0
                while i < 30 {
                    mixed_instance <- {
                        array_data: {},
                        dict_data: {},
                        string_data: """",
                        number_data: 0
                    }

                    j <- 0
                    while j < 100 {
                        mixed_instance.array_data[j] <- j * i
                        j <- j + 1
                    }

                    j <- 0
                    while j < 50 {
                        key <- ""mixed_"" + j
                        mixed_instance.dict_data[key] <- j * 2.5
                        j <- j + 1
                    }

                    mixed_instance.string_data <- ""Mixed collection instance "" + i
                    mixed_instance.number_data <- i * 100.5

                    mixed_collections[i] <- mixed_instance
                    i <- i + 1
                }

                return {
                    array_count: arrays.length,
                    dict_count: dictionaries.length,
                    mixed_count: mixed_collections.length
                }
            }

            collection_result <- collection_memory_test()

            result <- collection_result
        ";
    }

    /// <summary>
    /// 测试对象创建的内存使用
    /// </summary>
    [Benchmark(Description = "Object Creation Memory")]
    public void ObjectCreationMemory()
    {
        var interpreter = new LangInterpreter();
        BlockStatement ast = interpreter.Build(ObjectCreationCode);
        ast.Run(interpreter.Manager);
    }

    /// <summary>
    /// 测试垃圾回收压力下的内存表现
    /// </summary>
    [Benchmark(Description = "Garbage Collection Pressure")]
    public void GarbageCollectionPressure()
    {
        var interpreter = new LangInterpreter();
        BlockStatement ast = interpreter.Build(GarbageCollectionCode);
        ast.Run(interpreter.Manager);
    }

    /// <summary>
    /// 测试深层结构的内存使用
    /// </summary>
    [Benchmark(Description = "Deep Structure Memory")]
    public void DeepStructureMemory()
    {
        var interpreter = new LangInterpreter();
        BlockStatement ast = interpreter.Build(MemoryLeakTestCode);
        ast.Run(interpreter.Manager);
    }

    /// <summary>
    /// 测试大对象的内存分配
    /// </summary>
    [Benchmark(Description = "Large Object Memory")]
    public void LargeObjectMemory()
    {
        var interpreter = new LangInterpreter();
        BlockStatement ast = interpreter.Build(LargeObjectCode);
        ast.Run(interpreter.Manager);
    }

    /// <summary>
    /// 测试频繁分配的内存效率
    /// </summary>
    [Benchmark(Description = "Frequent Allocation Memory")]
    public void FrequentAllocationMemory()
    {
        var interpreter = new LangInterpreter();
        BlockStatement ast = interpreter.Build(FrequentAllocationCode);
        ast.Run(interpreter.Manager);
    }

    /// <summary>
    /// 测试字符串操作的内存使用
    /// </summary>
    [Benchmark(Description = "String Operations Memory")]
    public void StringOperationsMemory()
    {
        var interpreter = new LangInterpreter();
        BlockStatement ast = interpreter.Build(StringMemoryCode);
        ast.Run(interpreter.Manager);
    }

    /// <summary>
    /// 测试集合类型的内存使用
    /// </summary>
    [Benchmark(Description = "Collection Types Memory")]
    public void CollectionTypesMemory()
    {
        var interpreter = new LangInterpreter();
        BlockStatement ast = interpreter.Build(CollectionMemoryCode);
        ast.Run(interpreter.Manager);
    }

    /// <summary>
    /// 测试内存效率优化场景
    /// </summary>
    [Benchmark(Description = "Memory Efficiency Optimization")]
    public void MemoryEfficiencyOptimization()
    {
        var optimizationCode = @"
            func optimized_data_processing() {
                // 优化的数据处理：重用对象
                reusable_object <- {
                    sum: 0,
                    count: 0,
                    max: -999999,
                    min: 999999
                }

                results <- {}
                i <- 0
                while i < 1000 {
                    // 重用同一个对象而不是创建新对象
                    data <- i * 2 + Math.sin(i) * 10

                    reusable_object.sum <- reusable_object.sum + data
                    reusable_object.count <- reusable_object.count + 1

                    if data > reusable_object.max {
                        reusable_object.max <- data
                    }
                    if data < reusable_object.min {
                        reusable_object.min <- data
                    }

                    // 只存储必要的结果
                    if i % 100 == 0 {
                        results[i / 100] <- {
                            partial_sum: reusable_object.sum,
                            count: reusable_object.count,
                            current_max: reusable_object.max,
                            current_min: reusable_object.min
                        }
                    }

                    i <- i + 1
                }

                return {
                    final_average: reusable_object.sum / reusable_object.count,
                    final_max: reusable_object.max,
                    final_min: reusable_object.min,
                    checkpoints: results
                }
            }

            func unoptimized_data_processing() {
                // 未优化的数据处理：每次创建新对象
                results <- {}
                i <- 0
                while i < 1000 {
                    data <- i * 2 + Math.sin(i) * 10

                    // 每次都创建新的统计对象
                    stats <- {
                        sum: 0,
                        count: 0,
                        max: -999999,
                        min: 999999
                    }

                    stats.sum <- data
                    stats.count <- 1
                    stats.max <- data
                    stats.min <- data

                    results[i] <- stats
                    i <- i + 1
                }

                return results
            }

            // 比较优化和非优化版本
            optimized_result <- optimized_data_processing()
            unoptimized_result <- unoptimized_data_processing()

            result <- {
                optimized_checkpoints: optimized_result.checkpoints.length,
                unoptimized_objects: unoptimized_result.length,
                memory_efficiency: optimized_result.checkpoints.length < unoptimized_result.length
            }
        ";

        var interpreter = new LangInterpreter();
        BlockStatement ast = interpreter.Build(optimizationCode);
        ast.Run(interpreter.Manager);
    }

    /// <summary>
    /// 测试内存池化效果
    /// </summary>
    [Benchmark(Description = "Memory Pooling Effect")]
    public void MemoryPoolingEffect()
    {
        var poolingCode = @"
            func create_object_pool() {
                pool <- {
                    available: {},
                    in_use: {},
                    total_created: 0,
                    max_size: 100
                }

                pool.acquire <- func() {
                    if self.available.length > 0 {
                        obj <- self.available[0]
                        self.available <- self.available.slice(1)
                        self.in_use[obj.id] <- obj
                        return obj
                    } else {
                        new_obj <- {
                            id: self.total_created,
                            data: 0,
                            processed: false
                        }
                        self.total_created <- self.total_created + 1
                        self.in_use[new_obj.id] <- new_obj
                        return new_obj
                    }
                }

                pool.release <- func(obj) {
                    if self.in_use[obj.id] != null {
                        delete self.in_use[obj.id]
                        self.available <- self.available.concat({obj})
                    }
                }

                return pool
            }

            func test_with_pool() {
                pool <- create_object_pool()
                results <- {}

                i <- 0
                while i < 1000 {
                    obj <- pool.acquire(pool)
                    obj.data <- i * 2
                    obj.processed <- true
                    results[i] <- obj.data

                    pool.release(pool, obj)
                    i <- i + 1
                }

                return {
                    total_processed: results.length,
                    pool_stats: {
                        total_created: pool.total_created,
                        available_objects: pool.available.length,
                        in_use_objects: pool.in_use.length
                    }
                }
            }

            func test_without_pool() {
                results <- {}

                i <- 0
                while i < 1000 {
                    // 每次都创建新对象
                    obj <- {
                        id: i,
                        data: i * 2,
                        processed: true
                    }
                    results[i] <- obj.data
                    i <- i + 1
                }

                return {
                    total_processed: results.length,
                    objects_created: results.length
                }
            }

            // 比较池化和非池化
            pooled_result <- test_with_pool()
            non_pooled_result <- test_without_pool()

            result <- {
                pooled_efficiency: pooled_result.pool_stats.total_created < non_pooled_result.objects_created,
                pool_reuse_ratio: 1 - (pooled_result.pool_stats.total_created / non_pooled_result.objects_created)
            }
        ";

        var interpreter = new LangInterpreter();
        BlockStatement ast = interpreter.Build(poolingCode);
        ast.Run(interpreter.Manager);
    }
}