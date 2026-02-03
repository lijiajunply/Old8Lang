# CLAUDE.md

use Chinese to answer the questions

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Old8Lang is a dynamically-typed programming language implemented in C# (.NET 10.0). It supports two execution modes:
- **Interpretation Mode**: Direct AST execution for rapid development and debugging
- **Compilation Mode**: IL (Intermediate Language) code generation for better performance

The language features functions, classes, exception handling, async/await, generics, and a comprehensive standard library.

## Build and Run Commands

### Build the Project
```bash
dotnet build Old8Lang.sln
```

### Run Tests
```bash
# Run all tests
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj

# Run specific test
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --filter "FullyQualifiedName~TestName"
```

### Run Old8Lang Code

**Interpretation Mode** (fast development, flexible):
```bash
dotnet run --project Old8Lang.App -- -f <file.old8>
```

**Compilation Mode** (better performance, stricter type checking):
```bash
dotnet run --project Old8Lang.App -- -c <file.old8>
```

**Syntax Check Only**:
```bash
dotnet run --project Old8Lang.App -- -s <file.old8>
```

**Bytecode VM Mode** (experimental):
```bash
dotnet run --project Old8Lang.App -- -vm <file.old8>
```

**Debug Mode** (enable verbose logging):
```bash
dotnet run --project Old8Lang.App -- -f <file.old8> -d
# Or with specific log level: --log-level debug|info|warning|error
```

### Package Management Commands

```bash
# Initialize a new project
dotnet run --project Old8Lang.App -- init <project-name>

# Install a package
dotnet run --project Old8Lang.App -- install <package-name>

# Remove a package
dotnet run --project Old8Lang.App -- remove <package-name>

# List installed packages
dotnet run --project Old8Lang.App -- list

# Publish a package (one-step: pack + sign)
dotnet run --project Old8Lang.App -- publish -c cert.pfx -p password
```

## High-Level Architecture

### Core Processing Pipeline

```
Source Code
    ↓
LangParser (Tokenization + Parsing)
    ↓
Abstract Syntax Tree (AST)
    ├→ Interpretation Mode → InterpreterVisitor → Direct Execution
    ├→ Compilation Mode → CompilerVisitor → IL Generation → Execution
    └→ Bytecode Mode → BytecodeVisitor → Bytecode → VM Execution
```

### Key Architectural Patterns

**1. Visitor Pattern**
- All AST nodes implement `Accept<TResult>(IVisitor<TResult> visitor)`
- Multiple visitor implementations:
  - `InterpreterVisitor` - Executes AST in interpretation mode
  - `CompilerVisitor` - Generates IL code for compilation mode
  - `BytecodeVisitor` - Generates bytecode for VM execution
  - `TypeInferenceVisitor` - Performs type inference

**2. Dual-Mode Execution**
- Each AST node implements both:
  - `Run(VariateManager manager)` - For interpretation mode
  - `GenerateIl(ILGenerator ilGenerator, LocalManager local)` - For compilation mode
- This allows the same AST to be executed in either mode

**3. Symbol Table Management**
- `VariateManager` - Manages variables and scopes during interpretation
- `SymbolTableCache` - Caches symbol information for performance
- `TypeAnnotationManager` - Manages type annotations

**4. Type System**
- `TypeChecker` - Validates type correctness
- `TypeInferenceEngine` - Infers types from context
- `GenericTypeInference` - Handles generic type parameters
- Compilation mode enforces stricter type checking than interpretation mode

### Directory Structure

**Core Language Implementation** (`Old8Lang/`):
- `AST/` - Abstract Syntax Tree node definitions
  - `Expression/` - Expression nodes (literals, operations, function calls)
  - `Statement/` - Statement nodes (assignments, loops, conditionals)
  - `Visitor/` - Visitor pattern implementations
- `LangParser/` - Lexical and syntax analysis
  - `Core/` - Parser core logic
  - `Parsers/` - Specialized parsers for different constructs
- `Compiler/` - IL code generation for compilation mode
- `Interpreter/` - Runtime execution for interpretation mode
- `TypeSystem/` - Type checking and inference
- `Bytecode/` - Bytecode VM implementation
- `ModuleSystem/` - Module loading and resolution
- `GlobalFunctions/` - Built-in global functions
- `StandardLibrary/` - Standard library implementation
- `Error/` - Error types and exception handling

**Application Layer** (`Old8Lang.App/`):
- `Program.cs` - CLI entry point with command registry
- `Commands/` - Command implementations (FromFileCommand, CompilerCommand, etc.)

**Standard Libraries**:
- `Old8LangLib/` - Core standard library (Math, String, File, Collection)
- `Old8Lang.NetLib/` - Network functionality (HTTP, MQTT, WebSocket)
- `Old8Lang.SerializationLib/` - Serialization support
- `Old8Lang.MachineLearningLib/` - ML capabilities
- `Old8Lang.DatabaseLib/` - Database operations
- `Old8Lang.LanguageServer/` - LSP server for IDE integration
- `Old8Lang.FirstUI/` - UI framework

**Testing** (`Old8Lang.Tests/`):
- Uses xUnit framework
- Test directories mirror the main codebase structure

## Important Implementation Details

### Adding New Language Features

1. **Define AST Node**: Create new node class in `AST/Expression/` or `AST/Statement/`
2. **Implement Dual Execution**:
   - `Run(VariateManager manager)` for interpretation mode
   - `GenerateIl(ILGenerator ilGenerator, LocalManager local)` for compilation mode
3. **Add Visitor Support**: Implement `Accept(IVisitor visitor)` method
4. **Update Parser**: Add parsing logic in appropriate parser class in `LangParser/Parsers/`
5. **Add Tests**: Create test cases in both `InterpreterTests/` and `CompilerTests/`

### Working with the Parser

- Entry point: `LangParser.Parse(string code)` returns AST
- Token definitions: `LangToken.cs` and `LangTokenType.cs`
- Parser uses recursive descent parsing
- Each language construct has a dedicated parser in `LangParser/Parsers/`

### Type System Considerations

- Interpretation mode is dynamically typed with runtime type checking
- Compilation mode requires type annotations and performs static type checking
- Generic types are supported: `list<T>`, `array<T>`, `dict<K,V>`
- Type inference engine can infer types from context in many cases

### Module System

- Modules are loaded via `import "ModuleName"` syntax
- Module resolution checks:
  1. Project-level packages (if `o8packages.json` exists)
  2. Global packages
  3. Standard library
- Module caching prevents duplicate loading

### Package Management

- Project mode: Detected by presence of `o8packages.json`
- Global mode: Used when no project configuration exists
- Packages are stored in `.o8pkg` format (compressed archives)
- Package signing and verification supported via certificates

## Common Development Patterns

### Reading Code Before Modifying

Always read the relevant files before making changes. The codebase uses consistent patterns:
- AST nodes follow a standard structure with `Run()` and `GenerateIl()` methods
- Parsers follow recursive descent pattern
- Error handling uses custom exception types in `Error/` directory

### Testing Changes

Test both execution modes when adding features:
```bash
# Test interpretation mode
dotnet run --project Old8Lang.App -- -f path/to/test.old8

# Test compilation mode
dotnet run --project Old8Lang.App -- -c path/to/test.old8

# Run unit tests
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj
```

### Debugging

Enable debug output to see detailed execution logs:
```bash
dotnet run --project Old8Lang.App -- -f test.old8 -d
```

This shows:
- Token stream from lexer
- AST structure
- IL generation steps (in compilation mode)
- Variable scope changes
- Type inference decisions

## Key Files to Understand

- `Old8Lang/LangParser/LangParser.cs` - Main parser entry point
- `Old8Lang/Interpreter/LangInterpreter.cs` - Interpretation mode executor
- `Old8Lang/Compiler/Compiler.cs` - Compilation mode IL generator
- `Old8Lang/AST/LangExpression.cs` - Base class for all expressions
- `Old8Lang/AST/OldStatement.cs` - Base class for all statements
- `Old8Lang/Interpreter/VariateManager.cs` - Variable/scope management
- `Old8Lang.App/Program.cs` - CLI command registry and entry point

## Documentation

Comprehensive documentation is available in the `Docs/` directory:
- `ARCHITECTURE.md` - Detailed architecture documentation
- `CLI_GUIDE.md` - Complete CLI command reference
- `LANGUAGE_FEATURES.md` - Language feature documentation
- `API_REFERENCE.md` - API documentation
- `Old8Lang_Grammar.md` - Formal grammar specification
