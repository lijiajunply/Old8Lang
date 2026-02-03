# Old8Lang 统一实例方法系统 - TODO 文档

## 📋 项目概述

**项目名称：** 统一实例方法系统 (Unified Instance Method System)

**项目目标：** 为 Old8Lang 设计并实现一个统一的实例方法系统，解决当前实例方法调用机制不统一、性能较低、缺少参数验证等问题。

**开始日期：** 2026-02-03

**当前状态：** 🟢 进行中 (核心基础设施已完成，部分方法已迁移)

---

## 🎯 实现目的

### 主要目标

1. **统一架构** - 提供与全局函数系统一致的架构设计
2. **性能优化** - 减少反射调用，提升方法执行效率
3. **参数验证** - 标准化的参数数量和类型检查
4. **命名参数支持** - 完整的命名参数重排序功能
5. **三种执行模式** - 支持解释器、编译器、VM 模式
6. **向后兼容** - 保留旧系统作为后备，确保现有代码正常运行

### 预期收益

- **性能提升**
  - 解释器模式：20-30%
  - 编译器模式：30-50%
  - 方法查找：50-70%

- **代码质量**
  - 统一的 API 设计
  - 更好的错误提示
  - 更易维护的代码结构

---

## 🏗️ 实现方法

### 核心架构

```
IInstanceMethod (接口)
    ↓
BaseInstanceMethod (抽象基类)
    ↓
具体方法实现 (ListAddMethod, StringLengthMethod, etc.)
```

### 注册管理

```
InstanceMethodRegistry (单例)
    ├─ Dictionary<Type, Dictionary<string, IInstanceMethod>>
    ├─ Register(IInstanceMethod)
    ├─ TryGetMethod(Type, string)
    └─ 线程安全机制 (Lock)
```

### 初始化流程

```
InstanceMethodInitializer
    ├─ Initialize() - 注册所有内置方法
    ├─ EnsureInitialized() - 延迟初始化
    └─ 双重检查锁定模式
```

### 集成方式

- 在 `Instance.cs` 中优先调用新系统
- 保留旧系统作为后备（反射调用）
- 支持命名参数重排序

---

## ✅ 已完成内容

### 阶段 1：核心基础设施 ✅

- [x] **创建接口和基类** (2026-02-03)
  - [x] `IInstanceMethod` 接口
  - [x] `BaseInstanceMethod` 抽象基类
  - [x] 参数验证和类型检查功能

- [x] **创建注册器** (2026-02-03)
  - [x] `InstanceMethodRegistry` 单例类
  - [x] 线程安全的注册和查找机制
  - [x] 按类型组织方法
  - [x] 支持继承查找

- [x] **创建初始化器** (2026-02-03)
  - [x] `InstanceMethodInitializer` 类
  - [x] 延迟初始化机制
  - [x] 双重检查锁定模式

### 阶段 2：List 方法迁移 ✅

- [x] **List 基础方法** (2026-02-03) - 5 个方法
  - [x] `ListAddMethod` - 添加元素
  - [x] `ListRemoveMethod` - 移除元素
  - [x] `ListCountMethod` - 获取元素数量
  - [x] `ListClearMethod` - 清空列表
  - [x] `ListContainsMethod` - 检查是否包含元素

- [x] **List 高级方法** (2026-02-04) - 13 个方法
  - [x] `ListRemoveAtMethod` - 根据索引移除
  - [x] `ListAddListMethod` - 添加另一个列表
  - [x] `ListFilterMethod` - 过滤元素
  - [x] `ListMapMethod` - 映射转换
  - [x] `ListReduceMethod` - 归约
  - [x] `ListReverseMethod` - 反转
  - [x] `ListIndexOfMethod` - 查找索引
  - [x] `ListConcatMethod` - 连接列表
  - [x] `ListFindMethod` - 查找元素
  - [x] `ListSkipMethod` - 跳过元素
  - [x] `ListTakeMethod` - 获取前n个元素
  - [x] `ListAnyMethod` - 检查是否有满足条件的元素
  - [x] `ListAllMethod` - 检查是否所有元素都满足条件

### 阶段 3：集成到现有系统 ✅

- [x] **修改 Instance.cs** (2026-02-03)
  - [x] 创建 `Instance.InstanceMethods.cs` 部分类文件
  - [x] 实现 `TryExecuteInstanceMethod()` 方法
  - [x] 实现 `TryGenerateInstanceMethodIl()` 方法
  - [x] 实现 `ReorderInstanceMethodArguments()` 方法
  - [x] 修改 `FromClassToResult()` 优先调用新系统

### 测试验证 ✅

- [x] **基础方法测试** (2026-02-03)
  - [x] 创建 `test_instance_methods.old8`
  - [x] 测试 Add, Remove, Count, Clear, Contains
  - [x] 所有测试通过

- [x] **高级方法测试** (2026-02-04)
  - [x] 创建 `test_list_advanced.old8`
  - [x] 测试 Filter, Map, Reduce, Reverse, IndexOf, Concat, Find, Skip, Take, Any, All
  - [x] 所有测试通过

---

## 🚧 进行中的工作

### 当前任务

- [ ] **继续迁移 List 方法** (优先级：高)
  - 目标：迁移剩余的约 35 个 List 方法
  - 预计完成时间：1-2 周

---

## 📝 待完成内容

### 阶段 4：完成 List 方法迁移 (优先级：高)

#### 4.1 排序方法 (约 7 个)

- [ ] `ListSortMethod` - 默认排序
- [ ] `ListSortWithComparerMethod` - 自定义比较器排序
- [ ] `ListQuickSortMethod` - 快速排序
- [ ] `ListMergeSortMethod` - 归并排序
- [ ] `ListBubbleSortMethod` - 冒泡排序
- [ ] `ListSelectionSortMethod` - 选择排序
- [ ] `ListInsertionSortMethod` - 插入排序
- [ ] `ListHeapSortMethod` - 堆排序
- [ ] `ListIsSortedMethod` - 检查是否已排序

**预计工作量：** 2-3 天

#### 4.2 聚合方法 (约 6 个)

- [ ] `ListAggregateMethod` - 聚合操作（无初始值）
- [ ] `ListAggregateWithSeedMethod` - 聚合操作（有初始值）
- [ ] `ListSumMethod` - 求和
- [ ] `ListAverageMethod` - 求平均值
- [ ] `ListMinMethod` - 求最小值
- [ ] `ListMaxMethod` - 求最大值

**预计工作量：** 1-2 天

#### 4.3 查询方法 (约 8 个)

- [ ] `ListFirstMethod` - 获取第一个元素
- [ ] `ListFirstWithPredicateMethod` - 获取第一个满足条件的元素
- [ ] `ListFirstOrDefaultMethod` - 获取第一个元素或默认值
- [ ] `ListLastMethod` - 获取最后一个元素
- [ ] `ListLastWithPredicateMethod` - 获取最后一个满足条件的元素
- [ ] `ListLastOrDefaultMethod` - 获取最后一个元素或默认值
- [ ] `ListSingleMethod` - 获取唯一元素
- [ ] `ListElementAtMethod` - 获取指定索引的元素

**预计工作量：** 2 天

#### 4.4 集合操作方法 (约 6 个)

- [ ] `ListUnionMethod` - 并集
- [ ] `ListIntersectMethod` - 交集
- [ ] `ListExceptMethod` - 差集
- [ ] `ListDistinctMethod` - 去重
- [ ] `ListZipMethod` - 压缩两个列表
- [ ] `ListGroupByMethod` - 分组

**预计工作量：** 2-3 天

#### 4.5 其他方法 (约 8 个)

- [ ] `ListSelectManyMethod` - 扁平化映射
- [ ] `ListFlatMapMethod` - 扁平化映射（别名）
- [ ] `ListForEachMethod` - 遍历执行
- [ ] `ListToArrayMethod` - 转换为数组
- [ ] `ListToDictMethod` - 转换为字典
- [ ] `ListToStrMethod` - 转换为字符串
- [ ] `ListJoinMethod` - 连接为字符串
- [ ] `ListSliceMethod` - 切片

**预计工作量：** 2-3 天

**阶段 4 总预计工作量：** 2 周

---

### 阶段 5：String 方法迁移 (优先级：高)

#### 5.1 基础方法 (约 8 个)

- [ ] `StringLengthMethod` - 获取长度
- [ ] `StringSubstringMethod` - 获取子字符串
- [ ] `StringReplaceMethod` - 替换
- [ ] `StringSplitMethod` - 分割
- [ ] `StringToUpperMethod` - 转大写
- [ ] `StringToLowerMethod` - 转小写
- [ ] `StringTrimMethod` - 去除空白
- [ ] `StringContainsMethod` - 检查是否包含

**预计工作量：** 2 天

#### 5.2 高级方法 (约 7 个)

- [ ] `StringIndexOfMethod` - 查找索引
- [ ] `StringStartsWithMethod` - 检查是否以指定字符串开头
- [ ] `StringEndsWithMethod` - 检查是否以指定字符串结尾
- [ ] `StringPadLeftMethod` - 左填充
- [ ] `StringPadRightMethod` - 右填充
- [ ] `StringReverseMethod` - 反转
- [ ] `StringToCharArrayMethod` - 转换为字符数组

**预计工作量：** 2 天

**阶段 5 总预计工作量：** 4 天

---

### 阶段 6：Dictionary 方法迁移 (优先级：中)

#### 6.1 基础方法 (约 8 个)

- [ ] `DictGetMethod` - 获取值
- [ ] `DictSetMethod` - 设置值
- [ ] `DictKeysMethod` - 获取所有键
- [ ] `DictValuesMethod` - 获取所有值
- [ ] `DictContainsKeyMethod` - 检查是否包含键
- [ ] `DictRemoveMethod` - 移除键值对
- [ ] `DictClearMethod` - 清空字典
- [ ] `DictCountMethod` - 获取元素数量

**预计工作量：** 2 天

**阶段 6 总预计工作量：** 2 天

---

### 阶段 7：Array 方法迁移 (优先级：中)

#### 7.1 基础方法 (约 5 个)

- [ ] `ArrayLengthMethod` - 获取长度
- [ ] `ArrayGetMethod` - 获取元素
- [ ] `ArraySetMethod` - 设置元素
- [ ] `ArrayToListMethod` - 转换为列表
- [ ] `ArraySliceMethod` - 切片

**预计工作量：** 1 天

**阶段 7 总预计工作量：** 1 天

---

### 阶段 8：其他类型方法迁移 (优先级：中)

#### 8.1 Task 方法 (约 4 个)

- [ ] `TaskThenMethod` - 链式调用
- [ ] `TaskCatchMethod` - 错误处理
- [ ] `TaskFinallyMethod` - 最终执行
- [ ] `TaskAwaitMethod` - 等待完成

**预计工作量：** 1 天

#### 8.2 Thread 方法 (约 3 个)

- [ ] `ThreadJoinMethod` - 等待线程结束
- [ ] `ThreadIsAliveMethod` - 检查是否存活
- [ ] `ThreadAbortMethod` - 中止线程

**预计工作量：** 0.5 天

#### 8.3 Tuple 方法 (约 2 个)

- [ ] `TupleGetMethod` - 获取元素
- [ ] `TupleToListMethod` - 转换为列表

**预计工作量：** 0.5 天

#### 8.4 Char 方法 (约 4 个)

- [ ] `CharToUpperMethod` - 转大写
- [ ] `CharToLowerMethod` - 转小写
- [ ] `CharIsDigitMethod` - 检查是否是数字
- [ ] `CharIsLetterMethod` - 检查是否是字母

**预计工作量：** 1 天

**阶段 8 总预计工作量：** 3 天

---

### 阶段 9：性能优化 (优先级：中)

#### 9.1 方法查找缓存

- [ ] 实现 `InstanceMethodCache` 类
- [ ] 缓存 (Type, MethodName) → IInstanceMethod 映射
- [ ] 使用 ThreadStatic 或 ConcurrentDictionary
- [ ] 性能基准测试

**预计工作量：** 2 天

#### 9.2 IL 生成优化

- [ ] 为常用方法生成优化的 IL 代码
- [ ] 减少装箱/拆箱操作
- [ ] 内联简单方法
- [ ] 性能基准测试

**预计工作量：** 3 天

#### 9.3 参数处理优化

- [ ] 优化参数求值流程
- [ ] 减少不必要的类型转换
- [ ] 性能基准测试

**预计工作量：** 2 天

**阶段 9 总预计工作量：** 1 周

---

### 阶段 10：测试和文档 (优先级：高)

#### 10.1 单元测试

- [ ] 测试每个迁移的方法（先删除旧的架构）
  - [ ] 解释器模式测试
  - [ ] 编译器模式测试
  - [ ] VM 模式测试
- [ ] 测试命名参数功能
- [ ] 测试参数验证
- [ ] 测试错误处理
- [ ] 测试线程安全性

**预计工作量：** 1 周

#### 10.2 集成测试

- [ ] 测试与现有代码的兼容性
- [ ] 测试性能改进
- [ ] 测试方法链调用
- [ ] 测试错误场景
- [ ] 验证向后兼容性

**预计工作量：** 3 天

#### 10.3 端到端测试

- [ ] 运行现有测试套件
- [ ] 运行示例程序（解释器、编译器和VM模式）
- [ ] 性能基准测试
- [ ] 压力测试

**预计工作量：** 2 天

#### 10.4 文档更新

- [ ] 更新 `ARCHITECTURE.md` - 添加实例方法系统架构
- [ ] 更新 `API_REFERENCE.md` - 添加新 API 文档
- [ ] 创建迁移指南
- [ ] 更新代码注释
- [ ] 添加使用示例
- [ ] 创建性能对比报告

**预计工作量：** 3 天

**阶段 10 总预计工作量：** 2.5 周

---

## 📊 进度统计

### 总体进度

```
核心基础设施：    ████████████████████ 100% (3/3)
List 方法迁移：   ████████░░░░░░░░░░░░  36% (18/50)
String 方法迁移： ░░░░░░░░░░░░░░░░░░░░   0% (0/15)
Dictionary 方法： ░░░░░░░░░░░░░░░░░░░░   0% (0/8)
Array 方法迁移：  ░░░░░░░░░░░░░░░░░░░░   0% (0/5)
其他类型方法：    ░░░░░░░░░░░░░░░░░░░░   0% (0/13)
性能优化：        ░░░░░░░░░░░░░░░░░░░░   0% (0/3)
测试和文档：      ░░░░░░░░░░░░░░░░░░░░   0% (0/4)

总体进度：        ████░░░░░░░░░░░░░░░░  21% (21/101)
```

### 方法迁移统计

| 类型 | 已完成 | 待完成 | 总计 | 完成率 |
|------|--------|--------|------|--------|
| List | 18 | 32 | 50 | 36% |
| String | 0 | 15 | 15 | 0% |
| Dictionary | 0 | 8 | 8 | 0% |
| Array | 0 | 5 | 5 | 0% |
| Task | 0 | 4 | 4 | 0% |
| Thread | 0 | 3 | 3 | 0% |
| Tuple | 0 | 2 | 2 | 0% |
| Char | 0 | 4 | 4 | 0% |
| **总计** | **18** | **73** | **91** | **20%** |

---

## ⏱️ 时间估算

### 已用时间

- 核心基础设施：1 天
- List 基础方法：0.5 天
- List 高级方法：0.5 天
- 集成和测试：0.5 天

**总计：** 2.5 天

### 剩余时间估算

| 阶段 | 预计工作量 | 优先级 |
|------|-----------|--------|
| 完成 List 方法迁移 | 2 周 | 高 |
| String 方法迁移 | 4 天 | 高 |
| Dictionary 方法迁移 | 2 天 | 中 |
| Array 方法迁移 | 1 天 | 中 |
| 其他类型方法迁移 | 3 天 | 中 |
| 性能优化 | 1 周 | 中 |
| 测试和文档 | 2.5 周 | 高 |

**总计：** 约 7-8 周

---

## 🎯 里程碑

### 里程碑 1：核心基础设施完成 ✅
- **完成日期：** 2026-02-03
- **内容：** 接口、基类、注册器、初始化器

### 里程碑 2：List 基础方法完成 ✅
- **完成日期：** 2026-02-03
- **内容：** 5 个基础方法 + 集成测试

### 里程碑 3：List 高级方法完成 ✅
- **完成日期：** 2026-02-04
- **内容：** 13 个高级方法 + 集成测试

### 里程碑 4：List 方法全部完成 🔄
- **预计日期：** 2026-02-18
- **内容：** 所有 50 个 List 方法

### 里程碑 5：String 方法完成 📅
- **预计日期：** 2026-02-22
- **内容：** 所有 15 个 String 方法

### 里程碑 6：所有方法迁移完成 📅
- **预计日期：** 2026-02-28
- **内容：** 所有 91 个方法

### 里程碑 7：性能优化完成 📅
- **预计日期：** 2026-03-07
- **内容：** 缓存、IL 优化、参数优化

### 里程碑 8：项目完成 📅
- **预计日期：** 2026-03-28
- **内容：** 测试、文档、发布

---

## 🚀 优先级排序

### P0 - 必须完成（高优先级）

1. ✅ 核心基础设施
2. 🔄 List 方法迁移（剩余 32 个）
3. 📅 String 方法迁移（15 个）
4. 📅 单元测试
5. 📅 文档更新

### P1 - 应该完成（中优先级）

6. 📅 Dictionary 方法迁移（8 个）
7. 📅 Array 方法迁移（5 个）
8. 📅 性能优化
9. 📅 集成测试

### P2 - 可以完成（低优先级）

10. 📅 Task 方法迁移（4 个）
11. 📅 Thread 方法迁移（3 个）
12. 📅 Tuple 方法迁移（2 个）
13. 📅 Char 方法迁移（4 个）
14. 📅 端到端测试

---

## 📝 注意事项

### 技术债务

1. **值比较问题** - 当前使用手动类型检查进行值比较，未来可以考虑在 `LangValueType` 基类中实现统一的 `Equal` 方法
2. **高阶函数 VM 支持** - 当前高阶函数（Filter、Map、Reduce 等）在 VM 模式下不支持，需要后续实现
3. **错误处理** - 部分方法的错误处理可以更加细化

### 风险和缓解措施

| 风险 | 影响 | 概率 | 缓解措施 |
|------|------|------|----------|
| 破坏现有代码 | 高 | 低 | 保留旧系统作为后备，充分测试 |
| 性能回退 | 中 | 低 | 性能基准测试，优化热点路径 |
| 维护成本增加 | 中 | 中 | 清晰的文档和注释，统一的代码风格 |
| 迁移工作量大 | 低 | 高 | 优先迁移常用方法，使用代码生成工具 |

### 依赖关系

- List 高级方法依赖于基础方法 ✅
- String 方法依赖于 List 方法完成 🔄
- Dictionary/Array 方法依赖于 String 方法完成 📅
- 性能优化依赖于所有方法迁移完成 📅
- 文档更新依赖于测试完成 📅

---

## 📞 联系信息

**项目负责人：** Claude Sonnet 4.5

**项目仓库：** Old8Lang

**文档位置：** `C:\Projects\RiderProjects\Old8Lang\TODO_INSTANCE_METHODS.md`

**最后更新：** 2026-02-04

---

## 📚 相关文档

- [ARCHITECTURE.md](./Docs/ARCHITECTURE.md) - 架构文档
- [CLAUDE.md](./CLAUDE.md) - 项目指南
- [统一实例方法系统设计方案](./Docs/INSTANCE_METHOD_DESIGN.md) - 详细设计文档（如果存在）

---

## 🔄 更新日志

### 2026-02-04
- ✅ 完成 List 高级方法迁移（13 个方法）
- ✅ 所有测试通过
- 📝 创建 TODO 文档

### 2026-02-03
- ✅ 完成核心基础设施
- ✅ 完成 List 基础方法迁移（5 个方法）
- ✅ 集成到 Instance.cs
- ✅ 基础测试通过

---

**图例：**
- ✅ 已完成
- 🔄 进行中
- 📅 计划中
- ⏸️ 暂停
- ❌ 已取消
