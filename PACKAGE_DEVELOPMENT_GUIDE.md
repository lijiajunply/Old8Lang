# Old8Lang 包开发和发布指南

## 📖 目录

1. [包结构](#包结构)
2. [创建包](#创建包)
3. [package.json 配置](#packagejson-配置)
4. [包依赖管理](#包依赖管理)
5. [测试包](#测试包)
6. [发布包](#发布包)
7. [最佳实践](#最佳实践)

## 📦 包结构

一个标准的 Old8Lang 包应该包含以下结构：

```
MyPackage/
├── o8package.json         # 包元数据（必需）
├── index.old8           # 主入口文件（或在 package.json 中指定）
├── README.md            # 包文档
├── LICENSE              # 许可证（默认为 MIT）
├── src/                 # 源代码目录（可选）
│   ├── utils.old8
│   └── helper.old8
├── test/                # 测试目录（可选）
└── dll/                 # 预编译文件目录/需要的 C# 原生dll文件（可选）
```

## 🚀 创建包

### 步骤 1：初始化包结构

```bash
mkdir MyPackage
cd MyPackage
old8lang package init
```

这会创建一个基本的 `package.json` 文件。

### 步骤 2：编写主入口文件

创建 `index.old8`：

```old8
// MyPackage/index.old8

// 导出函数
func greet(name:string) -> string {
    return "Hello, " + name + "!"
}

// 导出常量
version:const <- "1.0.0"

// 导出类
class Helper {
    public func calculate(a:int, b:int) -> int {
        return a + b
    }
}

// 包初始化代码（可选）
PrintLine("MyPackage loaded successfully") //不会显示
```

## 📝 package.json 配置

### 基本配置

```json
{
  "id": "MyPackage",
  "version": "1.0.0",
  "description": "一个示例 Old8Lang 包",
  "author": "Your Name",
  "license": "MIT",
  "main": "index.old8",
  "keywords": ["utility", "helper", "example"],
  "repository": {
    "type": "git",
    "url": "https://github.com/username/MyPackage"
  },
  "dependencies": []
}
```

### 完整配置选项

```json
{
  "id": "MyPackage",                    // 包名（必需，唯一标识符）
  "version": "1.0.0",                   // 版本号（必需，遵循 SemVer）
  "description": "包描述",               // 简短描述
  "author": "作者名",                   // 作者信息
  "license": "MIT",                     // 许可证
  "main": "index.old8",                 // 主入口文件
  "keywords": ["关键词1", "关键词2"],    // 搜索关键词
  "homepage": "https://...",            // 项目主页
  "repository": {                       // 代码仓库
    "type": "git",
    "url": "https://github.com/..."
  },
  "dependencies": [                     // 依赖包列表
    {
      "id": "Logger",
      "version": "^1.2.0",
      "optional": false
    }
  ],
  "devDependencies": [                  // 开发依赖
    {
      "id": "TestFramework",
      "version": "^2.0.0"
    }
  ],
  "framework": "old8lang-1.0",          // 目标框架
  "engines": {                          // 运行环境要求
    "old8lang": ">=1.0.0"
  }
}
```

## 📚 包依赖管理

### 添加依赖

在 `package.json` 中添加依赖：

```json
{
  "dependencies": [
    {
      "id": "Logger",
      "version": "^1.2.0"
    },
    {
      "id": "HttpClient",
      "version": "~2.0.0"
    }
  ]
}
```

### 版本范围说明

- `1.2.3` - 精确版本
- `^1.2.3` - 兼容版本（允许次版本和补丁版本更新）
- `~1.2.3` - 补丁版本（只允许补丁版本更新）
- `>=1.2.3` - 大于等于指定版本
- `*` - 任意版本（不推荐）

### 在代码中使用依赖

```old8
// 导入依赖包
import "Logger"

// 使用包中的功能
Logger.info("这是一条日志消息")
```

## 🧪 测试包

### 1. 本地测试

在开发过程中进行本地测试：

```bash
# 在包目录中
cd MyPackage

# 测试主入口文件
old8lang -f index.old8

# 测试特定功能
old8lang -f test/test_greet.old8
```

### 2. 在其他项目中测试

```bash
# 在另一个项目中
old8lang init                    # 初始化项目
old8lang add ../MyPackage@1.0.0  # 添加本地包
old8lang restore                 # 恢复依赖
```

创建测试文件：

```old8
// test_usage.old8
import "MyPackage"

result <- MyPackage.greet("World")
PrintLine(result)  // 输出: Hello, World!
```

运行测试：

```bash
old8lang -f test_usage.old8
```

## 📤 发布包

### 准备发布

1. **更新版本号**（在 `package.json` 中）
2. **更新 README.md** - 确保文档完整
3. **检查依赖** - 确保所有依赖都已声明
4. **运行测试** - 确保所有测试通过

### 发布到本地仓库

```bash
# 打包
cd MyPackage
old8lang package pack

# 发布到本地仓库
old8lang package publish --local

# 或发布到全局
old8lang package publish --global
```

###发布到远程仓库（未来功能）

```bash
# 登录包管理器
old8lang package login

# 发布包
old8lang package publish
```

## ✨ 最佳实践

### 1. 命名规范

- **包名**：使用 PascalCase（如 `MyPackage`）
- **函数名**：使用 camelCase（如 `greet`, `calculateTotal`）
- **类名**：使用 PascalCase（如 `Helper`, `DataProcessor`）
- **常量**：使用 camelCase 或 UPPER_CASE

### 2. 导出规范

明确标记公共 API：

```old8
// ✅ 好的做法
public func publicFunction() -> void {
    privateHelper()
}

func privateHelper() -> void {
    // 内部使用，不导出
}

// ❌ 不好的做法 - 所有内容都默认导出
func shouldBePrivate() -> void {
    // 这会被导出，可能造成命名冲突
}
```

### 3. 依赖管理

- 尽量减少依赖数量
- 使用精确的版本范围
- 避免循环依赖

### 4. 文档规范

在 README.md 中包含：

- 安装说明
- 使用示例
- API 文档
- 贡献指南
- 许可证信息

### 5. 版本控制

遵循 [语义化版本](https://semver.org/lang/zh-CN/)：

- **主版本号**（Major）：不兼容的 API 变更
- **次版本号**（Minor）：向后兼容的功能添加
- **修订号**（Patch）：向后兼容的问题修正

## 📋 示例：完整的包

### package.json

```json
{
  "id": "StringUtils",
  "version": "1.0.0",
  "description": "字符串处理工具集",
  "author": "Old8Lang Team",
  "license": "MIT",
  "main": "index.old8",
  "keywords": ["string", "utility", "text"],
  "homepage": "https://github.com/old8lang/StringUtils",
  "repository": {
    "type": "git",
    "url": "https://github.com/old8lang/StringUtils.git"
  },
  "dependencies": [],
  "framework": "old8lang-1.0",
  "engines": {
    "old8lang": ">=1.0.0"
  }
}
```

### index.old8

```old8
// StringUtils 包 - 字符串处理工具集

// ============================================
// 公共 API
// ============================================

/// 反转字符串
/// @param str 要反转的字符串
/// @return 反转后的字符串
public func reverse(str:string) -> string {
    result <- ""
    for i <- str.Length - 1; i >= 0; i <- i - 1 {
        result <- result + str[i]
    }
    return result
}

/// 将字符串转换为标题格式（每个单词首字母大写）
/// @param str 要转换的字符串
/// @return 标题格式的字符串
public func toTitleCase(str:string) -> string {
    words <- str.Split(" ")
    result <- {}

    for word in words {
        if word.Length > 0 {
            firstChar <- word[0].ToUpper()
            rest <- word.Substring(1).ToLower()
            result.Add(firstChar + rest)
        }
    }

    return result.Join(" ")
}

/// 统计字符串中某个子串出现的次数
/// @param str 源字符串
/// @param substring 要查找的子串
/// @return 出现次数
public func count(str:string, substring:string) -> int {
    if substring.Length == 0 {
        return 0
    }

    count <- 0
    index <- 0

    while true {
        pos <- str.IndexOf(substring, index)
        if pos == -1 {
            break
        }
        count <- count + 1
        index <- pos + substring.Length
    }

    return count
}

// ============================================
// 包信息
// ============================================

public VERSION <- "1.0.0"
public AUTHOR <- "Old8Lang Team"

// 包初始化
PrintLine("StringUtils v" + VERSION + " loaded")
```

### README.md

```markdown
# StringUtils

Old8Lang 字符串处理工具集。

## 安装

\`\`\`bash
old8lang add StringUtils
\`\`\`

## 使用

\`\`\`old8
import "StringUtils"

// 反转字符串
reversed <- StringUtils.reverse("Hello")
PrintLine(reversed)  // olleH

// 转换为标题格式
title <- StringUtils.toTitleCase("hello world")
PrintLine(title)  // Hello World

// 统计子串出现次数
count <- StringUtils.count("banana", "na")
PrintLine(count)  // 2
\`\`\`

## API 文档

### `reverse(str: string) -> string`
反转字符串。

### `toTitleCase(str: string) -> string`
将字符串转换为标题格式（每个单词首字母大写）。

### `count(str: string, substring: string) -> int`
统计字符串中某个子串出现的次数。

## 许可证

MIT
\`\`\`

## 🔗 相关资源

- [Old8Lang 官方文档](https://github.com/old8lang/old8lang)
- [Old8Lang.PackageManager.Core](https://www.nuget.org/packages/Old8Lang.PackageManager.Core/)
- [包管理器集成指南](./INTEGRATION_GUIDE.md)
- [语义化版本规范](https://semver.org/lang/zh-CN/)

## 💡 提示

- 使用 `old8lang package init` 快速创建包结构
- 使用 `old8lang add` 添加依赖
- 使用 `old8lang restore` 恢复所有依赖
- 使用 `old8lang list` 查看已安装的包

---

**祝您开发愉快！** 🎉
