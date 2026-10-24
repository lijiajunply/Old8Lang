import * as fs from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';
import {
    Executable,
    LanguageClient,
    LanguageClientOptions,
    ServerOptions,
    TransportKind
} from 'vscode-languageclient/node';

type CompilerMode = 'interpreter' | 'il' | 'vm' | 'check';

interface LspPosition {
    line: number;
    character: number;
}

interface LspRange {
    start: LspPosition;
    end: LspPosition;
}

interface LspLocation {
    uri: string;
    range: LspRange;
}

interface Old8LangTaskDefinition extends vscode.TaskDefinition {
    command: CompilerMode;
    /** 要运行的文件；留空时使用 ${file} */
    file?: string;
    /** 追加到命令行末尾的参数 */
    args?: string[];
}

const COMPILER_FLAGS: Record<CompilerMode, string> = {
    interpreter: '-f',
    il: '-il',
    vm: '-vm',
    check: '-s'
};

const COMPILER_LABELS: Record<CompilerMode, string> = {
    interpreter: '运行（解释模式）',
    il: '运行（IL 模式）',
    vm: '运行（虚拟机模式）',
    check: '语法检查'
};

const DIAGNOSTIC_LEVELS: Record<string, vscode.DiagnosticSeverity> = {
    error: vscode.DiagnosticSeverity.Error,
    warning: vscode.DiagnosticSeverity.Warning,
    information: vscode.DiagnosticSeverity.Information,
    hint: vscode.DiagnosticSeverity.Hint
};

let client: LanguageClient | undefined;
let taskProvider: Old8LangTaskProvider | undefined;
let extensionContext: vscode.ExtensionContext | undefined;
const shownWarnings = new Set<string>();

export function activate(context: vscode.ExtensionContext): void {
    extensionContext = context;

    validateConfiguration();

    context.subscriptions.push(
        vscode.workspace.onDidChangeConfiguration(event => {
            if (event.affectsConfiguration('old8lang')) {
                validateConfiguration();
            }
        }),
        vscode.commands.registerCommand('old8lang.showReferences', showReferences),
        vscode.commands.registerCommand('old8lang.runFile', () => runActiveFile()),
        vscode.commands.registerCommand('old8lang.checkFile', () => runActiveFile('check')),
        vscode.commands.registerCommand('old8lang.restartLanguageServer', restartClient)
    );

    taskProvider = new Old8LangTaskProvider();
    context.subscriptions.push(vscode.tasks.registerTaskProvider('old8lang', taskProvider));

    startClient();
}

export function deactivate(): Thenable<void> | undefined {
    if (!client) {
        return undefined;
    }

    const running = client;
    client = undefined;

    // 不等待 stop 完成，让 VSCode 自己终止进程，避免关闭超时错误
    running.stop().catch(err => {
        console.log('Old8Lang language server shutdown:', err?.message ?? 'completed');
    });

    return undefined;
}

function startClient(): void {
    if (!extensionContext) {
        return;
    }

    const serverOptions = resolveServerOptions(extensionContext);
    if (!serverOptions) {
        return;
    }

    const clientOptions: LanguageClientOptions = {
        documentSelector: [{ scheme: 'file', language: 'old8lang' }],
        synchronize: {
            fileEvents: vscode.workspace.createFileSystemWatcher('**/*.old8')
        },
        initializationOptions: {},
        connectionOptions: {
            maxRestartCount: 3,
            cancellationStrategy: undefined
        },
        middleware: {
            handleDiagnostics: (uri, diagnostics, next) => {
                next(uri, filterDiagnostics(diagnostics));
            }
        }
    };

    client = new LanguageClient(
        'old8langLanguageServer',
        'Old8Lang Language Server',
        serverOptions,
        clientOptions
    );

    client.start().catch(err => {
        client = undefined;
        void vscode.window.showErrorMessage(
            `Old8Lang Language Server 启动失败：${err?.message ?? err}`);
    });
}

async function restartClient(): Promise<void> {
    const current = client;
    client = undefined;

    if (current) {
        try {
            await current.stop();
        } catch {
            // 忽略关闭错误，继续重启
        }
    }

    startClient();
    void vscode.window.showInformationMessage('Old8Lang Language Server 已重新启动。');
}

function resolveServerOptions(context: vscode.ExtensionContext): ServerOptions | undefined {
    const config = vscode.workspace.getConfiguration('old8lang');
    const cwd = workspaceRoot();
    const dotnet = getDotnetPath();

    const configured = (config.get<string>('languageServer.path') ?? '').trim();
    if (configured) {
        return toServerOptions(configured, [], cwd, dotnet);
    }

    const executableName = process.platform === 'win32'
        ? 'Old8Lang.LanguageServer.exe'
        : 'Old8Lang.LanguageServer';
    const bundledExecutable = context.asAbsolutePath(path.join('server', executableName));
    if (fs.existsSync(bundledExecutable)) {
        return {
            run: toExecutable(bundledExecutable, [], cwd),
            debug: toExecutable(bundledExecutable, [], cwd)
        };
    }

    const bundledDll = context.asAbsolutePath(path.join('server', 'Old8Lang.LanguageServer.dll'));
    if (fs.existsSync(bundledDll)) {
        return {
            run: toExecutable(dotnet, [bundledDll], cwd),
            debug: toExecutable(dotnet, [bundledDll], cwd)
        };
    }

    // 开发环境：从源码仓库直接启动
    const project = config.get<string>('languageServer.project') ?? 'Old8Lang.LanguageServer';
    if (cwd) {
        const projectPath = path.isAbsolute(project) ? project : path.join(cwd, project);
        if (fs.existsSync(projectPath)) {
            const args = ['run', '--project', projectPath];
            return {
                run: toExecutable(dotnet, args, cwd),
                debug: toExecutable(dotnet, args, cwd)
            };
        }
    }

    warnOnce(
        'language-server-missing',
        '未找到 Old8Lang Language Server。请设置 old8lang.languageServer.path，' +
        '或先构建并将服务器输出复制到扩展的 server 目录。语法高亮与代码片段仍可使用。'
    );
    return undefined;
}

function toServerOptions(
    configuredPath: string,
    extraArgs: string[],
    cwd: string | undefined,
    dotnet: string
): ServerOptions {
    if (configuredPath.toLowerCase().endsWith('.dll')) {
        return {
            run: toExecutable(dotnet, [configuredPath, ...extraArgs], cwd),
            debug: toExecutable(dotnet, [configuredPath, ...extraArgs], cwd)
        };
    }

    return {
        run: toExecutable(configuredPath, extraArgs, cwd),
        debug: toExecutable(configuredPath, extraArgs, cwd)
    };
}

function toExecutable(command: string, args: string[], cwd: string | undefined): Executable {
    return {
        command,
        args,
        transport: TransportKind.stdio,
        options: cwd ? { cwd } : undefined
    };
}

function filterDiagnostics(diagnostics: vscode.Diagnostic[]): vscode.Diagnostic[] {
    const level = vscode.workspace.getConfiguration('old8lang')
        .get<string>('diagnostics.level', 'hint') ?? 'hint';
    const maximum = DIAGNOSTIC_LEVELS[level] ?? vscode.DiagnosticSeverity.Hint;

    return diagnostics.filter(diagnostic =>
        diagnostic.severity === undefined || diagnostic.severity <= maximum);
}

function getDotnetPath(): string {
    const configured = (vscode.workspace.getConfiguration('old8lang')
        .get<string>('dotnet.path') ?? '').trim();
    return configured || 'dotnet';
}

function workspaceRoot(): string | undefined {
    return vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
}

function validateConfiguration(): void {
    const config = vscode.workspace.getConfiguration('old8lang');

    const serverPath = (config.get<string>('languageServer.path') ?? '').trim();
    validatePathSetting('languageServer.path', serverPath,
        'old8lang.languageServer.path 指向的文件不存在');

    const compilerPath = (config.get<string>('compiler.path') ?? '').trim();
    validatePathSetting('compiler.path', compilerPath,
        'old8lang.compiler.path 指向的文件不存在');

    const mode = config.get<string>('compiler.mode') ?? 'interpreter';
    if (!(mode in COMPILER_FLAGS)) {
        warnOnce('compiler-mode',
            `old8lang.compiler.mode 的值 "${mode}" 无效，应为 interpreter、il 或 vm。`);
    }

    const level = config.get<string>('diagnostics.level') ?? 'hint';
    if (!(level in DIAGNOSTIC_LEVELS)) {
        warnOnce('diagnostics-level',
            `old8lang.diagnostics.level 的值 "${level}" 无效，应为 error、warning、information 或 hint。`);
    }

    const trace = config.get<string>('trace.server') ?? 'off';
    if (!['off', 'messages', 'verbose'].includes(trace)) {
        warnOnce('trace-server',
            `old8lang.trace.server 的值 "${trace}" 无效，应为 off、messages 或 verbose。`);
    }
}

function validatePathSetting(key: string, value: string, message: string): void {
    if (!value) {
        return;
    }

    // 只校验明确的路径；不含分隔符的值可能是在 PATH 上的命令名
    const looksLikePath = path.isAbsolute(value) || value.includes('/') || value.includes('\\');
    if (looksLikePath && !fs.existsSync(value)) {
        warnOnce(key, `${message}：${value}`);
    }
}

function warnOnce(key: string, message: string): void {
    if (shownWarnings.has(key)) {
        return;
    }

    shownWarnings.add(key);
    void vscode.window.showWarningMessage(message);
}

async function showReferences(
    uri: string,
    position: LspPosition,
    locations: LspLocation[]
): Promise<void> {
    const targetUri = vscode.Uri.parse(uri);
    const positionObject = new vscode.Position(position.line, position.character);
    const locationObjects = (locations ?? []).map(location => new vscode.Location(
        vscode.Uri.parse(location.uri),
        new vscode.Range(
            location.range.start.line,
            location.range.start.character,
            location.range.end.line,
            location.range.end.character)));

    await vscode.commands.executeCommand(
        'editor.action.showReferences',
        targetUri,
        positionObject,
        locationObjects);
}

async function runActiveFile(forceMode?: CompilerMode): Promise<void> {
    const editor = vscode.window.activeTextEditor;

    if (!editor || editor.document.languageId !== 'old8lang') {
        void vscode.window.showWarningMessage('请先打开一个 .old8 文件。');
        return;
    }

    if (!taskProvider) {
        void vscode.window.showWarningMessage('Old8Lang 任务提供器尚未初始化。');
        return;
    }

    await editor.document.save();

    const mode = forceMode ?? getConfiguredMode();
    const task = taskProvider.createTask({
        type: 'old8lang',
        command: mode,
        file: editor.document.uri.fsPath
    });

    await vscode.tasks.executeTask(task);
}

function getConfiguredMode(): CompilerMode {
    const configured = vscode.workspace.getConfiguration('old8lang')
        .get<string>('compiler.mode', 'interpreter') ?? 'interpreter';

    return configured in COMPILER_FLAGS ? configured as CompilerMode : 'interpreter';
}

class Old8LangTaskProvider implements vscode.TaskProvider {
    public provideTasks(): vscode.Task[] {
        return (['interpreter', 'il', 'vm', 'check'] as CompilerMode[])
            .map(mode => this.createTask({ type: 'old8lang', command: mode, file: '${file}' }));
    }

    public resolveTask(task: vscode.Task): vscode.Task | undefined {
        const definition = task.definition as Old8LangTaskDefinition;
        if (!definition?.command || !(definition.command in COMPILER_FLAGS)) {
            return undefined;
        }

        return this.createTask({
            ...definition,
            type: 'old8lang',
            file: definition.file ?? '${file}'
        });
    }

    public createTask(definition: Old8LangTaskDefinition): vscode.Task {
        const mode: CompilerMode = definition.command in COMPILER_FLAGS
            ? definition.command
            : 'interpreter';
        const file = definition.file ?? '${file}';
        const invocation = buildCompilerInvocation(mode, file);
        const args = [...invocation.args, ...(definition.args ?? [])];

        const task = new vscode.Task(
            { ...definition, type: 'old8lang', command: mode },
            vscode.TaskScope.Workspace,
            `Old8Lang: ${COMPILER_LABELS[mode]}`,
            'old8lang',
            new vscode.ShellExecution(invocation.command, args, { cwd: invocation.cwd }),
            []);

        if (mode === 'check') {
            task.group = vscode.TaskGroup.Build;
        }

        task.presentationOptions = {
            reveal: vscode.TaskRevealKind.Always,
            panel: vscode.TaskPanelKind.Shared,
            clear: true
        };

        return task;
    }
}

function buildCompilerInvocation(
    mode: CompilerMode,
    file: string
): { command: string; args: string[]; cwd: string | undefined } {
    const config = vscode.workspace.getConfiguration('old8lang');
    const flag = COMPILER_FLAGS[mode];
    const dotnet = getDotnetPath();
    const root = workspaceRoot();

    const compilerPath = (config.get<string>('compiler.path') ?? '').trim();
    if (compilerPath) {
        if (compilerPath.toLowerCase().endsWith('.dll')) {
            return { command: dotnet, args: [compilerPath, flag, file], cwd: root };
        }

        return { command: compilerPath, args: [flag, file], cwd: root };
    }

    const project = (config.get<string>('compiler.project') ?? 'Old8Lang.App').trim() || 'Old8Lang.App';
    const projectPath = path.isAbsolute(project) || !root ? project : path.join(root, project);

    return {
        command: dotnet,
        args: ['run', '--project', projectPath, '--', flag, file],
        cwd: root
    };
}
