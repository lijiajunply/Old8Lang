# Old8Lang 调试器使用指南

## 概述

Old8Lang 调试器是一个功能强大的调试工具，支持断点、变量监视、单步执行和调用栈分析等调试功能。

## 主要功能

### 1. 断点管理
- **行断点**: 在指定文件的指定行设置断点
- **函数断点**: 在函数入口处设置断点
- **条件断点**: 满足特定条件时才触发的断点

### 2. 变量监视
- 实时监视变量值的变化
- 支持复杂表达式监视
- 自动更新变量状态

### 3. 执行控制
- **继续执行**: 继续运行程序直到下一个断点
- **单步进入**: 逐行执行，遇到函数时进入函数内部
- **单步跳过**: 逐行执行，遇到函数时跳过函数调用
- **单步跳出**: 执行完当前函数并返回到调用者

### 4. 调用栈分析
- 显示完整的函数调用链
- 查看每个栈帧的局部变量
- 跟踪程序的执行流程

## 命令参考

### 启动调试

```bash
# 启动调试会话
old8lang debug-start <文件路径>

# 示例
old8lang debug-start TestFiles/DebuggerTests/test_breakpoints.old8
```

### 断点管理

```bash
# 添加行断点
old8lang debug-bp add <文件路径> <行号> [条件]

# 添加函数断点
old8lang debug-bp func <函数名>

# 列出所有断点
old8lang debug-bp list

# 移除断点
old8lang debug-bp remove <断点ID>

# 清除所有断点
old8lang debug-bp clear

# 示例
old8lang debug-bp add test.old8 10
old8lang debug-bp add test.old8 15 "x > 5"
old8lang debug-bp func main
old8lang debug-bp list
old8lang debug-bp remove 1
```

### 调试控制

```bash
# 继续执行
old8lang debug continue

# 单步执行
old8lang debug step        # 单步进入
old8lang debug stepinto    # 单步进入（同上）
old8lang debug stepover    # 单步跳过
old8lang debug stepout     # 单步跳出

# 暂停执行
old8lang debug pause

# 停止调试
old8lang debug stop

# 显示调用栈
old8lang debug stack

# 显示当前变量
old8lang debug vars
```

## 使用示例

### 基础调试流程

1. **启动调试会话**:
   ```bash
   old8lang debug-start example.old8
   ```

2. **设置断点**:
   ```bash
   old8lang debug-bp add example.old8 10
   old8lang debug-bp func calculate
   ```

3. **开始执行**:
   程序会在断点处暂停

4. **检查状态**:
   ```bash
   old8lang debug stack
   old8lang debug vars
   ```

5. **单步执行**:
   ```bash
   old8lang debug step
   ```

6. **继续执行**:
   ```bash
   old8lang debug continue
   ```

### 条件断点示例

设置只在特定条件下触发的断点：

```bash
old8lang debug-bp add loop.old8 15 "i == 5"
old8lang debug-bp add logic.old8 20 "flag == true"
```

### 变量监视

虽然当前版本通过 `debug vars` 查看变量，未来的版本将支持：

```bash
# 添加变量监视（计划功能）
old8lang debug-watch add x
old8lang debug-watch add "result * 2"

# 列出监视变量
old8lang debug-watch list
```

## 测试文件

调试器包含以下测试文件：

- `TestFiles/DebuggerTests/test_breakpoints.old8` - 断点功能测试
- `TestFiles/DebuggerTests/test_variables.old8` - 变量监视测试
- `TestFiles/DebuggerTests/test_callstack.old8` - 调用栈测试

### 运行测试

```bash
# 编译测试
dotnet build

# 运行调试器单元测试
dotnet test Old8Lang.Tests --filter "Debugger"

# 测试调试功能
old8lang debug-start TestFiles/DebuggerTests/test_breakpoints.old8
```

## 架构说明

调试器由以下核心组件组成：

1. **BreakpointManager**: 管理断点的设置、移除和命中检测
2. **VariableWatcher**: 监视变量值的变化
3. **Debugger**: 调试器核心引擎，协调各组件工作
4. **CallStack**: 管理函数调用栈
5. **DebuggableInterpreter**: 支持调试的解释器包装器

## 事件系统

调试器提供丰富的事件通知：

- **StateChanged**: 调试状态变化
- **BreakpointHit**: 断点命中
- **ErrorOccurred**: 运行时错误

这些事件可以用于构建图形化调试界面。

## 注意事项

1. 调试器目前主要支持解释模式
2. 条件断点只支持简单的变量检查
3. 编译模式的调试支持正在开发中
4. 调试会话会在程序结束或手动停止时自动清理

## 故障排除

### 常见问题

**Q: 断点不命中？**
A: 确保文件路径正确，行号有效，断点已启用。

**Q: 变量显示不正确？**
A: 检查变量是否在当前作用域内，确保已执行到相关代码。

**Q: 调试器启动失败？**
A: 确保文件存在且有读取权限，检查语法是否正确。

### 调试技巧

1. 在关键逻辑处设置断点
2. 使用条件断点避免不必要的暂停
3. 结合调用栈分析程序流程
4. 利用单步执行逐步验证逻辑
5. 注意作用域对变量可见性的影响

## 未来改进

计划中的功能：

- [ ] 图形化调试界面
- [ ] 更强大的条件表达式
- [ ] 编译模式调试支持
- [ ] 远程调试功能
- [ ] 性能分析工具
- [ ] 内存使用监控