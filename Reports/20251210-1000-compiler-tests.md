# 编译模式测试报告

## 测试信息
- 测试时间: 2025-12-10 10:00
- 测试类型: 编译模式测试
- 测试工具: Old8Lang.App
- 测试目录: CompilerTests/

## 测试文件列表

| 文件编号 | 文件名 | 测试结果 | 编译时间(ms) | 执行时间(ms) | 总时间(ms) |
|---------|--------|----------|--------------|--------------|------------|
| 01 | 01_basic_literals.old8 | ✅ 成功 | 38.6845 | 3.3275 | 42.012 |
| 1 | 01_basic_literals.old8 | ✅ 成功 | 35.8451ms | 2.1163ms | 37.961400000000005ms |
| 2 | 02_data_types.old8 | ❌ 失败 |  |  |  |
| 3 | 03_arithmetic_expressions.old8 | ✅ 成功 | 37.2175ms | 2.8208ms | 40.0383ms |
| 4 | 04_comparison_expressions.old8 | ✅ 成功 | 36.2641ms | 2.3675ms | 38.6316ms |
| 5 | 05_logical_expressions.old8 | ❌ 失败 |  |  |  |
| 6 | 06_assignment_expressions.old8 | ❌ 失败 |  |  |  |
| 7 | 07_member_access.old8 | ❌ 失败 |  |  |  |
| 8 | 08_if_elif_else.old8 | ❌ 失败 |  |  |  |
| 9 | 09_for_statement.old8 | ✅ 成功 | 34.7681ms | 2.094ms | 36.8621ms |
| 10 | 10_while_statement.old8 | ✅ 成功 | 33.5614ms | 2.2072ms | 35.7686ms |
| 11 | 11_for_in_statement.old8 | ❌ 失败 |  |  |  |
| 12 | 12_switch_statement.old8 | ✅ 成功 | 36.5156ms | 3.1388ms | 39.654399999999995ms |
| 13 | 13_function_declaration.old8 | ❌ 失败 |  |  |  |
| 14 | 14_lambda_expressions.old8 | ❌ 失败 |  |  |  |
| 15 | 15_class_declaration.old8 | ❌ 失败 |  |  |  |
| 16 | 16_class_methods.old8 | ❌ 失败 |  |  |  |
| 17 | 17_exception_handling.old8 | ❌ 失败 |  |  |  |
| 18 | 18_string_templates.old8 | ❌ 失败 |  |  |  |
| 19 | 19_list_comprehensions.old8 | ✅ 成功 | 39.8748ms | 2.1702ms | 42.045ms |
| 20 | 20_type_annotations.old8 | ❌ 失败 |  |  |  |
| 21 | 21_type_conversion.old8 | ❌ 失败 |  |  |  |
| 22 | 22_scientific_notation.old8 | ✅ 成功 | 30.66ms | 2.2872ms | 32.9472ms |
| 23 | 23_class_inheritance.old8 | ❌ 失败 |  |  |  |
| 24 | 24_throw_statement.old8 | ✅ 成功 | 39.7175ms | 2.1665ms | 41.884ms |
| 25 | 25_type_annotation_restrictions_test.old8 | ❌ 失败 |  |  |  |
| 26 | test_simple_assignment.old8 | ✅ 成功 | 32.2197ms | 2.4045ms | 34.6242ms |
| 27 | type_conversion_enhanced.old8 | ❌ 失败 |  |  |  |
| 1 | 01_basic_literals.old8 | ✅ 成功 | 35.4394ms | 2.2115ms | 37.6509ms |
| 2 | 02_data_types.old8 | ✅ 成功 | 36.5226ms | 3.5121ms | 40.0347ms |
| 3 | 03_arithmetic_expressions.old8 | ✅ 成功 | 33.3995ms | 2.2261ms | 35.625600000000006ms |
| 4 | 04_comparison_expressions.old8 | ✅ 成功 | 35.8032ms | 2.1185ms | 37.921699999999994ms |
| 5 | 05_logical_expressions.old8 | ✅ 成功 | 33.9516ms | 2.3871ms | 36.3387ms |
| 6 | 06_assignment_expressions.old8 | ✅ 成功 | 36.713ms | 2.1718ms | 38.8848ms |
| 7 | 07_member_access.old8 | ❌ 失败 |  |  |  |
| 8 | 08_if_elif_else.old8 | ✅ 成功 | 32.9852ms | 1.994ms | 34.9792ms |
| 9 | 09_for_statement.old8 | ✅ 成功 | 35.1917ms | 2.3021ms | 37.4938ms |
| 10 | 10_while_statement.old8 | ✅ 成功 | 34.9513ms | 2.1603ms | 37.1116ms |
| 11 | 11_for_in_statement.old8 | ❌ 失败 |  |  |  |
| 12 | 12_switch_statement.old8 | ✅ 成功 | 34.4232ms | 2.5949ms | 37.018100000000004ms |
| 13 | 13_function_declaration.old8 | ❌ 失败 |  |  |  |
| 14 | 14_lambda_expressions.old8 | ❌ 失败 |  |  |  |
| 15 | 15_class_declaration.old8 | ❌ 失败 |  |  |  |
| 16 | 16_class_methods.old8 | ❌ 失败 |  |  |  |
| 17 | 17_exception_handling.old8 | ❌ 失败 |  |  |  |
| 18 | 18_string_templates.old8 | ❌ 失败 |  |  |  |
| 19 | 19_list_comprehensions.old8 | ✅ 成功 | 38.2854ms | 2.1268ms | 40.412200000000006ms |
| 20 | 20_type_annotations.old8 | ❌ 失败 |  |  |  |
| 21 | 21_type_conversion.old8 | ❌ 失败 |  |  |  |
| 22 | 22_scientific_notation.old8 | ✅ 成功 | 31.3474ms | 2.2462ms | 33.5936ms |
| 23 | 23_class_inheritance.old8 | ❌ 失败 |  |  |  |
| 24 | 24_throw_statement.old8 | ✅ 成功 | 37.1278ms | 1.9174ms | 39.0452ms |
| 25 | 25_type_annotation_restrictions_test.old8 | ❌ 失败 |  |  |  |
| 26 | test_simple_assignment.old8 | ✅ 成功 | 32.8576ms | 2.4816ms | 35.3392ms |
| 27 | type_conversion_enhanced.old8 | ❌ 失败 |  |  |  |
