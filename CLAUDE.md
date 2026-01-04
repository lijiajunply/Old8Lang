# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Language Preference

**IMPORTANT: Always respond in Chinese (中文) when working in this repository.** This is a Chinese language project (Old8Lang - 老八语言), and all communication should be in Chinese unless specifically requested otherwise.

## Project Overview

Old8Lang is a dynamic programming language designed with C#/Java-like syntax, supporting both interpreter and compiler modes. The language features dynamic typing with optional type annotations, classes (without inheritance/generics), JSON operations, and flexible type conversion.

## Solution Structure

The solution consists of 6 main projects:

- **Old8Lang**: Core language library containing AST, parser, interpreter, and compiler
- **Old8Lang.App**: CLI application for running Old8Lang code
- **Old8LangLib**: Native library providing OS, file, network, and terminal utilities
- **Old8Lang.NetLib**: Network library providing MQTT and advanced networking features (requires MQTTnet)
- **Old8Lang.Tests**: Unit tests using xUnit
- **Old8Lang.Benchmarks**: Performance benchmarks using BenchmarkDotNet

## Building and Testing

**Requirements**: .NET 10.0 SDK

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
./run_syntax_tests.sh                  # Test syntax parsing only
./run_interpreter_tests.sh             # Test interpreter mode execution
./run_compiler_tests.sh                # Test compiler mode execution
./run_comprehensive_compiler_tests.sh  # Run all compiler tests with detailed report

# Run performance benchmarks
dotnet run --project Old8Lang.Benchmarks --configuration Release
```

## Architecture

### AST (Abstract Syntax Tree)

The AST is organized in `Old8Lang/AST/`:

- **Expression/**: Expression nodes including operations, values, function calls, member access
  - `Operation.cs`: Binary operations (arithmetic, comparison, logical)
  - `LangId.cs`: Variable identifiers
  - `FunctionCallExpression.cs`: Function call expressions
  - `ClassMemberId.cs`: Member access expressions
  - `Value/`: Literal value types (int, double, string, bool, char, arrays, lists, dictionaries, tuples, ranges)
  - `Intermediates/`: Complex expressions (lambdas, instantiation, string templates)

- **Statement/**: Statement nodes
  - `SetStatement.cs`: Variable declaration and assignment
  - `IfStatement.cs` & `IfChild.cs`: Conditional statements
  - `ForStatement.cs`, `ForInStatement.cs`, `WhileStatement.cs`: Loop statements
  - `SwitchStatement.cs`: Switch-case statements
  - `FuncInit.cs`, `AsyncFuncInit.cs`: Function declarations
  - `ClassInit.cs`: Class declarations
  - `ImportStatement.cs`: Import statements
  - `NativeStatement.cs`: Native C# method bindings
  - `BlockStatement.cs`: Block statements
  - `ReturnStatement.cs`, `BreakStatement.cs`, `ContinueStatement.cs`: Control flow
  - `TryStatement.cs`, `ThrowStatement.cs`: Exception handling
  - `YieldStatement.cs`: Generator support
  - `UsingStatement.cs`: Resource management with automatic disposal
  - `SelectStatement.cs`: Channel multiplexing (Go-style select)

- **Visitor/**: Visitor pattern implementation for AST traversal

### Parser and Interpreter

Located in `Old8Lang/LangParser/`:

- `LangParser.cs`: Main parser (手写递归下降解析器) - converts tokens to AST
- `LangToken.cs` & `LangTokenType.cs`: Token definitions and lexical analysis
- `LangInterpreter.cs`: Interpreter orchestrating parsing and execution
- `VariateManager.cs`: Variable storage and scope management
- `Parsers/`: Specialized parsers
  - `ExpressionParser.cs`: Expression parsing
  - `StatementParser.cs`: Statement parsing
  - `FunctionParser.cs`: Function declaration parsing
  - `ClassParser.cs`: Class declaration parsing
  - `PrimaryParser.cs`: Primary expression parsing

### Compiler

Located in `Old8Lang/Compiler/`:

- `Compiler.cs`: Compiles AST to intermediate code (IL-like)
- `LocalManager.cs`: Manages local variables during compilation
- `ILVerifier.cs`: Verifies generated IL code
- `TypeConversion.cs`: Handles type conversions
- `AsyncStateMachineGenerator.cs`: Generates async state machines

### Type System

Located in `Old8Lang/TypeSystem/`:

- `TypeInferenceEngine.cs`: Progressive type inference engine (TypeScript-style)
- `TypeConstraintCollector.cs`: Collects type constraints from AST
- `TypeConstraintSolver.cs`: Solves type constraints using iterative algorithm
- `TypeInferenceConfig.cs`: Configuration for type inference behavior
- `TypeChecker.cs`: Global type validation
- `TypeAnnotationManager.cs`: Type annotation parsing and management

**Note**: Type inference is **enabled by default**. To disable if needed:
```csharp
Compiler.EnableTypeInference = false;
TypeInferenceConfig.Instance.EnableTypeInference = false;
```

See `Docs/TypeInference.md` for detailed documentation.

### Compiler Configuration

The compiler provides several configuration options in `Compiler.cs`:

- `DebugOutputEnabled`: Enable/disable debug output (default: false)
- `ilVerificationEnabled`: Enable/disable IL code verification (default: true)
- `CurrentLogLevel`: Set logging level (Error, Warning, Info, Debug) (default: Info)

Example usage in code:
```csharp
Compiler.DebugOutputEnabled = true;
Compiler.CurrentLogLevel = Compiler.LogLevel.Debug;
```

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

Full syntax is documented in `Old8Lang_Grammar.md` and EBNF in `Old8Lang/Old8Lang.ebnf`.

Key syntax elements:
- Assignment: `<-` operator (e.g., `a <- 123`)
- Type annotations: `:type` syntax (e.g., `a:int <- 123`)
- Functions: `func name(params) { }` or `name(params) -> { }`
- Classes: `class Name { }` with `public`/`private`/`static` modifiers
- Control flow: `if/elif/else`, `for/while/for-in`, `switch/case/default`
- Resource management: `using` statement for automatic disposal
- Channel multiplexing: `select` statement (Go-style)
- Lists: `{1, 2, 3}` (braces for list literals)
- Dictionaries: `{"key": value}` (braces with colons for dictionary literals)
- Arrays: `[1, 2, 3]` (square brackets for array literals)
- String templates: `$"text {expr}"` (C# style)
- Comments: `//` (NOT `#`)
- ToString Method: `.ToStr()` (NOT `.ToString()`)

## Development Workflow

### Adding New Syntax

When adding new language features:

1. **Syntax testing**: Add test to `SyntaxTests/` (or `Old8Lang.Tests/Parser/`), ensure parsing works
2. **Interpreter testing**: Add test to `InterpreterTests/` (or `Old8Lang.Tests/Interpreter/`), verify interpreter execution
3. **Compiler testing**: Add test to `CompilerTests/` (or `Old8Lang.Tests/Compiler/`), verify compiler execution
4. **Documentation**: Update `Old8Lang/Old8Lang.ebnf` and `Old8Lang_Grammar.md`

### Changelog Guidelines

When updating CHANGELOG (both `CHANGELOG.en-US.md` and `CHANGELOG.zh-CN.md`):

- **Write from developer perspective**: Describe problems and their impact on developers, not implementation details
- **Describe original user problems**: Focus on what issue users experienced, not how you solved it
- **Skip minor changes**: Don't mention documentation fixes, minor style optimizations, or code refactoring that users won't notice
- **Mark non-syntax changes**: Use "-" prefix for tooling/infrastructure changes (e.g., build tools, development workflow)
- **Keep it meaningful**: Only include changes that affect user experience or API usage

### Testing and Bug Fixing Principles

**IMPORTANT**: When fixing failing tests, follow these principles:

1. **Fix Code Logic, Not Test Expectations**:
   - **Never** modify test expectations to accommodate buggy implementations
   - **Always** fix the underlying code logic to produce the correct expected behavior
   - Test failures indicate bugs in the implementation, not incorrect expectations

2. **Root Cause Analysis**:
   - Investigate why the actual result differs from the expected result
   - Understand the language specification and intended behavior
   - Identify the exact point where the implementation deviates from expectations

3. **Proper Fix Implementation**:
   - Fix the actual bug in the code implementation
   - Ensure the fix aligns with the language design and specification
   - Verify that the fix doesn't break other functionality

4. **Example of Wrong Approach**:
   ```csharp
   // WRONG: Changing expectation to match buggy behavior
   Assert.Equal(8, actualValue); // Because implementation returns 8 instead of 16
   ```

5. **Example of Right Approach**:
   ```csharp
   // RIGHT: Keep correct expectation and fix the implementation
   Assert.Equal(16, actualValue); // Because 4 * 4 should equal 16
   // Then go fix the code that produces 8 instead of 16
   ```

6. **When to Modify Tests**:
   - Only modify tests when the test itself is wrong (e.g., misunderstanding of language semantics)
   - Never modify tests just to make them pass with buggy implementations
   - If uncertain about expected behavior, consult language specification or project maintainers

### Compiler Mode Type Annotation Rules

**IMPORTANT**: Compiler mode (`-c`) and interpreter mode (`-f`) have different type annotation requirements:

| Feature | Interpreter Mode | Compiler Mode |
|---------|-----------------|---------------|
| Function parameter type annotations | Optional | **Required** (or default value) |
| Function default parameter inference | Supported | **Supported** |
| Function return type annotations | Optional (inferred) | **Required** |
| Lambda parameter type annotations | Optional | **Required** |
| Lambda return type annotations | Optional (inferred) | Optional (inferred) |

#### Function Parameter Type Requirements

In compiler mode, function parameters must satisfy **one of the following**:
1. **Explicit type annotation**: `param:int`
2. **Default value for type inference**: `param: 123`

Compiler mode examples:

```old8
// Method 1: Explicit type annotations
func add(a:int, b:int) -> int {
    return a + b
}

// Method 2: Default value inference
func greet(name:string, message: "Hello") -> void {
    PrintLine(message + ", " + name)
}

// Method 3: Mixed approach
func calculate(x:int, y: 0, operation: "add") -> int {
    if operation == "add" {
        return x + y
    } else {
        return x * y
    }
}

// Correct: Lambda with parameter types, return type can be inferred
transform <- (n:int) -> n * 2
```

Error examples:

```old8
// Error: missing return type
func calculate(x:int, y:int) {
    return x + y
}

// Error: parameter has neither type annotation nor default value
func calculate(x, y) -> int {
    return x + y
}

// Error: Lambda missing parameter type
transform <- (n) -> n * 2
```

#### Default Parameter Type Inference

When a parameter has a default value, the compiler infers its type:

```old8
func example(
    intParam: 123,           // inferred as int
    doubleParam: 3.14,       // inferred as double
    stringParam: "text",     // inferred as string
    boolParam: true          // inferred as bool
) -> void {
    // function body
}
```

**Known Limitations**:
- Default parameter inference works for **validation** (passes type checking)
- Due to known IL generation issues, functions with default parameters may encounter runtime issues in compiler mode
- Interpreter mode handles default parameters correctly

**Note**: Interpreter mode remains flexible and allows type inference for all cases.

### Concurrency Primitives

Old8Lang provides built-in global functions for concurrency primitives. These are NOT imported from a library but are natively available:

**Mutex (5 functions)**:
- `MutexCreate()` → int
- `MutexLock(mutexId:int)` → void
- `MutexTryLock(mutexId:int, timeoutMs:int)` → bool
- `MutexUnlock(mutexId:int)` → void
- `MutexDispose(mutexId:int)` → void

**Semaphore (5 functions)**:
- `SemaphoreCreate(initialCount:int, maxCount:int)` → int
- `SemaphoreAcquire(semaphoreId:int)` → void
- `SemaphoreTryAcquire(semaphoreId:int, timeoutMs:int)` → bool
- `SemaphoreRelease(semaphoreId:int)` → void
- `SemaphoreDispose(semaphoreId:int)` → void

**AtomicInt (8 functions)**:
- `AtomicIntCreate(initialValue:int)` → int
- `AtomicIntGet(atomicId:int)` → int
- `AtomicIntSet(atomicId:int, newValue:int)` → void
- `AtomicIntIncrement(atomicId:int)` → int
- `AtomicIntDecrement(atomicId:int)` → int
- `AtomicIntAdd(atomicId:int, delta:int)` → int
- `AtomicIntCompareAndSet(atomicId:int, expectedValue:int, newValue:int)` → bool
- `AtomicIntDispose(atomicId:int)` → void

**Channel (8 functions)**:
- `ChannelCreate()` → int
- `ChannelCreateBounded(capacity:int)` → int
- `ChannelSend(channelId:int, value:object)` → void
- `ChannelTrySend(channelId:int, value:object, timeoutMs:int)` → bool
- `ChannelReceive(channelId:int)` → object
- `ChannelTryReceive(channelId:int, timeoutMs:int)` → object?
- `ChannelClose(channelId:int)` → void
- `ChannelDispose(channelId:int)` → void

**ReadWriteLock (8 functions)**:
- `ReadWriteLockCreate()` → int
- `ReadLockAcquire(lockId:int)` → void
- `ReadLockRelease(lockId:int)` → void
- `WriteLockAcquire(lockId:int)` → void
- `WriteLockRelease(lockId:int)` → void
- `ReadLockTryAcquire(lockId:int, timeoutMs:int)` → bool
- `WriteLockTryAcquire(lockId:int, timeoutMs:int)` → bool
- `ReadWriteLockDispose(lockId:int)` → void

**CountDownLatch (6 functions)**:
- `CountDownLatchCreate(count:int)` → int
- `CountDownLatchCountDown(latchId:int)` → void
- `CountDownLatchWait(latchId:int)` → void
- `CountDownLatchWaitTimeout(latchId:int, timeoutMs:int)` → bool
- `CountDownLatchGetCount(latchId:int)` → int
- `CountDownLatchDispose(latchId:int)` → void

**CyclicBarrier (6 functions)**:
- `CyclicBarrierCreate(participantCount:int)` → int
- `CyclicBarrierAwait(barrierId:int)` → void
- `CyclicBarrierAwaitTimeout(barrierId:int, timeoutMs:int)` → bool
- `CyclicBarrierGetParticipantCount(barrierId:int)` → int
- `CyclicBarrierGetWaitingCount(barrierId:int)` → int
- `CyclicBarrierDispose(barrierId:int)` → void

**CancellationTokenSource (4 functions)**:
- `CreateCancellationTokenSource()` → int
- `Cancel(ctsId:int)` → void
- `CancelAfter(ctsId:int, delayMs:int)` → void
- `DisposeCancellationTokenSource(ctsId:int)` → void

**Utility Functions (3 functions)**:
- `Sleep(milliseconds:int)` → void
- `GetCurrentThreadId()` → int
- `GetProcessorCount()` → int

### Using Statement

The `using` statement provides automatic resource management with disposal:

```old8
// Form 1: With variable declaration
using mutex <- MutexCreate() {
    MutexLock(mutex)
    // ... critical section
    MutexUnlock(mutex)
}  // MutexDispose(mutex) called automatically

// Form 2: With existing variable
ch <- ChannelCreate()
using ch {
    ChannelSend(ch, data)
}  // ChannelDispose(ch) called automatically
```

**How it works**:
- Resources are automatically disposed when exiting the `using` block
- Works with any resource returning an ID (int) that has a corresponding Dispose function
- Uses try-finally internally to ensure disposal even on exceptions
- Supported in both interpreter and compiler modes

### Select Statement

The `select` statement enables Go-style channel multiplexing:

```old8
select {
    case ch1 <- 100 -> {
        PrintLine("Sent 100 to ch1")
    }
    case val from ch2 -> {
        PrintLine("Received from ch2: " + val.ToStr())
    }
    default -> {
        PrintLine("No channel ready")
    }
}
```

**Syntax**:
- **Send operation**: `case channel <- value -> { ... }`
- **Receive operation**: `case variable from channel -> { ... }`
- **Default case**: `default -> { ... }`

**How it works**:
- Uses polling strategy to check multiple channels
- Executes the first available case (send or receive)
- If no case is ready and `default` exists, executes default immediately
- If no case is ready and no default, blocks until a case becomes available

**Implementation**:
- ✅ Fully supported in both interpreter mode (`-f`) and compiler mode (`-c`)
- IL code generation implemented with proper channel polling logic

### Visitor Pattern Implementation

**Note**: The codebase is in the process of transitioning to visitor pattern:
- AST nodes are being refactored to support visitor pattern
- When working with AST nodes, be aware that the visitor pattern implementation is ongoing

### Recent Refactoring

Recent changes include:
- **Native Concurrency Primitives**: Migrated from AsyncLib to global functions (Mutex, Semaphore, AtomicInt, Channel, ReadWriteLock, CountDownLatch, CyclicBarrier, CancellationTokenSource)
- **Using Statement**: Added automatic resource management with disposal
- **Select Statement**: Added channel multiplexing (Go-style select)
- Visitor pattern implementation for AST nodes
- AST expression type system refactoring
- Renamed `OldIf` to `IfChild`
- Removed `GetChildType()` method from `ILangList` interface
