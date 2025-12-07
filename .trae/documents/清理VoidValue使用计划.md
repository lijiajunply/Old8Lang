## 清理VoidValue使用计划

### 目标
将VoidValue的使用限制在函数调用结果中，其他情况使用Error体系替代。

### 分析结果

#### 符合要求的VoidValue使用（保留）
- **VariateManager.cs**：Result属性初始值和Reset方法
- **FuncStatic.cs**：
  - GetJsonElement方法处理Json null/undefined
  - AddList方法返回值
- **ValueType.cs**：ObjToValue处理null值
- **Instance.cs**：
  - Exec函数执行后返回
  - ShowValues函数返回
  - PrintLine函数返回
  - Print函数返回
  - Compiler函数返回

#### 需要修改的VoidValue使用

1. **Instance.cs**：
   - 第24行：Exec函数参数类型错误
   - 第37行：Json转换失败
   - 第41行：ToObj转换失败

2. **AnyValue.cs**：
   - 第75行：Converse方法转换目标类型错误

3. **DictionaryValue.cs**：
   - 第43行：Dot方法无效操作
   - 第71行：Converse方法转换目标类型错误

4. **ListValue.cs**：
   - 第42行：Dot方法无效操作

5. **OldItem.cs**：
   - 第23行：Run方法访问失败

6. **TypeValue.cs**：
   - 第16行：Run方法result为null

### 实施步骤

1. **修改Instance.cs**：
   - 将Exec函数参数类型错误改为抛出TypeError
   - 将Json转换失败改为抛出InvalidOperationError
   - 将ToObj转换失败改为抛出InvalidOperationError

2. **修改AnyValue.cs**：
   - 将Converse方法转换目标类型错误改为抛出TypeError

3. **修改DictionaryValue.cs**：
   - 将Dot方法无效操作改为抛出InvalidOperationError
   - 将Converse方法转换目标类型错误改为抛出TypeError

4. **修改ListValue.cs**：
   - 将Dot方法无效操作改为抛出InvalidOperationError

5. **修改OldItem.cs**：
   - 将Run方法访问失败改为抛出IndexError或KeyError

6. **修改TypeValue.cs**：
   - 将Run方法result为null改为抛出InvalidOperationError

### 验证方法
- 运行构建命令检查编译错误
- 运行测试命令确保所有测试通过
- 再次使用Grep检查VoidValue使用情况

### 预期结果
- 只有符合要求的VoidValue使用被保留
- 其他情况都改为抛出适当的错误
- 构建和测试都通过
- 代码库中VoidValue的使用更加规范