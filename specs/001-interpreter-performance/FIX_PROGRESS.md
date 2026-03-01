# 修复进展报告

**日期**: 2026-02-21
**状态**: 进行中

## 已完成的工作

### 1. 问题分析 ✅
- 创建了详细的问题报告（CRITICAL_ISSUES.md）
- 创建了修复策略文档（FIX_STRATEGY.md）
- 通过调试发现了根本原因

### 2. 根本原因确认 ✅
扩展方法和内置方法注册在不同的类型键下：
- 内置方法：注册在 `LangValueType`（基类）
- 扩展方法：注册在 `IntLangValue`（子类）

当查找 `IntLangValue.toStr()` 时：
1. 精确匹配找到 `IntLangValue` 的扩展方法
2. 不会继续查找基类 `LangValueType` 的内置方法
3. 导致扩展方法内部调用 `this.ToStr()` 时再次匹配到扩展方法
4. 形成无限递归

### 3. 实现的修复 ✅
1. 添加了 `IsExtensionMethod` 属性到 `IInstanceMethod` 接口
2. 在 `InstanceMethodOverloadGroup.ResolveOverload` 中实现内置方法优先级
3. 在 `VariateManager` 中添加了扩展方法调用栈跟踪
4. 在 `ExtensionMethodWrapper.Execute` 中启用递归检测

### 4. 当前问题 ⚠️
递归检测没有触发，因为：
- 方法签名生成在 `Execute` 方法内部
- 但递归发生在方法查找阶段（`ResolveOverload`）
- 所以 `EnterExtensionMethod` 被调用，但检测逻辑没有阻止方法查找

## 问题的本质

真正的问题不是递归检测的时机，而是：
**扩展方法和内置方法根本不在同一个重载组中！**

当查找 `IntLangValue.toStr()` 时：
1. `GetOverloadGroup(IntLangValue, "toStr")` 返回只包含扩展方法的重载组
2. `GetOverloadGroup(LangValueType, "ToStr")` 返回只包含内置方法的重载组
3. 它们是两个完全独立的重载组！

## 正确的修复方案

需要修改 `InstanceMethodRegistry.GetOverloadGroup` 方法，使其：
1. 查找当前类型的方法
2. **同时查找所有基类的方法**
3. **合并到一个虚拟的重载组中**
4. 然后在这个合并的重载组中应用"内置方法优先"规则

## 下一步行动

由于问题的复杂性，我建议采用以下策略之一：

### 方案 A: 修改 GetOverloadGroup（推荐）
修改方法查找逻辑，合并基类和子类的方法到统一视图。

### 方案 B: 禁止扩展方法覆盖内置方法（简单）
在注册扩展方法时，检查是否会覆盖基类的内置方法，如果会则拒绝注册并给出错误。

### 方案 C: 修改测试用例（临时）
将测试中的 `this.ToStr()` 改为其他不冲突的调用，承认这是已知限制。

## 建议

鉴于时间和复杂度，我建议：
1. **立即采用方案 B**：在扩展方法注册时检测冲突并拒绝
2. **文档化限制**：明确说明扩展方法不能与内置方法同名
3. **长期采用方案 A**：重新设计方法查找系统

这样可以：
- ✅ 立即解决栈溢出问题
- ✅ 给用户清晰的错误信息
- ✅ 保持代码简单可维护
- ✅ 为未来的改进留下空间

---

**当前状态**: 等待用户确认修复方案
**预计完成时间**: 方案 B 约 30 分钟
