# 编译器模式完成 TODO

**当前完成度**: 90.8% (99/109 功能完全支持)

---

## 📊 状态概览

| 状态 | 数量 | 说明 |
|------|------|------|
| ✅ 完全支持 | 99 | 已完整实现并测试通过 |
| ⚠️ 部分支持 | 4 | 功能基本可用但有限制 |
| ❌ 不支持 | 6 | 功能未实现 |

---

## 🔴 高优先级任务

### 1. 完善异步编程支持

**当前状态**: ⚠️ 部分支持

**涉及功能**:
- async/await 基础 (第 1989 行)
- 异步生成器 (第 2006 行)
- Task API (第 2031 行)

**问题描述**:
- 编译器对异步功能的支持不完整
- 某些异步特性在编译模式下可能无法正常工作

**实施步骤**:
1. 分析当前编译器的异步支持情况
2. 识别缺失或不完整的异步功能
3. 完善 `AsyncStateMachineGenerator.cs` 的实现
4. 添加完整的异步测试用例
5. 更新文档标记为 `[✅ | ✅ | ❌]`

**相关文件**:
- `Old8Lang/Compiler/AsyncStateMachineGenerator.cs`
- `Old8Lang/AST/Statement/AsyncFuncInit.cs`
- `Old8Lang/Visitor/CompilerVisitor.cs`

**测试文件**:
- `CompilerTests/async_*.old8`
- `Old8Lang.Tests/Compiler/AsyncTests.cs`

---

### 2. 验证 SelectStatement 编译器实现

**当前状态**: ✅ 已实现（需验证文档）

**问题描述**:
- 文档标记为 `[✅ | ❌ | ❌]`，但代码中有完整实现
- 测试已验证编译器模式完全支持

**实施步骤**:
1. ✅ 已完成：修正文档标记为 `[✅ | ✅ | ✅]`
2. ✅ 已完成：删除错误的限制说明
3. ✅ 已完成：验证测试通过

**结论**: 已完成，无需进一步操作

---

## 🟡 中优先级任务

### 3. 实现泛型函数支持

**当前状态**: ❌ 不支持

**涉及功能**:
- 泛型函数 (第 1249 行)

**问题描述**:
- 编译器不支持泛型函数语法
- 解释器模式已完全支持

**实施步骤**:
1. 分析解释器的泛型函数实现
2. 设计编译器的泛型函数 IL 生成策略
3. 实现 `CompilerVisitor.VisitFuncInit` 的泛型支持
4. 处理泛型类型参数的 IL 代码生成
5. 添加泛型函数测试用例
6. 更新文档标记为 `[✅ | ✅ | ❌]`

**相关文件**:
- `Old8Lang/AST/Statement/FuncInit.cs`
- `Old8Lang/Compiler/Compiler.cs`
- `Old8Lang/Visitor/CompilerVisitor.cs`

**技术难点**:
- 泛型类型参数的 IL 表示
- 泛型方法调用的类型推断
- 泛型约束的编译时检查

---

### 4. 实现泛型类支持

**当前状态**: ❌ 不支持

**涉及功能**:
- 泛型类 (第 1544 行)

**问题描述**:
- 编译器不支持泛型类语法
- 解释器模式已完全支持

**实施步骤**:
1. 分析解释器的泛型类实现
2. 设计编译器的泛型类 IL 生成策略
3. 实现 `CompilerVisitor.VisitClassInit` 的泛型支持
4. 处理泛型类实例化的 IL 代码生成
5. 添加泛型类测试用例
6. 更新文档标记为 `[✅ | ✅ | ❌]`

**相关文件**:
- `Old8Lang/AST/Statement/ClassInit.cs`
- `Old8Lang/Compiler/Compiler.cs`
- `Old8Lang/Visitor/CompilerVisitor.cs`

**技术难点**:
- 泛型类的 IL 类型定义
- 泛型字段和方法的处理
- 泛型继承和接口实现

---

## 🟢 低优先级任务

### 5. 实现 P/Invoke 支持

**当前状态**: ❌ 不支持

**涉及功能**:
- Extern P/Invoke (第 2244 行)

**问题描述**:
- 编译器不支持 `native extern "dll"` 语法
- 解释器模式已完全支持

**实施步骤**:
1. 分析解释器的 P/Invoke 实现
2. 设计编译器的 P/Invoke IL 生成策略
3. 实现 `CompilerVisitor.VisitNativeStatement` 的 P/Invoke 支持
4. 处理不同调用约定（cdecl、stdcall、winapi）
5. 添加 P/Invoke 测试用例
6. 更新文档标记为 `[✅ | ✅ | ❌]`

**相关文件**:
- `Old8Lang/AST/Statement/NativeStatement.cs`
- `Old8Lang/Compiler/Compiler.cs`
- `Old8Lang/Visitor/CompilerVisitor.cs`

**技术难点**:
- DllImport 特性的 IL 生成
- 类型映射和 marshaling
- 调用约定的处理

---

### 6. 实现 Python 互操作支持

**当前状态**: ❌ 不支持

**涉及功能**:
- Extern Python (第 2329 行)

**问题描述**:
- 编译器不支持 `native extern "py:"` 语法
- 解释器模式已完全支持

**实施步骤**:
1. 分析解释器的 Python 互操作实现
2. 评估编译器模式下的 Python.NET 集成可行性
3. 设计编译器的 Python 互操作 IL 生成策略
4. 实现 `CompilerVisitor.VisitNativeStatement` 的 Python 支持
5. 添加 Python 互操作测试用例
6. 更新文档标记为 `[✅ | ✅ | ❌]`

**相关文件**:
- `Old8Lang/AST/Statement/NativeStatement.cs`
- `Old8Lang/Compiler/Compiler.cs`
- `Old8Lang/Visitor/CompilerVisitor.cs`

**技术难点**:
- Python.NET 的编译时集成
- 动态类型转换的 IL 生成
- Python 模块加载的处理

---

## 📝 实施建议

### 推荐实施顺序

1. **第一阶段**（高优先级）
   - 完善异步编程支持
   - 这是最常用的功能，影响面广

2. **第二阶段**（中优先级）
   - 实现泛型函数支持
   - 实现泛型类支持
   - 这两个功能相关性强，可以一起实现

3. **第三阶段**（低优先级）
   - 实现 P/Invoke 支持
   - 实现 Python 互操作支持
   - 这些是高级互操作功能，使用频率较低

### 技术债务

- 编译器的异步支持需要重构
- 泛型支持需要完整的类型系统改造
- 互操作功能需要更好的 IL 生成框架

---

## 🎯 完成标准

### 阶段一完成标准
- ✅ 所有异步测试通过（解释器和编译器模式）
- ✅ 异步功能文档标记更新为 `[✅ | ✅ | ❌]`
- ✅ 编译器完成度提升至 94%+

### 阶段二完成标准
- ✅ 泛型函数和泛型类测试通过
- ✅ 泛型功能文档标记更新为 `[✅ | ✅ | ❌]`
- ✅ 编译器完成度提升至 96%+

### 阶段三完成标准
- ✅ P/Invoke 和 Python 互操作测试通过
- ✅ 互操作功能文档标记更新为 `[✅ | ✅ | ❌]`
- ✅ 编译器完成度达到 100%

---

## 📂 关键文件索引

### 编译器核心
- `Old8Lang/Compiler/Compiler.cs` - 主编译器
- `Old8Lang/Compiler/LocalManager.cs` - 局部变量管理
- `Old8Lang/Compiler/AsyncStateMachineGenerator.cs` - 异步状态机生成器
- `Old8Lang/Visitor/CompilerVisitor.cs` - 编译器 Visitor

### AST 节点
- `Old8Lang/AST/Statement/AsyncFuncInit.cs` - 异步函数
- `Old8Lang/AST/Statement/FuncInit.cs` - 函数声明
- `Old8Lang/AST/Statement/ClassInit.cs` - 类声明
- `Old8Lang/AST/Statement/NativeStatement.cs` - 原生导入

### 测试
- `Old8Lang.Tests/Compiler/` - 编译器单元测试
- `CompilerTests/` - 编译器集成测试

---

**最后更新**: 2026-01-16
**维护者**: Claude Code
