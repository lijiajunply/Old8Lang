using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;
using Old8Lang.LanguageServer.Services;
using Old8Lang.LanguageServer.Handlers;

namespace Old8Lang.LanguageServer;

class Program
{
    static async Task Main(string[] args)
    {
        var server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(options =>
            options
                .WithInput(Console.OpenStandardInput())
                .WithOutput(Console.OpenStandardOutput())
                .ConfigureLogging(x => x
                    .AddLanguageProtocolLogging()
                    .SetMinimumLevel(LogLevel.Debug))
                .WithServices(ConfigureServices)
                .WithHandler<TextDocumentSyncHandler>()
                .WithHandler<CompletionHandler>()
                .WithHandler<DefinitionHandler>()
                .WithHandler<ReferencesHandler>()
                .WithHandler<HoverHandler>()
        );

        await server.WaitForExit;
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<DocumentManager>();
    }
}
