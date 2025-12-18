using BenchmarkDotNet.Attributes;
using Old8Lang.AST.Statement;
using Old8Lang.Interpreter;

namespace Old8Lang.Benchmarks;

/// <summary>
/// 大数据量性能基准测试
/// 测试大规模数据处理、内存使用和执行时间性能
/// </summary>
public class LargeDataPerformanceTests
{
    // 大数据量测试代码
    private string LargeArrayCode = "";
    private string LargeListCode = "";
    private string LargeDictionaryCode = "";
    private string DeepRecursionCode = "";
    private string MemoryIntensiveCode = "";
    private string StringProcessingCode = "";
    private string CollectionOperationsCode = "";
    private string ComplexAlgorithmsCode = "";

    /// <summary>
    /// 初始化测试数据
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        // 大数组操作测试
        LargeArrayCode = @"
            // 创建和操作大数组
            large_array <- []
            i <- 0
            while i < 10000 {
                large_array <- large_array.concat({i})
                i <- i + 1
            }

            // 数组操作
            sum <- 0
            i <- 0
            while i < large_array.length {
                sum <- sum + large_array[i]
                i <- i + 1
            }

            // 数组搜索
            found_index <- -1
            search_target <- 5000
            i <- 0
            while i < large_array.length {
                if large_array[i] == search_target {
                    found_index <- i
                    break
                }
                i <- i + 1
            }

            result <- {sum: sum, found_index: found_index}
        ";

        // 大列表操作测试
        LargeListCode = @"
            // 创建和操作大列表
            large_list <- {}
            i <- 0
            while i < 5000 {
                large_list <- large_list.concat({i * 2})
                i <- i + 1
            }

            // 列表操作：筛选偶数
            even_numbers <- {}
            i <- 0
            while i < large_list.length {
                if large_list[i] % 2 == 0 {
                    even_numbers <- even_numbers.concat({large_list[i]})
                }
                i <- i + 1
            }

            // 列表映射操作
            doubled_list <- {}
            i <- 0
            while i < even_numbers.length {
                doubled_list <- doubled_list.concat({even_numbers[i] * 2})
                i <- i + 1
            }

            result <- {original_count: large_list.length, even_count: even_numbers.length, doubled_count: doubled_list.length}
        ";

        // 大字典操作测试
        LargeDictionaryCode = @"
            // 创建大字典
            large_dict <- {}
            i <- 0
            while i < 2000 {
                key <- ""key_"" + i
                value <- ""value_"" + i
                large_dict[key] <- value
                i <- i + 1
            }

            // 字典查找操作
            lookup_results <- {}
            keys_to_find <- {""key_100"", ""key_500"", ""key_1000"", ""key_1500"", ""key_1999""}
            i <- 0
            while i < keys_to_find.length {
                lookup_key <- keys_to_find[i]
                found_value <- large_dict[lookup_key]
                lookup_results[lookup_key] <- found_value
                i <- i + 1
            }

            // 字典遍历
            key_count <- 0
            for key, value in large_dict {
                key_count <- key_count + 1
            }

            result <- {total_keys: key_count, lookup_results: lookup_results}
        ";

        // 深度递归测试
        DeepRecursionCode = @"
            func factorial(n) {
                if n <= 1 {
                    return 1
                }
                return n * factorial(n - 1)
            }

            func fibonacci(n) {
                if n <= 1 {
                    return n
                }
                return fibonacci(n - 1) + fibonacci(n - 2)
            }

            func quick_sort(arr, low, high) {
                if low < high {
                    pivot_index <- partition(arr, low, high)
                    quick_sort(arr, low, pivot_index - 1)
                    quick_sort(arr, pivot_index + 1, high)
                }
            }

            func partition(arr, low, high) {
                pivot <- arr[high]
                i <- low - 1
                j <- low
                while j < high {
                    if arr[j] < pivot {
                        i <- i + 1
                        temp <- arr[i]
                        arr[i] <- arr[j]
                        arr[j] <- temp
                    }
                    j <- j + 1
                }
                temp <- arr[i + 1]
                arr[i + 1] <- arr[high]
                arr[high] <- temp
                return i + 1
            }

            // 测试递归性能
            factorial_result <- factorial(20)
            fibonacci_result <- fibonacci(30)

            // 测试数组排序
            sort_array <- {5, 2, 8, 1, 9, 3, 7, 4, 6}
            quick_sort(sort_array, 0, sort_array.length - 1)

            result <- {factorial: factorial_result, fibonacci: fibonacci_result, sorted_array: sort_array}
        ";

        // 内存密集型测试
        MemoryIntensiveCode = @"
            func create_large_matrix(rows, cols) {
                matrix <- {}
                i <- 0
                while i < rows {
                    matrix[i] <- {}
                    j <- 0
                    while j < cols {
                        matrix[i][j] <- i * cols + j
                        j <- j + 1
                    }
                    i <- i + 1
                }
                return matrix
            }

            func matrix_multiply(matrix_a, matrix_b) {
                rows_a <- matrix_a.length
                cols_a <- matrix_a[0].length
                rows_b <- matrix_b.length
                cols_b <- matrix_b[0].length

                if cols_a != rows_b {
                    return null
                }

                result <- {}
                i <- 0
                while i < rows_a {
                    result[i] <- {}
                    j <- 0
                    while j < cols_b {
                        sum <- 0
                        k <- 0
                        while k < cols_a {
                            sum <- sum + matrix_a[i][k] * matrix_b[k][j]
                            k <- k + 1
                        }
                        result[i][j] <- sum
                        j <- j + 1
                    }
                    i <- i + 1
                }
                return result
            }

            // 创建两个大矩阵
            matrix1 <- create_large_matrix(50, 50)
            matrix2 <- create_large_matrix(50, 50)

            // 矩阵乘法（内存密集型操作）
            result_matrix <- matrix_multiply(matrix1, matrix2)

            result <- {matrix1_rows: matrix1.length, matrix2_rows: matrix2.length, result_available: result_matrix != null}
        ";

        // 字符串处理密集型测试
        StringProcessingCode = @"
            func generate_large_text() {
                text <- """"
                words <- {""hello"", ""world"", ""performance"", ""test"", ""benchmark"", ""large"", ""data"", ""processing""}
                i <- 0
                while i < 1000 {
                    j <- 0
                    while j < words.length {
                        text <- text + words[j] + "" ""
                        j <- j + 1
                    }
                    text <- text + ""\n""
                    i <- i + 1
                }
                return text
            }

            func count_words(text) {
                words <- text.split("" "")
                return words.length
            }

            func find_longest_word(text) {
                words <- text.split("" "")
                longest <- """"
                i <- 0
                while i < words.length {
                    word <- words[i]
                    if word.length > longest.length {
                        longest <- word
                    }
                    i <- i + 1
                }
                return longest
            }

            func reverse_string(s) {
                reversed <- """"
                i <- s.length - 1
                while i >= 0 {
                    reversed <- reversed + s[i]
                    i <- i - 1
                }
                return reversed
            }

            // 生成大文本
            large_text <- generate_large_text()

            // 字符串操作
            word_count <- count_words(large_text)
            longest_word <- find_longest_word(large_text)
            reversed_text <- reverse_string(large_text)

            result <- {word_count: word_count, longest_word: longest_word, text_length: large_text.length}
        ";

        // 集合操作密集型测试
        CollectionOperationsCode = @"
            // 大规模集合操作
            main_collection <- {}
            i <- 0
            while i < 10000 {
                main_collection <- main_collection.concat({i})
                i <- i + 1
            }

            // 集合过滤
            filtered_collection <- {}
            i <- 0
            while i < main_collection.length {
                item <- main_collection[i]
                if item % 3 == 0 || item % 5 == 0 {
                    filtered_collection <- filtered_collection.concat({item})
                }
                i <- i + 1
            }

            // 集合映射
            mapped_collection <- {}
            i <- 0
            while i < filtered_collection.length {
                original_item <- filtered_collection[i]
                mapped_item <- original_item * original_item
                mapped_collection <- mapped_collection.concat({mapped_item})
                i <- i + 1
            }

            // 集合聚合
            total_sum <- 0
            i <- 0
            while i < mapped_collection.length {
                total_sum <- total_sum + mapped_collection[i]
                i <- i + 1
            }

            // 去重操作
            unique_collection <- {}
            i <- 0
            while i < main_collection.length {
                item <- main_collection[i] % 100  // 只保留模100的值来制造重复
                already_exists <- false
                j <- 0
                while j < unique_collection.length {
                    if unique_collection[j] == item {
                        already_exists <- true
                        break
                    }
                    j <- j + 1
                }
                if !already_exists {
                    unique_collection <- unique_collection.concat({item})
                }
                i <- i + 1
            }

            result <- {
                original_count: main_collection.length,
                filtered_count: filtered_collection.length,
                mapped_count: mapped_collection.length,
                total_sum: total_sum,
                unique_count: unique_collection.length
            }
        ";

        // 复杂算法密集型测试
        ComplexAlgorithmsCode = @"
            func sieve_of_eratosthenes(n) {
                sieve <- {}
                i <- 0
                while i <= n {
                    sieve[i] <- true
                    i <- i + 1
                }
                sieve[0] <- false
                sieve[1] <- false

                p <- 2
                while p * p <= n {
                    if sieve[p] {
                        multiple <- p * p
                        while multiple <= n {
                            sieve[multiple] <- false
                            multiple <- multiple + p
                        }
                    }
                    p <- p + 1
                }

                primes <- {}
                i <- 0
                while i <= n {
                    if sieve[i] {
                        primes <- primes.concat({i})
                    }
                    i <- i + 1
                }
                return primes
            }

            func longest_common_subsequence(seq1, seq2) {
                m <- seq1.length
                n <- seq2.length
                dp <- {}

                i <- 0
                while i <= m {
                    dp[i] <- {}
                    j <- 0
                    while j <= n {
                        if i == 0 || j == 0 {
                            dp[i][j] <- 0
                        } else if seq1[i - 1] == seq2[j - 1] {
                            dp[i][j] <- dp[i - 1][j - 1] + 1
                        } else {
                            dp[i][j] <- dp[i - 1][j] if dp[i - 1][j] > dp[i][j - 1] else dp[i][j - 1]
                        }
                        j <- j + 1
                    }
                    i <- i + 1
                }

                return dp[m][n]
            }

            func dijkstra_shortest_path(graph, start) {
                distances <- {}
                visited <- {}
                nodes <- {}

                // 初始化
                for node, edges in graph {
                    distances[node] <- 999999  // 无穷大
                    visited[node] <- false
                    nodes <- nodes.concat({node})
                }
                distances[start] <- 0

                i <- 0
                while i < nodes.length {
                    min_distance <- 999999
                    min_node <- null

                    j <- 0
                    while j < nodes.length {
                        node <- nodes[j]
                        if !visited[node] && distances[node] < min_distance {
                            min_distance <- distances[node]
                            min_node <- node
                        }
                        j <- j + 1
                    }

                    if min_node == null {
                        break
                    }

                    visited[min_node] <- true

                    for neighbor, weight in graph[min_node] {
                        if !visited[neighbor] {
                            alt <- distances[min_node] + weight
                            if alt < distances[neighbor] {
                                distances[neighbor] <- alt
                            }
                        }
                    }
                    i <- i + 1
                }

                return distances
            }

            // 测试算法
            primes <- sieve_of_eratosthenes(1000)

            seq1 <- {1, 2, 3, 4, 5}
            seq2 <- {2, 4, 5, 6, 7}
            lcs_length <- longest_common_subsequence(seq1, seq2)

            test_graph <- {
                ""A"": {""B"": 1, ""C"": 4},
                ""B"": {""A"": 1, ""C"": 2, ""D"": 5},
                ""C"": {""A"": 4, ""B"": 2, ""D"": 1},
                ""D"": {""B"": 5, ""C"": 1}
            }
            shortest_paths <- dijkstra_shortest_path(test_graph, ""A"")

            result <- {
                prime_count: primes.length,
                lcs_length: lcs_length,
                shortest_path_a_to_d: shortest_paths[""D""]
            }
        ";
    }

    /// <summary>
    /// 测试大数组操作性能
    /// </summary>
    [Benchmark(Description = "Large Array Operations")]
    public void LargeArrayOperations()
    {
        var interpreter = new LangInterpreter();
        BlockStatement ast = interpreter.Build(LargeArrayCode);
        ast.Run(interpreter.Manager);
    }

    /// <summary>
    /// 测试大列表操作性能
    /// </summary>
    [Benchmark(Description = "Large List Operations")]
    public void LargeListOperations()
    {
        var interpreter = new LangInterpreter();
        BlockStatement ast = interpreter.Build(LargeListCode);
        ast.Run(interpreter.Manager);
    }

    /// <summary>
    /// 测试大字典操作性能
    /// </summary>
    [Benchmark(Description = "Large Dictionary Operations")]
    public void LargeDictionaryOperations()
    {
        var interpreter = new LangInterpreter();
        BlockStatement ast = interpreter.Build(LargeDictionaryCode);
        ast.Run(interpreter.Manager);
    }

    /// <summary>
    /// 测试深度递归性能
    /// </summary>
    [Benchmark(Description = "Deep Recursion Performance")]
    public void DeepRecursionPerformance()
    {
        var interpreter = new LangInterpreter();
        BlockStatement ast = interpreter.Build(DeepRecursionCode);
        ast.Run(interpreter.Manager);
    }

    /// <summary>
    /// 测试内存密集型操作性能
    /// </summary>
    [Benchmark(Description = "Memory Intensive Operations")]
    public void MemoryIntensiveOperations()
    {
        var interpreter = new LangInterpreter();
        BlockStatement ast = interpreter.Build(MemoryIntensiveCode);
        ast.Run(interpreter.Manager);
    }

    /// <summary>
    /// 测试字符串处理密集型操作性能
    /// </summary>
    [Benchmark(Description = "String Processing Intensive")]
    public void StringProcessingIntensive()
    {
        var interpreter = new LangInterpreter();
        BlockStatement ast = interpreter.Build(StringProcessingCode);
        ast.Run(interpreter.Manager);
    }

    /// <summary>
    /// 测试集合操作密集型性能
    /// </summary>
    [Benchmark(Description = "Collection Operations Intensive")]
    public void CollectionOperationsIntensive()
    {
        var interpreter = new LangInterpreter();
        BlockStatement ast = interpreter.Build(CollectionOperationsCode);
        ast.Run(interpreter.Manager);
    }

    /// <summary>
    /// 测试复杂算法密集型性能
    /// </summary>
    [Benchmark(Description = "Complex Algorithms Performance")]
    public void ComplexAlgorithmsPerformance()
    {
        var interpreter = new LangInterpreter();
        BlockStatement ast = interpreter.Build(ComplexAlgorithmsCode);
        ast.Run(interpreter.Manager);
    }

    /// <summary>
    /// 测试极大数据量处理性能
    /// </summary>
    [Benchmark(Description = "Extreme Data Processing")]
    public void ExtremeDataProcessing()
    {
        var extremeDataCode = @"
            // 极大数据量测试
            ultra_large_array <- []
            i <- 0
            while i < 50000 {
                ultra_large_array <- ultra_large_array.concat({i * 7 % 1000})
                i <- i + 1
            }

            // 复杂数据处理
            frequency_map <- {}
            i <- 0
            while i < ultra_large_array.length {
                value <- ultra_large_array[i]
                if frequency_map[value] == null {
                    frequency_map[value] <- 0
                }
                frequency_map[value] <- frequency_map[value] + 1
                i <- i + 1
            }

            // 查找最高频值
            max_frequency <- 0
            most_frequent_value <- -1
            for value, frequency in frequency_map {
                if frequency > max_frequency {
                    max_frequency <- frequency
                    most_frequent_value <- value
                }
            }

            result <- {array_length: ultra_large_array.length, most_frequent: most_frequent_value, frequency: max_frequency}
        ";

        var interpreter = new LangInterpreter();
        BlockStatement ast = interpreter.Build(extremeDataCode);
        ast.Run(interpreter.Manager);
    }

    /// <summary>
    /// 测试内存分配和释放性能
    /// </summary>
    [Benchmark(Description = "Memory Allocation Performance")]
    public void MemoryAllocationPerformance()
    {
        var memoryCode = @"
            // 内存分配和释放测试
            memory_blocks <- {}
            i <- 0
            while i < 1000 {
                block <- {}
                j <- 0
                while j < 100 {
                    block[j] <- {id: i * 100 + j, data: ""data_"" + (i * 100 + j), timestamp: i}
                    j <- j + 1
                }
                memory_blocks[i] <- block
                i <- i + 1
            }

            // 内存访问和操作
            total_accessed <- 0
            i <- 0
            while i < memory_blocks.length {
                block <- memory_blocks[i]
                j <- 0
                while j < block.length {
                    item <- block[j]
                    total_accessed <- total_accessed + item.id
                    j <- j + 1
                }
                i <- i + 1
            }

            result <- {blocks_created: memory_blocks.length, total_accessed: total_accessed}
        ";

        var interpreter = new LangInterpreter();
        BlockStatement ast = interpreter.Build(memoryCode);
        ast.Run(interpreter.Manager);
    }

    /// <summary>
    /// 测试计算密集型操作性能
    /// </summary>
    [Benchmark(Description = "Compute Intensive Operations")]
    public void ComputeIntensiveOperations()
    {
        var computeCode = @"
            // 计算密集型操作测试
            func pi_approximation(iterations) {
                pi <- 0.0
                sign <- 1
                i <- 0
                while i < iterations {
                    pi <- pi + sign / (2 * i + 1)
                    sign <- sign * -1
                    i <- i + 1
                }
                return pi * 4
            }

            func matrix_transpose(matrix) {
                rows <- matrix.length
                cols <- matrix[0].length
                transposed <- {}

                i <- 0
                while i < cols {
                    transposed[i] <- {}
                    j <- 0
                    while j < rows {
                        transposed[i][j] <- matrix[j][i]
                        j <- j + 1
                    }
                    i <- i + 1
                }
                return transposed
            }

            // 创建测试矩阵
            test_matrix <- {}
            i <- 0
            while i < 20 {
                test_matrix[i] <- {}
                j <- 0
                while j < 20 {
                    test_matrix[i][j] <- i * j + 1
                    j <- j + 1
                }
                i <- i + 1
            }

            // 执行计算密集型操作
            pi_estimation <- pi_approximation(10000)
            transposed_matrix <- matrix_transpose(test_matrix)

            result <- {
                pi_approximation: pi_estimation,
                original_matrix_size: test_matrix.length,
                transposed_size: transposed_matrix.length
            }
        ";

        var interpreter = new LangInterpreter();
        BlockStatement ast = interpreter.Build(computeCode);
        ast.Run(interpreter.Manager);
    }
}