I will implement full support for Tuples in the Old8Lang Virtual Machine.

### Implementation Plan

1.  **Analyze Current State**:
    *   `OpCode.NewTuple` is already defined (0x64).
    *   `BytecodeVisitor.VisitTupleLangValue` already implements tuple creation using `OpCode.NewTuple`.
    *   `VirtualMachine` handles `OpCode.NewTuple` by creating a `Tuple<object?, object?>`.
    *   **Missing**: Nested tuple handling in `OpCode.NewTuple` (currently only creates a simple 2-element tuple).
    *   **Missing**: Tuple indexing support in `OpCode.GetIndex`.
    *   **Missing**: Tuple length support in `OpCode.ArrayLength`.
    *   **Missing**: Tuple string representation (optional but good for debugging).

2.  **Enhance `VirtualMachine.cs`**:
    *   **Update `OpCode.NewTuple`**: Modify it to handle multi-element tuples by creating nested `Tuple<object?, object?>` structures, similar to `TupleLangValue`.
    *   **Update `OpCode.GetIndex`**: Add support for indexing into nested `Tuple<object?, object?>` structures. This requires "flattening" the access logic.
    *   **Update `OpCode.ArrayLength`**: Add support for calculating the length of nested tuples.
    *   **Update `OpCode.SetIndex`**: Tuple is immutable, so this should throw a specific error for tuples.

3.  **Verification**:
    *   Create `TestFiles/VirtualMachine/TupleTest.old8` to test:
        *   Tuple creation (2 elements, >2 elements).
        *   Tuple indexing (positive, maybe negative if easy).
        *   Tuple length.
        *   Nested tuple access.
    *   Run the test using `-vm`.

### Key Technical Details

*   **Tuple Structure**: Old8Lang uses `(1, 2, 3)` -> `(1, (2, 3))`. The VM must match this structure to be compatible with the compiler/interpreter logic.
*   **Flattening Index**: When accessing `tuple[2]`, the VM needs to traverse the nested structure: `tuple.Item2.Item1` (if `Item2` is a tuple).
