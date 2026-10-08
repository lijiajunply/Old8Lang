# Old8Lang 环境管理指南

## 概述

Old8Lang 支持开发环境和生产环境的配置管理，允许您为不同的运行环境设置不同的配置、环境变量和行为。

## 环境类型

Old8Lang 支持任意命名的环境，但通常使用以下标准环境：

- **development** (开发环境): 用于本地开发和调试
- **production** (生产环境): 用于生产部署
- **test** (测试环境): 用于自动化测试
- **staging** (预发布环境): 用于预发布测试

## 环境配置方式

### 1. 项目配置文件 (o8package.json)

在 `o8package.json` 中定义环境配置：

```json
{
  "name": "myapp",
  "version": "1.0.0",
  "old8lang": {
    "version": "^1.0.0",
    "runtime": "interpreter",
    "environments": {
      "development": {
        "debug": true,
        "logLevel": "debug",
        "runtime": "interpreter",
        "env": {
          "API_URL": "http://localhost:3000",
          "DB_HOST": "localhost"
        },
        "dependencies": {
          "DebugTools": "1.0.0"
        },
        "scripts": {
          "start": "old8lang run src/main.old8 -d"
        }
      },
      "production": {
        "debug": false,
        "logLevel": "error",
        "runtime": "compiler",
        "env": {
          "API_URL": "https://api.production.com",
          "DB_HOST": "prod-db.example.com"
        }
      }
    }
  }
}
```

### 2. 环境变量文件

创建环境变量文件来存储敏感信息和环境特定配置：

#### `.old8env` (通用环境变量)
```bash
# 所有环境共享的配置
APP_NAME=MyOld8App
APP_VERSION=1.0.0
```

#### `.old8env.development` (开发环境)
```bash
# 开发环境特定配置
DEBUG=true
LOG_LEVEL=debug
API_URL=http://localhost:3000
DB_HOST=localhost
DB_PORT=5432
DB_NAME=old8lang_dev
```

#### `.old8env.production` (生产环境)
```bash
# 生产环境特定配置
DEBUG=false
LOG_LEVEL=error
API_URL=https://api.production.com
DB_HOST=prod-db.example.com
DB_PORT=5432
DB_NAME=old8lang_prod
```

#### `.old8env.local` (本地覆盖，不提交到版本控制)
```bash
# 本地开发者特定配置（不应提交到 Git）
DB_PASSWORD=my_local_password
API_KEY=my_dev_api_key
```

### 3. 系统环境变量

系统环境变量具有最高优先级，会覆盖文件中的配置：

```bash
# Windows
set OLD8_ENV=production
set API_KEY=secret_key

# Linux/Mac
export OLD8_ENV=production
export API_KEY=secret_key
```

## 环境变量加载优先级

环境变量按以下优先级加载（后者覆盖前者）：

1. `.old8env` (通用配置)
2. `.old8env.{environment}` (环境特定配置)
3. `o8package.json` 中的 `environments.{env}.env`
4. `.old8env.local` (本地覆盖)
5. 系统环境变量 (最高优先级)

## 使用环境

### 指定运行环境

使用 `--env` 参数指定环境：

```bash
# 开发环境运行
old8lang -f src/main.old8 --env development

# 生产环境运行
old8lang -f src/main.old8 --env production

# 使用环境变量指定
export OLD8_ENV=production
old8lang -f src/main.old8
```

### 在代码中读取环境变量

使用 `GetEnv()` 函数读取环境变量：

```old8
// 获取环境变量
var apiUrl = GetEnv("API_URL") ?? "http://localhost:3000"
var debug = GetEnv("DEBUG") ?? "false"

PrintLine($"API URL: {apiUrl}")

// 根据环境执行不同逻辑
if (debug == "true") {
    PrintLine("[DEBUG] 调试模式已启用")
} else {
    PrintLine("[INFO] 生产模式运行")
}
```

## 环境管理命令

### 初始化环境配置

```bash
# 创建示例环境配置文件
old8lang env init
```

这会创建以下文件：
- `.old8env` - 通用环境变量
- `.old8env.development` - 开发环境配置
- `.old8env.production` - 生产环境配置
- `.gitignore` - 忽略敏感文件

### 列出所有环境

```bash
# 查看项目中配置的所有环境
old8lang env list
```

输出示例：
```
可用环境:

* development
    运行时: interpreter
    调试: true
    日志级别: debug

  production
    运行时: compiler
    调试: false
    日志级别: error

当前环境: development
```

### 显示环境配置

```bash
# 显示当前环境配置
old8lang env show

# 显示指定环境配置
old8lang env show production
```

输出示例：
```
环境: production
运行时模式: compiler
调试模式: false
日志级别: error

环境变量:
  API_URL=https://api.production.com
  DB_HOST=prod-db.example.com
  DEBUG=false
  LOG_LEVEL=error
```

### 设置环境变量

```bash
# 设置环境变量（保存到 .old8env.local）
old8lang env set API_KEY my_secret_key
old8lang env set DB_PASSWORD my_password
```

### 获取环境变量

```bash
# 获取环境变量的值
old8lang env get API_URL
```

## 项目初始化

使用 `init` 命令创建新项目时，会自动创建环境配置：

```bash
# 创建新项目
old8lang init

# 使用默认配置快速创建
old8lang init -y
```

生成的 `o8package.json` 会包含默认的开发和生产环境配置。

## 环境特定的依赖

可以为不同环境配置不同的依赖包：

```json
{
  "dependencies": {
    "Logger": "1.0.0",
    "HttpClient": "2.0.0"
  },
  "old8lang": {
    "environments": {
      "development": {
        "dependencies": {
          "DebugTools": "1.0.0",
          "MockServer": "1.5.0"
        }
      },
      "production": {
        "dependencies": {
          "Monitoring": "2.0.0"
        }
      }
    }
  }
}
```

## 环境特定的脚本

为不同环境定义不同的脚本命令：

```json
{
  "scripts": {
    "start": "old8lang run src/main.old8",
    "dev": "old8lang run src/main.old8 --env development",
    "prod": "old8lang run src/main.old8 --env production",
    "test": "old8lang run tests/test_main.old8 --env test"
  },
  "old8lang": {
    "environments": {
      "development": {
        "scripts": {
          "watch": "old8lang run src/main.old8 -d --watch"
        }
      }
    }
  }
}
```

## 最佳实践

### 1. 不要提交敏感信息

将包含敏感信息的文件添加到 `.gitignore`：

```gitignore
# 环境文件
.old8env.local
.old8env.*.local

# 敏感配置
config/secrets.json
*.key
*.pem
```

### 2. 使用环境变量存储敏感信息

不要在代码中硬编码敏感信息：

```old8
// ❌ 不好的做法
var apiKey = "sk_live_1234567890abcdef"

// ✅ 好的做法
var apiKey = GetEnv("API_KEY") ?? throw "API_KEY not set"
```

### 3. 为每个环境提供示例配置

创建 `.old8env.example` 文件作为模板：

```bash
# .old8env.example
API_URL=http://localhost:3000
DB_HOST=localhost
DB_PORT=5432
DB_NAME=myapp_dev
API_KEY=your_api_key_here
```

### 4. 验证必需的环境变量

在应用启动时验证必需的环境变量：

```old8
// 验证必需的环境变量
var requiredVars = ["API_URL", "DB_HOST", "API_KEY"]

for (var varName in requiredVars) {
    var value = GetEnv(varName)
    if (value == null) {
        throw $"Required environment variable '{varName}' is not set"
    }
}

PrintLine("✓ 所有必需的环境变量已设置")
```

### 5. 使用不同的运行时模式

开发环境使用解释模式以获得更快的启动速度，生产环境使用 IL 模式以获得更好的性能：

```json
{
  "old8lang": {
    "runtime": "interpreter",
    "environments": {
      "development": {
        "runtime": "interpreter"
      },
      "production": {
        "runtime": "compiler"
      }
    }
  }
}
```

## 示例项目结构

```
myproject/
├── o8package.json          # 项目配置
├── .old8env                # 通用环境变量
├── .old8env.development    # 开发环境配置
├── .old8env.production     # 生产环境配置
├── .old8env.local          # 本地配置（不提交）
├── .old8env.example        # 配置模板
├── .gitignore              # Git 忽略规则
├── README.md               # 项目文档
├── src/
│   └── main.old8           # 主程序
└── tests/
    └── test_main.old8      # 测试文件
```

## 故障排除

### 环境变量未生效

1. 检查环境变量文件是否存在
2. 确认文件格式正确（KEY=VALUE）
3. 检查是否有系统环境变量覆盖
4. 使用 `old8lang env show` 查看实际加载的配置

### 找不到项目配置

确保在项目根目录（包含 `o8package.json` 的目录）或其子目录中运行命令。

### 环境切换不生效

使用 `--env` 参数明确指定环境，或设置 `OLD8_ENV` 环境变量。

## 相关命令

- `old8lang init` - 初始化项目（包含环境配置）
- `old8lang env init` - 创建环境配置文件
- `old8lang env list` - 列出所有环境
- `old8lang env show [env]` - 显示环境配置
- `old8lang env set <key> <value>` - 设置环境变量
- `old8lang env get <key>` - 获取环境变量
- `old8lang -f <file> --env <env>` - 指定环境运行

## 总结

Old8Lang 的环境管理系统提供了灵活的配置方式，支持：

- ✅ 多环境配置（开发、生产、测试等）
- ✅ 环境变量文件（.old8env）
- ✅ 环境特定的依赖和脚本
- ✅ 运行时模式切换
- ✅ 优先级明确的配置加载
- ✅ 敏感信息保护

通过合理使用环境管理功能，可以让您的 Old8Lang 项目在不同环境下平滑运行。
