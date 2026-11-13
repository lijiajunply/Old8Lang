# 架构迁移的 C# API 迁移说明

状态：阶段 2 首批，2026-11-14。

本文只记录已发生的公开 C# 源码 API 变更；Old8 源码语法、CLI 参数和三种执行模式的可见行为不变。

| 旧 API | 新 API | 使用方迁移方式 |
|---|---|---|
| `Old8Lang.AST.ILiteralSyntax` | `Old8Lang.Syntax.ILiteralSyntax` | 将 `using Old8Lang.AST;` 改为 `using Old8Lang.Syntax;`；现有 `LiteralValue` 保持不变，新增 `Kind` 和 `Position`。 |
| 无 | `Old8Lang.Syntax.LiteralSyntax` | 面向解析器、语言工具和未来语法节点：用 `LiteralSyntax.String/Char/Bool/Null/Void` 或 `TryParseInteger/TryParseDouble` 创建纯语法数据。不要把它当成可执行运行时值。 |
| 无 | `Old8Lang.Syntax.LiteralKind` | 根据字面量源码类别分支；不要通过 `LangValueType` 子类推断语法类别。 |

Runtime 中的 `IntLangValue`、`DoubleLangValue`、`StringLangValue`、`BoolLangValue`、`CharLangValue`、
`NullLangValue`、`VoidLangValue` 暂时仍实现新接口，以降低现有 Visitor/扩展调用方的迁移成本。后续将
在独立 AST 字面量节点稳定后移除这层兼容关系；那一轮会在本文补充构造器和访问策略的替代路径。

仓库内已更新的直接使用方是上述七个运行时值类与架构测试；搜索 `ILiteralSyntax` 可定位外部源码使用方。
