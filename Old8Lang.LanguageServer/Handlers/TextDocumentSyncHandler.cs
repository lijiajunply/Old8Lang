using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 文档同步处理器（支持 didChange 实时诊断防抖与后台执行）
/// </summary>
public class TextDocumentSyncHandler(
    DocumentManager documentManager,
    ILanguageServerFacade? languageServer,
    DiagnosticsScheduler? diagnosticsScheduler = null)
    : ITextDocumentSyncHandler
{
    private readonly DiagnosticsScheduler _diagnosticsScheduler = diagnosticsScheduler ?? new DiagnosticsScheduler(
        documentManager,
        new DiagnosticsSchedulerOptions
        {
            DebounceDelay = TimeSpan.Zero,
            RunInBackground = false
        });

    public TextDocumentSyncKind Change => TextDocumentSyncKind.Full;

    public TextDocumentChangeRegistrationOptions GetRegistrationOptions(
        TextSynchronizationCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new TextDocumentChangeRegistrationOptions()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang"),
            SyncKind = Change
        };
    }

    TextDocumentOpenRegistrationOptions
        IRegistration<TextDocumentOpenRegistrationOptions, TextSynchronizationCapability>.GetRegistrationOptions(
            TextSynchronizationCapability capability,
            ClientCapabilities clientCapabilities)
    {
        return new TextDocumentOpenRegistrationOptions()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang")
        };
    }

    TextDocumentCloseRegistrationOptions
        IRegistration<TextDocumentCloseRegistrationOptions, TextSynchronizationCapability>.GetRegistrationOptions(
            TextSynchronizationCapability capability,
            ClientCapabilities clientCapabilities)
    {
        return new TextDocumentCloseRegistrationOptions()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang")
        };
    }

    TextDocumentSaveRegistrationOptions
        IRegistration<TextDocumentSaveRegistrationOptions, TextSynchronizationCapability>.GetRegistrationOptions(
            TextSynchronizationCapability capability,
            ClientCapabilities clientCapabilities)
    {
        return new TextDocumentSaveRegistrationOptions()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang")
        };
    }

    public TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
    {
        return new TextDocumentAttributes(uri, "old8lang");
    }

    public Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        var text = request.TextDocument.Text;

        _diagnosticsScheduler.FlushNow(uri, text, PublishDiagnostics, cancellationToken);
        return Unit.Task;
    }

    public async Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        var text = request.ContentChanges.FirstOrDefault()?.Text ?? "";

        await _diagnosticsScheduler
            .ScheduleAsync(uri, text, PublishDiagnostics, cancellationToken)
            .ConfigureAwait(false);

        return Unit.Value;
    }

    public Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        if (documentManager.HasPendingUpdate(uri))
        {
            _diagnosticsScheduler.FlushNow(uri, request.Text, PublishDiagnostics, cancellationToken);
        }

        return Unit.Task;
    }

    public Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        _diagnosticsScheduler.Cancel(uri);
        documentManager.CloseDocument(uri);
        ClearDiagnostics(uri);
        return Unit.Task;
    }

    private void PublishDiagnostics(string uri, Models.DocumentParseResult result)
    {
        if (languageServer == null)
        {
            return;
        }

        var diagnostics = result.Diagnostics.Select(d => new Diagnostic
        {
            Severity = ConvertSeverity(d.Severity),
            Range = new Range(
                new Position(Math.Max(0, d.Line - 1), Math.Max(0, d.Column)),
                new Position(Math.Max(0, d.Line - 1), Math.Max(0, d.Column))
            ),
            Message = d.Message,
            Source = d.Source
        }).ToArray();

        languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
        {
            Uri = DocumentUri.From(uri),
            Diagnostics = new Container<Diagnostic>(diagnostics)
        });
    }

    private void ClearDiagnostics(string uri)
    {
        if (languageServer == null)
        {
            return;
        }

        languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
        {
            Uri = DocumentUri.From(uri),
            Diagnostics = new Container<Diagnostic>()
        });
    }

    private static DiagnosticSeverity ConvertSeverity(Models.DiagnosticSeverity severity)
    {
        return severity switch
        {
            Models.DiagnosticSeverity.Error => DiagnosticSeverity.Error,
            Models.DiagnosticSeverity.Warning => DiagnosticSeverity.Warning,
            Models.DiagnosticSeverity.Information => DiagnosticSeverity.Information,
            Models.DiagnosticSeverity.Hint => DiagnosticSeverity.Hint,
            _ => DiagnosticSeverity.Error
        };
    }
}
