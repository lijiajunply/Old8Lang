using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        await server.WaitForExit;
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<DocumentManager>();
        services.AddSingleton<DebugProfilerService>();
        services.AddSingleton<FormattingService>();
    }
}
