# High Priority Tasks Implementation Plan

Based on the analysis of `TODO_VirtualMachine.md` and the codebase, the following tasks are identified as high priority:

1.  **String Template (`$"..."`) Support**
2.  **Match Expression Support**

## Status Analysis

*   **String Template**: The bytecode generation logic exists in `BytecodeVisitor.Values.cs`, using `OpCode.CallNative` with `"System.String::Concat"`. However, the `VirtualMachine` does not currently handle this native function call, which would cause a runtime error.
*   **Match Expression**: The bytecode generation logic exists in `BytecodeVisitor.Expressions.cs` and the necessary native helpers (`CheckRange`, `FlattenTuple`, `GetCount`) are already implemented in `VirtualMachine.cs`. It appears to be implemented but marked as unsupported, possibly due to lack of verification.

## Implementation Steps

### 1. Implement String Template Support in VM
*   **File**: `Old8Lang/Bytecode/VirtualMachine.cs`
*   **Action**: Modify `CallNativeFunction` to handle `"System.String::Concat"`.
*   **Logic**:
    ```csharp
    case "System.String::Concat":
        if (args.Length > 0 && args[0] is object[] array)
        {
            return string.Concat(array); // Join array elements
        }
        return string.Concat(args); // Fallback
    ```

### 2. Verify Functionality
*   **String Template Test**: Run `TestFiles/VirtualMachine/StringTemplateTest.old8`.
*   **Match Expression Test**: Run `TestFiles/VirtualMachine/MatchExpressionTest.old8`.

### 3. Update Documentation
*   **File**: `Old8Lang/Docs/TODO_VirtualMachine.md`
*   **Action**: Mark String Templates and Match Expressions as Completed (✅).

## Verification Plan
After applying the changes, I will run the created test files using the VM mode (`-vm` flag) to ensure both features work as expected.
