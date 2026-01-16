I will implement support for **Nullable Types** (`int?`) and **Union Types** (`int | string`) in the Old8Lang Virtual Machine. This addresses high-priority tasks in the "Advanced Type System" section of `TODO_VirtualMachine.md`.

### Implementation Plan

1.  **Enhance Runtime Type Checking (`VirtualMachine.cs`)**
    *   Modify the `OpCode.IsType` instruction handler in `Old8Lang/Bytecode/VirtualMachine.cs`.
    *   Add logic to handle **Nullable Types**: Check if the type string ends with `?`. If so, allow `null` values or check the base type.
    *   Add logic to handle **Union Types**: Check if the type string contains `|`. If so, split the string and check if the value matches *any* of the specified types.

2.  **Enable Type Checking in Bytecode Generation (`BytecodeVisitor.Statements.cs`)**
    *   Modify `VisitSetStatement` in `Old8Lang/AST/Visitor/BytecodeVisitor.Statements.cs`.
    *   Detect if a variable declaration (`SetStatement`) has a type annotation (`AssumptionType`).
    *   If a type annotation is present, generate bytecode to:
        1.  Duplicate the value on the stack (`Dup`).
        2.  Check the value against the type (`IsType`).
        3.  If the check fails, throw a runtime exception (`Throw`).

3.  **Verification**
    *   Create `TestFiles/VirtualMachine/NullableTypeTest.old8` to test nullable types (e.g., `x:int? <- null`).
    *   Create `TestFiles/VirtualMachine/UnionTypeTest.old8` to test union types (e.g., `x:int|string <- "hello"`).
    *   Run these tests using the VM mode (`-vm`).
