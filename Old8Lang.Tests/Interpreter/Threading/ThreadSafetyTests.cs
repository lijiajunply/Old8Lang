using Xunit.Abstractions;
using Old8Lang.Interpreter;
namespace Old8Lang.Tests.Interpreter.Threading;
/// <summary>
/// 线程安全测试
/// 测试多线程环境下的数据竞争、死锁、活锁等线程安全问题
/// </summary>
[Trait("Category", "Interpreter")]
[Trait("Category", "Interpreter-Threading")]
[Trait("Category", "Interpreter-ThreadSafety")]
public class ThreadSafetyTests
{
    private readonly ITestOutputHelper _output;

    public ThreadSafetyTests(ITestOutputHelper output)
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
    public void ThreadSafety_SharedCounter_RaceCondition()
    {
        var code = @"
            // 模拟共享计数器的竞态条件
            counter <- 0
            increment_count <- 100
            // 非线程安全的递增操作
            unsafe_increment <- func() {
                old_value <- counter
                // 模拟处理延迟
                temp_value <- old_value + 1
                counter <- temp_value
                return counter
            }
            // 模拟多个线程同时递增
            i <- 0
            while i < increment_count {
                unsafe_increment()
                i <- i + 1
            }
            final_counter <- counter
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("final_counter"));
        Assert.Equal(100, Convert.ToInt32(result["final_counter"]));
    }
    [Fact]
    public void ThreadSafety_ThreadSafeCounter_AtomicOperations()
    {
        var code = @"
            // 线程安全的计数器
            safe_counter <- func(initial_value) {
                return {
                    value: initial_value,
                    increment: func(self) {
                        self.value <- self.value + 1
                        return self.value
                    },
                    decrement: func(self) {
                        self.value <- self.value - 1
                        return self.value
                    },
                    get: func(self) {
                        return self.value
                    }
                }
            }
            counter <- safe_counter(0)
            i <- 0
            while i < 50 {
                counter.increment(counter)
                i <- i + 1
            }
            final_value <- counter.get(counter)
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("final_value"));
        Assert.Equal(50, Convert.ToInt32(result["final_value"]));
    }
    [Fact]
    public void ThreadSafety_DeadlockDetection_TwoLocks()
    {
        var code = @"
            // 死锁检测模拟
            lock1 <- {owner: null, locked: false}
            lock2 <- {owner: null, locked: false}
            acquire_lock <- func(lock_obj, thread_id) {
                if !lock_obj.locked {
                    lock_obj.locked <- true
                    lock_obj.owner <- thread_id
                    return true
                } else if lock_obj.owner == thread_id {
                    return true  // 重入
                } else {
                    return false  // 锁被其他线程持有
                }
            }
            release_lock <- func(lock_obj, thread_id) {
                if lock_obj.owner == thread_id {
                    lock_obj.locked <- false
                    lock_obj.owner <- null
                    return true
                } else {
                    return false
                }
            }
            // 模拟死锁场景
            thread1_gets_lock1 <- acquire_lock(lock1, ""thread1"")
            thread2_gets_lock2 <- acquire_lock(lock2, ""thread2"")
            // 现在尝试获取对方的锁（会导致死锁）
            thread1_tries_lock2 <- acquire_lock(lock2, ""thread1"")
            thread2_tries_lock1 <- acquire_lock(lock1, ""thread2"")
            deadlock_detected <- (!thread1_tries_lock2) && (!thread2_tries_lock1)
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("deadlock_detected"));
        Assert.Equal(true, result["deadlock_detected"]);
    }
    [Fact]
    public void ThreadSafety_LivelockDetection_ConstantRetrying()
    {
        var code = @"
            // 活锁检测模拟
            thread1_state <- ""trying""
            thread2_state <- ""trying""
            attempts <- 0
            // 两个线程都有礼貌地退让
            polite_thread1 <- func() {
                if thread2_state == ""trying"" {
                    thread1_state <- ""yielding""
                    return ""yielded""
                } else {
                    return ""proceeded""
                }
            }
            polite_thread2 <- func() {
                if thread1_state == ""trying"" {
                    thread2_state <- ""yielding""
                    return ""yielded""
                } else {
                    return ""proceeded""
                }
            }
            // 模拟活锁：两个线程不断退让
            i <- 0
            while i < 10 {
                result1 <- polite_thread1()
                result2 <- polite_thread2()
                attempts <- attempts + 1
                // 重置状态继续尝试
                thread1_state <- ""trying""
                thread2_state <- ""trying""
                i <- i + 1
            }
            livelock_detected <- attempts > 5  // 如果尝试次数过多，可能是活锁
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("livelock_detected"));
        Assert.Equal(true, result["livelock_detected"]);
    }
    [Fact]
    public void ThreadSafety_CriticalSection_MutualExclusion()
    {
        var code = @"
            // 临界区保护
            shared_resource <- 0
            critical_section_active <- false
            enter_critical_section <- func(thread_id) {
                if critical_section_active {
                    return false  // 临界区被占用
                } else {
                    critical_section_active <- true
                    return true
                }
            }
            exit_critical_section <- func(thread_id) {
                critical_section_active <- false
            }
            // 模拟线程进入临界区
            thread1_entered <- enter_critical_section(""thread1"")
            thread2_entered <- enter_critical_section(""thread2"")
            // 只有线程1应该成功进入
            only_thread1_entered <- thread1_entered && !thread2_entered
            // 线程1退出后，线程2才能进入
            exit_critical_section(""thread1"")
            thread2_enters_after <- enter_critical_section(""thread2"")
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("only_thread1_entered"));
        Assert.True(result.ContainsKey("thread2_enters_after"));
        Assert.Equal(true, result["only_thread1_entered"]);
        Assert.Equal(true, result["thread2_enters_after"]);
    }
    [Fact]
    public void ThreadSafety_VolatileVariable_Visibility()
    {
        var code = @"
            // volatile变量模拟（可见性保证）
            volatile_flag <- false
            non_volatile_data <- 0
            writer_thread <- func() {
                non_volatile_data <- 42
                volatile_flag <- true  // 写入volatile变量，确保数据可见性
            }
            reader_thread <- func() {
                if volatile_flag {
                    return non_volatile_data
                } else {
                    return 0
                }
            }
            // 模拟执行顺序
            writer_thread()
            result <- reader_thread()
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("result"));
        Assert.Equal(42, Convert.ToInt32(result["result"]));
    }
    [Fact]
    public void ThreadSafety_ThreadSafeCollection_ConcurrentModification()
    {
        var code = @"
            // 线程安全集合模拟
            thread_safe_list <- func() {
                return {
                    items: {},
                    size: 0,
                    lock_acquired: false,
                    add: func(self, item) {
                        // 简化的锁机制
                        if !self.lock_acquired {
                            self.lock_acquired <- true
                            self.items <- self.items.concat({item})
                            self.size <- self.size + 1
                            self.lock_acquired <- false
                            return true
                        } else {
                            return false
                        }
                    },
                    get: func(self, index) {
                        if index >= 0 && index < self.size {
                            return self.items[index]
                        } else {
                            return null
                        }
                    },
                    get_size: func(self) {
                        return self.size
                    }
                }
            }
            safe_list <- thread_safe_list()
            safe_list.add(safe_list, ""item1"")
            safe_list.add(safe_list, ""item2"")
            safe_list.add(safe_list, ""item3"")
            size <- safe_list.get_size(safe_list)
            item1 <- safe_list.get(safe_list, 0)
            item2 <- safe_list.get(safe_list, 1)
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("size"));
        Assert.True(result.ContainsKey("item1"));
        Assert.True(result.ContainsKey("item2"));
        Assert.Equal(3, Convert.ToInt32(result["size"]));
        Assert.Equal("item1", result["item1"]);
        Assert.Equal("item2", result["item2"]);
    }
    [Fact]
    public void ThreadSafety_RaceConditionPrevention_AtomicCompareAndSwap()
    {
        var code = @"
            // 使用比较并交换防止竞态条件
            atomic_value <- func(initial_value) {
                return {
                    value: initial_value,
                    compare_and_set: func(self, expected, new_value) {
                        if self.value == expected {
                            self.value <- new_value
                            return true
                        } else {
                            return false
                        }
                    },
                    get: func(self) {
                        return self.value
                    }
                }
            }
            counter <- atomic_value(0)
            // 原子递增操作
            atomic_increment <- func(atomic_counter) {
                while true {
                    current <- atomic_counter.get(atomic_counter)
                    new_value <- current + 1
                    if atomic_counter.compare_and_set(atomic_counter, current, new_value) {
                        return new_value
                    }
                    // 如果失败，重试
                }
            }
            result1 <- atomic_increment(counter)
            result2 <- atomic_increment(counter)
            final_value <- counter.get(counter)
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("result1"));
        Assert.True(result.ContainsKey("result2"));
        Assert.True(result.ContainsKey("final_value"));
        Assert.Equal(1, Convert.ToInt32(result["result1"]));
        Assert.Equal(2, Convert.ToInt32(result["result2"]));
        Assert.Equal(2, Convert.ToInt32(result["final_value"]));
    }
    [Fact]
    public void ThreadSafety_ReentrantLock_NestedLocking()
    {
        var code = @"
            // 可重入锁测试
            reentrant_lock <- func() {
                return {
                    owner: null,
                    count: 0,
                    lock: func(self, thread_id) {
                        if self.owner == null {
                            self.owner <- thread_id
                            self.count <- 1
                            return true
                        } else if self.owner == thread_id {
                            self.count <- self.count + 1
                            return true
                        } else {
                            return false
                        }
                    },
                    unlock: func(self, thread_id) {
                        if self.owner == thread_id {
                            self.count <- self.count - 1
                            if self.count == 0 {
                                self.owner <- null
                            }
                            return true
                        } else {
                            return false
                        }
                    },
                    get_count: func(self) {
                        return self.count
                    },
                    get_owner: func(self) {
                        return self.owner
                    }
                }
            }
            lock_obj <- reentrant_lock()
            // 同一线程多次获取锁
            first_lock <- lock_obj.lock(lock_obj, ""thread1"")
            second_lock <- lock_obj.lock(lock_obj, ""thread1"")
            third_lock <- lock_obj.lock(lock_obj, ""thread1"")
            lock_count <- lock_obj.get_count(lock_obj)
            owner <- lock_obj.get_owner(lock_obj)
            // 释放锁（需要释放三次）
            first_unlock <- lock_obj.unlock(lock_obj, ""thread1"")
            second_unlock <- lock_obj.unlock(lock_obj, ""thread1"")
            third_unlock <- lock_obj.unlock(lock_obj, ""thread1"")
            final_count <- lock_obj.get_count(lock_obj)
            final_owner <- lock_obj.get_owner(lock_obj)
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("lock_count"));
        Assert.True(result.ContainsKey("final_count"));
        Assert.True(result.ContainsKey("final_owner"));
        Assert.Equal(3, Convert.ToInt32(result["lock_count"]));
        Assert.Equal(0, Convert.ToInt32(result["final_count"]));
        Assert.Null(result["final_owner"]);
    }
    [Fact]
    public void ThreadSafety_ReadWriteLock_MultipleReaders()
    {
        var code = @"
            // 读写锁：允许多个读者，但只允许一个写者
            rw_lock <- func() {
                return {
                    readers: 0,
                    writer: null,
                    read_lock: func(self, thread_id) {
                        if self.writer == null {
                            self.readers <- self.readers + 1
                            return true
                        } else {
                            return false
                        }
                    },
                    read_unlock: func(self, thread_id) {
                        if self.readers > 0 {
                            self.readers <- self.readers - 1
                            return true
                        } else {
                            return false
                        }
                    },
                    write_lock: func(self, thread_id) {
                        if self.writer == null && self.readers == 0 {
                            self.writer <- thread_id
                            return true
                        } else {
                            return false
                        }
                    },
                    write_unlock: func(self, thread_id) {
                        if self.writer == thread_id {
                            self.writer <- null
                            return true
                        } else {
                            return false
                        }
                    },
                    get_reader_count: func(self) {
                        return self.readers
                    }
                }
            }
            lock_obj <- rw_lock()
            // 多个读者可以同时获取锁
            reader1_lock <- lock_obj.read_lock(lock_obj, ""reader1"")
            reader2_lock <- lock_obj.read_lock(lock_obj, ""reader2"")
            reader3_lock <- lock_obj.read_lock(lock_obj, ""reader3"")
            reader_count <- lock_obj.get_reader_count(lock_obj)
            // 写者无法在有读者时获取锁
            writer_lock_attempt <- lock_obj.write_lock(lock_obj, ""writer"")
            // 读者释放锁后，写者才能获取
            lock_obj.read_unlock(lock_obj, ""reader1"")
            lock_obj.read_unlock(lock_obj, ""reader2"")
            lock_obj.read_unlock(lock_obj, ""reader3"")
            writer_lock_after <- lock_obj.write_lock(lock_obj, ""writer"")
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("reader_count"));
        Assert.True(result.ContainsKey("writer_lock_attempt"));
        Assert.True(result.ContainsKey("writer_lock_after"));
        Assert.Equal(3, Convert.ToInt32(result["reader_count"]));
        Assert.Equal(false, result["writer_lock_attempt"]);
        Assert.Equal(true, result["writer_lock_after"]);
    }
    [Fact]
    public void ThreadSafety_ThreadLocalVariable_ThreadIsolation()
    {
        var code = @"
            // 线程局部变量模拟
            thread_local_storage <- func() {
                return {
                    storage: {},
                    set: func(self, thread_id, key, value) {
                        if self.storage[thread_id] == null {
                            self.storage[thread_id] <- {}
                        }
                        self.storage[thread_id][key] <- value
                    },
                    get: func(self, thread_id, key) {
                        if self.storage[thread_id] != null {
                            return self.storage[thread_id][key]
                        } else {
                            return null
                        }
                    },
                    remove: func(self, thread_id) {
                        self.storage[thread_id] <- null
                    }
                }
            }
            tls <- thread_local_storage()
            // 不同线程存储不同的值
            tls.set(tls, ""thread1"", ""counter"", 10)
            tls.set(tls, ""thread2"", ""counter"", 20)
            tls.set(tls, ""thread1"", ""name"", ""Alice"")
            tls.set(tls, ""thread2"", ""name"", ""Bob"")
            thread1_counter <- tls.get(tls, ""thread1"", ""counter"")
            thread2_counter <- tls.get(tls, ""thread2"", ""counter"")
            thread1_name <- tls.get(tls, ""thread1"", ""name"")
            thread2_name <- tls.get(tls, ""thread2"", ""name"")
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("thread1_counter"));
        Assert.True(result.ContainsKey("thread2_counter"));
        Assert.True(result.ContainsKey("thread1_name"));
        Assert.True(result.ContainsKey("thread2_name"));
        Assert.Equal(10, Convert.ToInt32(result["thread1_counter"]));
        Assert.Equal(20, Convert.ToInt32(result["thread2_counter"]));
        Assert.Equal("Alice", result["thread1_name"]);
        Assert.Equal("Bob", result["thread2_name"]);
    }
    [Fact]
    public void ThreadSafety_MemoryBarrier_VisibilityGuarantee()
    {
        var code = @"
            // 内存屏障模拟
            memory_barrier <- func() {
                return {
                    barrier_reached: false,
                    data: null,
                    write_with_barrier: func(self, value) {
                        self.data <- value
                        self.barrier_reached <- true
                        return true
                    },
                    read_with_barrier: func(self) {
                        if self.barrier_reached {
                            return self.data
                        } else {
                            return null
                        }
                    }
                }
            }
            barrier <- memory_barrier()
            // 写入数据并通过内存屏障
            write_result <- barrier.write_with_barrier(barrier, ""important_data"")
            // 读取数据（内存屏障确保可见性）
            read_result <- barrier.read_with_barrier(barrier)
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("write_result"));
        Assert.True(result.ContainsKey("read_result"));
        Assert.Equal(true, result["write_result"]);
        Assert.Equal("important_data", result["read_result"]);
    }
    [Fact]
    public void ThreadSafety_CircularWaitCondition_DeadlockAvoidance()
    {
        var code = @"
            // 检测和避免循环等待条件
            lock_manager <- func() {
                return {
                    locks: {},
                    acquire_lock: func(self, lock_id, thread_id) {
                        if self.locks[lock_id] == null {
                            self.locks[lock_id] <- thread_id
                            return true
                        } else {
                            return false
                        }
                    },
                    check_circular_wait: func(self, thread_id, requested_locks) {
                        // 简化的循环等待检测
                        i <- 0
                        while i < requested_locks.length {
                            lock_id <- requested_locks[i]
                            owner <- self.locks[lock_id]
                            if owner != thread_id {
                                // 检查该锁的拥有者是否也在等待当前线程持有的锁
                                // 这里简化处理，假设有循环等待
                                return true  // 检测到潜在的循环等待
                            }
                            i <- i + 1
                        }
                        return false
                    }
                }
            }
            manager <- lock_manager()
            // 设置锁的拥有关系
            manager.acquire_lock(manager, ""lock1"", ""thread1"")
            manager.acquire_lock(manager, ""lock2"", ""thread2"")
            // thread1请求lock2，可能导致循环等待
            circular_wait_detected1 <- manager.check_circular_wait(manager, ""thread1"", {""lock2""})
            // thread2请求lock1，可能导致循环等待
            circular_wait_detected2 <- manager.check_circular_wait(manager, ""thread2"", {""lock1""})
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("circular_wait_detected1"));
        Assert.True(result.ContainsKey("circular_wait_detected2"));
        Assert.Equal(true, result["circular_wait_detected1"]);
        Assert.Equal(true, result["circular_wait_detected2"]);
    }
    [Fact]
    public void ThreadSafety_HierarchicalLocking_DeadlockPrevention()
    {
        var code = @"
            // 分层锁机制：按固定顺序获取锁以避免死锁
            hierarchical_lock_manager <- func() {
                return {
                    lock_hierarchy: {
                        ""database"": 1,
                        ""file"": 2,
                        ""network"": 3,
                        ""memory"": 4
                    },
                    held_locks: {},
                    acquire_locks: func(self, thread_id, requested_locks) {
                        // 按层级顺序排序锁
                        sorted_locks <- requested_locks
                        // 检查是否按层级顺序请求
                        i <- 0
                        while i < sorted_locks.length - 1 {
                            current_lock <- sorted_locks[i]
                            next_lock <- sorted_locks[i + 1]
                            if self.lock_hierarchy[current_lock] > self.lock_hierarchy[next_lock] {
                                return false  // 违反层级顺序
                            }
                            i <- i + 1
                        }
                        // 按顺序获取锁
                        j <- 0
                        while j < sorted_locks.length {
                            lock_id <- sorted_locks[j]
                            self.held_locks[lock_id] <- thread_id
                            j <- j + 1
                        }
                        return true
                    },
                    release_locks: func(self, thread_id, locks) {
                        i <- 0
                        while i < locks.length {
                            lock_id <- locks[i]
                            self.held_locks[lock_id] <- null
                            i <- i + 1
                        }
                    }
                }
            }
            h_manager <- hierarchical_lock_manager()
            // 正确的锁请求顺序（按层级）
            correct_order <- h_manager.acquire_locks(h_manager, ""thread1"", {""database"", ""file""})
            // 错误的锁请求顺序（违反层级）
            wrong_order <- h_manager.acquire_locks(h_manager, ""thread2"", {""file"", ""database""})
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("correct_order"));
        Assert.True(result.ContainsKey("wrong_order"));
        Assert.Equal(true, result["correct_order"]);
        Assert.Equal(false, result["wrong_order"]);
    }
    [Fact]
    public void ThreadSafety_TimeoutBasedLocking_DeadlockTimeout()
    {
        var code = @"
            // 基于超时的锁获取
            timeout_lock <- func() {
                return {
                    owner: null,
                    locked: false,
                    wait_start_time: null,
                    try_lock_with_timeout: func(self, thread_id, timeout_ms) {
                        if !self.locked {
                            self.locked <- true
                            self.owner <- thread_id
                            return {success: true, reason: ""acquired immediately""}
                        } else if self.owner == thread_id {
                            return {success: true, reason: ""already owner""}
                        } else {
                            if self.wait_start_time == null {
                                self.wait_start_time <- 0  // 模拟时间戳
                            }
                            // 简化的超时检查
                            elapsed_time <- 0  // 在实际实现中会计算真实时间差
                            if elapsed_time > timeout_ms {
                                return {success: false, reason: ""timeout""}
                            } else {
                                return {success: false, reason: ""still waiting""}
                            }
                        }
                    },
                    unlock: func(self, thread_id) {
                        if self.owner == thread_id {
                            self.locked <- false
                            self.owner <- null
                            self.wait_start_time <- null
                            return true
                        } else {
                            return false
                        }
                    }
                }
            }
            tlock <- timeout_lock()
            // 第一个线程获取锁
            acquire1 <- tlock.try_lock_with_timeout(tlock, ""thread1"", 100)
            // 第二个线程尝试获取锁（会超时）
            acquire2 <- tlock.try_lock_with_timeout(tlock, ""thread2"", 50)
            // 释放锁
            release1 <- tlock.unlock(tlock, ""thread1"")
            // 第二个线程现在可以获取锁
            acquire2_after <- tlock.try_lock_with_timeout(tlock, ""thread2"", 50)
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("acquire1"));
        Assert.True(result.ContainsKey("release1"));
        Assert.True(result.ContainsKey("acquire2_after"));
        Assert.Equal(true, (result["acquire1"] as dynamic).success);
        Assert.Equal(true, result["release1"]);
        Assert.Equal(true, (result["acquire2_after"] as dynamic).success);
    }
    [Fact]
    public void ThreadSafe_AsyncOperation_AsyncThreadSafety()
    {
        var code = @"
            // 异步操作的线程安全
            async_safe_state <- func(initial_value) {
                return {
                    value: initial_value,
                    processing: false,
                    async_update: func(self, new_value) {
                        if !self.processing {
                            self.processing <- true
                            self.value <- new_value
                            self.processing <- false
                            return true
                        } else {
                            return false
                        }
                    },
                    get_value: func(self) {
                        return self.value
                    },
                    is_processing: func(self) {
                        return self.processing
                    }
                }
            }
            safe_state <- async_safe_state(0)
            // 异步更新操作
            update1 <- safe_state.async_update(safe_state, 10)
            update2 <- safe_state.async_update(safe_state, 20)
            update3 <- safe_state.async_update(safe_state, 30)
            value1 <- safe_state.get_value(safe_state)
            processing1 <- safe_state.is_processing(safe_state)
            // 模拟异步操作完成
            safe_state.processing <- false
            update4 <- safe_state.async_update(safe_state, 40)
            value2 <- safe_state.get_value(safe_state)
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("update1"));
        Assert.True(result.ContainsKey("value1"));
        Assert.True(result.ContainsKey("update4"));
        Assert.True(result.ContainsKey("value2"));
        Assert.Equal(true, result["update1"]);
        Assert.Equal(10, Convert.ToInt32(result["value1"]));
        Assert.Equal(true, result["update4"]);
        Assert.Equal(40, Convert.ToInt32(result["value2"]));
    }
    [Fact]
    public void ThreadSafety_ConcurrentHashMap_SynchronizedAccess()
    {
        var code = @"
            // 并发哈希表模拟
            concurrent_hash_map <- func() {
                return {
                    buckets: {},
                    bucket_count: 16,
                    hash_key: func(self, key) {
                        // 简化的哈希函数
                        hash_value <- 0
                        i <- 0
                        while i < key.length {
                            hash_value <- hash_value + (key[i].charCodeAt(0) if key[i].charCodeAt != null else 0)
                            i <- i + 1
                        }
                        return hash_value % self.bucket_count
                    },
                    put: func(self, key, value) {
                        bucket_index <- self.hash_key(self, key)
                        if self.buckets[bucket_index] == null {
                            self.buckets[bucket_index] <- {}
                        }
                        self.buckets[bucket_index][key] <- value
                        return true
                    },
                    get: func(self, key) {
                        bucket_index <- self.hash_key(self, key)
                        if self.buckets[bucket_index] != null {
                            return self.buckets[bucket_index][key]
                        } else {
                            return null
                        }
                    },
                    remove: func(self, key) {
                        bucket_index <- self.hash_key(self, key)
                        if self.buckets[bucket_index] != null {
                            old_value <- self.buckets[bucket_index][key]
                            self.buckets[bucket_index][key] <- null
                            return old_value
                        } else {
                            return null
                        }
                    }
                }
            }
            chm <- concurrent_hash_map()
            // 并发访问测试
            chm.put(chm, ""key1"", ""value1"")
            chm.put(chm, ""key2"", ""value2"")
            chm.put(chm, ""key3"", ""value3"")
            retrieved_value1 <- chm.get(chm, ""key1"")
            retrieved_value2 <- chm.get(chm, ""key2"")
            non_existent <- chm.get(chm, ""key4"")
            removed_value <- chm.remove(chm, ""key2"")
            after_removal <- chm.get(chm, ""key2"")
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("retrieved_value1"));
        Assert.True(result.ContainsKey("retrieved_value2"));
        Assert.True(result.ContainsKey("non_existent"));
        Assert.True(result.ContainsKey("removed_value"));
        Assert.True(result.ContainsKey("after_removal"));
        Assert.Equal("value1", result["retrieved_value1"]);
        Assert.Equal("value2", result["retrieved_value2"]);
        Assert.Null(result["non_existent"]);
        Assert.Equal("value2", result["removed_value"]);
        Assert.Null(result["after_removal"]);
    }
    [Fact]
    public void ThreadSafety_BlockingQueue_ThreadSafeOperations()
    {
        var code = @"
            // 线程安全的阻塞队列
            thread_safe_blocking_queue <- func(capacity) {
                return {
                    items: {},
                    capacity: capacity,
                    size: 0,
                    operation_in_progress: false,
                    put: func(self, item) {
                        if self.operation_in_progress {
                            return false
                        }
                        self.operation_in_progress <- true
                        if self.size < self.capacity {
                            self.items <- self.items.concat({item})
                            self.size <- self.size + 1
                            self.operation_in_progress <- false
                            return true
                        } else {
                            self.operation_in_progress <- false
                            return false
                        }
                    },
                    take: func(self) {
                        if self.operation_in_progress {
                            return null
                        }
                        self.operation_in_progress <- true
                        if self.size > 0 {
                            item <- self.items[0]
                            self.items <- self.items.slice(1)
                            self.size <- self.size - 1
                            self.operation_in_progress <- false
                            return item
                        } else {
                            self.operation_in_progress <- false
                            return null
                        }
                    },
                    get_size: func(self) {
                        return self.size
                    },
                    is_empty: func(self) {
                        return self.size == 0
                    }
                }
            }
            queue <- thread_safe_blocking_queue(3)
            // 线程安全的操作
            put1 <- queue.put(queue, ""item1"")
            put2 <- queue.put(queue, ""item2"")
            put3 <- queue.put(queue, ""item3"")
            put4 <- queue.put(queue, ""item4"")  // 应该失败，队列已满
            size_before <- queue.get_size(queue)
            is_empty_before <- queue.is_empty(queue)
            take1 <- queue.take(queue)
            take2 <- queue.take(queue)
            size_after <- queue.get_size(queue)
            is_empty_after <- queue.is_empty(queue)
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("put1"));
        Assert.True(result.ContainsKey("put4"));
        Assert.True(result.ContainsKey("size_before"));
        Assert.True(result.ContainsKey("size_after"));
        Assert.Equal(true, result["put1"]);
        Assert.Equal(false, result["put4"]);
        Assert.Equal(3, Convert.ToInt32(result["size_before"]));
        Assert.Equal(1, Convert.ToInt32(result["size_after"]));
    }
}