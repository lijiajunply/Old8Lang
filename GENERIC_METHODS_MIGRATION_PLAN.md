# ILangList 通用方法迁移计划

## 方法分类

### 第一优先级：查询和访问方法（适用于所有 ILangList）

1. ✅ **Count/Length** - 已实现
2. ✅ **Contains** - 已实现
3. ✅ **Any** - 已实现
4. ✅ **All** - 已实现
5. **First** - 获取第一个元素
6. **FirstOrDefault** - 获取第一个元素或默认值
7. **FirstWithPredicate** - 获取第一个满足条件的元素
8. **Last** - 获取最后一个元素
9. **LastOrDefault** - 获取最后一个元素或默认值
10. **LastWithPredicate** - 获取最后一个满足条件的元素
11. **ElementAt** - 获取指定索引的元素
12. **IndexOf** - 查找元素索引
13. **Find** - 查找满足条件的元素

### 第二优先级：转换和投影方法（适用于所有 ILangList）

14. ✅ **Reverse** - 已实现
15. ✅ **Filter/Where** - 已实现
16. ✅ **Map/Select** - 已实现
17. **Skip** - 跳过前 N 个元素
18. **Take** - 取前 N 个元素
19. **Concat** - 连接两个列表
20. **Distinct** - 去重
21. **ToStr** - 转换为字符串

### 第三优先级：聚合方法（适用于数值类型的 ILangList）

22. **Sum** - 求和
23. **Average** - 平均值
24. **Min** - 最小值
25. **Max** - 最大值
26. **Reduce** - 归约
27. **Aggregate** - 聚合
28. **AggregateWithSeed** - 带初始值的聚合

### 第四优先级：集合操作方法（适用于所有 ILangList）

29. **Union** - 并集
30. **Intersect** - 交集
31. **Except** - 差集
32. **Zip** - 拉链操作
33. **GroupBy** - 分组

### 第五优先级：排序方法（适用于可排序的 ILangList）

34. **Sort** - 排序
35. **SortWithComparer** - 带比较器的排序
36. **IsSorted** - 检查是否已排序
37. **QuickSort** - 快速排序
38. **MergeSort** - 归并排序
39. **BubbleSort** - 冒泡排序
40. **SelectionSort** - 选择排序
41. **InsertionSort** - 插入排序
42. **HeapSort** - 堆排序

### 第六优先级：迭代方法（适用于所有 ILangList）

43. **ForEach** - 遍历执行
44. **Join** - 连接为字符串

### 不适合通用化的方法（List 特定）

- **Add** - 需要修改列表（Tuple/Array 不可变）
- **Remove** - 需要修改列表
- **RemoveAt** - 需要修改列表
- **Clear** - 需要修改列表
- **Insert** - 需要修改列表
- **AddList** - 需要修改列表

## String 特殊考虑

String 虽然不是 ILangList，但可以考虑为其实现部分方法：
- **Filter** - 过滤字符
- **Map** - 映射字符
- **Reverse** - 反转字符串（已有）
- **Contains** - 检查是否包含（已有）
- **Any** - 检查是否有字符满足条件
- **All** - 检查是否所有字符满足条件
- **First/Last** - 获取第一个/最后一个字符
- **Skip/Take** - 跳过/取前 N 个字符

## 实现策略

### 阶段 1：查询和访问方法（13 个）
创建通用实现，为 Tuple、Array 创建包装类

### 阶段 2：转换和投影方法（7 个）
创建通用实现，为 Tuple、Array 创建包装类

### 阶段 3：聚合方法（7 个）
创建通用实现，为 Tuple、Array 创建包装类

### 阶段 4：集合操作方法（5 个）
创建通用实现，为 Tuple、Array 创建包装类

### 阶段 5：排序方法（9 个）
创建通用实现，为 Tuple、Array 创建包装类

### 阶段 6：迭代方法（2 个）
创建通用实现，为 Tuple、Array 创建包装类

### 阶段 7：String 特殊方法
为 String 创建特定的方法实现

## 预期成果

- 为 Tuple 添加约 40+ 个方法
- 为 Array 添加约 40+ 个方法
- 为 String 添加约 10+ 个方法
- 大幅减少代码重复
- 统一所有 ILangList 类型的行为
