# Old8Lang 虚拟环境和项目管理系统设计

**版本**: 1.0
**日期**: 2025-12-24
**状态**: 设计阶段

---

## 📋 概述

设计一个类似于 **Python venv + npm** 的包管理系统，支持：
- ✅ 虚拟环境隔离（项目级别的包管理）
- ✅ 项目配置文件（类似 package.json）
- ✅ 版本锁定（固定 Old8Lang 版本和依赖版本）
- ✅ 全局包 + 本地包混合管理
- ✅ 依赖自动安装

---

## 🎯 设计目标

### 核心功能
1. **项目初始化**: `old8lang init` - 创建项目配置
2. **虚拟环境**: 项目本地的 `packages/` 目录
3. **配置文件**: `old8.project.json` - 记录项目信息和依赖
4. **锁文件**: `old8.lock.json` - 锁定精确版本
5. **包管理**: `old8lang install/add/remove` - 包操作
6. **版本锁定**: 固定 Old8Lang 运行时版本

### 用户体验目标
- 🎯 简单直观的命令行界面
- 🎯 自动依赖解析和安装
- 🎯 清晰的错误提示
- 🎯 与现有全局包系统兼容

---

## 📁 项目结构

### 标准项目结构
```
my-project/
├── old8.project.json          # 项目配置文件（必需）
├── old8.lock.json             # 版本锁定文件（自动生成）
├── packages/                   # 本地包目录（虚拟环境）
│   ├── Logger@1.2.0/
│   │   ├── package.json
│   │   └── Logger.old8
│   └── CustomLib@2.0.0/
│       ├── package.json
│       └── CustomLib.old8
├── src/                        # 源代码目录
│   ├── main.old8
│   └── utils.old8
├── tests/                      # 测试目录
│   └── test_main.old8
└── README.md
```

---

## 📄 配置文件格式

### old8.project.json

```json
{
  "name": "my-awesome-project",
  "version": "1.0.0",
  "description": "My awesome Old8Lang project",
  "author": "Your Name <your.email@example.com>",
  "license": "MIT",

  "old8lang": {
    "version": "^1.0.0",
    "runtime": "interpreter"
  },

  "main": "src/main.old8",

  "dependencies": {
    "Logger": "^1.2.0",
    "HttpClient": "^2.0.0"
  },

  "devDependencies": {
    "TestFramework": "^1.0.0"
  },

  "scripts": {
    "start": "old8lang run src/main.old8",
    "test": "old8lang run tests/test_main.old8",
    "build": "old8lang compile src/main.old8"
  },

  "repositories": [
    "https://packages.old8lang.org"
  ],

  "packageManager": {
    "useVirtualEnv": true,
    "packagesDir": "./packages"
  }
}
```

### old8.lock.json（自动生成）

```json
{
  "lockfileVersion": 1,
  "generated": "2025-12-24T15:30:00Z",

  "old8lang": {
    "version": "1.0.0",
    "hash": "sha256:abc123..."
  },

  "packages": {
    "Logger": {
      "version": "1.2.0",
      "resolved": "https://packages.old8lang.org/Logger-1.2.0.tar.gz",
      "integrity": "sha256:def456...",
      "dependencies": {}
    },
    "HttpClient": {
      "version": "2.0.0",
      "resolved": "https://packages.old8lang.org/HttpClient-2.0.0.tar.gz",
      "integrity": "sha256:ghi789...",
      "dependencies": {
        "Logger": "^1.0.0"
      }
    }
  }
}
```

---

## 🔧 命令行接口

### 1. 项目初始化

```bash
# 交互式创建项目
old8lang init

# 使用默认配置快速创建
old8lang init -y

# 指定模板
old8lang init --template=library
```

**交互式提示**:
```
? 项目名称: (my-project)
? 项目版本: (1.0.0)
? 项目描述:
? 作者:
? License: (MIT)
? 使用虚拟环境? (Y/n)
? Old8Lang 版本: (^1.0.0)
```

**生成结果**:
- ✅ 创建 `old8.project.json`
- ✅ 创建 `packages/` 目录
- ✅ 创建基本项目结构（可选）

### 2. 包安装

```bash
# 安装所有依赖（根据 old8.project.json）
old8lang install

# 添加新依赖
old8lang add Logger
old8lang add Logger@1.2.0
old8lang add Logger@^1.2.0

# 添加开发依赖
old8lang add --dev TestFramework

# 从全局安装（不加入 dependencies）
old8lang add --global Logger

# 安装并保存到项目
old8lang install Logger --save
```

**行为**:
- ✅ 下载包到 `packages/PackageName@version/`
- ✅ 更新 `old8.project.json` 的 dependencies
- ✅ 更新或创建 `old8.lock.json`
- ✅ 自动解析并安装依赖的依赖

### 3. 包移除

```bash
# 移除依赖
old8lang remove Logger

# 移除开发依赖
old8lang remove --dev TestFramework

# 仅从 node_modules 删除，保留配置
old8lang uninstall Logger
```

### 4. 包列表

```bash
# 列出所有依赖
old8lang list

# 树形显示依赖关系
old8lang list --tree

# 显示过时的包
old8lang outdated
```

**输出示例**:
```
my-project@1.0.0 /path/to/my-project
├── Logger@1.2.0
└── HttpClient@2.0.0
    └── Logger@1.2.0 (已安装)

Old8Lang: 1.0.0
```

### 5. 运行脚本

```bash
# 运行脚本
old8lang run start
old8lang run test

# 等价于
old8lang start
old8lang test
```

### 6. 虚拟环境管理

```bash
# 启用虚拟环境
old8lang venv enable

# 禁用虚拟环境（使用全局包）
old8lang venv disable

# 查看虚拟环境状态
old8lang venv status

# 清理虚拟环境
old8lang venv clean
```

### 7. 版本管理

```bash
# 检查 Old8Lang 版本
old8lang --version

# 检查项目使用的 Old8Lang 版本
old8lang version check

# 更新 Old8Lang（如果项目允许）
old8lang version update
```

---

## 🔍 包查找优先级（更新）

### 启用虚拟环境时

```
import "PackageName"
  ↓
1. 检查是否存在 old8.project.json
   ↓ 是
2. 读取项目配置，启用虚拟环境模式
   ↓
3. 查找顺序：
   a. 项目本地包：./packages/PackageName@version/
   b. 项目本地包（任意版本）：./packages/PackageName@*/
   c. 全局包：~/.old8lang/packages/PackageName/
   d. 标准库：Old8LangLib
   ↓
4. 版本检查：
   - 如果 old8.lock.json 存在，使用锁定版本
   - 如果不存在，使用 dependencies 中的版本范围
   ↓
5. 版本不匹配时：
   - 警告用户
   - 提示运行 old8lang install
```

### 未启用虚拟环境时（兼容模式）

```
import "PackageName"
  ↓
保持现有行为：
1. 全局包：~/.old8lang/packages/PackageName/
2. 当前目录：./packages/PackageName/
3. 执行文件附近的 packages/（向上5层）
4. 标准库
```

---

## 💻 实现计划

### Phase 1: 核心配置系统（本次实现）

**文件**: `Old8Lang/ProjectManagement/`

1. **ProjectConfig.cs** - 项目配置模型
   ```csharp
   public class ProjectConfig
   {
       public string Name { get; set; }
       public string Version { get; set; }
       public string Description { get; set; }
       public Old8LangConfig Old8Lang { get; set; }
       public Dictionary<string, string> Dependencies { get; set; }
       public Dictionary<string, string> DevDependencies { get; set; }
       public PackageManagerConfig PackageManager { get; set; }

       public static ProjectConfig? LoadFromDirectory(string directory);
       public void Save(string directory);
   }
   ```

2. **LockFile.cs** - 锁文件模型
   ```csharp
   public class LockFile
   {
       public int LockfileVersion { get; set; }
       public DateTime Generated { get; set; }
       public Dictionary<string, PackageLockInfo> Packages { get; set; }

       public static LockFile? LoadFromDirectory(string directory);
       public void Save(string directory);
   }
   ```

3. **VirtualEnvironment.cs** - 虚拟环境管理
   ```csharp
   public class VirtualEnvironment
   {
       public string ProjectRoot { get; }
       public ProjectConfig Config { get; }
       public LockFile? LockFile { get; }

       public bool IsEnabled { get; }
       public string PackagesDirectory { get; }

       public static VirtualEnvironment? Detect(string startPath);
       public List<string> GetPackageSearchPaths();
       public PackageInfo? ResolvePackage(string packageName);
   }
   ```

4. **PackageManager 更新** - 集成虚拟环境
   ```csharp
   public class PackageManager
   {
       private VirtualEnvironment? _virtualEnv;

       public PackageManager(string? packagesDir = null, string? projectRoot = null)
       {
           // 检测虚拟环境
           if (projectRoot != null)
           {
               _virtualEnv = VirtualEnvironment.Detect(projectRoot);
           }

           // 根据虚拟环境配置搜索路径
           if (_virtualEnv?.IsEnabled == true)
           {
               AddSearchPath(_virtualEnv.PackagesDirectory);
           }

           // 添加全局路径
           AddSearchPath(GetDefaultPackagesDirectory());
       }
   }
   ```

### Phase 2: CLI 命令实现

**文件**: `Old8Lang.App/Commands/`

1. **InitCommand.cs** - `old8lang init`
2. **AddCommand.cs** - `old8lang add`
3. **RemoveCommand.cs** - `old8lang remove`
4. **InstallCommand.cs** - `old8lang install`
5. **ListCommand.cs** - `old8lang list`
6. **VenvCommand.cs** - `old8lang venv`

### Phase 3: 包安装和解析

**文件**: `Old8Lang.PackageManagement/`

1. **PackageInstaller.cs** - 包下载和安装
2. **DependencyResolver.cs** - 依赖解析
3. **VersionMatcher.cs** - 语义版本匹配
4. **PackageRegistry.cs** - 包注册表客户端

---

## 🧪 测试场景

### 场景 1: 新项目创建

```bash
mkdir my-app && cd my-app
old8lang init -y
old8lang add Logger
old8lang add HttpClient

# 目录结构
my-app/
├── old8.project.json
├── old8.lock.json
└── packages/
    ├── Logger@1.2.0/
    └── HttpClient@2.0.0/
```

### 场景 2: 克隆项目

```bash
git clone https://github.com/user/old8-project.git
cd old8-project
old8lang install  # 根据 old8.lock.json 安装精确版本

# 所有依赖安装完成，版本一致
```

### 场景 3: 混合使用全局和本地包

```bash
# 全局安装常用库
old8lang add --global Logger

# 项目特定版本
cd my-project
old8lang add Logger@2.0.0  # 项目使用新版本

# import "Logger" 会优先使用项目本地的 2.0.0 版本
```

### 场景 4: 版本冲突处理

```bash
# old8.project.json: "Logger": "^1.0.0"
# old8.lock.json: "Logger": "1.2.0"
# packages/: Logger@1.5.0 (手动更新)

old8lang run main.old8
# 警告: Logger 版本不匹配
#   - 期望: 1.2.0 (锁定)
#   - 实际: 1.5.0
#   - 建议: 运行 old8lang install 修复

old8lang install  # 恢复到 1.2.0
```

---

## 📊 版本语义

支持 npm 风格的版本范围：

| 符号 | 含义 | 示例 | 匹配版本 |
|------|------|------|----------|
| `1.2.0` | 精确版本 | `1.2.0` | 1.2.0 |
| `^1.2.0` | 兼容版本 | `^1.2.0` | ≥1.2.0, <2.0.0 |
| `~1.2.0` | 补丁版本 | `~1.2.0` | ≥1.2.0, <1.3.0 |
| `>=1.2.0` | 最小版本 | `>=1.2.0` | ≥1.2.0 |
| `1.x` | 主版本 | `1.x` | ≥1.0.0, <2.0.0 |
| `*` | 任意版本 | `*` | 最新版本 |

---

## 🔐 安全性考虑

1. **包完整性验证**: 使用 SHA256 校验和
2. **HTTPS 传输**: 所有包下载使用 HTTPS
3. **签名验证**: 包签名验证（未来）
4. **依赖审计**: `old8lang audit` 检查已知漏洞（未来）

---

## 🚀 迁移路径

### 从现有项目迁移

```bash
# 1. 进入现有项目
cd my-old-project

# 2. 初始化项目配置
old8lang init

# 3. 自动检测 packages/ 目录并生成配置
old8lang migrate

# 4. 生成的 old8.project.json 包含检测到的依赖
```

### 向后兼容

- ✅ 没有 `old8.project.json` 时使用传统模式
- ✅ 全局包始终可用
- ✅ 现有代码无需修改

---

## 📈 未来扩展

1. **Workspace 支持**: Monorepo 多项目管理
2. **私有仓库**: 企业内部包仓库
3. **包镜像**: 加速下载（类似 npm 淘宝镜像）
4. **构建工具集成**: 与编译器深度集成
5. **IDE 插件**: VSCode/Rider 插件支持

---

## 📚 参考资料

- **npm**: package.json, package-lock.json
- **Python venv**: 虚拟环境隔离
- **Cargo**: 语义版本和锁文件
- **Go modules**: 依赖管理和版本控制

---

**设计完成，等待实现！**
