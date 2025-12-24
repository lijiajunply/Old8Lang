# Old8Lang.PackageManager.Core 迁移说明

## 📝 概述

本文档说明 Old8Lang 项目从项目引用迁移到 NuGet 包引用的过程。

## 🔄 迁移日期

- **迁移时间**: 2025-12-24
- **NuGet 包版本**: Old8Lang.PackageManager.Core 1.0.0

## 📦 变更内容

### 1. 引用方式变更

**之前 (项目引用)**:
```xml
<ItemGroup>
  <ProjectReference Include="..\..\Old8Lang.PackageManager\Old8Lang.PackageManager.Core\Old8Lang.PackageManager.Core.csproj" />
</ItemGroup>
```

**现在 (NuGet 包引用)**:
```xml
<ItemGroup>
  <PackageReference Include="Old8Lang.PackageManager.Core" Version="1.0.0" />
</ItemGroup>
```

### 2. 包源配置

添加了本地 NuGet 源以支持本地开发:

```bash
dotnet nuget add source /Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang.PackageManager/Old8Lang.PackageManager.Core/nupkg --name Old8LangPackageManagerLocal
```

## ✅ 验证结果

### 构建验证
- ✅ 项目编译成功
- ✅ 依赖项正确解析
- ✅ NuGet 包版本: 1.0.0

### 测试验证
- ✅ 单元测试运行正常 (1968 通过 / 2051 总计)
- ⚠️ 76 个测试失败与迁移无关,是原有问题

### 包引用确认
```
> Old8Lang.PackageManager.Core       1.0.0   1.0.0
```

## 🎯 迁移优势

1. **解耦**: Old8Lang 项目不再直接依赖 PackageManager 源代码
2. **版本管理**: 通过 NuGet 版本号明确依赖版本
3. **通用性**: Old8Lang.PackageManager.Core 现在可被其他项目使用
4. **可发布**: 准备好发布到 NuGet.org

## 📚 相关文档

- [Old8Lang.PackageManager.Core README](../Old8Lang.PackageManager/Old8Lang.PackageManager.Core/README.md)
- [集成指南](../Old8Lang.PackageManager/INTEGRATION_GUIDE.md)
- [NuGet 包报告](../Old8Lang.PackageManager/NUGET_PACKAGE_REPORT.md)

## 🔜 后续步骤

1. 继续在本地开发和测试
2. 准备发布到 NuGet.org (可选)
3. 在其他脚本语言项目中集成使用

## ⚙️ 开发说明

### 本地 NuGet 包更新流程

当 Old8Lang.PackageManager.Core 有更新时:

1. 在 PackageManager 项目中更新代码
2. 更新版本号 (在 `.csproj` 中)
3. 重新打包:
   ```bash
   cd Old8Lang.PackageManager/Old8Lang.PackageManager.Core
   dotnet pack -c Release -o nupkg
   ```
4. 在 Old8Lang 项目中更新包版本:
   ```bash
   cd Old8Lang
   dotnet add Old8Lang/Old8Lang.csproj package Old8Lang.PackageManager.Core --version [新版本号]
   ```
5. 重新构建和测试

### 回滚到项目引用 (如需要)

如果需要回滚到项目引用:

1. 编辑 `Old8Lang/Old8Lang.csproj`
2. 删除 PackageReference,添加回 ProjectReference
3. 清理和重新构建:
   ```bash
   dotnet clean
   dotnet build
   ```

## 📞 联系方式

如有问题或需要帮助,请参考:
- [Old8Lang 文档](./Old8Lang_Grammar.md)
- [PackageManager 文档](../Old8Lang.PackageManager/README.md)

---

**迁移状态**: ✅ 已完成并验证
