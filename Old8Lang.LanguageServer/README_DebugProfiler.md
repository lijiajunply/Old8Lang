# Old8Lang Language Server - 调试和性能分析功能

## 概述

Old8Lang.LanguageServer 现已集成调试模式和性能分析功能，为 VS Code 等编辑器提供强大的调试和性能分析支持。

## 架构组成

### 1. 核心服务 (`Services/DebugProfilerService.cs`)

统一管理调试会话和性能分析会话的核心服务：

- **调试会话管理**：支持多个并发调试会话（按文档 URI 区分）
- **性能分析会话管理**：支持多个并发性能分析会话
- **会话生命周期**：启动、停止、查询状态

### 2. 数据模型 (`Models/ProfilerModels.cs`)

定义了完整的请求/响应模型，实现 LSP 协议兼容：

#### 性能分析相关
- `StartProfilingRequest` - 启动性能分析请求
- `StopProfilingRequest` - 停止性能分析请求
- `GetProfilingStatusRequest` - 获取性能分析状态请求
- `ProfilerSessionStatusResponse` - 性能分析会话状态响应
- `PerformanceReportResponse` - 性能报告响应
- `SessionSummaryInfo` - 会话摘要信息
- `HotFunctionInfo` - 热点函数信息

#### 调试相关
- `StartDebuggingRequest` - 启动调试请求
- `StopDebuggingRequest` - 停止调试请求
- `DebugSessionStatusResponse` - 调试会话状态响应
- `DebugEventInfo` - 调试事件信息
- `PositionInfo` - 位置信息

#### 断点操作
- `BreakpointRequest` - 断点操作请求（添加/移除）

#### 调试控制
- `DebugControlRequest` - 调试控制请求（继续/暂停/单步）

### 3. LSP Handler (`Handlers/DebugProfilerHandler.cs`)

实现了 8 个自定义 LSP 命令：

#### 性能分析命令
1. **`old8lang/startProfiling`** - 启动性能分析
   - 请求: `StartProfilingRequest`
   - 响应: `ProfilerSessionStatusResponse`

2. **`old8lang/stopProfiling`** - 停止性能分析并生成报告
   - 请求: `StopProfilingRequest`
   - 响应: `PerformanceReportResponse`

3. **`old8lang/getProfilingStatus`** - 查询性能分析状态
   - 请求: `GetProfilingStatusRequest`
   - 响应: `ProfilerSessionStatusResponse`

#### 调试命令
4. **`old8lang/startDebugging`** - 启动调试会话
   - 请求: `StartDebuggingRequest`
   - 响应: `DebugSessionStatusResponse`

5. **`old8lang/stopDebugging`** - 停止调试会话
   - 请求: `StopDebuggingRequest`
   - 响应: `DebugSessionStatusResponse`

#### 断点操作命令
6. **`old8lang/addBreakpoint`** - 添加断点
   - 请求: `BreakpointRequest`
   - 响应: `bool`

7. **`old8lang/removeBreakpoint`** - 移除断点
   - 请求: `BreakpointRequest`
   - 响应: `bool`

#### 调试控制命令
8. **`old8lang/debugControl`** - 调试控制（继续/暂停/单步执行）
   - 请求: `DebugControlRequest`
   - 响应: `bool`

## 使用示例

### 性能分析示例

#### 1. 启动性能分析

```typescript
// VS Code Extension 中调用
const response = await languageClient.sendRequest('old8lang/startProfiling', {
    uri: 'file:///path/to/script.old8',
    sessionName: 'MyProfilingSession',
    executionMode: 'interpreter' // 或 'compiler'
});

console.log('Session ID:', response.sessionId);
console.log('Is Profiling:', response.isProfiling);
```

#### 2. 获取性能分析状态

```typescript
const status = await languageClient.sendRequest('old8lang/getProfilingStatus', {
    uri: 'file:///path/to/script.old8'
});

console.log('Duration:', status.durationMs, 'ms');
console.log('Function Calls:', status.functionCallCount);
console.log('Data Points:', status.dataPointCount);
```

#### 3. 停止性能分析并获取报告

```typescript
const report = await languageClient.sendRequest('old8lang/stopProfiling', {
    uri: 'file:///path/to/script.old8'
});

console.log('Report Format:', report.format);
console.log('Generated At:', report.generatedAt);
console.log('Markdown Report:\n', report.content);

// 访问摘要信息
console.log('Total Execution Time:', report.summary.totalExecutionTimeMs, 'ms');
console.log('Total Function Calls:', report.summary.totalFunctionCalls);
console.log('Peak Memory:', report.summary.peakMemoryMb, 'MB');

// 查看热点函数
report.summary.hotFunctions.forEach((fn, index) => {
    console.log(`${index + 1}. ${fn.name}`);
    console.log(`   - Calls: ${fn.callCount}`);
    console.log(`   - Total Time: ${fn.totalTimeMs}ms`);
    console.log(`   - Average Time: ${fn.averageTimeMs}ms`);
});
```

### 调试示例

#### 1. 启动调试会话

```typescript
const response = await languageClient.sendRequest('old8lang/startDebugging', {
    uri: 'file:///path/to/script.old8',
    executionMode: 'interpreter'
});

console.log('Is Debugging:', response.isDebugging);
console.log('State:', response.state);
console.log('Breakpoint Count:', response.breakpointCount);
console.log('Call Stack Depth:', response.callStackDepth);
```

#### 2. 添加断点

```typescript
// 添加行断点
const added = await languageClient.sendRequest('old8lang/addBreakpoint', {
    uri: 'file:///path/to/script.old8',
    line: 42,
    condition: null // 可选：条件表达式
});

console.log('Breakpoint added:', added);

// 添加条件断点
const addedConditional = await languageClient.sendRequest('old8lang/addBreakpoint', {
    uri: 'file:///path/to/script.old8',
    line: 50,
    condition: 'x > 10' // 条件表达式
});
```

#### 3. 调试控制

```typescript
// 继续执行
await languageClient.sendRequest('old8lang/debugControl', {
    uri: 'file:///path/to/script.old8',
    command: 'continue'
});

// 暂停执行
await languageClient.sendRequest('old8lang/debugControl', {
    uri: 'file:///path/to/script.old8',
    command: 'pause'
});

// 单步进入
await languageClient.sendRequest('old8lang/debugControl', {
    uri: 'file:///path/to/script.old8',
    command: 'stepinto'
});

// 单步跳过
await languageClient.sendRequest('old8lang/debugControl', {
    uri: 'file:///path/to/script.old8',
    command: 'stepover'
});

// 单步跳出
await languageClient.sendRequest('old8lang/debugControl', {
    uri: 'file:///path/to/script.old8',
    command: 'stepout'
});
```

#### 4. 移除断点

```typescript
const removed = await languageClient.sendRequest('old8lang/removeBreakpoint', {
    uri: 'file:///path/to/script.old8',
    line: 42
});

console.log('Breakpoint removed:', removed);
```

#### 5. 停止调试会话

```typescript
const response = await languageClient.sendRequest('old8lang/stopDebugging', {
    uri: 'file:///path/to/script.old8'
});

console.log('Is Debugging:', response.isDebugging);
console.log('State:', response.state);
```

## 性能报告格式

停止性能分析后会生成 Markdown 格式的报告，包含以下信息：

```markdown
# 性能分析报告

## 会话信息
- **会话名称**: MyProfilingSession
- **执行模式**: interpreter
- **总执行时间**: 1234.56 ms
- **函数调用次数**: 10000
- **峰值内存**: 45.67 MB
- **性能评分**: 85.5/100 (B)

## 最热函数 (Top 5)
1. **calculateSum** - 调用 5000 次，总耗时 567.89 ms，平均 0.1136 ms
2. **processData** - 调用 3000 次，总耗时 345.67 ms，平均 0.1152 ms
3. **validateInput** - 调用 2000 次，总耗时 123.45 ms，平均 0.0617 ms
...

## 时间分布
- **解析时间**: 12.34 ms
- **编译时间**: 45.67 ms

## 性能瓶颈
1. 函数 calculateSum 在循环中频繁调用，考虑优化
2. 内存使用过高，建议检查数据结构
3. 建议使用缓存减少重复计算

## 建议
根据性能分析结果，建议优化热点函数并减少不必要的计算。
```

## 集成到 VS Code Extension

### package.json 配置

```json
{
  "contributes": {
    "commands": [
      {
        "command": "old8lang.startProfiling",
        "title": "Old8Lang: 启动性能分析"
      },
      {
        "command": "old8lang.stopProfiling",
        "title": "Old8Lang: 停止性能分析"
      },
      {
        "command": "old8lang.showProfilingReport",
        "title": "Old8Lang: 显示性能报告"
      },
      {
        "command": "old8lang.startDebugging",
        "title": "Old8Lang: 启动调试"
      },
      {
        "command": "old8lang.addBreakpoint",
        "title": "Old8Lang: 添加断点"
      }
    ]
  }
}
```

### Extension 代码示例

```typescript
import * as vscode from 'vscode';
import { LanguageClient } from 'vscode-languageclient/node';

export function activate(context: vscode.ExtensionContext) {
    const client: LanguageClient = /* 初始化 Language Client */;

    // 注册启动性能分析命令
    context.subscriptions.push(
        vscode.commands.registerCommand('old8lang.startProfiling', async () => {
            const editor = vscode.window.activeTextEditor;
            if (!editor) return;

            const uri = editor.document.uri.toString();
            const response = await client.sendRequest('old8lang/startProfiling', {
                uri,
                sessionName: `Profiling_${Date.now()}`,
                executionMode: 'interpreter'
            });

            vscode.window.showInformationMessage(
                `性能分析已启动 (Session ID: ${response.sessionId})`
            );
        })
    );

    // 注册停止性能分析命令
    context.subscriptions.push(
        vscode.commands.registerCommand('old8lang.stopProfiling', async () => {
            const editor = vscode.window.activeTextEditor;
            if (!editor) return;

            const uri = editor.document.uri.toString();
            const report = await client.sendRequest('old8lang/stopProfiling', {
                uri
            });

            // 在新面板中显示报告
            const panel = vscode.window.createWebviewPanel(
                'old8langPerfReport',
                '性能分析报告',
                vscode.ViewColumn.Two,
                {}
            );

            // 将 Markdown 转换为 HTML
            panel.webview.html = markdownToHtml(report.content);
        })
    );

    // 注册添加断点命令
    context.subscriptions.push(
        vscode.commands.registerCommand('old8lang.addBreakpoint', async () => {
            const editor = vscode.window.activeTextEditor;
            if (!editor) return;

            const uri = editor.document.uri.toString();
            const line = editor.selection.active.line + 1; // LSP uses 1-based line numbers

            const added = await client.sendRequest('old8lang/addBreakpoint', {
                uri,
                line,
                condition: null
            });

            if (added) {
                vscode.window.showInformationMessage(`断点已添加在第 ${line} 行`);
            }
        })
    );
}

function markdownToHtml(markdown: string): string {
    // 使用 markdown-it 或其他库转换
    // 这里简化处理
    return `
        <!DOCTYPE html>
        <html>
        <head>
            <style>
                body { font-family: sans-serif; padding: 20px; }
                h1, h2 { color: #333; }
                pre { background: #f5f5f5; padding: 10px; }
            </style>
        </head>
        <body>
            <pre>${markdown}</pre>
        </body>
        </html>
    `;
}
```

## 注意事项

1. **会话隔离**：每个文档 URI 对应独立的调试/性能分析会话
2. **并发支持**：可以同时对多个文档进行性能分析或调试
3. **资源清理**：建议及时停止不需要的会话以释放资源
4. **断点管理**：断点通过文件路径和行号标识，支持条件断点
5. **性能影响**：启用性能分析会对执行性能产生一定影响

## 技术依赖

- **Old8Lang.Debugger**: 提供断点管理、调用栈追踪、变量监视等调试功能
- **Old8Lang.Profiler**: 提供性能数据收集、分析和报告生成功能
- **OmniSharp.Extensions.LanguageServer**: LSP 协议实现
- **MediatR**: 请求/响应处理框架

## 后续优化方向

1. 添加实时性能数据推送（使用 LSP Notification）
2. 支持更复杂的断点条件表达式
3. 提供调试变量查看功能
4. 支持性能报告导出为多种格式（JSON、CSV、HTML）
5. 添加性能趋势分析和历史对比功能
