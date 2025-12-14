# 完善FileLib支持更多文件格式

## 1. 现状分析
当前FileLib.cs提供了基本的文件操作功能：
- FileRead/FileReadLines：文本文件读取
- CopyFile：文件复制
- UnpackZip/CompressZip/ZipReadAll：ZIP文件操作

项目中已有JsonLib.cs提供JSON文件处理功能。

## 2. 改进计划

### 2.1 增强基本文件操作
- 添加文件写入功能（FileWrite/FileWriteLines）
- 添加文件追加功能（FileAppend/FileAppendLines）
- 添加文件删除功能（DeleteFile）
- 添加文件重命名功能（RenameFile）
- 添加文件信息获取功能（GetFileInfo）

### 2.2 支持更多文件格式
- 添加CSV文件支持：
  - ReadCsv：读取CSV文件为二维数组
  - WriteCsv：将二维数组写入CSV文件
- 添加XML文件支持：
  - ReadXml：读取XML文件
  - WriteXml：写入XML文件
- 添加YAML文件支持：
  - ReadYaml：读取YAML文件
  - WriteYaml：写入YAML文件

### 2.3 增强现有功能
- 为现有方法添加编码支持
- 优化错误处理
- 添加文件存在检查方法（FileExists）

## 3. 实现细节

### 3.1 基本文件操作
- 使用System.IO命名空间的现有方法
- 保持与现有代码风格一致
- 添加适当的异常处理

### 3.2 CSV文件支持
- 实现简单的CSV解析和生成
- 处理引号和逗号转义

### 3.3 XML文件支持
- 使用System.Xml命名空间
- 提供简单的API接口

### 3.4 YAML文件支持
- 需要添加YAML库依赖（如YamlDotNet）
- 实现序列化和反序列化功能

## 4. 测试计划
- 为新功能添加单元测试
- 在InterpreterTests和CompilerTests中添加测试用例
- 确保现有功能不受影响

## 5. 依赖管理
- 对于YAML支持，需要添加YamlDotNet包依赖

## 6. 实现顺序
1. 增强基本文件操作
2. 添加CSV文件支持
3. 添加XML文件支持
4. 添加YAML文件支持
5. 编写测试用例
6. 优化和修复bug