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
/// 文档同步处理器
/// </summary>
public class TextDocumentSyncHandler(DocumentManager documentManager, ILanguageServerFacade languageServer)
    : ITextDocumentSyncHandler
{
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

        var result = documentManager.UpdateDocument(uri, text);
        PublishDiagnostics(uri, result);

        return Unit.Task;
    }

    public Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        var text = request.ContentChanges.FirstOrDefault()?.Text ?? "";

        var result = documentManager.UpdateDocument(uri, text);
        PublishDiagnostics(uri, result);

        return Unit.Task;
    }

    public Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
    {
        return Unit.Task;
    }

    public Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        documentManager.CloseDocument(uri);
        return Unit.Task;
    }

    private void PublishDiagnostics(string uri, Models.DocumentParseResult result)
    {
        var diagnostics = result.Diagnostics.Select(d => new Diagnostic
        {
            Severity = ConvertSeverity(d.Severity),
            Range = new Range(
                new Position(Math.Max(0, d.Line - 1), Math.Max(0, d.Column - 1)),
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