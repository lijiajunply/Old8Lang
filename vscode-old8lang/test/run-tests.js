'use strict';

const assert = require('assert');
const cp = require('child_process');
const fs = require('fs');
const os = require('os');
const path = require('path');
const { pathToFileURL } = require('url');
const rpc = require('vscode-jsonrpc/node');

const repoRoot = path.resolve(__dirname, '..', '..');
const extRoot = path.resolve(__dirname, '..');

function locateBinary(projectName) {
    const candidates = [
        path.join(repoRoot, projectName, 'bin', 'Debug', 'net10.0', `${projectName}.dll`),
        path.join(repoRoot, projectName, 'bin', 'Release', 'net10.0', `${projectName}.dll`),
        path.join(extRoot, 'server', `${projectName}.dll`)
    ];
    for (const candidate of candidates) {
        if (fs.existsSync(candidate)) {
            return candidate;
        }
    }
    throw new Error(`Cannot locate ${projectName}.dll. Checked:\n${candidates.join('\n')}`);
}

async function testGrammarRules() {
    const grammarPath = path.join(extRoot, 'syntaxes', 'old8lang.tmLanguage.json');
    const raw = fs.readFileSync(grammarPath, 'utf8');
    const grammar = JSON.parse(raw);

    assert.strictEqual(grammar.scopeName, 'source.old8lang');
    assert.ok(grammar.repository.comments, 'missing #comments');
    assert.ok(grammar.repository.directives, 'missing #directives');
    assert.ok(grammar.repository.decorators, 'missing #decorators');
    assert.ok(grammar.repository.strings, 'missing #strings');
    assert.ok(grammar.repository['interpolation-nested-braces'], 'missing #interpolation-nested-braces');
    assert.ok(grammar.repository.generics, 'missing #generics');
    assert.ok(grammar.repository['nested-structures'], 'missing #nested-structures');

    // Verify doc comment rules appear BEFORE plain `//` comment rule
    const commentPatterns = grammar.repository.comments.patterns;
    const docIdx = commentPatterns.findIndex(p => p.name === 'comment.line.documentation.old8lang');
    const lineIdx = commentPatterns.findIndex(p => p.name === 'comment.line.double-slash.old8lang');
    assert.ok(docIdx >= 0 && lineIdx > docIdx, '/// doc comment rule must precede // line comment rule');

    // Verify doc comment sub-patterns match JavaDoc, Sphinx, Google, Chinese, and inline code
    const docSubPatterns = commentPatterns[docIdx].patterns.map(p => new RegExp(p.match));
    assert.ok(docSubPatterns.some(r => r.test('@param count 数量')), 'JavaDoc @param should match');
    assert.ok(docSubPatterns.some(r => r.test(':param count: 数量')), 'Sphinx :param: should match');
    assert.ok(docSubPatterns.some(r => r.test('Args:')), 'Google Args: should match');
    assert.ok(docSubPatterns.some(r => r.test('参数：')), 'Chinese 参数： should match');
    assert.ok(docSubPatterns.some(r => r.test('`value`')), 'Inline code backticks should match');

    // Verify string interpolation handles escaped {{ }} before `{` and supports nested braces
    const interpRule = grammar.repository.strings.patterns.find(
        p => p.name === 'string.interpolated.double.old8lang'
    );
    assert.ok(interpRule, 'missing string.interpolated.double.old8lang');
    const braceEscIdx = interpRule.patterns.findIndex(p => p.name === 'constant.character.escape.brace.old8lang');
    const embedIdx = interpRule.patterns.findIndex(p => p.name === 'meta.embedded.expression.old8lang');
    assert.ok(braceEscIdx >= 0 && embedIdx > braceEscIdx, 'escaped {{ }} must precede interpolation {');
    assert.ok(new RegExp(interpRule.patterns[braceEscIdx].match).test('{{'));
    assert.ok(new RegExp(interpRule.patterns[braceEscIdx].match).test('}}'));
    assert.ok(
        interpRule.patterns[embedIdx].patterns.some(p => p.include === '#interpolation-nested-braces'),
        'interpolation must include #interpolation-nested-braces'
    );

    // Verify preprocessor/header directives and decorators match
    const dirPatterns = grammar.repository.directives.patterns.map(p => new RegExp(p.match));
    assert.ok(dirPatterns.some(r => r.test('#!encoding utf-8')), '#! header directive should match');
    assert.ok(dirPatterns.some(r => r.test('#if DEBUG')), '#if directive should match');

    const decPattern = new RegExp(grammar.repository.decorators.patterns[0].match);
    assert.ok(decPattern.test('@cached'), '@decorator should match');

    console.log('  [PASS] TextMate grammar validation (doc comments, interpolation, directives, nesting)');
}

async function testLspClientServerE2E() {
    const serverDll = locateBinary('Old8Lang.LanguageServer');
    const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'old8lang-vscode-lsp-e2e-'));

    const mainPath = path.join(tempDir, 'main.old8');
    const mainUri = pathToFileURL(mainPath).toString();
    const rootUri = pathToFileURL(tempDir).toString();

    const source = [
        '/// 基类',
        'class Animal {',
        '    public name: string <- "a"',
        '    private secret: int <- 7',
        '    public func speak() -> string {',
        '        return this.name',
        '    }',
        '}',
        'class Dog extends Animal {',
        '    public func bark() -> string {',
        '        return this.speak()',
        '    }',
        '}',
        'func run(times: int) -> string {',
        '    dog <- Dog()',
        '    out <- dog.bark()',
        '    return out',
        '}',
        'res <- run(1)',
        ''
    ].join('\n');
    fs.writeFileSync(mainPath, source, 'utf8');

    const child = cp.spawn('dotnet', [serverDll], {
        cwd: tempDir,
        stdio: ['pipe', 'pipe', 'pipe']
    });
    let stderrBuf = '';
    child.stderr.on('data', chunk => { stderrBuf += chunk.toString('utf8'); });

    const connection = rpc.createMessageConnection(
        new rpc.StreamMessageReader(child.stdout),
        new rpc.StreamMessageWriter(child.stdin)
    );

    const diagnosticsQueue = [];
    let diagnosticsResolver = null;

    connection.onNotification('textDocument/publishDiagnostics', params => {
        if (diagnosticsResolver) {
            const resolve = diagnosticsResolver;
            diagnosticsResolver = null;
            resolve(params);
        } else {
            diagnosticsQueue.push(params);
        }
    });

    const logMessages = [];
    connection.onNotification('window/logMessage', params => {
        logMessages.push(`[LSP ${params.type}] ${params.message}`);
    });

    // Handle server-initiated dynamic registration requests gracefully
    connection.onRequest('client/registerCapability', () => null);
    connection.listen();

    function waitForDiagnostics(timeoutMs = 10000) {
        if (diagnosticsQueue.length > 0) {
            return Promise.resolve(diagnosticsQueue.shift());
        }
        return new Promise((resolve, reject) => {
            const timer = setTimeout(() => {
                diagnosticsResolver = null;
                reject(new Error('Timed out waiting for textDocument/publishDiagnostics'));
            }, timeoutMs);
            diagnosticsResolver = params => {
                clearTimeout(timer);
                resolve(params);
            };
        });
    }

    async function request(method, params) {
        try {
            return params === undefined
                ? await connection.sendRequest(method)
                : await connection.sendRequest(method, params);
        } catch (err) {
            err.message = `[LSP ${method}] ${err.message}`;
            throw err;
        }
    }

    try {
        // 1. initialize
        const initResult = await request('initialize', {
            processId: process.pid,
            rootUri,
            capabilities: {
                textDocument: {
                    completion: { dynamicRegistration: false },
                    hover: { dynamicRegistration: false },
                    definition: { dynamicRegistration: false },
                    references: { dynamicRegistration: false },
                    callHierarchy: { dynamicRegistration: false },
                    typeHierarchy: { dynamicRegistration: false },
                    inlineValue: { dynamicRegistration: false }
                }
            }
        });
        assert.ok(initResult.capabilities.completionProvider, 'missing completionProvider');
        assert.ok(initResult.capabilities.callHierarchyProvider, 'missing callHierarchyProvider');
        assert.ok(initResult.capabilities.typeHierarchyProvider, 'missing typeHierarchyProvider');
        assert.ok(initResult.capabilities.inlineValueProvider, 'missing inlineValueProvider');

        connection.sendNotification('initialized', {});

        // 2. didOpen -> diagnostics
        connection.sendNotification('textDocument/didOpen', {
            textDocument: { uri: mainUri, languageId: 'old8lang', version: 1, text: source }
        });
        const openDiag = await waitForDiagnostics();
        assert.strictEqual(openDiag.uri, mainUri);
        assert.strictEqual(openDiag.diagnostics.length, 0);

        // 3. Completion (external topDog. should show public speak/bark/name, hide private secret)
        const editedWithDot = source + 'topDog <- Dog()\nx <- topDog.\n';
        connection.sendNotification('textDocument/didChange', {
            textDocument: { uri: mainUri, version: 2 },
            contentChanges: [{ text: editedWithDot }]
        });
        const comp = await request('textDocument/completion', {
            textDocument: { uri: mainUri },
            position: { line: 20, character: 12 }
        });
        const labels = new Set((comp.items || comp).map(i => i.label));
        assert.ok(labels.has('bark'), 'expected bark in completion');
        assert.ok(labels.has('speak'), 'expected inherited speak in completion');
        assert.ok(!labels.has('secret'), 'private secret must NOT appear in external completion');

        // Restore valid source (also exercises debounce coalescing)
        connection.sendNotification('textDocument/didChange', {
            textDocument: { uri: mainUri, version: 3 },
            contentChanges: [{ text: 'bad <- missingVar\n' }]
        });
        connection.sendNotification('textDocument/didChange', {
            textDocument: { uri: mainUri, version: 4 },
            contentChanges: [{ text: source }]
        });
        const settledDiag = await waitForDiagnostics();
        assert.strictEqual(settledDiag.diagnostics.length, 0);

        // 4. Call Hierarchy
        const callItems = await request('textDocument/prepareCallHierarchy', {
            textDocument: { uri: mainUri },
            position: { line: 9, character: 17 }
        });
        assert.strictEqual(callItems.length, 1);
        assert.strictEqual(callItems[0].name, 'bark');

        const incoming = await request('callHierarchy/incomingCalls', { item: callItems[0] });
        assert.ok(incoming.some(c => c.from.name === 'run'), 'expected run in incomingCalls of bark');

        const outgoing = await request('callHierarchy/outgoingCalls', { item: callItems[0] });
        assert.ok(outgoing.some(c => c.to.name === 'speak'), 'expected speak in outgoingCalls of bark');

        // 5. Type Hierarchy
        const typeItems = await request('textDocument/prepareTypeHierarchy', {
            textDocument: { uri: mainUri },
            position: { line: 8, character: 7 }
        });
        assert.strictEqual(typeItems.length, 1);
        assert.strictEqual(typeItems[0].name, 'Dog');

        const supers = await request('typeHierarchy/supertypes', { item: typeItems[0] });
        assert.ok(supers.some(s => s.name === 'Animal'), 'expected Animal in Dog supertypes');

        const subs = await request('typeHierarchy/subtypes', { item: supers[0] });
        assert.ok(subs.some(s => s.name === 'Dog'), 'expected Dog in Animal subtypes');

        // 6. Inline Values with debug snapshot bridge
        const snapOk = await request('old8lang/updateDebugSnapshot', {
            uri: mainUri,
            frameId: 1,
            stoppedLine: 16,
            variables: { times: '1', out: '"a"' }
        });
        assert.strictEqual(snapOk, true);

        const inlineValues = await request('textDocument/inlineValue', {
            textDocument: { uri: mainUri },
            range: { start: { line: 13, character: 0 }, end: { line: 17, character: 0 } },
            context: {
                frameId: 1,
                stoppedLocation: { start: { line: 16, character: 0 }, end: { line: 16, character: 15 } }
            }
        });
        assert.ok(
            inlineValues.some(iv => iv.text === 'out = "a"'),
            `expected inline value 'out = "a"', got: ${JSON.stringify(inlineValues)}`
        );

        // 7. shutdown & exit
        await request('shutdown');
        connection.sendNotification('exit');
        console.log('  [PASS] Language Client <-> Language Server E2E over stdio (vscode-jsonrpc)');
    } catch (err) {
        if (logMessages.length > 0) {
            console.error('LanguageServer logs:\n' + logMessages.join('\n'));
        }
        if (stderrBuf) {
            console.error('LanguageServer stderr:\n' + stderrBuf);
        }
        throw err;
    } finally {
        connection.dispose();
        child.kill();
        fs.rmSync(tempDir, { recursive: true, force: true });
    }
}

class DapClient {
    constructor(stdin, stdout) {
        this.stdin = stdin;
        this.stdout = stdout;
        this.seq = 1;
        this.buffer = Buffer.alloc(0);
        this.pendingRequests = new Map();
        this.eventQueue = [];
        this.eventWaiters = [];

        stdout.on('data', chunk => {
            this.buffer = Buffer.concat([this.buffer, chunk]);
            this._drain();
        });
    }

    _drain() {
        while (true) {
            const headerEnd = this.buffer.indexOf('\r\n\r\n');
            if (headerEnd === -1) return;
            const headerStr = this.buffer.slice(0, headerEnd).toString('ascii');
            const match = /Content-Length:\s*(\d+)/i.exec(headerStr);
            if (!match) return;
            const len = parseInt(match[1], 10);
            const total = headerEnd + 4 + len;
            if (this.buffer.length < total) return;

            const body = this.buffer.slice(headerEnd + 4, total).toString('utf8');
            this.buffer = this.buffer.slice(total);

            const msg = JSON.parse(body);
            if (msg.type === 'response') {
                const pending = this.pendingRequests.get(msg.request_seq);
                if (pending) {
                    this.pendingRequests.delete(msg.request_seq);
                    pending.resolve(msg);
                }
            } else if (msg.type === 'event') {
                const idx = this.eventWaiters.findIndex(w => w.eventName === msg.event);
                if (idx >= 0) {
                    const waiter = this.eventWaiters.splice(idx, 1)[0];
                    waiter.resolve(msg);
                } else {
                    this.eventQueue.push(msg);
                }
            }
        }
    }

    sendRequest(command, args = {}, timeoutMs = 10000) {
        const seq = this.seq++;
        const payload = JSON.stringify({ seq, type: 'request', command, arguments: args });
        const bodyBuf = Buffer.from(payload, 'utf8');
        const headerBuf = Buffer.from(`Content-Length: ${bodyBuf.length}\r\n\r\n`, 'ascii');
        this.stdin.write(Buffer.concat([headerBuf, bodyBuf]));

        return new Promise((resolve, reject) => {
            const timer = setTimeout(() => {
                this.pendingRequests.delete(seq);
                reject(new Error(`DAP request '${command}' timed out`));
            }, timeoutMs);
            this.pendingRequests.set(seq, {
                resolve: msg => {
                    clearTimeout(timer);
                    resolve(msg);
                }
            });
        });
    }

    waitForEvent(eventName, timeoutMs = 10000) {
        const idx = this.eventQueue.findIndex(e => e.event === eventName);
        if (idx >= 0) {
            return Promise.resolve(this.eventQueue.splice(idx, 1)[0]);
        }
        return new Promise((resolve, reject) => {
            const timer = setTimeout(() => {
                const wIdx = this.eventWaiters.findIndex(w => w.resolve === wrappedResolve);
                if (wIdx >= 0) this.eventWaiters.splice(wIdx, 1);
                reject(new Error(`Timed out waiting for DAP event '${eventName}'`));
            }, timeoutMs);
            const wrappedResolve = msg => {
                clearTimeout(timer);
                resolve(msg);
            };
            this.eventWaiters.push({ eventName, resolve: wrappedResolve });
        });
    }
}

async function testDapClientServerE2E() {
    const appDll = locateBinary('Old8Lang.App');
    const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'old8lang-vscode-dap-e2e-'));
    const scriptPath = path.join(tempDir, 'debug_target.old8');
    fs.writeFileSync(
        scriptPath,
        [
            'func mul(x: int, y: int) -> int {',  // 1
            '    prod <- x * y',                   // 2
            '    PrintLine(prod)',                 // 3
            '    return prod',                     // 4
            '}',                                   // 5
            'ans <- mul(6, 7)',                    // 6
            ''
        ].join('\n'),
        'utf8'
    );

    const child = cp.spawn('dotnet', [appDll, '--dap'], {
        cwd: tempDir,
        stdio: ['pipe', 'pipe', 'pipe']
    });

    const dap = new DapClient(child.stdin, child.stdout);

    try {
        const initResp = await dap.sendRequest('initialize', { adapterID: 'old8lang' });
        assert.strictEqual(initResp.success, true);
        await dap.waitForEvent('initialized');

        const launchResp = await dap.sendRequest('launch', {
            program: scriptPath,
            cwd: tempDir,
            stopOnEntry: false
        });
        assert.strictEqual(launchResp.success, true);

        const bpResp = await dap.sendRequest('setBreakpoints', {
            source: { path: scriptPath },
            breakpoints: [{ line: 3 }]
        });
        assert.strictEqual(bpResp.body.breakpoints[0].verified, true);

        await dap.sendRequest('configurationDone');
        const stopped = await dap.waitForEvent('stopped');
        assert.strictEqual(stopped.body.reason, 'breakpoint');

        const stack = await dap.sendRequest('stackTrace', { threadId: 1 });
        assert.strictEqual(stack.body.stackFrames[0].name, 'mul');
        assert.strictEqual(stack.body.stackFrames[0].line, 3);

        const frameId = stack.body.stackFrames[0].id;
        const scopes = await dap.sendRequest('scopes', { frameId });
        const varsResp = await dap.sendRequest('variables', {
            variablesReference: scopes.body.scopes[0].variablesReference
        });
        const vars = Object.fromEntries(varsResp.body.variables.map(v => [v.name, v.value]));
        assert.strictEqual(vars.prod, '42');

        const evalResp = await dap.sendRequest('evaluate', { expression: 'prod + 8', frameId });
        assert.strictEqual(evalResp.body.result, '50');

        await dap.sendRequest('continue', { threadId: 1 });
        const outputEvt = await dap.waitForEvent('output');
        assert.ok(outputEvt.body.output.includes('42'), `expected stdout '42', got: ${JSON.stringify(outputEvt.body)}`);
        await dap.waitForEvent('terminated');
        await dap.sendRequest('disconnect');

        console.log('  [PASS] DAP Client <-> Old8Lang.App --dap E2E over stdio');
    } finally {
        child.kill();
        fs.rmSync(tempDir, { recursive: true, force: true });
    }
}

async function main() {
    console.log('Running vscode-old8lang E2E & grammar tests...');
    await testGrammarRules();
    await testLspClientServerE2E();
    await testDapClientServerE2E();
    console.log('All vscode-old8lang tests passed!');
}

main().catch(err => {
    console.error('Test failed:', err);
    process.exit(1);
});
