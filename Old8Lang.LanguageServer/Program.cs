using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Server;
using Old8Lang.LanguageServer.Services;
using Old8Lang.LanguageServer.Handlers;
using Old8Lang.GlobalFunctions.Core;

namespace Old8Lang.LanguageServer;

class Program
{
    static async Task Main(string[] args)
    {
        // 在启动时立即初始化全局函数，避免在请求处理时阻塞
        GlobalFunctionInitializer.EnsureInitialized();

        var server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(options =>
            options
                .WithInput(Console.OpenStandardInput())
                .WithOutput(Console.OpenStandardOutput())
                .ConfigureLogging(x => x
                    .AddLanguageProtocolLogging()
                    .SetMinimumLevel(LogLevel.Warning)) // 降低日志级别，减少干扰
                .WithServices(ConfigureServices)
                // 核心功能
                .WithHandler<TextDocumentSyncHandler>()
                .WithHandler<CompletionHandler>()
                .WithHandler<DefinitionHandler>()
                .WithHandler<ReferencesHandler>()
                .WithHandler<RenameHandler>()
                .WithHandler<HoverHandler>()
                // 新增高优先级功能
                .WithHandler<DocumentSymbolHandler>()
                .WithHandler<SignatureHelpHandler>()
                .WithHandler<DocumentFormattingHandler>()
                .WithHandler<CodeActionHandler>()
                // 新增 LSP 高级功能
                .WithHandler<WorkspaceSymbolHandler>()
                .WithHandler<DocumentHighlightHandler>()
                .WithHandler<FoldingRangeHandler>()
                .WithHandler<SemanticTokensHandler>()
                // 新增更多 LSP 功能
                .WithHandler<DocumentLinkHandler>()
                .WithHandler<CodeLensHandler>()
                .WithHandler<SelectionRangeHandler>()
                .WithHandler<InlayHintHandler>()
                // Debug and Profiler Handlers
                .WithHandler<StartProfilingHandler>()
                .WithHandler<StopProfilingHandler>()
                .WithHandler<GetProfilingStatusHandler>()
                .WithHandler<StartDebuggingHandler>()
                .WithHandler<StopDebuggingHandler>()
                .WithHandler<AddBreakpointHandler>()
                .WithHandler<RemoveBreakpointHandler>()
                .WithHandler<DebugControlHandler>()
        );

        // 初始化完成后设置工作区索引根目录，并在工作区文件夹变化时刷新
        await server.WasStarted;
        var workspaceIndex = server.Services.GetRequiredService<WorkspaceIndex>();
        workspaceIndex.SetRoots(ResolveWorkspaceRoots(server));
        server.WorkspaceFolderManager.WorkspaceFolders.Subscribe(
            new WorkspaceFolderObserver(workspaceIndex));

        await server.WaitForExit;
    }

    private static IEnumerable<string> ResolveWorkspaceRoots(
        OmniSharp.Extensions.LanguageServer.Server.LanguageServer server)
    {
        var roots = new List<string>();

        foreach (var folder in server.WorkspaceFolderManager.CurrentWorkspaceFolders)
        {
            var path = folder.Uri.GetFileSystemPath();
            if (!string.IsNullOrEmpty(path))
            {
                roots.Add(path);
            }
        }

        if (roots.Count == 0 && server.ClientSettings.RootUri is { } rootUri)
        {
            var path = rootUri.GetFileSystemPath();
            if (!string.IsNullOrEmpty(path))
            {
                roots.Add(path);
            }
        }

        if (roots.Count == 0)
        {
            roots.Add(Directory.GetCurrentDirectory());
        }

        return roots;
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<DocumentManager>();
        services.AddSingleton<WorkspaceIndex>();
        services.AddSingleton<DebugProfilerService>();
        services.AddSingleton<FormattingService>();
    }

    /// <summary>把工作区文件夹变化同步到索引根目录</summary>
    private sealed class WorkspaceFolderObserver(WorkspaceIndex workspaceIndex)
        : IObserver<IEnumerable<WorkspaceFolder>>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(IEnumerable<WorkspaceFolder> value)
        {
            var roots = value
                .Select(folder => folder.Uri.GetFileSystemPath())
                .Where(path => !string.IsNullOrEmpty(path))
                .ToList();

            workspaceIndex.SetRoots(roots);
        }
    }
}
