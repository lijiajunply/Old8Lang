# Old8Lang CLI 指南

本文档介绍 Old8Lang 命令行工具的使用方法，包括运行代码、编译、测试以及包管理功能。

## 基础运行命令

Old8Lang 提供了多种运行模式，可以通过 `Old8Lang.App` 项目直接运行。

### 前置条件
确保已安装 .NET 10.0 SDK，并在项目根目录下执行：
```bash
dotnet build Old8Lang.sln
```

### 1. 解释器模式 (Interpreter Mode)
逐行解释执行代码，适合开发和调试。

```bash
dotnet run --project Old8Lang.App -- -f <文件路径> [参数]
```

**示例**:
```bash
dotnet run --project Old8Lang.App -- -f scripts/hello.old8
```

### 2. 编译模式 (Compiler Mode)
将代码编译为 IL 并执行，性能更高，但要求更严格的类型注解。

```bash
dotnet run --project Old8Lang.App -- -c <文件路径> [参数]
```

**示例**:
```bash
dotnet run --project Old8Lang.App -- -c scripts/benchmark.old8
```

### 3. 语法检查 (Syntax Check)
仅解析代码语法，不执行。用于验证代码格式。

```bash
dotnet run --project Old8Lang.App -- -s <文件路径>
```

### 4. 虚拟机模式 (Virtual Machine Mode)
使用 Old8Lang 虚拟机执行字节码（实验性功能）。

```bash
dotnet run --project Old8Lang.App -- -vm <文件路径>
```

### 5. 调试模式 (Debug Mode)
启用调试输出，查看详细的执行日志。

```bash
# 配合其他模式使用，添加 -d 参数
dotnet run --project Old8Lang.App -- -f scripts/test.old8 -d
```

## 执行模式对比 (Execution Modes Comparison)

Old8Lang 支持三种主要执行模式，每种模式有不同的特点和适用场景：

### 模式对比表

| 特性 | 解释模式 (`-f`) | 编译模式 (`-c`) | VM 模式 (`-vm`) |
|------|----------------|----------------|----------------|
| **启动速度** | 快 | 慢（需编译） | 中等 |
| **运行性能** | 中等 | 高 | 中等偏高 |
| **类型系统** | 动态类型 | 静态类型 | 动态类型 |
| **类型注解** | 可选 | 必需 | 可选 |
| **泛型支持** | ✅ | ❌ | ✅ |
| **运算符重载** | ✅ | ❌ | ✅ |
| **Python 互操作** | ✅ | ❌ | ✅ |
| **调试支持** | 基础 | 基础 | 高级（内置调试器） |
| **性能分析** | 无 | 无 | ✅ |
| **跨平台分发** | 需源代码 | 需源代码 | ✅ 字节码 |
| **完成度** | 90-95% | 70-85% | 90-95% |

### 模式选择指南

#### 解释模式 (`-f`) - 推荐用于开发

**适用场景**:
- 快速开发和原型验证
- 脚本和自动化任务
- 需要动态特性（泛型、运算符重载）
- 学习和调试语言特性

**示例**:
```bash
# 快速运行脚本
dotnet run --project Old8Lang.App -- -f scripts/automation.old8

# 使用泛型函数
dotnet run --project Old8Lang.App -- -f examples/generics_demo.old8

# 运算符重载示例
dotnet run --project Old8Lang.App -- -f examples/operator_overload.old8
```

#### 编译模式 (`-c`) - 推荐用于生产

**适用场景**:
- 生产环境部署
- 性能关键的应用
- 需要静态类型保证
- 长时间运行的服务

**要求**:
- 必须提供完整的类型注解
- 不支持泛型函数和类
- 不支持运算符重载

**示例**:
```bash
# 高性能计算
dotnet run --project Old8Lang.App -- -c scripts/performance_critical.old8

# 生产服务
dotnet run --project Old8Lang.App -- -c services/api_server.old8
```

#### VM 模式 (`-vm`) - 推荐用于分发和调试 ⚠️ 实验性

**适用场景**:
- 跨平台分发（一次编译，到处运行）
- 需要高级调试功能（断点、单步、变量查看）
- 性能分析和优化
- 沙箱执行环境

**特点**:
- 支持字节码序列化
- 内置调试器和性能分析器
- 完整的语言特性支持

**示例**:
```bash
# 直接执行
dotnet run --project Old8Lang.App -- -vm scripts/app.old8

# 编译为字节码
dotnet run --project Old8Lang.App -- compile-bytecode scripts/app.old8 -o app.o8bc

# 执行字节码
dotnet run --project Old8Lang.App -- execute-bytecode app.o8bc
```

**⚠️ 注意**: VM 模式目前处于实验阶段，虽然功能已完整实现，但建议在生产环境中谨慎使用。

### 性能对比示例

以下是三种模式在相同任务下的性能对比（仅供参考）：

```bash
# 测试脚本: 计算前 1000000 个数字的和

# 解释模式
dotnet run --project Old8Lang.App -- -f benchmark/sum.old8
# 预期时间: ~2-3 秒

# 编译模式
dotnet run --project Old8Lang.App -- -c benchmark/sum_typed.old8
# 预期时间: ~0.5-1 秒

# VM 模式
dotnet run --project Old8Lang.App -- -vm benchmark/sum.old8
# 预期时间: ~1-1.5 秒
```

---

## 包管理 CLI 命令

Old8Lang 提供了完整的包管理 CLI 命令，支持包打包、签名、验证和证书管理等功能。

> **注意**: 以下命令假设您已将 `Old8Lang.App` 安装为全局工具 `old8lang`。
> 如果未安装，可以使用 `dotnet run --project Old8Lang.App -- <command>` 替代 `old8lang <command>`。
> 例如: `old8lang publish` 等同于 `dotnet run --project Old8Lang.App -- publish`。

### 命令列表

### 🚀 推荐：`publish` - 一键发布命令

**最简单的发布方式！** 自动完成打包、签名和准备发布的全部流程。

#### 用法
```bash
old8lang publish [选项]
```

#### 选项
- `-o, --output <目录>` - 输出目录（默认 `./dist`）
- `-c, --cert <路径>` - 证书文件路径
- `-p, --password <密码>` - 证书密码
- `--auto-cert` - 自动生成自签名证书（仅用于开发/测试）
- `--cert-name <名称>` - 自动生成证书时的主题名称
- `--cert-email <邮箱>` - 自动生成证书时的电子邮件
- `--no-sign` - 跳过签名步骤（不推荐）
- `--skip-validation` - 跳过发布前验证
- `-h, --help` - 显示帮助信息

#### 发布流程
1. 读取项目配置（o8packages.json）
2. 验证包结构和元数据
3. 打包为 .o8pkg 文件
4. 签名包文件（可选）
5. 输出发布文件到目标目录

#### 示例
```bash
# 基本发布（使用现有证书）
old8lang publish -c my-cert.pfx -p mypassword

# 发布到指定目录
old8lang publish -o ./release -c my-cert.pfx

# 使用自动生成的证书（开发环境）
old8lang publish --auto-cert --cert-name "Dev Publisher"

# 不签名发布（不推荐）
old8lang publish --no-sign
```

#### 优势
- ✅ **一键完成** - 无需手动执行多个命令
- ✅ **自动验证** - 确保包结构正确
- ✅ **智能提示** - 交互式引导完成发布
- ✅ **证书管理** - 自动生成或使用现有证书
- ✅ **完整摘要** - 清晰展示发布结果

---

### 1. `pack` - 打包命令

将包文件夹打包成 `.o8pkg` 压缩包文件。

#### 用法
```bash
old8lang pack [选项] <源路径>
```

#### 参数
- `<源路径>` - 包文件夹路径（必需）

#### 选项
- `-o, --output <路径>` - 输出文件路径（可选，默认在源路径旁边生成）
- `-v, --validate` - 仅验证包结构，不执行打包
- `-h, --help` - 显示帮助信息

#### 示例
```bash
# 打包到默认位置
old8lang pack ./my-package

# 指定输出路径
old8lang pack ./my-package -o dist/package.o8pkg

# 仅验证包结构
old8lang pack ./my-package -v
```

#### 包结构要求
- 必须包含 `package.json` 元数据文件
- 建议包含 `lib` 文件夹存放库文件
- 支持的入口文件（优先级）：
  1. `index.old8`
  2. `{packageName}.old8`
  3. `main.old8`

---

### 2. `unpack` - 解包命令

解包 `.o8pkg` 文件到指定目录。

#### 用法
```bash
old8lang unpack [选项] <包文件>
```

#### 参数
- `<包文件>` - .o8pkg 包文件路径（必需）

#### 选项
- `-o, --output <路径>` - 解包目标目录（可选，默认为当前目录下的包名文件夹）
- `-f, --force` - 强制覆盖已存在的目录
- `-h, --help` - 显示帮助信息

#### 示例
```bash
# 解包到默认位置
old8lang unpack package.o8pkg

# 解包到指定目录
old8lang unpack package.o8pkg -o ./extracted

# 强制覆盖已存在的目录
old8lang unpack package.o8pkg -f
```

---

### 3. `sign` - 签名命令

对包文件进行数字签名。

#### 用法
```bash
old8lang sign [选项] <包文件>
```

#### 参数
- `<包文件>` - .o8pkg 包文件路径（必需）

#### 选项
- `-c, --cert <路径>` - 证书文件路径（.pfx 或 .cer）
- `-p, --password <密码>` - 证书密码（用于加密的 .pfx 文件）
- `-o, --output <路径>` - 签名文件输出路径（默认为 `<包文件>.sig`）
- `--auto-cert` - 自动生成自签名证书
- `--cert-name <名称>` - 自动生成证书时的主题名称
- `--cert-email <邮箱>` - 自动生成证书时的电子邮件
- `-h, --help` - 显示帮助信息

#### 示例
```bash
# 使用现有证书签名
old8lang sign package.o8pkg -c my-cert.pfx -p mypassword

# 自动生成证书并签名
old8lang sign package.o8pkg --auto-cert --cert-name "John Doe" --cert-email john@example.com

# 指定签名文件输出路径
old8lang sign package.o8pkg -c my-cert.pfx -o custom.sig
```

#### 签名算法
- **数字签名**: RSA-SHA256
- **哈希算法**: SHA256
- **证书格式**: X.509

---

### 4. `verify` - 验证签名命令

验证包文件的数字签名。

#### 用法
```bash
old8lang verify [选项] <包文件>
```

#### 参数
- `<包文件>` - .o8pkg 包文件路径（必需）

#### 选项
- `-s, --signature <路径>` - 签名文件路径（默认为 `<包文件>.sig`）
- `-v, --verbose` - 显示详细信息
- `-h, --help` - 显示帮助信息

#### 示例
```bash
# 使用默认签名文件验证
old8lang verify package.o8pkg

# 指定签名文件
old8lang verify package.o8pkg -s custom.sig

# 显示详细信息
old8lang verify package.o8pkg -v
```

#### 验证结果
- ✓ **签名验证通过** - 包文件未被篡改，可安全使用
- ✗ **签名验证失败** - 包文件可能已被篡改或签名损坏

---

### 5. `cert` - 证书管理命令

证书管理工具，支持生成、查看和导出证书。

#### 子命令

##### 5.1 `generate` - 生成自签名证书

```bash
old8lang cert generate [选项]
```

**选项**：
- `-n, --name <名称>` - 证书主题名称（必需）
- `-e, --email <邮箱>` - 电子邮件地址（可选）
- `-y, --years <年数>` - 有效期（年，默认 5 年）
- `-o, --output <路径>` - 输出文件路径（默认 `certificate.pfx`）
- `-p, --password <密码>` - 证书密码（用于 .pfx 文件）

**示例**：
```bash
old8lang cert generate -n "John Doe" -e john@example.com -o my-cert.pfx -p mypassword
```

##### 5.2 `info` - 查看证书信息

```bash
old8lang cert info [选项]
```

**选项**：
- `-c, --cert <路径>` - 证书文件路径（必需）
- `-p, --password <密码>` - 证书密码（用于加密的 .pfx 文件）

**示例**：
```bash
old8lang cert info -c my-cert.pfx -p mypassword
```

##### 5.3 `export` - 导出证书

```bash
old8lang cert export [选项]
```

**选项**：
- `-c, --cert <路径>` - 输入证书文件路径（必需）
- `-o, --output <路径>` - 输出文件路径（必需）
- `-p, --password <密码>` - 输入证书密码
- `--out-password <密码>` - 输出证书密码（仅适用于 .pfx）

**示例**：
```bash
# 导出为公钥格式（.cer）
old8lang cert export -c my-cert.pfx -p mypassword -o public-cert.cer
```

---

## 调试和性能分析命令 (Debugging and Profiling Commands)

Old8Lang 提供了强大的调试和性能分析工具，特别是在 VM 模式下支持高级调试功能。

### 1. `debug-start` - 启动调试会话

启动 Old8Lang 调试器，支持断点、单步执行和变量查看。

#### 用法
```bash
old8lang debug-start [选项] <文件路径>
```

#### 参数
- `<文件路径>` - 要调试的 Old8Lang 文件（必需）

#### 选项
- `-m, --mode <模式>` - 执行模式（`vm`, `interpreter`，默认 `vm`）
- `-p, --port <端口>` - 调试服务器端口（默认 5858）
- `-b, --break-on-start` - 在第一行代码处暂停
- `-h, --help` - 显示帮助信息

#### 示例
```bash
# 启动 VM 模式调试
old8lang debug-start app.old8

# 在第一行暂停
old8lang debug-start app.old8 --break-on-start

# 使用解释模式调试
old8lang debug-start app.old8 -m interpreter

# 指定调试端口
old8lang debug-start app.old8 -p 9229
```

#### 调试器功能
- ✅ **断点管理** - 设置、删除、列出断点
- ✅ **单步执行** - step-in, step-over, step-out
- ✅ **变量查看** - 查看局部变量、全局变量、调用栈
- ✅ **表达式求值** - 在断点处求值表达式
- ✅ **调用栈追踪** - 查看完整的函数调用栈

---

### 2. `debug-breakpoint` - 断点管理

管理调试断点（添加、删除、列出）。

#### 用法
```bash
old8lang debug-breakpoint <操作> [参数]
```

#### 操作
- `add <文件>:<行号>` - 添加断点
- `remove <ID>` - 删除断点
- `list` - 列出所有断点
- `clear` - 清除所有断点

#### 示例
```bash
# 添加断点
old8lang debug-breakpoint add app.old8:10
old8lang debug-breakpoint add app.old8:25

# 列出断点
old8lang debug-breakpoint list

# 删除断点
old8lang debug-breakpoint remove 1

# 清除所有断点
old8lang debug-breakpoint clear
```

---

### 3. `debug-control` - 调试控制

控制调试会话的执行流程。

#### 用法
```bash
old8lang debug-control <操作>
```

#### 操作
- `continue` - 继续执行到下一个断点
- `step-in` - 单步进入（进入函数内部）
- `step-over` - 单步跳过（不进入函数）
- `step-out` - 跳出当前函数
- `pause` - 暂停执行
- `stop` - 停止调试会话

#### 示例
```bash
# 继续执行
old8lang debug-control continue

# 单步进入
old8lang debug-control step-in

# 单步跳过
old8lang debug-control step-over

# 跳出函数
old8lang debug-control step-out

# 暂停执行
old8lang debug-control pause

# 停止调试
old8lang debug-control stop
```

---

### 4. `profile` - 性能分析

分析代码性能，生成详细的性能报告。

#### 用法
```bash
old8lang profile [选项] <文件路径>
```

#### 参数
- `<文件路径>` - 要分析的 Old8Lang 文件（必需）

#### 选项
- `-m, --mode <模式>` - 执行模式（`vm`, `interpreter`, `compiler`，默认 `vm`）
- `-o, --output <路径>` - 报告输出路径（默认 `profile-report.json`）
- `-f, --format <格式>` - 报告格式（`json`, `html`, `text`，默认 `json`）
- `--samples <数量>` - 采样次数（默认 1000）
- `--include-memory` - 包含内存分析
- `--include-gc` - 包含 GC 统计
- `-h, --help` - 显示帮助信息

#### 示例
```bash
# 基本性能分析
old8lang profile app.old8

# 生成 HTML 报告
old8lang profile app.old8 -f html -o report.html

# 包含内存和 GC 分析
old8lang profile app.old8 --include-memory --include-gc

# 对比不同模式的性能
old8lang profile app.old8 -m interpreter -o interpreter-profile.json
old8lang profile app.old8 -m compiler -o compiler-profile.json
old8lang profile app.old8 -m vm -o vm-profile.json
```

#### 性能报告内容
- **执行时间** - 总执行时间、函数级别时间
- **函数调用统计** - 调用次数、平均时间、最大/最小时间
- **热点分析** - 最耗时的函数和代码行
- **内存使用** - 内存分配、峰值内存、GC 统计（可选）
- **调用图** - 函数调用关系图

---

### 5. 调试器交互式命令

在调试会话中，可以使用以下交互式命令：

#### 断点命令
```
break <文件>:<行号>    # 设置断点
delete <ID>            # 删除断点
list                   # 列出断点
```

#### 执行控制
```
continue (c)           # 继续执行
step (s)               # 单步进入
next (n)               # 单步跳过
finish (f)             # 跳出函数
```

#### 变量查看
```
print <变量名>         # 打印变量值
locals                 # 显示局部变量
globals                # 显示全局变量
watch <表达式>         # 监视表达式
```

#### 调用栈
```
backtrace (bt)         # 显示调用栈
frame <编号>           # 切换栈帧
up                     # 向上移动栈帧
down                   # 向下移动栈帧
```

#### 其他命令
```
help                   # 显示帮助
quit (q)               # 退出调试器
```

---

### 6. 调试示例工作流

#### 场景 1: 调试程序错误

```bash
# 1. 启动调试器
old8lang debug-start app.old8 --break-on-start

# 2. 在交互式调试器中：
(old8lang-debugger) break app.old8:15    # 设置断点
(old8lang-debugger) continue              # 继续执行到断点

# 3. 检查变量
(old8lang-debugger) locals                # 查看局部变量
(old8lang-debugger) print myVariable      # 打印特定变量

# 4. 单步执行
(old8lang-debugger) step                  # 单步进入
(old8lang-debugger) next                  # 单步跳过

# 5. 查看调用栈
(old8lang-debugger) backtrace             # 显示调用栈

# 6. 退出
(old8lang-debugger) quit
```

#### 场景 2: 性能优化

```bash
# 1. 运行性能分析
old8lang profile app.old8 -f html -o profile.html --include-memory

# 2. 打开 HTML 报告查看热点

# 3. 针对热点函数进行优化

# 4. 重新分析对比
old8lang profile app.old8 -f html -o profile-optimized.html

# 5. 对比两次报告，验证优化效果
```

#### 场景 3: 内存泄漏检测

```bash
# 运行带内存分析的性能分析
old8lang profile app.old8 --include-memory --include-gc -o memory-profile.json

# 查看报告中的内存增长趋势和 GC 统计
# 识别可能的内存泄漏点
```

---

### 7. 调试最佳实践

#### 使用 VM 模式调试
VM 模式提供最完整的调试支持：
```bash
# 推荐：使用 VM 模式调试
old8lang debug-start app.old8 -m vm
```

#### 设置条件断点
在代码中使用 `debugger` 语句：
```old8lang
func processData(data) {
    if data.Length() > 1000 {
        debugger  // 仅在数据量大时触发断点
    }
    // 处理数据...
}
```

#### 使用日志辅助调试
结合 `PrintLine` 和调试器：
```old8lang
func calculate(x, y) {
    PrintLine("Debug: x=" + x.ToStr() + ", y=" + y.ToStr())
    result <- x * y + 10
    PrintLine("Debug: result=" + result.ToStr())
    return result
}
```

#### 性能分析技巧
1. **先整体后局部** - 先分析整体性能，再针对热点优化
2. **对比测试** - 优化前后都运行性能分析，对比效果
3. **多次采样** - 增加采样次数以获得更准确的结果
4. **关注内存** - 内存问题往往比 CPU 问题更难发现

---

## 完整工作流示例

### 🌟 场景 0: 使用 publish 命令快速发布（推荐）

```bash
# 进入项目目录
cd my-package

# 一键发布（使用现有证书）
old8lang publish -c my-cert.pfx -o ./release

# 或者首次发布时自动生成证书
old8lang publish --auto-cert --cert-name "John Doe" --cert-email john@example.com

# 发布文件自动生成在 dist 或指定目录：
# - my-package.1.0.0.o8pkg
# - my-package.1.0.0.o8pkg.sig
```

**优势**: 一条命令完成所有步骤，自动验证、打包、签名！

---

### 场景 1: 手动打包并签名发布包（高级用户）

```bash
# 1. 创建包文件夹（假设已有 package.json）
cd my-package

# 2. 验证包结构
old8lang pack . -v

# 3. 打包
old8lang pack . -o ../my-package.1.0.0.o8pkg

# 4. 生成证书（首次）
old8lang cert generate -n "Package Publisher" -e publisher@example.com -o publisher.pfx

# 5. 签名包
old8lang sign ../my-package.1.0.0.o8pkg -c publisher.pfx

# 6. 验证签名
old8lang verify ../my-package.1.0.0.o8pkg -v

# 7. 发布文件
# - my-package.1.0.0.o8pkg
# - my-package.1.0.0.o8pkg.sig
```

### 场景 2: 验证并安装包

```bash
# 1. 下载包和签名文件
# - downloaded-package.o8pkg
# - downloaded-package.o8pkg.sig

# 2. 验证签名
old8lang verify downloaded-package.o8pkg -v

# 3. 如果验证通过，解包
old8lang unpack downloaded-package.o8pkg -o ~/.old8lang/packages/package-name

# 4. 安装依赖（如果需要）
cd ~/.old8lang/packages/package-name
old8lang install
```

### 场景 3: 证书管理

```bash
# 1. 生成开发证书
old8lang cert generate -n "Dev Certificate" -e dev@company.com -o dev-cert.pfx -y 1

# 2. 查看证书信息
old8lang cert info -c dev-cert.pfx

# 3. 导出公钥（用于分发）
old8lang cert export -c dev-cert.pfx -o dev-cert-public.cer

# 4. 生成生产证书（长期有效）
old8lang cert generate -n "Production Certificate" -e production@company.com -o prod-cert.pfx -y 10
```

---

## 安全最佳实践

### 证书管理
1. **使用强密码保护 .pfx 文件**
   ```bash
   # 使用复杂密码
   old8lang cert generate -n "My Name" -o cert.pfx -p "MyStr0ng!P@ssw0rd"
   ```

2. **定期更新证书**
   - 开发环境：1 年有效期
   - 生产环境：5-10 年有效期
   - 在证书到期前更新

3. **妥善保管私钥**
   - 不要将 .pfx 文件提交到版本控制系统
   - 使用安全的密钥管理服务
   - 限制证书文件的访问权限

### 包签名
1. **始终签名发布的包**
   ```bash
   old8lang sign package.o8pkg -c production-cert.pfx
   ```

2. **验证第三方包**
   ```bash
   # 安装前验证
   old8lang verify third-party-package.o8pkg -v
   ```

3. **检查证书信息**
   - 验证签名者身份
   - 检查证书有效期
   - 确认证书颁发者

### 包完整性
1. **保持签名文件与包文件配对**
   ```
   package.o8pkg
   package.o8pkg.sig  ← 必须一起分发
   ```

2. **验证包的校验和**
   - 包元数据中包含 SHA256 校验和
   - 签名验证会自动检查完整性

---

## 错误处理

### 常见错误及解决方案

#### 1. 包结构验证失败
```bash
错误: 包结构验证失败: Missing required file: package.json
```
**解决方案**: 确保包文件夹中存在 `package.json` 文件。

#### 2. 证书密码错误
```bash
错误: 无法加载证书
```
**解决方案**: 检查证书密码是否正确，或使用交互式输入密码。

#### 3. 签名验证失败
```bash
✗ 签名验证失败
可能的原因:
  1. 包文件已被篡改
  2. 签名文件损坏
  3. 签名与包文件不匹配
```
**解决方案**:
- 重新下载包和签名文件
- 联系包发布者确认
- 不要使用未验证通过的包

#### 4. 证书已过期
```bash
⚠ 警告: 签名证书已过期
```
**解决方案**:
- 联系包发布者获取新版本
- 如果是自己的证书，重新生成并重新签名

---

## 配置文件

### package.json 示例

```json
{
  "id": "my-package",
  "version": "1.0.0",
  "description": "My awesome package",
  "author": "John Doe <john@example.com>",
  "license": "MIT",
  "main": "index.old8",
  "tags": ["utility", "helper"],
  "dependencies": {
    "another-package": "^1.0.0"
  },
  "devDependencies": {
    "test-framework": "^2.0.0"
  }
}
```

---

## 相关文档

- [PackageService API 文档](./PACKAGE_SERVICE_USAGE.md)
- [包格式规范](./PACKAGE_FORMAT.md)
- [证书管理最佳实践](./CERTIFICATE_BEST_PRACTICES.md)
- [Old8Lang 项目配置](./PROJECT_CONFIG.md)

---

## 附录

### 文件扩展名

| 扩展名 | 说明 |
|--------|------|
| `.o8pkg` | Old8Lang 包文件（ZIP 压缩格式） |
| `.sig` | 包签名文件（JSON 格式） |
| `.pfx` | 包含私钥的证书文件（需要密码保护） |
| `.cer` | 公钥证书文件（可公开分发） |
| `.old8` | Old8Lang 源代码文件 |

### 默认路径

| 描述 | 默认路径 |
|------|----------|
| 全局包目录 | `~/.old8lang/packages/` |
| 项目包目录 | `<项目根目录>/packages/` |
| 包配置文件 | `o8packages.json` |
| 包元数据 | `package.json` |

### 支持的证书格式

- **PFX/PKCS#12** (`.pfx`) - 包含私钥和公钥，需要密码保护
- **X.509 Certificate** (`.cer`, `.crt`) - 仅包含公钥，可公开分发
- **PEM Format** - 文本格式，支持导入导出

---

## 获取帮助

```bash
# 查看所有命令
old8lang -h

# 查看特定命令帮助
old8lang pack -h
old8lang sign -h
old8lang verify -h
old8lang cert -h

# 查看子命令帮助
old8lang cert generate -h
```

## 反馈与贡献

如有问题或建议，请访问项目仓库。
