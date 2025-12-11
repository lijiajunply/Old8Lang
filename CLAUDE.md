# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Language Preference

**IMPORTANT: Always respond in Chinese (中文) when working in this repository.** This is a Chinese language project (Old8Lang - 老八语言), and all communication should be in Chinese unless specifically requested otherwise.

## Project Overview

Old8Lang is a dynamic programming language designed with C#/Java-like syntax, supporting both interpreter and compiler modes. The language features dynamic typing with optional type annotations, classes (without inheritance/generics), JSON operations, and flexible type conversion.

## Solution Structure

The solution consists of 4 main projects:

- **Old8Lang**: Core language library containing AST, parser, interpreter, and compiler
- **Old8Lang.App**: CLI application for running Old8Lang code
- **Old8LangLib**: Native library providing OS, file, network, and terminal utilities
- **Old8Lang.Tests**: Unit tests using xUnit

## Building and Testing

### Build Commands

```bash
# Build entire solution
dotnet build Old8Lang.sln

# Build specific project
dotnet build Old8Lang/Old8Lang.csproj
dotnet build Old8Lang.App/Old8Lang.App.csproj
```

### Running Old8Lang Code

```bash
# Interpreter mode (解释模式) - executes Run() method
dotnet run --project Old8Lang.App -- -f <path-to-file.old8>

# Compiler mode (编译模式) - executes GenerateIl() method
dotnet run --project Old8Lang.App -- -c <path-to-file.old8>

# Syntax-only test (语法测试) - only parses, does not execute
dotnet run --project Old8Lang.App -- -s <path-to-file.old8>
```

### Test Commands

```bash
# Run all unit tests
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj

# Run specific test class
dotnet test --filter "FullyQualifiedName~Old8Lang.Tests.ParsersTests"

# Run test scripts for Old8Lang test files
./run_syntax_tests.sh         # Test syntax parsing only
./run_interpreter_tests.sh    # Test interpreter mode execution
./run_compiler_tests.sh       # Test compiler mode execution
./run_all_compiler_tests.sh   # Run all compiler tests
```

## Architecture

### AST (Abstract Syntax Tree)

The AST is organized in `Old8Lang/AST/`:

- **Expression/**: Expression nodes including operations, values, function calls, member access
  - `Operation.cs`: Binary operations (arithmetic, comparison, logical)
  - `LangId.cs`: Variable identifiers
  - `FuncStatic.cs`: Static function calls
  - `ClassMemberId.cs`: Member access expressions
  - `Value/`: Literal value types (int, double, string, bool, char, arrays, lists, dictionaries, tuples, ranges)
  - `Intermediates/`: Complex expressions (lambdas, instantiation, string templates)

- **Statement/**: Statement nodes
  - `SetStatement.cs`: Variable declaration and assignment
  - `IfStatement.cs` & `IfChild.cs`: Conditional statements
  - `ForStatement.cs`, `ForInStatement.cs`, `WhileStatement.cs`: Loop statements
  - `SwitchStatement.cs`: Switch-case statements
  - `FuncInit.cs`: Function declarations
  - `ClassInit.cs`: Class declarations
  - `ImportStatement.cs`: Import statements
  - `NativeStatement.cs`: Native C# method bindings
  - `BlockStatement.cs`: Block statements
  - `ReturnStatement.cs`, `BreakStatement.cs`, `ContinueStatement.cs`: Control flow

- **Visitor/**: Visitor pattern implementation for AST traversal

### Parser and Interpreter

Located in `Old8Lang/LangParser/`:

- `LangParser.cs`: Main parser (手写递归下降解析器) - converts tokens to AST
- `LangToken.cs` & `LangTokenType.cs`: Token definitions and lexical analysis
- `LangInterpreter.cs`: Interpreter orchestrating parsing and execution
- `VariateManager.cs`: Variable storage and scope management

### Compiler

Located in `Old8Lang/Compiler/`:

- `Compiler.cs`: Compiles AST to intermediate code (IL-like)
- `LocalManager.cs`: Manages local variables during compilation

### Error Handling

Located in `Old8Lang/Error/`: Contains various exception types for syntax errors, runtime errors, type errors, etc.

## Testing Conventions

### Test File Organization

Old8Lang test files use `.old8` extension and must follow Old8Lang syntax (see `Old8Lang_Grammar.md`).

Test files are organized by mode:
- `SyntaxTests/`: Syntax parsing tests (no execution)
- `InterpreterTests/`: Interpreter mode tests
- `CompilerTests/`: Compiler mode tests

### Test File Marking

- Files expecting **errors** should end with a line containing "error" (case-insensitive)
- Test scripts check this marker to determine expected pass/fail

### Test Reports

After testing, generate test reports in `Reports/` directory:
- Use Markdown format
- Filename convention: `日期-小时-分钟-测试类型.md`
- Include test file paths and execution results

## Language Syntax Reference

Full syntax is documented in `Old8Lang_Grammar.md` and EBNF in `Old8Lang.ebnf`.

Key syntax elements:
- Assignment: `<-` operator (e.g., `a <- 123`)
- Type annotations: `:type` syntax (e.g., `a:int <- 123`)
- Functions: `func name(params) { }` or `name(params) -> { }`
- Classes: `class Name { }` with `public`/`private`/`static` modifiers
- Control flow: `if/elif/else`, `for/while/for-in`, `switch/case/default`
- String templates: `$"text {expr}"` (C# style)
- Comments: `//` (NOT `#`)
- ToString Method: `.ToStr()` (NOT `.ToString()`)

## Development Workflow

### Adding New Syntax

When adding new language features:

1. **Syntax testing**: Add test to `SyntaxTests/`, ensure parsing works
2. **Interpreter testing**: Add test to `InterpreterTests/`, verify interpreter execution
3. **Compiler testing**: Add test to `CompilerTests/`, verify compiler execution
4. **Documentation**: Update `Old8Lang.ebnf` and `Old8Lang_Grammar.md`

### Visitor Pattern Implementation

The codebase recently transitioned to visitor pattern for AST traversal. When working with AST nodes:
- Implement `Accept` methods for visitor pattern
- Check `Old8Lang/AST/Visitor/` for visitor interfaces
- See recent commits (3ced530, 39ae648) for visitor pattern refactoring examples

### Recent Refactoring

Recent changes include:
- Visitor pattern implementation for AST nodes
- AST expression type system refactoring
- Renamed `OldIf` to `IfChild`
- Removed `GetChildType()` method from `ILangList` interface

If encountering issues with these changes, refer to commits: c515c17, 835c531, 39ae648, 3ced530.
