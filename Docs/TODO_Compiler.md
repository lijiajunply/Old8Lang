# Compiler Todo List

## High Priority
- [ ] **Fix `return` statement in `if` block**
  - **Issue**: `return` in `if` block might cause "Invalid program" or stack imbalance if not handled correctly with `Ret`.
  - **Status**: Needs investigation.

- [ ] **Implement `try-catch-finally`**
  - **Status**: Partially implemented (AST nodes exist), but `CompilerVisitor` needs full support.
  - **Ref**: `TryStatement.cs`, `ThrowStatement.cs`.

## Medium Priority
- [x] **Implement Generic Function Support**
  - **Status**: ✅ Completed.
  - **Details**: 
    - Fixed `FuncInit` to defer code generation for generic functions.
    - Fixed `GenericMethodSpecializer` to handle `ReturnValueLocal` and `ReturnLabel`.
    - Verified with explicit type arguments (e.g., `func<int>()`).
    - *Note*: Implicit type inference for generic functions is not yet supported in compiler mode.

- [x] **Implement Generic Class Support**
  - **Status**: ✅ Completed.
  - **Details**:
    - Fixed `FuncInit`, `GenericMethodSpecializer`, and `GenericClassSpecializer` to propagate `GenericClasses` context.
    - Implemented `init` method call for generic class instantiation in `GenericInstanceExpression`.
    - Verified with basic generic classes (e.g., `Box<T>`).
    - *Note*: Complex generic classes with multiple parameters might still have issues.

- [ ] **Optimize `switch` statement**
  - **Status**: Currently implemented as `if-else` chain. Can be optimized using `Switch` instruction for integer/string.

## Low Priority
- [ ] **Support `defer` in Generic Methods**
  - **Issue**: `GenericMethodSpecializer` currently does not wrap body in `try-finally` for `defer`.
  - **Impact**: `defer` statement will be ignored or cause errors in generic functions.

- [ ] **Optimize Variable Access**
  - **Current**: Dictionary lookup.
  - **Goal**: Array indexing or direct IL local mapping.

## Known Issues
- Implicit type inference for generic function calls (e.g., `identity(1)`) throws error in compiler mode.
