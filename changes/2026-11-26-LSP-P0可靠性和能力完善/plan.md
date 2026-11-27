# 实施计划：Language Server P0 可靠性与能力完善

## 阶段划分与依赖关系

```text
阶段 1 (Parser 位置修复 + 移除 token 变通)
    ↓
阶段 2 (SymbolScope 作用域链 + 全 Handler 作用域感知)
    ↓
阶段 3 (TypeResolver + 继承/静态成员访问类型推断与补全)
    ↓
阶段 4 (SemanticAnalyzer 增强诊断 + 跨文件语义分析)
    ↓
阶段 5 (26 个 Handler 完整单测 + JSON-RPC 协议级测试 + VS Code 扩展构建验证)
```

## 每阶段验证与回滚点

1. **阶段 1 完成后**：运行 `Old8Lang.Tests.Parser`、`Old8Lang.Tests.LanguageServer`、`Old8Lang.Tests.Architecture`，确认所有既有测试与新增 Parser 位置测试通过。
2. **阶段 2 完成后**：运行 `Old8Lang.Tests.LanguageServer`，确认作用域链、遮蔽、局部重命名/高亮/跳转/补全通过，解除 `NullableTypeParameter_ShouldComplete` Skip。
3. **阶段 3 完成后**：运行 `Old8Lang.Tests.LanguageServer`，确认继承链、Mixin、静态访问、`this`/`super`、多级属性/方法链补全与悬停通过，解除 `ChainOfPropertyAccess_ShouldComplete` Skip。
4. **阶段 4 完成后**：运行 `Old8Lang.Tests.LanguageServer`，确认类型不匹配、未使用变量、成员不存在、重复定义、跨文件导入与工作区语义分析通过且无误报。
5. **阶段 5 完成后**：运行全量 `Old8Lang.Tests.LanguageServer` + `Old8Lang.Tests.Parser` + `Old8Lang.Tests.Architecture` + `vscode-old8lang` 编译；撰写 `test.md` 并更新 `changes/Todo.md`。
