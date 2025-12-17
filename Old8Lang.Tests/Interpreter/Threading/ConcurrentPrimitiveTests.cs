using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Threading;
using Old8Lang.Interpreter;
using System.Collections.Generic;
/// <summary>
/// 并发原语测试
/// 测试各种并发编程中的原子操作、锁、信号量等原语
/// </summary>
[Trait("Category", "Interpreter")]
[Trait("Category", "Interpreter-Threading")]
[Trait("Category", "Interpreter-Concurrency")]
public class ConcurrentPrimitiveTests
{
    private readonly ITestOutputHelper _output;

    public ConcurrentPrimitiveTests(ITestOutputHelper output)
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
    public void ConcurrentPrimitive_AtomicIncrement_PerformsAtomicOperation()
    {
        var code = @"
            counter <- 0
            // 原子递增操作模拟
            atomic_increment <- (ref_value) -> {
                current <- ref_value
                result <- current + 1
                return result
            }
            result <- atomic_increment(counter)
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("result"));
        Assert.Equal(1, Convert.ToInt32(result["result"]));
    }
    [Fact]
    public void ConcurrentPrimitive_AtomicCompareAndSwap_PerformsCASOperation()
    {
        var code = @"
            value <- 10
            // 原子比较并交换操作模拟
            compare_and_swap <- (ref_value, expected, new_value) -> {
                if ref_value == expected {
                    return new_value
                } else {
                    return ref_value
                }
            }
            result <- compare_and_swap(value, 10, 20)
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("result"));
        Assert.Equal(20, Convert.ToInt32(result["result"]));
    }
    [Fact]
    public void ConcurrentPrimitive_SpinLock_WaitsForLockRelease()
    {
        var code = @"
            lock_acquired <- false
            lock_var <- false
            // 自旋锁模拟
            spin_lock <- func(lock_var) {
                while lock_var {
                    // 忙等待
                }
                lock_var <- true
                return true
            }
            spin_unlock <- func(lock_var) {
                lock_var <- false
            }
            acquired <- spin_lock(lock_var)
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("acquired"));
        Assert.Equal(true, result["acquired"]);
    }
    [Fact]
    public void ConcurrentPrimitive_ReentrantLock_SupportsReentry()
    {
        var code = @"
            lock_count <- 0
            owner_id <- null
            // 可重入锁模拟
            reentrant_lock <- func(lock_id) {
                if owner_id == null {
                    owner_id <- lock_id
                    lock_count <- 1
                    return true
                } else if owner_id == lock_id {
                    lock_count <- lock_count + 1
                    return true
                } else {
                    return false
                }
            }
            acquired <- reentrant_lock(""thread1"")
            reacquired <- reentrant_lock(""thread1"")
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("acquired"));
        Assert.True(result.ContainsKey("reacquired"));
        Assert.Equal(true, result["acquired"]);
        Assert.Equal(true, result["reacquired"]);
    }
    [Fact]
    public void ConcurrentPrimitive_ReadWriteLock_AllowsConcurrentReads()
    {
        var code = @"
            readers_count <- 0
            writer_active <- false
            // 读写锁的读操作
            read_lock <- func() {
                if writer_active {
                    return false
                } else {
                    readers_count <- readers_count + 1
                    return true
                }
            }
            read_unlock <- func() {
                readers_count <- readers_count - 1
            }
            // 模拟多个并发读者
            read1_acquired <- read_lock()
            read2_acquired <- read_lock()
            read3_acquired <- read_lock()
            total_readers <- readers_count
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("total_readers"));
        Assert.Equal(3, Convert.ToInt32(result["total_readers"]));
    }
    [Fact]
    public void ConcurrentPrimitive_CountDownLatch_WaitsForCount()
    {
        var code = @"
            count <- 3
            latch_reached <- false
            // 倒计时门闩模拟
            count_down <- func() {
                count <- count - 1
                if count == 0 {
                    latch_reached <- true
                }
            }
            // 模拟多个工作完成
            count_down()  // 工作1完成
            count_down()  // 工作2完成
            count_down()  // 工作3完成
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("latch_reached"));
        Assert.Equal(true, result["latch_reached"]);
    }
    [Fact]
    public void ConcurrentPrimitive_CyclicBarrier_WaitsForParties()
    {
        var code = @"
            parties <- 3
            waiting <- 0
            barrier_broken <- false
            // 循环栅栏模拟
            await_barrier <- func() {
                waiting <- waiting + 1
                if waiting == parties {
                    // 所有线程都在等待，打破栅栏
                    barrier_broken <- true
                    waiting <- 0
                    return true
                } else {
                    return false
                }
            }
            // 模拟线程到达栅栏
            thread1 <- await_barrier()
            thread2 <- await_barrier()
            thread3 <- await_barrier()
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("barrier_broken"));
        Assert.Equal(true, result["barrier_broken"]);
        Assert.True(result.ContainsKey("thread3"));
        Assert.Equal(true, result["thread3"]);
    }
    [Fact]
    public void ConcurrentPrimitive_AtomicReference_ThreadSafeReference()
    {
        var code = @"
            // 原子引用模拟
            atomic_ref <- func(initial_value) {
                return {
                    value: initial_value,
                    get: func(self) { return self.value },
                    set: func(self, new_value) {
                        old_value <- self.value
                        self.value <- new_value
                        return old_value
                    },
                    compare_and_set: func(self, expected, new_value) {
                        if self.value == expected {
                            self.value <- new_value
                            return true
                        } else {
                            return false
                        }
                    }
                }
            }
            ref <- atomic_ref(""initial"")
            old_value <- ref.set(ref, ""updated"")
            current_value <- ref.get(ref)
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("old_value"));
        Assert.True(result.ContainsKey("current_value"));
        Assert.Equal("initial", result["old_value"]);
        Assert.Equal("updated", result["current_value"]);
    }
    [Fact]
    public void ConcurrentPrimitive_Exchanger_SwapsValues()
    {
        var code = @"
            // 交换器模拟
            exchanger <- func() {
                return {
                    first_value: null,
                    second_value: null,
                    first_waiting: false,
                    second_waiting: false,
                    exchange: func(self, value) {
                        if !self.first_waiting {
                            self.first_value <- value
                            self.first_waiting <- true
                            return null  // 等待另一个线程
                        } else if !self.second_waiting {
                            self.second_value <- value
                            self.second_waiting <- true
                            first_result <- self.second_value
                            second_result <- self.first_value
                            // 重置状态
                            self.first_waiting <- false
                            self.second_waiting <- false
                            return first_result
                        } else {
                            return null
                        }
                    }
                }
            }
            ex <- exchanger()
            result1 <- ex.exchange(ex, ""value1"")
            result2 <- ex.exchange(ex, ""value2"")
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("result1"));
        Assert.True(result.ContainsKey("result2"));
    }
    [Fact]
    public void ConcurrentPrimitive_FutureStore_AsyncComputation()
    {
        var code = @"
            // Future存储模拟
            future_store <- func() {
                return {
                    completed: false,
                    value: null,
                    exception: null,
                    set_value: func(self, value) {
                        self.value <- value
                        self.completed <- true
                    },
                    get_value: func(self) {
                        if self.completed {
                            return self.value
                        } else {
                            return ""pending""
                        }
                    },
                    is_completed: func(self) {
                        return self.completed
                    }
                }
            }
            future <- future_store()
            before_completion <- future.is_completed(future)
            pending_value <- future.get_value(future)
            // 完成future
            future.set_value(future, ""result"")
            after_completion <- future.is_completed(future)
            completed_value <- future.get_value(future)
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("before_completion"));
        Assert.True(result.ContainsKey("after_completion"));
        Assert.True(result.ContainsKey("pending_value"));
        Assert.True(result.ContainsKey("completed_value"));
        Assert.Equal(false, result["before_completion"]);
        Assert.Equal(true, result["after_completion"]);
        Assert.Equal("pending", result["pending_value"]);
        Assert.Equal("result", result["completed_value"]);
    }
    [Fact]
    public void ConcurrentPrimitive_Phaser_MultiPhaseBarrier()
    {
        var code = @"
            phase <- 0
            parties <- 2
            registered <- 0
            // 阶段器模拟
            phaser <- func() {
                return {
                    register: func(self) {
                        registered <- registered + 1
                        return registered
                    },
                    arrive_and_await_advance: func(self) {
                        registered <- registered - 1
                        if registered == 0 {
                            phase <- phase + 1
                            return phase  // 新的阶段号
                        } else {
                            return phase  // 当前阶段号
                        }
                    },
                    get_phase: func(self) {
                        return phase
                    }
                }
            }
            ph <- phaser()
            ph.register(ph)
            ph.register(ph)
            initial_phase <- ph.get_phase(ph)
            new_phase1 <- ph.arrive_and_await_advance(ph)
            new_phase2 <- ph.arrive_and_await_advance(ph)
            final_phase <- ph.get_phase(ph)
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("initial_phase"));
        Assert.True(result.ContainsKey("final_phase"));
        Assert.Equal(0, Convert.ToInt32(result["initial_phase"]));
        Assert.Equal(1, Convert.ToInt32(result["final_phase"]));
    }
    [Fact]
    public void ConcurrentPrimitive_TransferQueue_TransfersData()
    {
        var code = @"
            // 传输队列模拟
            transfer_queue <- func() {
                return {
                    queue: {},
                    waiting_consumers: {},
                    put: func(self, item) {
                        if self.waiting_consumers.length > 0 {
                            // 直接传输给等待的消费者
                            consumer <- self.waiting_consumers[0]
                            self.waiting_consumers <- self.waiting_consumers.slice(1)
                            return ""transferred""
                        } else {
                            // 放入队列
                            self.queue <- self.queue.concat({item})
                            return ""queued""
                        }
                    },
                    take: func(self) {
                        if self.queue.length > 0 {
                            // 从队列中取出
                            item <- self.queue[0]
                            self.queue <- self.queue.slice(1)
                            return item
                        } else {
                            // 等待生产者
                            return ""waiting""
                        }
                    },
                    size: func(self) {
                        return self.queue.length
                    }
                }
            }
            tq <- transfer_queue()
            put_result1 <- tq.put(tq, ""item1"")
            put_result2 <- tq.put(tq, ""item2"")
            queue_size <- tq.size(tq)
            take_result <- tq.take(tq)
            final_size <- tq.size(tq)
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("put_result1"));
        Assert.True(result.ContainsKey("queue_size"));
        Assert.True(result.ContainsKey("take_result"));
        Assert.True(result.ContainsKey("final_size"));
        Assert.Equal("queued", result["put_result1"]);
        Assert.Equal("item1", result["take_result"]);
    }
    [Fact]
    public void ConcurrentPrimitive_StampedLock_OptimizedReading()
    {
        var code = @"
            // 乐观读锁模拟
            stamped_lock <- func() {
                return {
                    stamp: 0,
                    state: ""unlocked"",  // unlocked, reading, writing
                    optimistic_read: func(self) {
                        stamp <- self.stamp
                        return {
                            stamp: stamp,
                            validate: func(lock_ref) {
                                return lock_ref.stamp == stamp
                            }
                        }
                    },
                    write_lock: func(self) {
                        if self.state == ""unlocked"" {
                            self.state <- ""writing""
                            self.stamp <- self.stamp + 1
                            return true
                        } else {
                            return false
                        }
                    },
                    write_unlock: func(self) {
                        self.state <- ""unlocked""
                        self.stamp <- self.stamp + 1
                    }
                }
            }
            sl <- stamped_lock()
            read_stamp <- sl.optimistic_read(sl)
            is_valid_before <- read_stamp.validate(sl)
            lock_acquired <- sl.write_lock(sl)
            sl.write_unlock(sl)
            is_valid_after <- read_stamp.validate(sl)
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("is_valid_before"));
        Assert.True(result.ContainsKey("is_valid_after"));
        Assert.True(result.ContainsKey("lock_acquired"));
        Assert.Equal(true, result["is_valid_before"]);
        Assert.Equal(false, result["is_valid_after"]);
        Assert.Equal(true, result["lock_acquired"]);
    }
    [Fact]
    public void ConcurrentPrimitive_BlockingQueue_ThreadSafeQueue()
    {
        var code = @"
            // 阻塞队列模拟
            blocking_queue <- func(capacity) {
                return {
                    items: {},
                    capacity: capacity,
                    put: func(self, item) {
                        if self.items.length < self.capacity {
                            self.items <- self.items.concat({item})
                            return true
                        } else {
                            return false  // 队列已满
                        }
                    },
                    take: func(self) {
                        if self.items.length > 0 {
                            item <- self.items[0]
                            self.items <- self.items.slice(1)
                            return item
                        } else {
                            return null  // 队列为空
                        }
                    },
                    size: func(self) {
                        return self.items.length
                    },
                    is_empty: func(self) {
                        return self.items.length == 0
                    }
                }
            }
            bq <- blocking_queue(3)
            put1 <- bq.put(bq, ""item1"")
            put2 <- bq.put(bq, ""item2"")
            size_after_puts <- bq.size(bq)
            is_empty_after_puts <- bq.is_empty(bq)
            taken_item <- bq.take(bq)
            size_after_take <- bq.size(bq)
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("size_after_puts"));
        Assert.True(result.ContainsKey("is_empty_after_puts"));
        Assert.True(result.ContainsKey("taken_item"));
        Assert.True(result.ContainsKey("size_after_take"));
        Assert.Equal(2, Convert.ToInt32(result["size_after_puts"]));
        Assert.Equal(false, result["is_empty_after_puts"]);
        Assert.Equal("item1", result["taken_item"]);
        Assert.Equal(1, Convert.ToInt32(result["size_after_take"]));
    }
    [Fact]
    public void ConcurrentPrimitive_LinkedBlockingQueue_UnboundedQueue()
    {
        var code = @"
            // 链式阻塞队列模拟
            linked_blocking_queue <- func() {
                return {
                    head: null,
                    tail: null,
                    count: 0,
                    node: func(value) {
                        return {
                            value: value,
                            next: null
                        }
                    },
                    put: func(self, value) {
                        new_node <- self.node(value)
                        if self.tail == null {
                            self.head <- new_node
                            self.tail <- new_node
                        } else {
                            self.tail.next <- new_node
                            self.tail <- new_node
                        }
                        self.count <- self.count + 1
                        return true
                    },
                    take: func(self) {
                        if self.head == null {
                            return null
                        } else {
                            value <- self.head.value
                            self.head <- self.head.next
                            if self.head == null {
                                self.tail <- null
                            }
                            self.count <- self.count - 1
                            return value
                        }
                    },
                    size: func(self) {
                        return self.count
                    }
                }
            }
            lbq <- linked_blocking_queue()
            lbq.put(lbq, ""first"")
            lbq.put(lbq, ""second"")
            size_before <- lbq.size(lbq)
            first_item <- lbq.take(lbq)
            size_after <- lbq.size(lbq)
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("size_before"));
        Assert.True(result.ContainsKey("first_item"));
        Assert.True(result.ContainsKey("size_after"));
        Assert.Equal(2, Convert.ToInt32(result["size_before"]));
        Assert.Equal("first", result["first_item"]);
        Assert.Equal(1, Convert.ToInt32(result["size_after"]));
    }
    [Fact]
    public void ConcurrentPrimitive_DelayQueue_TimeBasedScheduling()
    {
        var code = @"
            // 延迟队列模拟
            delay_queue <- func() {
                return {
                    items: {},
                    add: func(self, item, delay_ms) {
                        delayed_item <- {
                            item: item,
                            execute_time: 0,  // 在实际实现中会是当前时间 + 延迟
                            delay: delay_ms
                        }
                        self.items <- self.items.concat({delayed_item})
                        return ""scheduled""
                    },
                    take_ready: func(self) {
                        ready_items <- {}
                        i <- 0
                        while i < self.items.length {
                            item <- self.items[i]
                            // 简化：假设所有延迟都到期的项目都可以取出
                            ready_items <- ready_items.concat({item.item})
                            i <- i + 1
                        }
                        // 清空已取出的项目
                        self.items <- {}
                        return ready_items
                    },
                    size: func(self) {
                        return self.items.length
                    }
                }
            }
            dq <- delay_queue()
            dq.add(dq, ""task1"", 1000)
            dq.add(dq, ""task2"", 500)
            dq.add(dq, ""task3"", 2000)
            size_before <- dq.size(dq)
            ready_tasks <- dq.take_ready(dq)
            size_after <- dq.size(dq)
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("size_before"));
        Assert.True(result.ContainsKey("size_after"));
        Assert.Equal(3, Convert.ToInt32(result["size_before"]));
        Assert.Equal(0, Convert.ToInt32(result["size_after"]));
    }
    [Fact]
    public void ConcurrentPrimitive_SynchronousQueue_DirectHandoff()
    {
        var code = @"
            // 同步队列模拟（直接传递）
            synchronous_queue <- func() {
                return {
                    waiting_producer: null,
                    waiting_consumer: null,
                    put: func(self, item) {
                        if self.waiting_consumer != null {
                            // 直接传递给等待的消费者
                            consumer_result <- self.waiting_consumer
                            self.waiting_consumer <- null
                            return {
                                success: true,
                                transferred_to: consumer_result
                            }
                        } else {
                            // 等待消费者
                            self.waiting_producer <- item
                            return {
                                success: false,
                                reason: ""waiting for consumer""
                            }
                        }
                    },
                    take: func(self) {
                        if self.waiting_producer != null {
                            // 直接从生产者获取
                            item <- self.waiting_producer
                            self.waiting_producer <- null
                            return item
                        } else {
                            // 等待生产者
                            self.waiting_consumer <- ""consumer_waiting""
                            return null
                        }
                    }
                }
            }
            sq <- synchronous_queue()
            consumer_waiting <- sq.take(sq)  // 消费者等待
            producer_result <- sq.put(sq, ""direct_item"")
            consumer_result <- sq.take(sq)  // 再次尝试获取
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("consumer_waiting"));
        Assert.True(result.ContainsKey("producer_result"));
        Assert.True(result.ContainsKey("consumer_result"));
    }
    [Fact]
    public void ConcurrentPrimitive_PriorityOrderedBlockingQueue_OrderedQueue()
    {
        var code = @"
            // 优先级阻塞队列模拟
            priority_queue <- func() {
                return {
                    items: {},
                    put: func(self, item, priority) {
                        prioritized_item <- {
                            item: item,
                            priority: priority
                        }
                        self.items <- self.items.concat({prioritized_item})
                        // 按优先级排序
                        i <- self.items.length - 1
                        while i > 0 {
                            if self.items[i].priority < self.items[i - 1].priority {
                                temp <- self.items[i]
                                self.items[i] <- self.items[i - 1]
                                self.items[i - 1] <- temp
                            }
                            i <- i - 1
                        }
                        return true
                    },
                    take: func(self) {
                        if self.items.length > 0 {
                            item <- self.items[0]
                            self.items <- self.items.slice(1)
                            return item.item
                        } else {
                            return null
                        }
                    },
                    size: func(self) {
                        return self.items.length
                    }
                }
            }
            pq <- priority_queue()
            pq.put(pq, ""low_priority"", 3)
            pq.put(pq, ""high_priority"", 1)
            pq.put(pq, ""medium_priority"", 2)
            size_before <- pq.size(pq)
            first_item <- pq.take(pq)  // 应该是高优先级项目
            second_item <- pq.take(pq)  // 应该是中优先级项目
            third_item <- pq.take(pq)   // 应该是低优先级项目
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("first_item"));
        Assert.True(result.ContainsKey("second_item"));
        Assert.True(result.ContainsKey("third_item"));
        Assert.Equal("high_priority", result["first_item"]);
        Assert.Equal("medium_priority", result["second_item"]);
        Assert.Equal("low_priority", result["third_item"]);
    }
    [Fact]
    public void ConcurrentPrimitive_CustomLock_FlexibleLocking()
    {
        var code = @"
            // 自定义锁实现
            custom_lock <- func() {
                return {
                    locked: false,
                    owner: null,
                    wait_count: 0,
                    try_lock: func(self, thread_id) {
                        if !self.locked {
                            self.locked <- true
                            self.owner <- thread_id
                            return true
                        } else {
                            return false
                        }
                    },
                    lock_with_timeout: func(self, thread_id, timeout_ms) {
                        if !self.locked {
                            self.locked <- true
                            self.owner <- thread_id
                            return {success: true, reason: ""acquired immediately""}
                        } else if self.owner == thread_id {
                            return {success: true, reason: ""already owner""}
                        } else {
                            self.wait_count <- self.wait_count + 1
                            return {success: false, reason: ""timeout"", waiting: self.wait_count}
                        }
                    },
                    unlock: func(self, thread_id) {
                        if self.owner == thread_id {
                            self.locked <- false
                            self.owner <- null
                            if self.wait_count > 0 {
                                self.wait_count <- self.wait_count - 1
                            }
                            return true
                        } else {
                            return false
                        }
                    },
                    is_locked: func(self) {
                        return self.locked
                    },
                    get_owner: func(self) {
                        return self.owner
                    }
                }
            }
            lock_obj <- custom_lock()
            initially_locked <- lock_obj.is_locked(lock_obj)
            acquire1 <- lock_obj.try_lock(lock_obj, ""thread1"")
            after_acquire1_locked <- lock_obj.is_locked(lock_obj)
            owner1 <- lock_obj.get_owner(lock_obj)
            try_acquire2 <- lock_obj.try_lock(lock_obj, ""thread2"")
            timeout_acquire2 <- lock_obj.lock_with_timeout(lock_obj, ""thread2"", 1000)
            release1 <- lock_obj.unlock(lock_obj, ""thread1"")
            after_release_locked <- lock_obj.is_locked(lock_obj)
            acquire2_after_release <- lock_obj.try_lock(lock_obj, ""thread2"")
        ";
        var result = TestInterpreter(code);
        Assert.True(result.ContainsKey("initially_locked"));
        Assert.True(result.ContainsKey("acquire1"));
        Assert.True(result.ContainsKey("try_acquire2"));
        Assert.True(result.ContainsKey("release1"));
        Assert.True(result.ContainsKey("after_release_locked"));
        Assert.Equal(false, result["initially_locked"]);
        Assert.Equal(true, result["acquire1"]);
        Assert.Equal(false, result["try_acquire2"]);
        Assert.Equal(true, result["release1"]);
        Assert.Equal(false, result["after_release_locked"]);
    }
}