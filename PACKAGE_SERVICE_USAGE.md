# PackageService 使用文档

## 概述

`PackageService` 现在集成了 `Old8Lang.PackageManager.Core` 的 `PackageArchiveService` 和 `PackageSignatureService`，提供完整的包管理、打包和签名功能。

## 功能列表

### 1. 包安装与管理

#### 安装包
```csharp
var service = new PackageService(projectRoot);
var result = await service.RestorePackagesAsync(projectConfig);
Console.WriteLine($"已安装: {result.InstalledCount}, 跳过: {result.SkippedCount}, 失败: {result.FailedCount}");
```

#### 获取已安装的包
```csharp
var packages = await service.GetInstalledPackagesAsync();
foreach (var package in packages)
{
    Console.WriteLine($"{package.Id} v{package.Version}");
}
```

#### 卸载包
```csharp
bool success = await service.UninstallPackageAsync("packageId", "1.0.0");
```

### 2. 包打包功能

#### 打包包文件夹
```csharp
var service = new PackageService(projectRoot);

// 验证包结构
var (isValid, message) = await service.ValidatePackageStructureAsync(packageFolder);
if (!isValid)
{
    Console.WriteLine($"包结构无效: {message}");
    return;
}

// 打包
string outputPath = await service.PackAsync(
    sourcePath: packageFolder,
    outputPath: "my-package.1.0.0.o8pkg"  // 可选，不指定则自动生成
);

Console.WriteLine($"包已打包到: {outputPath}");
```

#### 解包
```csharp
await service.UnpackAsync(
    packagePath: "my-package.1.0.0.o8pkg",
    destinationPath: "/path/to/extract"
);
```

#### 读取包元数据
```csharp
var package = await service.ReadPackageMetadataAsync(packageFolder);
if (package != null)
{
    Console.WriteLine($"包名: {package.Id}");
    Console.WriteLine($"版本: {package.Version}");
    Console.WriteLine($"作者: {package.Author}");
    Console.WriteLine($"描述: {package.Description}");
}
```

### 3. 包签名功能

#### 生成自签名证书
```csharp
var service = new PackageService(projectRoot);

// 生成证书
var certificate = service.GenerateSelfSignedCertificate(
    subjectName: "My Package Publisher",
    email: "publisher@example.com",
    validityYears: 5
);

// 导出证书（带密码保护）
await service.ExportCertificateAsync(
    certificate: certificate,
    outputPath: "my-certificate.pfx",
    password: "secure-password"
);

// 查看证书信息
Console.WriteLine(service.GetCertificateInfo(certificate));
```

#### 签名包文件
```csharp
// 加载证书
var certificate = await service.LoadCertificateAsync(
    certPath: "my-certificate.pfx",
    password: "secure-password"
);

// 签名包
var signature = await service.SignPackageAsync(
    packagePath: "my-package.1.0.0.o8pkg",
    certificate: certificate
);

// 保存签名文件
await service.WriteSignatureAsync(
    signature: signature,
    signatureFilePath: "my-package.1.0.0.o8pkg.sig"
);

Console.WriteLine($"包已签名");
Console.WriteLine($"签名者: {signature.Signer.Name}");
Console.WriteLine($"签名时间: {signature.Timestamp}");
```

#### 验证包签名
```csharp
// 读取签名
var signature = await service.ReadSignatureAsync("my-package.1.0.0.o8pkg.sig");
if (signature == null)
{
    Console.WriteLine("签名文件不存在");
    return;
}

// 验证签名
bool isValid = await service.VerifySignatureAsync(
    packagePath: "my-package.1.0.0.o8pkg",
    signature: signature
);

if (isValid)
{
    Console.WriteLine("✓ 签名验证通过");
    Console.WriteLine($"签名者: {signature.Signer.Name}");
    Console.WriteLine($"证书指纹: {signature.Signer.CertificateThumbprint}");
}
else
{
    Console.WriteLine("✗ 签名验证失败");
}
```

## 完整工作流示例

### 发布包的完整流程

```csharp
using Old8Lang.App.Services;
using Old8Lang.ProjectManagement;

async Task PublishPackageAsync(string packageFolder, string publisherName, string publisherEmail)
{
    var service = new PackageService(Environment.CurrentDirectory);

    // 1. 验证包结构
    Console.WriteLine("正在验证包结构...");
    var (isValid, message) = await service.ValidatePackageStructureAsync(packageFolder);
    if (!isValid)
    {
        Console.WriteLine($"✗ 包结构无效: {message}");
        return;
    }
    Console.WriteLine("✓ 包结构有效");

    // 2. 读取包信息
    var package = await service.ReadPackageMetadataAsync(packageFolder);
    Console.WriteLine($"包名: {package.Id} v{package.Version}");

    // 3. 打包
    Console.WriteLine("正在打包...");
    string packagePath = await service.PackAsync(packageFolder);
    Console.WriteLine($"✓ 包已创建: {packagePath}");

    // 4. 生成或加载证书
    Console.WriteLine("正在准备证书...");
    var certPath = "publisher.pfx";
    var certPassword = "your-secure-password";

    X509Certificate2 certificate;
    if (File.Exists(certPath))
    {
        certificate = await service.LoadCertificateAsync(certPath, certPassword);
    }
    else
    {
        certificate = service.GenerateSelfSignedCertificate(publisherName, publisherEmail);
        await service.ExportCertificateAsync(certificate, certPath, certPassword);
        Console.WriteLine($"✓ 证书已生成: {certPath}");
    }

    // 5. 签名包
    Console.WriteLine("正在签名包...");
    var signature = await service.SignPackageAsync(packagePath, certificate);
    var signaturePath = $"{packagePath}.sig";
    await service.WriteSignatureAsync(signature, signaturePath);
    Console.WriteLine($"✓ 包已签名: {signaturePath}");

    Console.WriteLine("\n包发布准备完成！");
    Console.WriteLine($"包文件: {packagePath}");
    Console.WriteLine($"签名文件: {signaturePath}");
}

// 使用示例
await PublishPackageAsync(
    packageFolder: "/path/to/my-package",
    publisherName: "John Doe",
    publisherEmail: "john@example.com"
);
```

### 安装并验证包

```csharp
async Task InstallAndVerifyPackageAsync(string packagePath, string signaturePath)
{
    var service = new PackageService(Environment.CurrentDirectory);

    // 1. 验证签名
    Console.WriteLine("正在验证包签名...");
    var signature = await service.ReadSignatureAsync(signaturePath);
    if (signature == null)
    {
        Console.WriteLine("⚠ 警告: 未找到签名文件");
    }
    else
    {
        bool isValid = await service.VerifySignatureAsync(packagePath, signature);
        if (isValid)
        {
            Console.WriteLine("✓ 签名验证通过");
            Console.WriteLine($"  签名者: {signature.Signer.Name}");
            Console.WriteLine($"  签名时间: {signature.Timestamp}");

            // 检查证书有效期
            if (signature.Signer.NotAfter < DateTime.UtcNow)
            {
                Console.WriteLine("  ⚠ 警告: 证书已过期");
            }
        }
        else
        {
            Console.WriteLine("✗ 签名验证失败！");
            Console.WriteLine("建议不要安装此包。");
            return;
        }
    }

    // 2. 解包到临时目录
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Console.WriteLine($"正在解包到: {tempDir}");
    await service.UnpackAsync(packagePath, tempDir);

    // 3. 读取包信息
    var package = await service.ReadPackageMetadataAsync(tempDir);
    Console.WriteLine($"\n包信息:");
    Console.WriteLine($"  名称: {package.Id}");
    Console.WriteLine($"  版本: {package.Version}");
    Console.WriteLine($"  作者: {package.Author}");
    Console.WriteLine($"  描述: {package.Description}");

    // 4. 复制到包目录
    var packagesDir = service.PackagesDirectory;
    var targetDir = Path.Combine(packagesDir, $"{package.Id}@{package.Version}");

    if (Directory.Exists(targetDir))
    {
        Console.WriteLine($"包已存在: {targetDir}");
    }
    else
    {
        Directory.Move(tempDir, targetDir);
        Console.WriteLine($"✓ 包已安装到: {targetDir}");
    }
}

// 使用示例
await InstallAndVerifyPackageAsync(
    packagePath: "my-package.1.0.0.o8pkg",
    signaturePath: "my-package.1.0.0.o8pkg.sig"
);
```

## 注意事项

1. **证书管理**：
   - 生产环境应使用受信任的证书颁发机构（CA）签发的证书
   - 自签名证书仅适用于测试和开发
   - 妥善保管私钥文件和密码

2. **包结构要求**：
   - 必须包含 `package.json` 元数据文件
   - 建议包含 `lib` 文件夹存放库文件
   - 入口文件优先级：`index.old8` > `{packageName}.old8` > `main.old8`

3. **签名验证**：
   - 始终在安装前验证包签名
   - 检查证书有效期
   - 验证签名者身份

4. **版本管理**：
   - 遵循语义化版本规范（SemVer）
   - 包文件名格式：`{packageId}.{version}.o8pkg`
   - 签名文件名格式：`{packageFile}.sig`

## 相关类型

### PackageSignature 结构
```csharp
public class PackageSignature
{
    public string Algorithm { get; set; }          // 签名算法（如 "RSA-SHA256"）
    public string SignatureData { get; set; }      // Base64 编码的签名数据
    public DateTimeOffset Timestamp { get; set; }  // 签名时间戳
    public string PackageHash { get; set; }        // 包文件哈希值
    public string HashAlgorithm { get; set; }      // 哈希算法（如 "SHA256"）
    public SignerInfo Signer { get; set; }         // 签名者信息
}

public class SignerInfo
{
    public string CertificateThumbprint { get; set; }  // 证书指纹
    public string PublicKey { get; set; }              // 公钥（PEM 格式）
    public string Name { get; set; }                   // 签名者姓名
    public string Email { get; set; }                  // 签名者邮箱
    public DateTime NotBefore { get; set; }            // 证书生效时间
    public DateTime NotAfter { get; set; }             // 证书过期时间
}
```

### Package 结构
```csharp
public class Package
{
    public string Id { get; set; }               // 包标识符
    public string Version { get; set; }          // 版本号
    public string Description { get; set; }      // 描述
    public string Author { get; set; }           // 作者
    public List<string> Tags { get; set; }       // 标签
    public string License { get; set; }          // 许可证
    public string Repository { get; set; }       // 仓库 URL
    public long Size { get; set; }               // 文件大小（字节）
    public string Checksum { get; set; }         // 校验和
    public DateTime? PublishedAt { get; set; }   // 发布时间
}
```

## 相关文档

- [Old8Lang.PackageManager.Core 文档](https://github.com/your-repo/Old8Lang.PackageManager)
- [包格式规范](./PACKAGE_FORMAT.md)
- [证书管理最佳实践](./CERTIFICATE_BEST_PRACTICES.md)
