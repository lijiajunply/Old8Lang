# Old8Lang Visitor 模式迁移计划

**版本：** v1.0
**创建日期：** 2025-12-22
**状态：** 规划阶段

---

## 目录

1. [项目背景与迁移目标](#1-项目背景与迁移目标)
2. [当前架构分析](#2-当前架构分析)
3. [Visitor 模式设计方案](#3-visitor-模式设计方案)
4. [代码生成器设计](#4-代码生成器设计)
5. [迁移步骤与里程碑](#5-迁移步骤与里程碑)
6. [风险控制与回退策略](#6-风险控制与回退策略)
7. [代码生成器实现示例](#7-代码生成器实现示例)
8. [附录](#8-附录)

---

## 1. 项目背景与迁移目标

### 1.1 项目概述

**Old8Lang** 是一个动态编程语言,采用 C#/Java 风格的语法，支持：
- **双执行模式：** 解释器模式（`-f`）和编译器模式（`-c`）
- **高级特性：** 异步/await、生成器/yield、类、Lambda、异常处理
- **类型系统：** 动态类型（解释器模式）+ 静态类型注解（编译器模式）

**当前 AST 规模：**
- 60+ 核心 AST 节点
- 14+ Statement 节点（控制流、声明）
- 32+ Expression 节点（运算、值类型、中间表达式）
- 99 个相关文件

### 1.2 迁移动机

**当前架构问题：**

1. **职责过载：** AST 节点同时负责解析、解释执行、IL 生成、类型推断
2. **扩展困难：** 添加新功能（如优化、验证、代码分析）需要修改所有节点
3. **代码重复：** 遍历逻辑散布在各个节点中
4. **测试复杂：** 无法单独测试遍历逻辑与执行逻辑
5. **维护成本高：** 某些节点文件过大（如 `Operation.cs` 达 82KB）

**迁移目标：**

1. ✅ **分离关注点：** 将遍历逻辑从 AST 节点中解耦
2. ✅ **提高可扩展性：** 轻松添加新的 Visitor（优化器、验证器、代码生成器）
3. ✅ **简化测试：** 独立测试各个 Visitor 的功能
4. ✅ **统一接口：** 为所有 AST 节点提供一致的访问接口
5. ✅ **保持兼容性：** 渐进式迁移，不破坏现有功能

### 1.3 迁移范围

**阶段一：核心基础设施（必须）**
- Visitor 接口定义
- Accept 方法生成
- 核心 Visitor 实现（InterpreterVisitor、CompilerVisitor）

**阶段二：全节点迁移（推荐）**
- 60+ 节点的 Accept 方法实现
- 所有 Statement 和 Expression 的 Visitor 支持

**阶段三：高级功能（可选）**
- 优化 Visitor（常量折叠、死代码消除）
- 验证 Visitor（类型检查、作用域验证）
- 代码分析 Visitor（复杂度分析、依赖分析）

---

## 2. 当前架构分析

### 2.1 AST 节点继承层次

```
IOldLangTree (接口)
├── OldStatement (抽象类)
│   ├── IfStatement
│   ├── ForStatement
│   ├── WhileStatement
│   ├── ForInStatement
│   ├── AsyncForInStatement
│   ├── BlockStatement
│   ├── SetStatement
│   ├── ReturnStatement
│   ├── BreakStatement
│   ├── ContinueStatement
│   ├── FuncInit
│   ├── AsyncFuncInit
│   ├── ClassInit
│   ├── YieldStatement
│   ├── SwitchStatement
│   ├── TryStatement
│   ├── ThrowStatement
│   ├── ImportStatement
│   └── NativeStatement
│
└── LangExpression (抽象类)
    ├── LangValueType (抽象类)
    │   ├── IntLangValue
    │   ├── DoubleLangValue
    │   ├── StringLangValue
    │   ├── BoolLangValue
    │   ├── CharLangValue
    │   ├── NullLangValue
    │   ├── ArrayLangValue
    │   ├── ListLangValue
    │   ├── DictionaryLangValue
    │   ├── TupleLangValue
    │   ├── FuncLangValue
    │   ├── AsyncFuncLangValue
    │   ├── TaskLangValue
    │   ├── GeneratorLangValue
    │   ├── AsyncGeneratorLangValue
    │   └── ... (21+ 值类型)
    │
    ├── LangId
    ├── Operation
    ├── FunctionCallExpression
    ├── ClassMemberId
    ├── TernaryExpression
    ├── AwaitExpression
    ├── AsyncStreamExpression
    └── SuperExpression
```

### 2.2 当前执行方法

**OldStatement 抽象类：**
```csharp
public abstract class OldStatement : IOldLangTree
{
    // 解释器模式执行
    public abstract void Run(VariateManager manager);

    // 编译器模式执行
    public abstract void GenerateIl(ILGenerator ilGenerator, LocalManager local);

    // 子节点访问
    public abstract OldStatement? this[int index] { get; }
    public abstract int Count { get; }
}
```

**LangExpression 抽象类：**
```csharp
public abstract class LangExpression : IOldLangTree
{
    // 解释器模式执行
    public virtual LangValueType Run(VariateManager manager);

    // 编译器模式：加载 IL 值
    public virtual void LoadIlValue(ILGenerator ilGenerator, LocalManager local);

    // 类型推断
    public virtual Type? OutputType(LocalManager local);
}
```

### 2.3 当前架构优缺点

**优点：**
- ✅ 直接高效：虚方法分派性能好
- ✅ 实现简单：逻辑集中在节点内
- ✅ 双模式支持：同时支持解释和编译

**缺点：**
- ❌ 职责混杂：节点承载过多职责
- ❌ 扩展困难：新功能需修改所有节点
- ❌ 代码膨胀：某些文件过大（如 Operation.cs）
- ❌ 测试困难：无法独立测试遍历逻辑
- ❌ 横切关注点：难以添加日志、验证、优化等功能

---

## 3. Visitor 模式设计方案

### 3.1 Visitor 接口设计

#### 3.1.1 核心 Visitor 接口

```csharp
namespace Old8Lang.AST.Visitor;

/// <summary>
/// Visitor 模式基础接口
/// </summary>
/// <typeparam name="TResult">返回结果类型</typeparam>
public interface IVisitor<out TResult>
{
    // ===== Statement 访问方法 =====
    TResult VisitIfStatement(IfStatement node);
    TResult VisitForStatement(ForStatement node);
    TResult VisitWhileStatement(WhileStatement node);
    TResult VisitForInStatement(ForInStatement node);
    TResult VisitAsyncForInStatement(AsyncForInStatement node);
    TResult VisitBlockStatement(BlockStatement node);
    TResult VisitSetStatement(SetStatement node);
    TResult VisitReturnStatement(ReturnStatement node);
    TResult VisitBreakStatement(BreakStatement node);
    TResult VisitContinueStatement(ContinueStatement node);
    TResult VisitFuncInit(FuncInit node);
    TResult VisitAsyncFuncInit(AsyncFuncInit node);
    TResult VisitClassInit(ClassInit node);
    TResult VisitYieldStatement(YieldStatement node);
    TResult VisitSwitchStatement(SwitchStatement node);
    TResult VisitTryStatement(TryStatement node);
    TResult VisitThrowStatement(ThrowStatement node);
    TResult VisitImportStatement(ImportStatement node);
    TResult VisitNativeStatement(NativeStatement node);

    // ===== Expression 访问方法 =====
    TResult VisitLangId(LangId node);
    TResult VisitOperation(Operation node);
    TResult VisitFunctionCallExpression(FunctionCallExpression node);
    TResult VisitClassMemberId(ClassMemberId node);
    TResult VisitTernaryExpression(TernaryExpression node);
    TResult VisitAwaitExpression(AwaitExpression node);
    TResult VisitAsyncStreamExpression(AsyncStreamExpression node);
    TResult VisitSuperExpression(SuperExpression node);

    // ===== Value 访问方法 =====
    TResult VisitIntLangValue(IntLangValue node);
    TResult VisitDoubleLangValue(DoubleLangValue node);
    TResult VisitStringLangValue(StringLangValue node);
    TResult VisitBoolLangValue(BoolLangValue node);
    TResult VisitCharLangValue(CharLangValue node);
    TResult VisitNullLangValue(NullLangValue node);
    TResult VisitArrayLangValue(ArrayLangValue node);
    TResult VisitListLangValue(ListLangValue node);
    TResult VisitDictionaryLangValue(DictionaryLangValue node);
    TResult VisitTupleLangValue(TupleLangValue node);
    TResult VisitFuncLangValue(FuncLangValue node);
    TResult VisitAsyncFuncLangValue(AsyncFuncLangValue node);
    TResult VisitTaskLangValue(TaskLangValue node);
    TResult VisitGeneratorLangValue(GeneratorLangValue node);
    TResult VisitAsyncGeneratorLangValue(AsyncGeneratorLangValue node);

    // ... 其他值类型访问方法
}
```

#### 3.1.2 Accept 方法接口

在 `IOldLangTree` 接口中添加 Accept 方法：

```csharp
namespace Old8Lang.AST;

public interface IOldLangTree
{
    SourcePosition Position { get; }

    // 新增：支持 Visitor 模式
    TResult Accept<TResult>(IVisitor<TResult> visitor);
}
```

### 3.2 核心 Visitor 实现

#### 3.2.1 解释器 Visitor

```csharp
namespace Old8Lang.AST.Visitor;

/// <summary>
/// 解释器模式 Visitor - 替代原有的 Run() 方法
/// </summary>
public class InterpreterVisitor : IVisitor<LangValueType>
{
    private readonly VariateManager _manager;

    public InterpreterVisitor(VariateManager manager)
    {
        _manager = manager;
    }

    public LangValueType VisitIfStatement(IfStatement node)
    {
        // 原 Run() 方法的实现
        // ...
    }

    public LangValueType VisitForStatement(ForStatement node)
    {
        // 原 Run() 方法的实现
        // ...
    }

    // ... 其他 Visit 方法
}
```

#### 3.2.2 编译器 Visitor

```csharp
namespace Old8Lang.AST.Visitor;

/// <summary>
/// 编译器模式 Visitor - 替代原有的 GenerateIl() 方法
/// </summary>
public class CompilerVisitor : IVisitor<object?>
{
    private readonly ILGenerator _ilGenerator;
    private readonly LocalManager _local;

    public CompilerVisitor(ILGenerator ilGenerator, LocalManager local)
    {
        _ilGenerator = ilGenerator;
        _local = local;
    }

    public object? VisitIfStatement(IfStatement node)
    {
        // 原 GenerateIl() 方法的实现
        // ...
        return null;
    }

    public object? VisitForStatement(ForStatement node)
    {
        // 原 GenerateIl() 方法的实现
        // ...
        return null;
    }

    // ... 其他 Visit 方法
}
```

#### 3.2.3 类型推断 Visitor

```csharp
namespace Old8Lang.AST.Visitor;

/// <summary>
/// 类型推断 Visitor - 替代原有的 OutputType() 方法
/// </summary>
public class TypeInferenceVisitor : IVisitor<Type?>
{
    private readonly LocalManager _local;

    public TypeInferenceVisitor(LocalManager local)
    {
        _local = local;
    }

    public Type? VisitOperation(Operation node)
    {
        // 原 OutputType() 方法的实现
        // ...
    }

    public Type? VisitFunctionCallExpression(FunctionCallExpression node)
    {
        // 原 OutputType() 方法的实现
        // ...
    }

    // ... 其他 Visit 方法
}
```

### 3.3 基础 Visitor 抽象类

为了简化 Visitor 实现，提供抽象基类：

```csharp
namespace Old8Lang.AST.Visitor;

/// <summary>
/// Visitor 抽象基类 - 提供默认遍历逻辑
/// </summary>
public abstract class BaseVisitor<TResult> : IVisitor<TResult>
{
    protected abstract TResult DefaultResult { get; }

    // 默认实现：遍历子节点
    public virtual TResult VisitIfStatement(IfStatement node)
    {
        node.Condition.Accept(this);
        node.ThenBlock.Accept(this);
        foreach (var elif in node.ElseIfBlocks)
        {
            elif.Accept(this);
        }
        node.ElseBlock?.Accept(this);
        return DefaultResult;
    }

    // ... 其他默认实现
}
```

### 3.4 Visitor 模式架构图

```
┌─────────────────────────────────────────────────────────────┐
│                     IOldLangTree                            │
│  + Accept<TResult>(IVisitor<TResult> visitor): TResult     │
└─────────────────────────────────────────────────────────────┘
                            ▲
                            │
        ┌───────────────────┴───────────────────┐
        │                                       │
┌───────┴───────┐                      ┌───────┴───────┐
│ OldStatement  │                      │ LangExpression│
│ + Accept(...) │                      │ + Accept(...) │
└───────┬───────┘                      └───────┬───────┘
        │                                      │
   ┌────┴────┐                            ┌────┴────┐
   │         │                            │         │
IfStatement ForStatement              LangId   Operation
SetStatement ...                      FunctionCall ...


┌─────────────────────────────────────────────────────────────┐
│                  IVisitor<TResult>                          │
│  + VisitIfStatement(IfStatement): TResult                   │
│  + VisitForStatement(ForStatement): TResult                 │
│  + VisitLangId(LangId): TResult                            │
│  + ...                                                      │
└─────────────────────────────────────────────────────────────┘
                            ▲
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
┌───────┴────────┐  ┌───────┴────────┐  ┌──────┴──────┐
│InterpreterVisit│  │CompilerVisitor │  │TypeInference│
│                │  │                │  │  Visitor    │
└────────────────┘  └────────────────┘  └─────────────┘
```

### 3.5 兼容性策略

#### 3.5.1 双模式共存

在迁移期间，保留原有方法并委托给 Visitor：

```csharp
public abstract class OldStatement : IOldLangTree
{
    // 原有方法（标记为过时）
    [Obsolete("请使用 Visitor 模式")]
    public void Run(VariateManager manager)
    {
        var visitor = new InterpreterVisitor(manager);
        Accept(visitor);
    }

    [Obsolete("请使用 Visitor 模式")]
    public void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        var visitor = new CompilerVisitor(ilGenerator, local);
        Accept(visitor);
    }

    // 新增：Visitor 模式接口
    public abstract TResult Accept<TResult>(IVisitor<TResult> visitor);
}
```

#### 3.5.2 渐进式迁移路径

**阶段 1：** 添加 Accept 方法，保留原有方法
**阶段 2：** 迁移核心代码到 Visitor
**阶段 3：** 标记原有方法为过时
**阶段 4：** 移除原有方法

---

## 4. 代码生成器设计

### 4.1 为什么需要代码生成器

**手动迁移的问题：**
- 60+ 节点，每个需要添加 Accept 方法
- 每个节点需要在 IVisitor 接口中添加对应的 Visit 方法
- 容易遗漏或出错
- 重复性工作，耗时且易出错

**代码生成器的优势：**
- ✅ 自动化：一键生成所有代码
- ✅ 一致性：确保所有节点遵循相同模式
- ✅ 可维护性：修改模板即可更新所有代码
- ✅ 减少错误：避免手动输入错误

### 4.2 代码生成器架构

```
┌───────────────────────────────────────────────────────────┐
│              VisitorCodeGenerator                         │
│                                                           │
│  1. 扫描 AST 目录                                         │
│  2. 识别所有 AST 节点类                                   │
│  3. 分类：Statement / Expression / Value                  │
│  4. 生成代码                                              │
└───────────────────────────────────────────────────────────┘
                          │
        ┌─────────────────┼─────────────────┐
        │                 │                 │
        ▼                 ▼                 ▼
┌───────────┐   ┌──────────────┐   ┌──────────────┐
│ IVisitor  │   │   Accept     │   │BaseVisitor   │
│ 接口生成  │   │  方法生成    │   │  抽象类生成  │
└───────────┘   └──────────────┘   └──────────────┘
```

### 4.3 生成器输入与输出

**输入：**
- AST 节点类的元数据（类名、命名空间、基类）
- 代码模板（IVisitor、Accept、BaseVisitor）
- 配置文件（包含/排除规则）

**输出：**
- `IVisitor.generated.cs` - Visitor 接口
- `*.Accept.generated.cs` - 每个节点的 Accept 方法（partial class）
- `BaseVisitor.generated.cs` - 基础 Visitor 抽象类

### 4.4 节点扫描策略

**扫描规则：**
1. 扫描 `Old8Lang/AST/` 目录下所有 `.cs` 文件
2. 识别继承自 `OldStatement` 或 `LangExpression` 的类
3. 排除抽象类（`abstract class`）
4. 排除特定类（如 `IfChild`、辅助类）
5. 提取类名、命名空间、基类信息

**节点分类：**
```csharp
public class AstNodeInfo
{
    public string ClassName { get; set; }        // 如：IfStatement
    public string FullTypeName { get; set; }     // 如：Old8Lang.AST.Statement.IfStatement
    public string Namespace { get; set; }        // 如：Old8Lang.AST.Statement
    public AstNodeCategory Category { get; set; } // Statement / Expression / Value
}

public enum AstNodeCategory
{
    Statement,
    Expression,
    Value
}
```

### 4.5 代码模板

#### 4.5.1 IVisitor 接口模板

```csharp
// 文件：IVisitor.generated.cs
// 此文件由代码生成器自动生成，请勿手动修改

using System;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Statement;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// Visitor 模式接口
/// 自动生成日期：{{GenerationDate}}
/// 节点数量：{{NodeCount}}
/// </summary>
public interface IVisitor<out TResult>
{
    {{#each StatementNodes}}
    /// <summary>
    /// 访问 {{ClassName}} 节点
    /// </summary>
    TResult Visit{{ClassName}}({{ClassName}} node);
    {{/each}}

    {{#each ExpressionNodes}}
    /// <summary>
    /// 访问 {{ClassName}} 节点
    /// </summary>
    TResult Visit{{ClassName}}({{ClassName}} node);
    {{/each}}

    {{#each ValueNodes}}
    /// <summary>
    /// 访问 {{ClassName}} 节点
    /// </summary>
    TResult Visit{{ClassName}}({{ClassName}} node);
    {{/each}}
}
```

#### 4.5.2 Accept 方法模板

```csharp
// 文件：{{ClassName}}.Accept.generated.cs
// 此文件由代码生成器自动生成，请勿手动修改

using Old8Lang.AST.Visitor;

namespace {{Namespace}};

/// <summary>
/// {{ClassName}} 的 Accept 方法实现（自动生成）
/// </summary>
public partial class {{ClassName}}
{
    /// <summary>
    /// 接受 Visitor 访问
    /// </summary>
    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        return visitor.Visit{{ClassName}}(this);
    }
}
```

#### 4.5.3 BaseVisitor 模板

```csharp
// 文件：BaseVisitor.generated.cs
// 此文件由代码生成器自动生成，请勿手动修改

using System;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Statement;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// Visitor 抽象基类 - 提供默认遍历实现
/// 自动生成日期：{{GenerationDate}}
/// </summary>
public abstract class BaseVisitor<TResult> : IVisitor<TResult>
{
    protected abstract TResult DefaultResult { get; }

    {{#each AllNodes}}
    public virtual TResult Visit{{ClassName}}({{ClassName}} node)
    {
        // 默认实现：遍历子节点
        {{#if HasChildren}}
        {{#each ChildProperties}}
        {{PropertyName}}?.Accept(this);
        {{/each}}
        {{/if}}
        return DefaultResult;
    }
    {{/each}}
}
```

### 4.6 生成器配置

**配置文件：** `visitor-codegen.json`

```json
{
  "scanDirectory": "Old8Lang/AST",
  "outputDirectory": "Old8Lang/AST/Visitor/Generated",
  "excludePatterns": [
    "**/ValueFunctions/**",
    "**/ModuleObjects/**",
    "**/*Helper.cs"
  ],
  "excludeClasses": [
    "IOldLangTree",
    "OldStatement",
    "LangExpression",
    "LangValueType",
    "IfChild"
  ],
  "templates": {
    "visitor": "Templates/IVisitor.template",
    "accept": "Templates/Accept.template",
    "baseVisitor": "Templates/BaseVisitor.template"
  },
  "generatePartialClasses": true,
  "addGeneratedCodeAttribute": true
}
```

### 4.7 生成器工作流程

```
┌─────────────────────────────────────────────────────────┐
│ 步骤 1：扫描 AST 目录                                   │
│  - 递归扫描所有 .cs 文件                                │
│  - 解析类定义和继承关系                                 │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│ 步骤 2：过滤和分类                                      │
│  - 应用排除规则                                         │
│  - 分类为 Statement / Expression / Value                │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│ 步骤 3：分析节点结构                                    │
│  - 提取子节点属性                                       │
│  - 识别集合属性（List, Array）                          │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│ 步骤 4：生成代码                                        │
│  - 生成 IVisitor 接口                                   │
│  - 生成每个节点的 Accept 方法                           │
│  - 生成 BaseVisitor 抽象类                              │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│ 步骤 5：写入文件                                        │
│  - 写入到 Generated 目录                                │
│  - 添加文件头注释                                       │
│  - 格式化代码                                           │
└─────────────────────────────────────────────────────────┘
```

### 4.8 使用示例

**命令行使用：**
```bash
# 在项目根目录执行
dotnet run --project Old8Lang.CodeGen -- generate-visitor

# 指定配置文件
dotnet run --project Old8Lang.CodeGen -- generate-visitor --config visitor-codegen.json

# 预览模式（不写入文件）
dotnet run --project Old8Lang.CodeGen -- generate-visitor --preview

# 增量生成（只生成变更的文件）
dotnet run --project Old8Lang.CodeGen -- generate-visitor --incremental
```

**输出示例：**
```
[INFO] 扫描目录：Old8Lang/AST
[INFO] 发现 99 个 .cs 文件
[INFO] 过滤后剩余 62 个 AST 节点
[INFO] 分类：Statement (19), Expression (21), Value (22)
[INFO] 生成 IVisitor.generated.cs (3.2 KB)
[INFO] 生成 62 个 Accept 方法文件
[INFO] 生成 BaseVisitor.generated.cs (8.5 KB)
[SUCCESS] 代码生成完成！
```

---

## 5. 迁移步骤与里程碑

### 5.1 迁移总体时间线

```
阶段 0: 准备阶段 (1-2 天)
    │
    ├─> 创建代码生成器项目
    ├─> 编写配置文件
    └─> 测试代码生成器

阶段 1: 基础设施 (3-5 天)
    │
    ├─> 生成 IVisitor 接口
    ├─> 在 IOldLangTree 添加 Accept 方法
    ├─> 生成所有节点的 Accept 实现
    └─> 单元测试 Accept 方法

阶段 2: 核心 Visitor 实现 (5-7 天)
    │
    ├─> 实现 InterpreterVisitor
    ├─> 实现 CompilerVisitor
    ├─> 实现 TypeInferenceVisitor
    └─> 迁移测试用例

阶段 3: 兼容性层 (2-3 天)
    │
    ├─> 保留原有 Run/GenerateIl 方法
    ├─> 委托到 Visitor
    ├─> 标记为 Obsolete
    └─> 验证所有测试通过

阶段 4: 清理与优化 (2-3 天)
    │
    ├─> 移除过时方法
    ├─> 代码审查
    ├─> 性能测试
    └─> 文档更新

总计：13-20 天
```

### 5.2 详细迁移步骤

#### 阶段 0：准备阶段（1-2 天）

**步骤 0.1：创建代码生成器项目**
```bash
# 创建新项目
dotnet new console -n Old8Lang.CodeGen
cd Old8Lang.CodeGen

# 添加依赖
dotnet add package Microsoft.CodeAnalysis.CSharp
dotnet add package System.CommandLine

# 添加项目引用
dotnet add reference ../Old8Lang/Old8Lang.csproj
```

**步骤 0.2：编写配置文件**
- 创建 `visitor-codegen.json`
- 定义扫描规则和排除列表
- 配置代码模板路径

**步骤 0.3：实现代码生成器核心逻辑**
- AST 节点扫描器
- 代码模板引擎
- 文件写入器

**步骤 0.4：测试代码生成器**
```bash
# 预览模式测试
dotnet run --project Old8Lang.CodeGen -- generate-visitor --preview

# 检查输出是否正确
# 验证生成的代码能够编译
```

**里程碑 0：** ✅ 代码生成器可用，能够生成正确的代码

---

#### 阶段 1：基础设施（3-5 天）

**步骤 1.1：生成 IVisitor 接口**
```bash
# 运行生成器
dotnet run --project Old8Lang.CodeGen -- generate-visitor

# 验证生成的文件
ls Old8Lang/AST/Visitor/Generated/IVisitor.generated.cs
```

**步骤 1.2：修改 IOldLangTree 接口**
```csharp
// Old8Lang/AST/OldLangTree.cs
public interface IOldLangTree
{
    SourcePosition Position { get; }

    // 新增：Visitor 模式支持
    TResult Accept<TResult>(IVisitor<TResult> visitor);
}
```

**步骤 1.3：生成所有节点的 Accept 方法**
- 运行代码生成器
- 为每个节点生成 `*.Accept.generated.cs`
- 确保所有节点类标记为 `partial class`

**步骤 1.4：编译验证**
```bash
# 编译整个解决方案
dotnet build Old8Lang.sln

# 修复编译错误
# - 将节点类改为 partial class
# - 添加必要的 using 语句
```

**步骤 1.5：单元测试 Accept 方法**
```csharp
// Old8Lang.Tests/VisitorTests/AcceptMethodTests.cs
public class AcceptMethodTests
{
    [Fact]
    public void IfStatement_Accept_CallsVisitIfStatement()
    {
        // Arrange
        var ifStmt = new IfStatement(/* ... */);
        var visitor = new MockVisitor();

        // Act
        ifStmt.Accept(visitor);

        // Assert
        Assert.True(visitor.VisitIfStatementCalled);
    }

    // 为每个节点类型添加类似测试
}
```

**里程碑 1：** ✅ 所有节点支持 Accept 方法，编译通过，单元测试通过

---

#### 阶段 2：核心 Visitor 实现（5-7 天）

**步骤 2.1：实现 InterpreterVisitor**

```csharp
// Old8Lang/AST/Visitor/InterpreterVisitor.cs
public class InterpreterVisitor : IVisitor<LangValueType>
{
    private readonly VariateManager _manager;

    public InterpreterVisitor(VariateManager manager)
    {
        _manager = manager;
    }

    public LangValueType VisitIfStatement(IfStatement node)
    {
        // 迁移原有 IfStatement.Run() 的实现
        var condition = node.Condition.Accept(this);
        if (condition.GetValue() is true)
        {
            return node.ThenBlock.Accept(this);
        }
        // ... 其他逻辑
    }

    // ... 实现所有 Visit 方法
}
```

**迁移策略：**
1. 从最简单的节点开始（如 `BreakStatement`、`ContinueStatement`）
2. 逐步迁移复杂节点（如 `IfStatement`、`ForStatement`）
3. 最后迁移最复杂的节点（如 `Operation`、`FunctionCallExpression`）

**步骤 2.2：实现 CompilerVisitor**

```csharp
// Old8Lang/AST/Visitor/CompilerVisitor.cs
public class CompilerVisitor : IVisitor<object?>
{
    private readonly ILGenerator _ilGenerator;
    private readonly LocalManager _local;

    public CompilerVisitor(ILGenerator ilGenerator, LocalManager local)
    {
        _ilGenerator = ilGenerator;
        _local = local;
    }

    public object? VisitIfStatement(IfStatement node)
    {
        // 迁移原有 IfStatement.GenerateIl() 的实现
        var elseLabel = _ilGenerator.DefineLabel();
        var endLabel = _ilGenerator.DefineLabel();

        // 生成条件判断代码
        node.Condition.Accept(this);
        _ilGenerator.Emit(OpCodes.Brfalse, elseLabel);

        // ... 其他逻辑
        return null;
    }

    // ... 实现所有 Visit 方法
}
```

**步骤 2.3：实现 TypeInferenceVisitor**

```csharp
// Old8Lang/AST/Visitor/TypeInferenceVisitor.cs
public class TypeInferenceVisitor : IVisitor<Type?>
{
    private readonly LocalManager _local;

    public TypeInferenceVisitor(LocalManager local)
    {
        _local = local;
    }

    public Type? VisitOperation(Operation node)
    {
        // 迁移原有 Operation.OutputType() 的实现
        var leftType = node.Left.Accept(this);
        var rightType = node.Right.Accept(this);

        return node.Op switch
        {
            OperationType.Plus => InferArithmeticType(leftType, rightType),
            OperationType.Minus => InferArithmeticType(leftType, rightType),
            // ... 其他操作
        };
    }

    // ... 实现所有 Visit 方法
}
```

**步骤 2.4：迁移测试用例**

为每个 Visitor 添加完整的测试套件：

```csharp
// Old8Lang.Tests/VisitorTests/InterpreterVisitorTests.cs
public class InterpreterVisitorTests
{
    [Fact]
    public void VisitIfStatement_TrueCondition_ExecutesThenBlock()
    {
        // Arrange
        var ifStmt = new IfStatement(
            condition: new BoolLangValue(true),
            thenBlock: new ReturnStatement(new IntLangValue(42))
        );
        var manager = new VariateManager();
        var visitor = new InterpreterVisitor(manager);

        // Act
        var result = ifStmt.Accept(visitor);

        // Assert
        Assert.Equal(42, result.GetValue());
    }

    // ... 更多测试
}
```

**步骤 2.5：运行所有测试**
```bash
# 运行所有单元测试
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj

# 运行解释器测试
./run_interpreter_tests.sh

# 运行编译器测试
./run_compiler_tests.sh
```

**里程碑 2：** ✅ 所有核心 Visitor 实现完成，测试通过率 ≥ 95%

---

#### 阶段 3：兼容性层（2-3 天）

**步骤 3.1：保留原有方法并委托**

```csharp
// Old8Lang/AST/OldStatement.cs
public abstract class OldStatement : IOldLangTree
{
    // 原有方法（委托给 Visitor）
    [Obsolete("请使用 Accept 方法配合 InterpreterVisitor", false)]
    public void Run(VariateManager manager)
    {
        var visitor = new InterpreterVisitor(manager);
        Accept(visitor);
    }

    [Obsolete("请使用 Accept 方法配合 CompilerVisitor", false)]
    public void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        var visitor = new CompilerVisitor(ilGenerator, local);
        Accept(visitor);
    }

    // Visitor 模式接口
    public abstract TResult Accept<TResult>(IVisitor<TResult> visitor);
}
```

**步骤 3.2：更新调用点（可选）**

逐步更新代码库中的调用点：

```csharp
// 旧代码
statement.Run(manager);

// 新代码
var visitor = new InterpreterVisitor(manager);
statement.Accept(visitor);
```

**步骤 3.3：验证所有测试**
```bash
# 确保所有测试仍然通过
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj
./run_interpreter_tests.sh
./run_compiler_tests.sh
```

**里程碑 3：** ✅ 兼容性层完成，所有现有代码仍能正常工作

---

#### 阶段 4：清理与优化（2-3 天）

**步骤 4.1：移除过时方法**

```csharp
// 从 OldStatement 和 LangExpression 中移除：
// - Run() 方法
// - GenerateIl() 方法
// - OutputType() 方法

// 只保留 Accept 方法
public abstract TResult Accept<TResult>(IVisitor<TResult> visitor);
```

**步骤 4.2：更新所有调用点**

在整个代码库中替换旧的调用方式：
```bash
# 搜索所有使用旧方法的位置
grep -r "\.Run(" Old8Lang/
grep -r "\.GenerateIl(" Old8Lang/
grep -r "\.OutputType(" Old8Lang/

# 逐一更新
```

**步骤 4.3：代码审查**
- 检查代码风格一致性
- 确保注释和文档完整
- 验证命名约定

**步骤 4.4：性能测试**
```bash
# 运行性能基准测试
dotnet run --project Old8Lang.Benchmarks --configuration Release

# 对比迁移前后的性能
# 确保性能没有显著下降（允许 5% 的差异）
```

**步骤 4.5：更新文档**
- 更新 `CLAUDE.md`
- 更新 `Old8Lang_Grammar.md`
- 添加 Visitor 模式使用指南
- 更新架构文档

**里程碑 4：** ✅ 迁移完成，代码清理，文档更新

---

### 5.3 里程碑检查清单

| 里程碑 | 检查项 | 状态 |
|--------|--------|------|
| **里程碑 0** | 代码生成器可运行 | ⬜ |
| | 生成的代码能够编译 | ⬜ |
| | 配置文件正确 | ⬜ |
| **里程碑 1** | 所有节点支持 Accept | ⬜ |
| | IVisitor 接口完整 | ⬜ |
| | 单元测试通过 | ⬜ |
| | 编译无错误无警告 | ⬜ |
| **里程碑 2** | InterpreterVisitor 完成 | ⬜ |
| | CompilerVisitor 完成 | ⬜ |
| | TypeInferenceVisitor 完成 | ⬜ |
| | 所有测试通过率 ≥ 95% | ⬜ |
| **里程碑 3** | 兼容性层实现 | ⬜ |
| | 旧代码仍能运行 | ⬜ |
| | 所有测试通过 | ⬜ |
| **里程碑 4** | 移除过时方法 | ⬜ |
| | 性能测试通过 | ⬜ |
| | 文档更新完成 | ⬜ |
| | 代码审查通过 | ⬜ |

---

## 6. 风险控制与回退策略

### 6.1 风险识别

| 风险类别 | 风险描述 | 概率 | 影响 | 优先级 |
|----------|----------|------|------|--------|
| **技术风险** | 生成器上下文理解不足，生成错误代码 | 中 | 高 | P1 |
| | 性能下降超过 10% | 中 | 中 | P2 |
| | 生成器 Visitor 实现逻辑错误 | 高 | 高 | P1 |
| | 测试覆盖不足，遗漏边界情况 | 中 | 高 | P1 |
| **兼容性风险** | 破坏现有 API | 低 | 高 | P1 |
| | 第三方库依赖问题 | 低 | 中 | P2 |
| **时间风险** | 迁移时间超出预期 | 中 | 中 | P2 |
| | 测试用例编写耗时过长 | 中 | 低 | P3 |
| **人员风险** | 团队对 Visitor 模式理解不足 | 低 | 中 | P2 |

### 6.2 风险缓解策略

**技术风险缓解：**

1. **分阶段迁移：** 先迁移简单节点，积累经验后再迁移复杂节点
2. **自动化测试：** 每个阶段都有完整的测试覆盖
3. **代码审查：** 每个 Visitor 实现都需要 peer review
4. **性能基准：** 在每个阶段运行性能测试，及时发现问题

**兼容性风险缓解：**

1. **保留旧接口：** 阶段 3 保留所有旧方法，确保向后兼容
2. **渐进式替换：** 逐步更新调用点，而非一次性替换
3. **版本控制：** 使用 Git 分支隔离迁移工作

**时间风险缓解：**

1. **预留缓冲时间：** 每个阶段预留 20% 的缓冲时间
2. **并行工作：** 代码生成器和 Visitor 实现可以并行进行
3. **增量交付：** 每个里程碑都是可交付的增量

### 6.3 回退策略

#### 回退触发条件

满足以下任一条件时触发回退：

1. **测试通过率 < 90%** 且无法在 2 天内修复
2. **性能下降 > 15%** 且无法优化
3. **发现严重设计缺陷**，需要重新设计
4. **关键功能破坏**，影响生产环境

#### 回退方案

**方案 A：完全回退（适用于阶段 0-1）**

```bash
# 删除生成的代码
rm -rf Old8Lang/AST/Visitor/Generated/

# 回退到迁移前的提交
git checkout <pre-migration-commit>

# 验证系统正常
dotnet test
```

**方案 B：部分回退（适用于阶段 2-3）**

```bash
# 保留 Visitor 基础设施
# 移除 Visitor 实现
git checkout HEAD -- Old8Lang/AST/Visitor/InterpreterVisitor.cs
git checkout HEAD -- Old8Lang/AST/Visitor/CompilerVisitor.cs

# 恢复旧方法
# 在 OldStatement 中恢复 Run() 和 GenerateIl()
```

**方案 C：降级方案（适用于阶段 4）**

```bash
# 保留 Visitor 模式
# 同时保留旧方法作为备用

# 添加配置开关
public static class VisitorConfig
{
    public static bool UseVisitorPattern { get; set; } = false;
}

// 在代码中使用配置
if (VisitorConfig.UseVisitorPattern)
{
    var visitor = new InterpreterVisitor(manager);
    statement.Accept(visitor);
}
else
{
    statement.Run(manager);
}
```

### 6.4 应急预案

**预案 1：生成器故障**

- **症状：** 代码生成器产生错误代码
- **应对：**
  1. 回退到最后一次正确的生成结果
  2. 手动修复生成器bug
  3. 重新生成并验证

**预案 2：性能严重下降**

- **症状：** 性能下降 > 15%
- **应对：**
  1. 使用性能分析工具定位瓶颈
  2. 优化 Visitor 实现（如内联、缓存）
  3. 如无法优化，触发回退方案 C

**预案 3：测试大面积失败**

- **症状：** 测试通过率 < 90%
- **应对：**
  1. 分析失败原因（逻辑错误 vs 测试错误）
  2. 修复 top 10 最关键的失败测试
  3. 评估是否需要回退

### 6.5 质量保证措施

**代码质量：**
- 每个 PR 必须经过至少 1 人代码审查
- 所有新代码必须有单元测试
- 测试覆盖率 ≥ 85%

**测试策略：**
- 单元测试：测试每个 Visitor 方法
- 集成测试：测试 AST 遍历完整流程
- 回归测试：运行所有现有测试用例
- 性能测试：对比迁移前后的性能

**文档要求：**
- 每个 Visitor 必须有 XML 文档注释
- 复杂逻辑必须有行内注释
- 更新架构文档和用户文档

---

## 7. 代码生成器实现示例

### 7.1 项目结构

```
Old8Lang.CodeGen/
├── Program.cs                      # 入口点
├── Configuration/
│   ├── CodeGenConfig.cs            # 配置模型
│   └── visitor-codegen.json        # 配置文件
├── Scanner/
│   ├── AstNodeScanner.cs           # AST 节点扫描器
│   └── AstNodeInfo.cs              # 节点信息模型
├── Generator/
│   ├── VisitorInterfaceGenerator.cs    # IVisitor 生成器
│   ├── AcceptMethodGenerator.cs        # Accept 方法生成器
│   └── BaseVisitorGenerator.cs         # BaseVisitor 生成器
├── Templates/
│   ├── IVisitor.template           # IVisitor 模板
│   ├── Accept.template             # Accept 模板
│   └── BaseVisitor.template        # BaseVisitor 模板
└── Utils/
    ├── CodeFormatter.cs            # 代码格式化工具
    └── FileWriter.cs               # 文件写入工具
```

### 7.2 核心代码实现

#### 7.2.1 节点扫描器

```csharp
// Scanner/AstNodeScanner.cs
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Old8Lang.CodeGen.Scanner;

public class AstNodeScanner
{
    private readonly string _scanDirectory;
    private readonly HashSet<string> _excludeClasses;

    public AstNodeScanner(string scanDirectory, HashSet<string> excludeClasses)
    {
        _scanDirectory = scanDirectory;
        _excludeClasses = excludeClasses;
    }

    public List<AstNodeInfo> ScanNodes()
    {
        var nodes = new List<AstNodeInfo>();
        var csFiles = Directory.GetFiles(_scanDirectory, "*.cs", SearchOption.AllDirectories);

        foreach (var file in csFiles)
        {
            var sourceCode = File.ReadAllText(file);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = syntaxTree.GetRoot();

            var classDeclarations = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>();

            foreach (var classDecl in classDeclarations)
            {
                if (ShouldIncludeNode(classDecl, file))
                {
                    var nodeInfo = ExtractNodeInfo(classDecl);
                    if (nodeInfo != null)
                    {
                        nodes.Add(nodeInfo);
                    }
                }
            }
        }

        return nodes;
    }

    private bool ShouldIncludeNode(ClassDeclarationSyntax classDecl, string filePath)
    {
        // 排除抽象类
        if (classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword)))
            return false;

        // 排除特定类
        var className = classDecl.Identifier.Text;
        if (_excludeClasses.Contains(className))
            return false;

        // 必须继承自 OldStatement 或 LangExpression
        var baseList = classDecl.BaseList?.Types;
        if (baseList == null)
            return false;

        var hasValidBase = baseList.Any(t =>
        {
            var typeName = t.Type.ToString();
            return typeName.Contains("OldStatement") || typeName.Contains("LangExpression");
        });

        return hasValidBase;
    }

    private AstNodeInfo? ExtractNodeInfo(ClassDeclarationSyntax classDecl)
    {
        var className = classDecl.Identifier.Text;
        var namespaceName = GetNamespace(classDecl);
        var category = DetermineCategory(classDecl, namespaceName);

        return new AstNodeInfo
        {
            ClassName = className,
            Namespace = namespaceName,
            FullTypeName = $"{namespaceName}.{className}",
            Category = category
        };
    }

    private string GetNamespace(SyntaxNode node)
    {
        var namespaceDecl = node.Ancestors()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .FirstOrDefault();

        return namespaceDecl?.Name.ToString() ?? "Old8Lang.AST";
    }

    private AstNodeCategory DetermineCategory(ClassDeclarationSyntax classDecl, string ns)
    {
        if (ns.Contains(".Statement"))
            return AstNodeCategory.Statement;
        if (ns.Contains(".Value"))
            return AstNodeCategory.Value;
        return AstNodeCategory.Expression;
    }
}

// Scanner/AstNodeInfo.cs
public class AstNodeInfo
{
    public string ClassName { get; set; } = "";
    public string FullTypeName { get; set; } = "";
    public string Namespace { get; set; } = "";
    public AstNodeCategory Category { get; set; }
}

public enum AstNodeCategory
{
    Statement,
    Expression,
    Value
}
```

#### 7.2.2 IVisitor 接口生成器

```csharp
// Generator/VisitorInterfaceGenerator.cs
using System.Text;

namespace Old8Lang.CodeGen.Generator;

public class VisitorInterfaceGenerator
{
    private readonly List<AstNodeInfo> _nodes;

    public VisitorInterfaceGenerator(List<AstNodeInfo> nodes)
    {
        _nodes = nodes;
    }

    public string Generate()
    {
        var sb = new StringBuilder();

        // 文件头
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("// 此文件由代码生成器自动生成，请勿手动修改");
        sb.AppendLine($"// 生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"// 节点数量：{_nodes.Count}");
        sb.AppendLine();

        // Using 语句
        sb.AppendLine("using System;");
        sb.AppendLine("using Old8Lang.AST.Expression;");
        sb.AppendLine("using Old8Lang.AST.Statement;");
        sb.AppendLine("using Old8Lang.AST.Expression.Value;");
        sb.AppendLine();

        // 命名空间
        sb.AppendLine("namespace Old8Lang.AST.Visitor;");
        sb.AppendLine();

        // 接口声明
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Visitor 模式基础接口");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("/// <typeparam name=\"TResult\">返回结果类型</typeparam>");
        sb.AppendLine("public interface IVisitor<out TResult>");
        sb.AppendLine("{");

        // Statement 节点
        var statements = _nodes.Where(n => n.Category == AstNodeCategory.Statement).ToList();
        if (statements.Any())
        {
            sb.AppendLine("    // ===== Statement 访问方法 =====");
            foreach (var node in statements.OrderBy(n => n.ClassName))
            {
                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// 访问 {node.ClassName} 节点");
                sb.AppendLine($"    /// </summary>");
                sb.AppendLine($"    TResult Visit{node.ClassName}({node.ClassName} node);");
                sb.AppendLine();
            }
        }

        // Expression 节点
        var expressions = _nodes.Where(n => n.Category == AstNodeCategory.Expression).ToList();
        if (expressions.Any())
        {
            sb.AppendLine("    // ===== Expression 访问方法 =====");
            foreach (var node in expressions.OrderBy(n => n.ClassName))
            {
                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// 访问 {node.ClassName} 节点");
                sb.AppendLine($"    /// </summary>");
                sb.AppendLine($"    TResult Visit{node.ClassName}({node.ClassName} node);");
                sb.AppendLine();
            }
        }

        // Value 节点
        var values = _nodes.Where(n => n.Category == AstNodeCategory.Value).ToList();
        if (values.Any())
        {
            sb.AppendLine("    // ===== Value 访问方法 =====");
            foreach (var node in values.OrderBy(n => n.ClassName))
            {
                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// 访问 {node.ClassName} 节点");
                sb.AppendLine($"    /// </summary>");
                sb.AppendLine($"    TResult Visit{node.ClassName}({node.ClassName} node);");
                sb.AppendLine();
            }
        }

        sb.AppendLine("}");

        return sb.ToString();
    }
}
```

#### 7.2.3 Accept 方法生成器

```csharp
// Generator/AcceptMethodGenerator.cs
using System.Text;

namespace Old8Lang.CodeGen.Generator;

public class AcceptMethodGenerator
{
    private readonly List<AstNodeInfo> _nodes;

    public AcceptMethodGenerator(List<AstNodeInfo> nodes)
    {
        _nodes = nodes;
    }

    public Dictionary<string, string> GenerateAll()
    {
        var result = new Dictionary<string, string>();

        foreach (var node in _nodes)
        {
            var code = GenerateForNode(node);
            var fileName = $"{node.ClassName}.Accept.generated.cs";
            result[fileName] = code;
        }

        return result;
    }

    private string GenerateForNode(AstNodeInfo node)
    {
        var sb = new StringBuilder();

        // 文件头
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("// 此文件由代码生成器自动生成，请勿手动修改");
        sb.AppendLine($"// 生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        // Using 语句
        sb.AppendLine("using Old8Lang.AST.Visitor;");
        sb.AppendLine();

        // 命名空间
        sb.AppendLine($"namespace {node.Namespace};");
        sb.AppendLine();

        // 类声明（partial）
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {node.ClassName} 的 Accept 方法实现（自动生成）");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public partial class {node.ClassName}");
        sb.AppendLine("{");

        // Accept 方法
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 接受 Visitor 访问");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public override TResult Accept<TResult>(IVisitor<TResult> visitor)");
        sb.AppendLine("    {");
        sb.AppendLine($"        return visitor.Visit{node.ClassName}(this);");
        sb.AppendLine("    }");

        sb.AppendLine("}");

        return sb.ToString();
    }
}
```

#### 7.2.4 主程序

```csharp
// Program.cs
using System.CommandLine;
using Old8Lang.CodeGen.Scanner;
using Old8Lang.CodeGen.Generator;
using Old8Lang.CodeGen.Configuration;

namespace Old8Lang.CodeGen;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Old8Lang Visitor 代码生成器");

        var generateCommand = new Command("generate-visitor", "生成 Visitor 模式代码");

        var configOption = new Option<string>(
            "--config",
            getDefaultValue: () => "visitor-codegen.json",
            description: "配置文件路径"
        );

        var previewOption = new Option<bool>(
            "--preview",
            description: "预览模式（不写入文件）"
        );

        generateCommand.AddOption(configOption);
        generateCommand.AddOption(previewOption);

        generateCommand.SetHandler(async (config, preview) =>
        {
            await GenerateVisitorCode(config, preview);
        }, configOption, previewOption);

        rootCommand.AddCommand(generateCommand);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task GenerateVisitorCode(string configPath, bool preview)
    {
        Console.WriteLine("[INFO] 开始生成 Visitor 代码...");

        // 1. 加载配置
        var config = CodeGenConfig.Load(configPath);
        Console.WriteLine($"[INFO] 扫描目录：{config.ScanDirectory}");

        // 2. 扫描节点
        var scanner = new AstNodeScanner(
            config.ScanDirectory,
            new HashSet<string>(config.ExcludeClasses)
        );
        var nodes = scanner.ScanNodes();
        Console.WriteLine($"[INFO] 发现 {nodes.Count} 个 AST 节点");

        // 分类统计
        var statementCount = nodes.Count(n => n.Category == AstNodeCategory.Statement);
        var expressionCount = nodes.Count(n => n.Category == AstNodeCategory.Expression);
        var valueCount = nodes.Count(n => n.Category == AstNodeCategory.Value);
        Console.WriteLine($"[INFO] 分类：Statement ({statementCount}), Expression ({expressionCount}), Value ({valueCount})");

        // 3. 生成 IVisitor 接口
        var visitorGen = new VisitorInterfaceGenerator(nodes);
        var visitorCode = visitorGen.Generate();
        var visitorPath = Path.Combine(config.OutputDirectory, "IVisitor.generated.cs");

        if (preview)
        {
            Console.WriteLine("\n===== IVisitor.generated.cs =====");
            Console.WriteLine(visitorCode);
        }
        else
        {
            Directory.CreateDirectory(config.OutputDirectory);
            await File.WriteAllTextAsync(visitorPath, visitorCode);
            Console.WriteLine($"[INFO] 生成 IVisitor.generated.cs ({visitorCode.Length} 字节)");
        }

        // 4. 生成 Accept 方法
        var acceptGen = new AcceptMethodGenerator(nodes);
        var acceptFiles = acceptGen.GenerateAll();

        if (preview)
        {
            Console.WriteLine($"\n===== {acceptFiles.Count} 个 Accept 方法文件 =====");
            foreach (var (fileName, code) in acceptFiles.Take(3))
            {
                Console.WriteLine($"\n--- {fileName} ---");
                Console.WriteLine(code);
            }
            Console.WriteLine($"... 还有 {acceptFiles.Count - 3} 个文件");
        }
        else
        {
            foreach (var (fileName, code) in acceptFiles)
            {
                var filePath = Path.Combine(config.OutputDirectory, fileName);
                await File.WriteAllTextAsync(filePath, code);
            }
            Console.WriteLine($"[INFO] 生成 {acceptFiles.Count} 个 Accept 方法文件");
        }

        Console.WriteLine("[SUCCESS] 代码生成完成！");
    }
}
```

### 7.3 使用指南

**第一步：创建配置文件**

```json
{
  "scanDirectory": "../../Old8Lang/AST",
  "outputDirectory": "../../Old8Lang/AST/Visitor/Generated",
  "excludePatterns": [
    "**/ValueFunctions/**",
    "**/ModuleObjects/**"
  ],
  "excludeClasses": [
    "IOldLangTree",
    "OldStatement",
    "LangExpression",
    "LangValueType",
    "IfChild"
  ]
}
```

**第二步：运行生成器**

```bash
# 预览模式
dotnet run --project Old8Lang.CodeGen -- generate-visitor --preview

# 正式生成
dotnet run --project Old8Lang.CodeGen -- generate-visitor
```

**第三步：验证生成的代码**

```bash
# 编译验证
dotnet build Old8Lang/Old8Lang.csproj

# 检查生成的文件
ls Old8Lang/AST/Visitor/Generated/
```

---

## 8. 附录

### 8.1 参考资料

**设计模式：**
- *Design Patterns: Elements of Reusable Object-Oriented Software* - Gang of Four
- Visitor Pattern on Wikipedia: https://en.wikipedia.org/wiki/Visitor_pattern

**C# Roslyn：**
- Microsoft.CodeAnalysis.CSharp API: https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp
- Roslyn Syntax Visualizer: https://github.com/dotnet/roslyn

**编译器设计：**
- *Compilers: Principles, Techniques, and Tools* (Dragon Book)
- *Modern Compiler Implementation in C* (Tiger Book)

### 8.2 常见问题 (FAQ)

**Q1: 为什么选择 Visitor 模式而不是其他模式？**

A: Visitor 模式特别适合以下场景：
- AST 节点种类繁多且稳定
- 需要添加多种操作（解释、编译、优化、验证）
- 操作逻辑与节点结构分离

**Q2: Visitor 模式会影响性能吗？**

A: 影响很小：
- 虚方法调用开销 < 5%
- 现代 JIT 编译器能够优化虚方法调用
- 可以通过内联、缓存等技术进一步优化

**Q3: 如果需要添加新的 AST 节点怎么办？**

A: 流程如下：
1. 添加新的节点类（继承自 OldStatement 或 LangExpression）
2. 运行代码生成器重新生成 IVisitor 接口和 Accept 方法
3. 在所有 Visitor 实现中添加新的 Visit 方法
4. 编译器会提示哪些 Visitor 缺少实现

**Q4: 可以保留部分旧方法吗？**

A: 可以，有两种策略：
- **兼容模式：** 保留旧方法并标记为 Obsolete
- **混合模式：** 部分节点使用 Visitor，部分保留旧方法

**Q5: 生成器生成的代码能手动修改吗？**

A: 不建议：
- 生成的代码会在下次运行时被覆盖
- 如需自定义，应修改模板或创建partial class扩展

### 8.3 术语表

| 术语 | 说明 |
|------|------|
| **AST** | Abstract Syntax Tree，抽象语法树 |
| **Visitor** | 访问者，实现遍历和操作逻辑的对象 |
| **Accept** | 接受访问的方法，每个节点都实现此方法 |
| **Visit** | 访问方法，Visitor 为每种节点类型实现一个 Visit 方法 |
| **Double Dispatch** | 双重分派，Visitor 模式的核心机制 |
| **Partial Class** | 部分类，允许将类定义分散到多个文件 |
| **Code Generation** | 代码生成，自动生成源代码的过程 |

### 8.4 相关文件清单

**核心文件：**
- `Old8Lang/AST/OldLangTree.cs` - IOldLangTree 接口
- `Old8Lang/AST/OldStatement.cs` - Statement 基类
- `Old8Lang/AST/LangExpression.cs` - Expression 基类
- `Old8Lang/AST/Visitor/IVisitor.generated.cs` - Visitor 接口（生成）
- `Old8Lang/AST/Visitor/InterpreterVisitor.cs` - 解释器 Visitor
- `Old8Lang/AST/Visitor/CompilerVisitor.cs` - 编译器 Visitor

**工具文件：**
- `Old8Lang.CodeGen/Program.cs` - 代码生成器入口
- `visitor-codegen.json` - 代码生成器配置

**文档文件：**
- `Visitor_Migration_Plan.md` - 本迁移计划（当前文件）
- `CLAUDE.md` - 项目指南
- `Old8Lang_Grammar.md` - 语法文档

### 8.5 版本历史

| 版本 | 日期 | 变更内容 |
|------|------|----------|
| v1.0 | 2025-12-22 | 初始版本，完整的迁移计划 |

---

## 结论

本迁移计划提供了从当前虚方法架构到 Visitor 模式的完整迁移路径。通过分阶段迁移、代码生成器自动化和全面的测试覆盖，我们能够安全地完成这次重大重构。

**关键要点：**

1. **自动化优先：** 使用代码生成器减少手动工作和错误
2. **渐进式迁移：** 分 4 个阶段逐步推进，每个阶段都可独立验证
3. **风险控制：** 明确的回退策略和应急预案
4. **质量保证：** 完整的测试覆盖和代码审查流程

**预期收益：**

- ✅ 更好的代码组织和职责分离
- ✅ 更容易添加新的操作（如优化、验证）
- ✅ 更清晰的架构和更低的维护成本
- ✅ 为未来的扩展打下坚实基础

**下一步：**

1. 审查本迁移计划并获得团队共识
2. 创建 Git 分支开始阶段 0 的工作
3. 按照计划逐步推进，定期回顾进度
4. 在每个里程碑进行验收和文档更新

---

**文档维护者：** Claude Code
**最后更新：** 2025-12-22
**文档状态：** 已完成
