# 创建 API 模式对比文档计划

我将为您创建一个详细的文档 `Docs/API_Mode_Comparison.md`，对比 Old8Lang 在解释器、编译器和虚拟机模式下的内部 API 实现机制。

## 1. 文档结构
文档将包含以下主要部分：
1.  **概述**: 简述三种模式的设计目标（灵活性 vs 性能 vs 可移植性）。
2.  **核心架构对比**:
    *   统一接口 (`IGlobalFunction`)
    *   执行机制 (运行时 vs IL 生成 vs 字节码)
3.  **API 实现机制详解**:
    *   **全局函数**: 对比 `ExecuteInternal`、`GenerateIlInternal` 和 `ExecuteInVMInternal`。
    *   **标准库 (Old8LangLib)**: 对比加载机制 (`StandardLibraryLoader` vs `ModuleRegistry`)。
    *   **基本类型方法**: 对比扩展方法模式在三种模式下的不同调用方式（反射 vs 静态调用重写 vs `OpCode.CallMethod`）。
4.  **功能支持矩阵**: 基于 `Mode_Support_Summary.md` 和 `TODO` 文件，列出各模式对关键 API 的支持情况。
5.  **代码参考**: 链接到关键的实现文件。

## 2. 实施步骤
1.  使用 `Write` 工具创建 `/Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang/Docs/API_Mode_Comparison.md`。
2.  内容将基于之前的调研结果，采用 Markdown 表格和代码块进行清晰展示。
3.  确保文档风格与现有文档保持一致。

## 3. 预期产出
*   一个名为 `Docs/API_Mode_Comparison.md` 的新文件，为您提供关于 Old8Lang 内部机制的清晰视图。
