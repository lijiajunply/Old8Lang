## 修改计划

### 1. 核心类修改

#### Operation.cs
- **当前实现**：`$"{left} {OperaToString()} {right}"`
- **修改要点**：确保操作符符合 Old8Lang 风格，主要操作符已符合要求，无需大改

#### Instance.cs
- **当前实现**：
  ```csharp
  return Id.IdName switch
  {
      "PrintLine" => $"Console.WriteLine({Apis.ListToString(Ids)});",
      "Print" => $"Console.Write({Apis.ListToString(Ids)});",
      _ => Id + "(" + Apis.ListToString(Ids) + ")"
  };
  ```
- **修改后**：
  ```csharp
  return Id.IdName switch
  {
      "PrintLine" => $"print({string.Join(", ", Ids)})",
      "Print" => $"print({string.Join(", ", Ids)})",
      _ => $"{Id}({string.Join(", ", Ids)})"
  };
  ```

#### FuncValue.cs
- **当前实现**：生成 C# 风格的方法定义
- **修改要点**：生成 Old8Lang 风格的函数定义

### 2. 基本类型修改

#### StringValue.cs
- **当前实现**：直接返回字符串值，不带引号
- **修改后**：返回带引号的字符串，符合 Old8Lang 语法

#### 其他基本类型（IntValue, DoubleValue, BoolValue, CharValue）
- 保持当前实现，因为它们已经生成符合 Old8Lang 风格的值

### 3. 复合类型修改

#### ListValue.cs
- **当前实现**：生成类似 C# 风格的列表
- **修改后**：生成 Old8Lang 风格的列表，使用 `[ ]` 包裹，元素用逗号分隔

#### DictionaryValue.cs
- **当前实现**：生成 JSON 风格的字典
- **修改后**：生成 Old8Lang 风格的字典，使用 `{ }` 包裹，键值对用 `: ` 分隔

#### ArrayValue.cs
- **当前实现**：生成 C# 风格的数组
- **修改后**：生成 Old8Lang 风格的数组，使用 `[ ]` 包裹

#### TupleValue.cs
- **当前实现**：`$"({Item1},{Item2})"`
- **修改后**：保持不变，因为已经符合 Old8Lang 风格

### 4. 中间类型修改

#### ArgList.cs, IdList.cs
- **修改要点**：生成 Old8Lang 风格的参数列表

#### RangeValue.cs, SliceValue.cs
- **修改要点**：生成 Old8Lang 风格的范围和切片表达式

#### VoidValue.cs
- **当前实现**：返回空字符串
- **修改后**：保持不变

## 实现细节

### 1. 首先修改核心类
- Operation.cs
- Instance.cs
- FuncValue.cs

### 2. 然后修改复合类型
- ListValue.cs
- DictionaryValue.cs
- ArrayValue.cs

### 3. 接着修改基本类型
- StringValue.cs

### 4. 最后修改中间类型
- ArgList.cs
- IdList.cs
- RangeValue.cs
- SliceValue.cs

## 验证

- 修改完成后，运行 `dotnet build` 验证编译通过
- 确保修改后的 ToString 方法生成的代码符合 Old8Lang 语法规范

## 预期结果

所有 Expression 目录下的类的 ToString 方法都将生成 Old8Lang 风格的代码字符串，与之前修改的 Statement 目录下的类保持一致。