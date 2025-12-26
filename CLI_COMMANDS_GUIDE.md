# Old8Lang 包管理 CLI 命令指南

## 概述

Old8Lang 现在提供了完整的包管理 CLI 命令，支持包打包、签名、验证和证书管理等功能。

## 命令列表

### 🚀 推荐：`old8lang publish` - 一键发布命令

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

### 1. `old8lang pack` - 打包命令

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

### 2. `old8lang unpack` - 解包命令

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

### 3. `old8lang sign` - 签名命令

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

### 4. `old8lang verify` - 验证签名命令

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

### 5. `old8lang cert` - 证书管理命令

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

如有问题或建议，请访问: https://github.com/your-org/Old8Lang
