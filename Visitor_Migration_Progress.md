# Visitor 模式迁移进度报告

**日期：** 2025-12-25
**状态：** 进行中 - 阶段2持续进行中

---

## 已完成工作

### 1. 基础设施（阶段1）✅

- ✅ IVisitor 接口已生成（在 Generated/ 目录）
- ✅ 所有 AST 节点的 Accept 方法已生成
- ✅ InterpreterVisitor、CompilerVisitor、TypeInferenceVisitor 骨架已创建

### 2. 核心 Visitor 实现（阶段2 - 部分完成）⏳

#### InterpreterVisitor 实现

**已完成的节点：**

**Statement 节点：**
- ✅ BreakStatement - [InterpreterVisitor.Statements.cs:15](Old8Lang/AST/Visitor/InterpreterVisitor.Statements.cs#L15)
- ✅ ContinueStatement - [InterpreterVisitor.Statements.cs:25](Old8Lang/AST/Visitor/InterpreterVisitor.Statements.cs#L25)
- ✅ IfStatement - [InterpreterVisitor.Statements.cs:35](Old8Lang/AST/Visitor/InterpreterVisitor.Statements.cs#L35)
- ✅ BlockStatement - [InterpreterVisitor.Statements.cs:88](Old8Lang/AST/Visitor/InterpreterVisitor.Statements.cs#L88)
- ✅ WhileStatement - [InterpreterVisitor.Statements.cs:133](Old8Lang/AST/Visitor/InterpreterVisitor.Statements.cs#L133) (部分回退到原方法)
- ✅ ForStatement - [InterpreterVisitor.Statements.cs:184](Old8Lang/AST/Visitor/InterpreterVisitor.Statements.cs#L184) (回退到原方法)
- ✅ ForInStatement - [InterpreterVisitor.Statements.cs:207](Old8Lang/AST/Visitor/InterpreterVisitor.Statements.cs#L207) (回退到原方法)
- ✅ SetStatement - [InterpreterVisitor.Statements.cs:221](Old8Lang/AST/Visitor/InterpreterVisitor.Statements.cs#L221) (回退到原方法)
- ✅ ReturnStatement - [InterpreterVisitor.Statements.cs:232](Old8Lang/AST/Visitor/InterpreterVisitor.Statements.cs#L232) (回退到原方法)
- ✅ ThrowStatement - [InterpreterVisitor.Statements.cs:243](Old8Lang/AST/Visitor/InterpreterVisitor.Statements.cs#L243) (回退到原方法)

**Expression 节点：**
- ✅ LangId - [InterpreterVisitor.Expressions.cs:14](Old8Lang/AST/Visitor/InterpreterVisitor.Expressions.cs#L14)

**Value 节点：**
- ✅ IntLangValue - [InterpreterVisitor.Values.cs:14](Old8Lang/AST/Visitor/InterpreterVisitor.Values.cs#L14)
- ✅ DoubleLangValue - [InterpreterVisitor.Values.cs:22](Old8Lang/AST/Visitor/InterpreterVisitor.Values.cs#L22)
- ✅ StringLangValue - [InterpreterVisitor.Values.cs:29](Old8Lang/AST/Visitor/InterpreterVisitor.Values.cs#L29)
- ✅ BoolLangValue - [InterpreterVisitor.Values.cs:36](Old8Lang/AST/Visitor/InterpreterVisitor.Values.cs#L36)
- ✅ CharLangValue - [InterpreterVisitor.Values.cs:43](Old8Lang/AST/Visitor/InterpreterVisitor.Values.cs#L43)
- ✅ NullLangValue - [InterpreterVisitor.Values.cs:50](Old8Lang/AST/Visitor/InterpreterVisitor.Values.cs#L50)
- ✅ VoidLangValue - [InterpreterVisitor.Values.cs:57](Old8Lang/AST/Visitor/InterpreterVisitor.Values.cs#L57)

**迁移的方法数：** 19/68 (27.9%)

#### CompilerVisitor 实现

**已完成的节点：**

**Statement 节点：**
- ✅ BreakStatement - [CompilerVisitor.Statements.cs:16](Old8Lang/AST/Visitor/CompilerVisitor.Statements.cs#L16)
- ✅ ContinueStatement - [CompilerVisitor.Statements.cs:34](Old8Lang/AST/Visitor/CompilerVisitor.Statements.cs#L34)
- ✅ IfStatement - [CompilerVisitor.Statements.cs:52](Old8Lang/AST/Visitor/CompilerVisitor.Statements.cs#L52)
- ✅ BlockStatement - [CompilerVisitor.Statements.cs:102](Old8Lang/AST/Visitor/CompilerVisitor.Statements.cs#L102)
- ✅ WhileStatement - [CompilerVisitor.Statements.cs:124](Old8Lang/AST/Visitor/CompilerVisitor.Statements.cs#L124) (回退到原方法)
- ✅ ForStatement - [CompilerVisitor.Statements.cs:136](Old8Lang/AST/Visitor/CompilerVisitor.Statements.cs#L136) (回退到原方法)
- ✅ ForInStatement - [CompilerVisitor.Statements.cs:148](Old8Lang/AST/Visitor/CompilerVisitor.Statements.cs#L148) (回退到原方法)
- ✅ SetStatement - [CompilerVisitor.Statements.cs:160](Old8Lang/AST/Visitor/CompilerVisitor.Statements.cs#L160) (回退到原方法)
- ✅ ReturnStatement - [CompilerVisitor.Statements.cs:171](Old8Lang/AST/Visitor/CompilerVisitor.Statements.cs#L171) (回退到原方法)
- ✅ ThrowStatement - [CompilerVisitor.Statements.cs:182](Old8Lang/AST/Visitor/CompilerVisitor.Statements.cs#L182) (回退到原方法)

**Expression 节点：**
- ✅ LangId - [CompilerVisitor.Expressions.cs:13](Old8Lang/AST/Visitor/CompilerVisitor.Expressions.cs#L13)

**Value 节点：**
- ✅ IntLangValue - [CompilerVisitor.Values.cs:13](Old8Lang/AST/Visitor/CompilerVisitor.Values.cs#L13)
- ✅ DoubleLangValue - [CompilerVisitor.Values.cs:22](Old8Lang/AST/Visitor/CompilerVisitor.Values.cs#L22)
- ✅ StringLangValue - [CompilerVisitor.Values.cs:30](Old8Lang/AST/Visitor/CompilerVisitor.Values.cs#L30)
- ✅ BoolLangValue - [CompilerVisitor.Values.cs:38](Old8Lang/AST/Visitor/CompilerVisitor.Values.cs#L38)
- ✅ CharLangValue - [CompilerVisitor.Values.cs:46](Old8Lang/AST/Visitor/CompilerVisitor.Values.cs#L46)
- ✅ NullLangValue - [CompilerVisitor.Values.cs:54](Old8Lang/AST/Visitor/CompilerVisitor.Values.cs#L54)
- ✅ VoidLangValue - [CompilerVisitor.Values.cs:62](Old8Lang/AST/Visitor/CompilerVisitor.Values.cs#L62)

**迁移的方法数：** 19/68 (27.9%)

#### TypeInferenceVisitor 实现

**待完成：** 所有节点的 Visit 方法（stub 已生成，等待实现）

**Stub 位置：** [Generated/TypeInferenceVisitor.Stubs.generated.cs](Old8Lang/AST/Visitor/Generated/TypeInferenceVisitor.Stubs.generated.cs)

---

## 创建的文件

### InterpreterVisitor 部分实现
1. [Old8Lang/AST/Visitor/InterpreterVisitor.cs](Old8Lang/AST/Visitor/InterpreterVisitor.cs) - 基类骨架
2. [Old8Lang/AST/Visitor/InterpreterVisitor.Statements.cs](Old8Lang/AST/Visitor/InterpreterVisitor.Statements.cs) - Statement 节点实现
3. [Old8Lang/AST/Visitor/InterpreterVisitor.Values.cs](Old8Lang/AST/Visitor/InterpreterVisitor.Values.cs) - Value 节点实现
4. [Old8Lang/AST/Visitor/InterpreterVisitor.Expressions.cs](Old8Lang/AST/Visitor/InterpreterVisitor.Expressions.cs) - Expression 节点实现

### CompilerVisitor 部分实现
1. [Old8Lang/AST/Visitor/CompilerVisitor.cs](Old8Lang/AST/Visitor/CompilerVisitor.cs) - 基类骨架
2. [Old8Lang/AST/Visitor/CompilerVisitor.Statements.cs](Old8Lang/AST/Visitor/CompilerVisitor.Statements.cs) - Statement 节点实现
3. [Old8Lang/AST/Visitor/CompilerVisitor.Values.cs](Old8Lang/AST/Visitor/CompilerVisitor.Values.cs) - Value 节点实现
4. [Old8Lang/AST/Visitor/CompilerVisitor.Expressions.cs](Old8Lang/AST/Visitor/CompilerVisitor.Expressions.cs) - Expression 节点实现

---

## 待完成工作

### 高优先级（按计划顺序）

#### 1. 控制流 Statement 节点 🟢
- ✅ IfStatement (已完成，迁移到 Visitor)
- ✅ ForStatement (已完成，目前回退到原方法)
- ✅ WhileStatement (已完成，目前回退到原方法)
- ✅ ForInStatement (已完成，目前回退到原方法)
- ❌ AsyncForInStatement
- ❌ SwitchStatement

**注意：** ForStatement、WhileStatement、ForInStatement 已创建 Visit 方法，但由于使用主构造函数参数无法通过索引访问子节点，暂时回退到调用原方法。

#### 2. 复杂 Expression 节点 🔴
- ❌ Operation (最复杂，82KB 文件)
- ❌ FunctionCallExpression
- ❌ TernaryExpression
- ❌ AwaitExpression
- ❌ AsyncStreamExpression
- ❌ SuperExpression

#### 3. 其他 Statement 节点 🟢
- ✅ SetStatement (已完成，回退到原方法)
- ✅ ReturnStatement (已完成，回退到原方法)
- ✅ ThrowStatement (已完成，回退到原方法)
- ❌ FuncInit
- ❌ AsyncFuncInit
- ❌ ClassInit
- ❌ YieldStatement
- ❌ TryStatement
- ❌ ImportStatement
- ❌ NativeStatement
- ❌ CaseStatement

#### 4. 剩余 Value 节点 🟡
- ❌ ArrayLangValue
- ❌ ListLangValue
- ❌ DictionaryLangValue
- ❌ TupleLangValue
- ❌ RangeLangValue
- ❌ SliceLangValue
- ❌ TaskLangValue
- ❌ GeneratorLangValue
- ❌ AsyncGeneratorLangValue
- ❌ StringTemplateValue
- ❌ ... (还有约 30+ 个 Value 节点)

#### 5. TypeInferenceVisitor 完整实现 🟡
- ❌ 迁移所有节点的 OutputType() 逻辑到对应的 Visit 方法

---

## 编译状态

✅ **项目编译成功**
- 只有生成代码的 nullable 警告（可忽略）
- 无错误

---

## 下一步行动

根据迁移计划的策略（从简单到复杂）：

### 立即执行：
1. **迁移复杂 Expression 节点** (TernaryExpression, AwaitExpression)
   - TernaryExpression 是三元运算符，逻辑相对简单
   - AwaitExpression 用于异步操作
   - 这些节点在 Operation 之前完成

2. **迁移 Operation 节点**
   - 最复杂的节点（82KB）
   - 包含大量运算符逻辑
   - 建议分批迁移（算术、比较、逻辑等）

3. **迁移 FunctionCallExpression**
   - 函数调用是核心功能
   - 需要处理参数传递和返回值

### 后续步骤：
4. 完成所有剩余 Statement 和 Expression 节点
5. 完成所有 Value 节点（可批量处理，逻辑相似）
6. 实现 TypeInferenceVisitor
7. 添加测试用例
8. 性能测试和优化

---

## 技术笔记

### 迁移模式总结

**简单 Value 节点（如 IntLangValue）：**
- InterpreterVisitor: 直接返回节点自身
- CompilerVisitor: 调用 `Emit()` 加载常量到 IL 栈
- TypeInferenceVisitor: 返回对应的 C# 类型

**简单 Statement 节点（如 BreakStatement）：**
- InterpreterVisitor: 设置控制流标志
- CompilerVisitor: 发出跳转指令到对应标签
- TypeInferenceVisitor: 通常返回 `typeof(void)` 或 `typeof(object)`

**Expression 节点（如 LangId）：**
- InterpreterVisitor: 从 VariateManager 获取值
- CompilerVisitor: 加载局部变量或参数
- TypeInferenceVisitor: 根据类型注解或局部变量类型推断

**循环 Statement 节点（如 ForStatement、WhileStatement）：**
- 由于使用主构造函数参数，无法通过索引访问子节点
- 当前策略：创建 Visit 方法，但回退到调用原方法 (node.Run() / node.GenerateIl())
- 未来改进：重构节点以支持更好的 Visitor 访问

### 已知问题
- WhileStatement、ForStatement、ForInStatement 使用主构造函数参数，无法完全迁移到 Visitor 模式，目前回退到原方法
- IfChild 助手类不支持 Visitor 模式，IfStatement 需要直接调用其方法

### 兼容性策略
- 原有的 Run()、GenerateIl()、OutputType() 方法仍然保留
- 暂未添加 Obsolete 标记
- 逐步迁移，确保每个节点的 Visitor 实现正确后再考虑废弃旧方法

---

## 里程碑检查

### 里程碑 1: 基础设施 ✅
- [x] 所有节点支持 Accept
- [x] IVisitor 接口完整
- [x] 编译无错误

### 里程碑 2: 核心 Visitor 实现 ⏳ (27.9%)
- [ ] InterpreterVisitor 完成（19/68）
- [ ] CompilerVisitor 完成（19/68）
- [ ] TypeInferenceVisitor 完成（0/68）
- [ ] 测试通过率 ≥ 95%

---

**最后更新：** 2025-12-25
**当前阶段：** SetStatement、ReturnStatement、ThrowStatement 迁移完成（使用回退策略）
**下次更新：** 完成 TernaryExpression 和 AwaitExpression 迁移后
