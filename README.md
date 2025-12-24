# Old8Lang

为西安建筑科技大学专门写的一个编程语言(bushi

西安建筑科技大学：https://baike.baidu.com/item/西安建筑科技大学/345895

西建大又被誉为“老八校”

## 语言特性

1. 语法简单，不支持oop的高级功能：多态，泛型
2. 使用变量储存器来储存变量
3. 支持原生Json操作和更灵活的类型转换
4. 动态类型语言，支持弱类型
5. 有两种模式：解释模式和编译模式。解释模式可以使用更灵活的场景，编译模式则更适合于性能要求高的场景
6. 智能包管理：支持项目级虚拟环境和全局包自动检测

## 包管理系统

### 虚拟环境（项目模式）
当检测到 `o8packages.json` 文件时，自动启用项目级包隔离：
```bash
old8lang init myproject
cd myproject
old8lang run src/main.old8  # 自动使用项目级包
```

### 全局包模式（非项目模式）
当没有项目配置时，自动使用全局包：
```bash
# 在任意目录运行单个文件
old8lang run script.old8     # 自动使用全局包
```

### 包导入语法
```old8
// 导入标准库
import "MathLib"
import "OS"

// 导入第三方包（全局或项目级）
import "ThirdPartyLib"

// 使用别名
import "Logger" as log
```

### 标准库
- **MathLib**: 数学函数（sqrt, abs, max, min）
- **OS**: 操作系统功能（OsInfo, Process）
- **File**: 文件操作
- **Terminal**: 终端控制
- **Time**: 时间处理
- **更多**: String, Collection, Crypto, Json, Vector 等

## 更新记录

请查看 [CHANGELOG.md](./CHANGELOG.md) 文件获取详细的更新记录。

## 开发人员
1. LuckyFish

本项目归LuckyFish和西安建筑科技大学iOS众创空间俱乐部所有。