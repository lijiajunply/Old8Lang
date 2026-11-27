import * as fs from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';
import { LanguageClient } from 'vscode-languageclient/node';

export interface Old8LangDebugConfiguration extends vscode.DebugConfiguration {
    type: 'old8lang';
    request: 'launch';
    name: string;
    program: string;
    args?: string[];
    cwd?: string;
    stopOnEntry?: boolean;
    noDebug?: boolean;
}

export interface DebugRuntimeInvocation {
    command: string;
    args: string[];
    cwd?: string;
}

/**
 * 解析 Old8Lang.App --dap 的启动命令。
 * 优先级：old8lang.compiler.path -> 扩展内置 server/Old8Lang.App -> 源码工程 dotnet run --project Old8Lang.App。
 */
export function resolveDapInvocation(
    context: vscode.ExtensionContext,
    workspaceFolder?: vscode.WorkspaceFolder
): DebugRuntimeInvocation {
    const config = vscode.workspace.getConfiguration('old8lang');
    const dotnet = (config.get<string>('dotnet.path') ?? '').trim() || 'dotnet';
    const root = workspaceFolder?.uri.fsPath ?? vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;

    const compilerPath = (config.get<string>('compiler.path') ?? '').trim();
    if (compilerPath) {
        if (compilerPath.toLowerCase().endsWith('.dll')) {
            return { command: dotnet, args: [compilerPath, '--dap'], cwd: root };
        }
        return { command: compilerPath, args: ['--dap'], cwd: root };
    }

    const appExeName = process.platform === 'win32' ? 'Old8Lang.App.exe' : 'Old8Lang.App';
    const bundledExe = context.asAbsolutePath(path.join('server', appExeName));
    if (fs.existsSync(bundledExe)) {
        return { command: bundledExe, args: ['--dap'], cwd: root };
    }

    const bundledDll = context.asAbsolutePath(path.join('server', 'Old8Lang.App.dll'));
    if (fs.existsSync(bundledDll)) {
        return { command: dotnet, args: [bundledDll, '--dap'], cwd: root };
    }

    const project = (config.get<string>('compiler.project') ?? 'Old8Lang.App').trim() || 'Old8Lang.App';
    const projectPath = path.isAbsolute(project) || !root ? project : path.join(root, project);

    return {
        command: dotnet,
        args: ['run', '--project', projectPath, '--', '--dap'],
        cwd: root
    };
}

/**
 * DAP 适配器描述符工厂：以 stdio 子进程启动 `Old8Lang.App --dap`。
 */
export class Old8LangDebugAdapterDescriptorFactory implements vscode.DebugAdapterDescriptorFactory {
    constructor(private readonly context: vscode.ExtensionContext) {}

    public createDebugAdapterDescriptor(
        session: vscode.DebugSession
    ): vscode.ProviderResult<vscode.DebugAdapterDescriptor> {
        const invocation = resolveDapInvocation(this.context, session.workspaceFolder);
        const options = invocation.cwd ? { cwd: invocation.cwd } : undefined;
        return new vscode.DebugAdapterExecutable(invocation.command, invocation.args, options);
    }
}

/**
 * 调试配置提供器：为当前打开的 .old8 文件补全默认 launch 配置。
 */
export class Old8LangDebugConfigurationProvider implements vscode.DebugConfigurationProvider {
    public provideDebugConfigurations(): vscode.ProviderResult<vscode.DebugConfiguration[]> {
        return [
            {
                type: 'old8lang',
                request: 'launch',
                name: 'Old8Lang: 调试当前文件',
                program: '${file}',
                cwd: '${workspaceFolder}',
                stopOnEntry: false
            }
        ];
    }

    public resolveDebugConfiguration(
        folder: vscode.WorkspaceFolder | undefined,
        config: vscode.DebugConfiguration
    ): vscode.ProviderResult<vscode.DebugConfiguration> {
        if (!config.type && !config.request && !config.name) {
            const editor = vscode.window.activeTextEditor;
            if (editor && editor.document.languageId === 'old8lang') {
                config.type = 'old8lang';
                config.name = 'Old8Lang: 调试当前文件';
                config.request = 'launch';
                config.program = editor.document.uri.fsPath;
                config.cwd = folder?.uri.fsPath ?? path.dirname(editor.document.uri.fsPath);
                config.stopOnEntry = false;
            }
        }

        if (!config.program) {
            void vscode.window.showErrorMessage('未指定要调试的 .old8 文件（program）。');
            return undefined;
        }

        if (!config.cwd && folder) {
            config.cwd = folder.uri.fsPath;
        }

        return config;
    }
}

/**
 * DAP 通信跟踪器工厂：监听 DAP 暂停栈帧与变量响应，将实时变量快照同步给 LSP 服务端，
 * 使 `textDocument/inlineValue` 既可走标准 DAP 协议映射，也能直接渲染当前暂停帧的变量快照。
 */
export class Old8LangDebugAdapterTrackerFactory implements vscode.DebugAdapterTrackerFactory {
    constructor(private readonly getClient: () => LanguageClient | undefined) {}

    public createDebugAdapterTracker(
        session: vscode.DebugSession
    ): vscode.ProviderResult<vscode.DebugAdapterTracker> {
        if (session.type !== 'old8lang') {
            return undefined;
        }

        let lastProgramUri = toDocumentUri(session.configuration?.program);
        let activeFrameId = 1;
        let activeStoppedLine = 0;
        const pendingScopeRequests = new Map<number, number>(); // request_seq -> frameId
        const refToFrame = new Map<number, number>(); // variablesReference -> frameId
        const pendingVarRequests = new Map<number, number>(); // request_seq -> frameId
        const frameVariables = new Map<number, Record<string, string>>();

        const pushSnapshot = (uri: string | undefined, frameId: number, stoppedLine: number, vars: Record<string, string>, clear: boolean): void => {
            const client = this.getClient();
            if (!client || !uri) {
                return;
            }

            void client.sendRequest('old8lang/updateDebugSnapshot', {
                uri,
                frameId,
                stoppedLine,
                variables: vars,
                clear
            }).catch(() => {
                // 忽略 LSP 未就绪或关闭时的竞态错误
            });
        };

        return {
            onWillReceiveMessage: (message: Record<string, unknown>) => {
                if (message?.type !== 'request') {
                    return;
                }

                const seq = typeof message.seq === 'number' ? message.seq : -1;
                const command = message.command as string | undefined;
                const args = (message.arguments ?? {}) as Record<string, unknown>;

                if (command === 'scopes' && typeof args.frameId === 'number') {
                    pendingScopeRequests.set(seq, args.frameId);
                } else if (command === 'variables' && typeof args.variablesReference === 'number') {
                    const frameId = refToFrame.get(args.variablesReference);
                    if (frameId !== undefined) {
                        pendingVarRequests.set(seq, frameId);
                    }
                }
            },

            onDidSendMessage: (message: Record<string, unknown>) => {
                if (message?.type === 'event') {
                    const eventName = message.event as string | undefined;
                    if (eventName === 'continued' || eventName === 'terminated' || eventName === 'exited') {
                        refToFrame.clear();
                        frameVariables.clear();
                        pushSnapshot(lastProgramUri, activeFrameId, 0, {}, true);
                    }
                    return;
                }

                if (message?.type !== 'response' || message.success !== true) {
                    return;
                }

                const reqSeq = typeof message.request_seq === 'number' ? message.request_seq : -1;
                const command = message.command as string | undefined;
                const body = (message.body ?? {}) as Record<string, unknown>;

                if (command === 'stackTrace' && Array.isArray(body.stackFrames) && body.stackFrames.length > 0) {
                    const top = body.stackFrames[0] as Record<string, unknown>;
                    if (typeof top.id === 'number') {
                        activeFrameId = top.id;
                    }
                    if (typeof top.line === 'number') {
                        activeStoppedLine = Math.max(0, top.line - 1);
                    }
                    const source = top.source as Record<string, unknown> | undefined;
                    if (typeof source?.path === 'string' && source.path.length > 0) {
                        lastProgramUri = toDocumentUri(source.path);
                    }
                } else if (command === 'scopes' && Array.isArray(body.scopes)) {
                    const frameId = pendingScopeRequests.get(reqSeq) ?? activeFrameId;
                    pendingScopeRequests.delete(reqSeq);
                    for (const scope of body.scopes as Array<Record<string, unknown>>) {
                        if (typeof scope.variablesReference === 'number' && scope.variablesReference > 0) {
                            refToFrame.set(scope.variablesReference, frameId);
                        }
                    }
                } else if (command === 'variables' && Array.isArray(body.variables)) {
                    const frameId = pendingVarRequests.get(reqSeq);
                    pendingVarRequests.delete(reqSeq);
                    if (frameId === undefined) {
                        return;
                    }

                    const merged = { ...(frameVariables.get(frameId) ?? {}) };
                    for (const variable of body.variables as Array<Record<string, unknown>>) {
                        if (typeof variable.name === 'string' && typeof variable.value === 'string') {
                            merged[variable.name] = variable.value;
                        }
                    }
                    frameVariables.set(frameId, merged);
                    pushSnapshot(lastProgramUri, frameId, activeStoppedLine, merged, false);
                }
            },

            onWillStopSession: () => {
                pushSnapshot(lastProgramUri, activeFrameId, 0, {}, true);
            }
        };
    }
}

function toDocumentUri(filePath: unknown): string | undefined {
    if (typeof filePath !== 'string' || filePath.trim().length === 0) {
        return undefined;
    }

    try {
        return vscode.Uri.file(filePath).toString();
    } catch {
        return undefined;
    }
}
