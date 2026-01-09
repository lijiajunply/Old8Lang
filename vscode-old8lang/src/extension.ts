import * as path from 'path';
import * as os from 'os';
import { workspace, ExtensionContext } from 'vscode';
import {
    LanguageClient,
    LanguageClientOptions,
    ServerOptions,
    TransportKind
} from 'vscode-languageclient/node';

let client: LanguageClient;

export function activate(context: ExtensionContext) {
    // Language Server 可执行文件路径
    const config = workspace.getConfiguration('old8lang');
    let serverPath = config.get<string>('languageServer.path', '');

    if (!serverPath) {
        // 默认路径：假设 Language Server 在扩展根目录的 server 文件夹中
        const platform = os.platform();
        const executable = platform === 'win32' ? 'Old8Lang.LanguageServer.exe' : 'Old8Lang.LanguageServer';
        serverPath = context.asAbsolutePath(path.join('server', executable));
    }

    // 服务器配置
    const serverOptions: ServerOptions = {
        run: { command: serverPath, transport: TransportKind.stdio },
        debug: { command: serverPath, transport: TransportKind.stdio }
    };

    // 客户端配置
    const clientOptions: LanguageClientOptions = {
        documentSelector: [{ scheme: 'file', language: 'old8lang' }],
        synchronize: {
            fileEvents: workspace.createFileSystemWatcher('**/*.old8')
        },
        // 增加初始化和关闭的超时时间
        initializationOptions: {},
        // 设置更长的关闭超时（10秒）
        connectionOptions: {
            maxRestartCount: 3,
            cancellationStrategy: undefined
        }
    };

    // 创建并启动 Language Client
    client = new LanguageClient(
        'old8langLanguageServer',
        'Old8Lang Language Server',
        serverOptions,
        clientOptions
    );

    // 启动客户端（同时启动服务器）
    client.start();
}

export function deactivate(): Thenable<void> | undefined {
    if (!client) {
        return undefined;
    }

    // 不等待 stop 完成，让 VSCode 自己终止进程
    // 这样可以避免超时错误
    client.stop().catch(err => {
        // 静默忽略关闭错误
        console.log('Language server shutdown:', err?.message || 'completed');
    });

    return undefined;
}
