# 解析器：类体在文件结束处被截断时死循环 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「优先顺序 1. 排查会使测试进程崩溃的 IL 问题」（同类：进程挂死）。

## 1. 现象

截断的源文件（常见于编辑器保存一半）会让整个进程**挂死**，`-s` 语法检查也一样，
只能 `kill -9`：

```bash
printf 'class A {' > /tmp/hang.old8
dotnet Old8Lang.App.dll -s /tmp/hang.old8    # 永不返回
```

## 2. 根因

`ClassParser.ParseClassBlock` 的成员循环：

```csharp
while (CurrentToken.Type != LangTokenType.RightBrace)
{
    if (CurrentToken.Type == LangTokenType.EndOfFile)
    {
        CurrentIndex++;
        continue;      // EOF 位置不会前进 → 无限循环
    }
    ...
}
```

词法器在文件结束处把位置固定在 EOF，`CurrentIndex++` 不会让 `CurrentToken` 改变，
于是循环永不结束。

## 3. 修复

EOF 时直接报语法错误：

```csharp
if (CurrentToken.Type == LangTokenType.EndOfFile)
{
    throw CreateSyntaxError("类定义缺少右花括号 '}'：文件在类体内结束");
}
```

涉及文件：`Old8Lang.Syntax/LangParser/Parsers/ClassParser.cs`。

## 4. 验证

### 4.1 新增回归测试

`Old8Lang.Tests/Parser/Classes/ClassBlockEofTests.cs`（2 项）：

- `UnclosedClass_ThrowsSyntaxError`：`class A {`
- `UnclosedClassWithField_ThrowsSyntaxError`：`class A {\n public x:int <- 0`

修复前两项都会挂住测试主机；修复后立即抛 `SyntaxError`，消息含「右花括号」。

### 4.2 其他命名空间

`dotnet test --filter "FullyQualifiedName~Old8Lang.Tests.Parser"` 889 项通过（详见 Todo 记录）。

## 5. 遗留

其他解析器是否也存在「EOF 时前进位置」的写法未逐一审计；本轮只修了可复现的类体循环。
