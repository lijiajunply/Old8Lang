#!/bin/bash

files=(
"./CompilerTests/35_advanced_control_flow.old8"
"./CompilerTests/21_type_conversion.old8"
"./CompilerTests/29_large_data_structures.old8"
"./CompilerTests/34_complex_data_structures.old8"
"./CompilerTests/39_performance_fibonacci.old8"
"./CompilerTests/40_performance_array_operations.old8"
"./CompilerTests/13_function_declaration.old8"
"./CompilerTests/27_deep_recursion.old8"
"./CompilerTests/07_member_access.old8"
"./CompilerTests/16_class_methods.old8"
"./CompilerTests/18_string_templates.old8"
"./CompilerTests/14_lambda_expressions.old8"
"./CompilerTests/25_type_annotation_restrictions_test.old8"
"./CompilerTests/22_scientific_notation.old8"
"./CompilerTests/42_data_dictionary_operations.old8"
"./CompilerTests/32_boundary_conditions.old8"
"./CompilerTests/31_math_library.old8"
"./CompilerTests/comprehensive_try_catch.old8"
"./CompilerTests/43_statement_nested_loops.old8"
"./CompilerTests/33_error_handling.old8"
"./CompilerTests/20_type_annotations.old8"
"./CompilerTests/30_time_library.old8"
"./CompilerTests/type_conversion_enhanced.old8"
"./CompilerTests/23_class_inheritance.old8"
"./CompilerTests/15_class_declaration.old8"
"./CompilerTests/28_large_loops.old8"
)

for file in "${files[@]}"; do
    echo "=== Testing: $file ==="
    output=$(dotnet run --project Old8Lang.App -- -c "$file" 2>&1)

    if echo "$output" | grep -q "SyntaxError"; then
        echo "ERROR TYPE: SyntaxError"
        echo "$output" | grep "SyntaxError" | head -1
    elif echo "$output" | grep -q "InvalidProgramException"; then
        echo "ERROR TYPE: InvalidProgramException"
    elif echo "$output" | grep -q "NotImplementedException"; then
        echo "ERROR TYPE: NotImplementedException"
        echo "$output" | grep "NotImplementedException" | head -1
    elif echo "$output" | grep -q "NullReferenceException"; then
        echo "ERROR TYPE: NullReferenceException"
    elif echo "$output" | grep -q "编译成功"; then
        echo "STATUS: Compiled successfully"
        if echo "$output" | grep -q "Unhandled exception"; then
            echo "ERROR TYPE: Runtime Exception"
            echo "$output" | grep "Unhandled exception" | head -1
        fi
    else
        echo "ERROR TYPE: Unknown"
    fi
    echo ""
done
