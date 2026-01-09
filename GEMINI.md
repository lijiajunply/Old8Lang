# Old8Lang Project Overview

This document provides a comprehensive overview of the Old8Lang project, including its purpose, architecture, and instructions for building and running the code.

## 1. Project Purpose

Old8Lang is a programming language developed for Xi'an University of Architecture and Technology. It is a dynamic language with features such as:

- **Dynamic Typing**: Supports weak typing and type inference.
- **Generics**: Allows for generic functions, classes, and collection types.
- **Native JSON Support**: Provides built-in capabilities for handling JSON.
- **Dual-Mode Execution**: Can be run in either interpreted or compiled mode, offering a balance between flexibility and performance.
- **Package Management**: Includes a smart package manager that supports both project-level virtual environments and global packages.

The project is owned by "LuckyFish" and the "Xi'an University of Architecture and Technology iOS众创空间俱乐部."

## 2. Project Architecture

The Old8Lang project is a .NET solution composed of multiple C# projects. The solution file, `Old8Lang.sln`, defines the structure and dependencies of these projects. The key components include:

- **`Old8Lang`**: The core project that contains the main logic for the language, including the interpreter, compiler, and type system.
- **`Old8Lang.App`**: A command-line application for running Old8Lang scripts.
- **`Old8Lang.LanguageServer`**: A language server that provides features like code completion, syntax highlighting, and error checking for VSCode.
- **`Old8Lang.Tests`**: A suite of tests for ensuring the correctness of the language and its features.
- **Libraries**: The solution also includes several libraries that extend the language's capabilities, such as:
  - `Old8Lang.NetLib`
  - `Old8Lang.DatabaseLib`
  - `Old8Lang.MachineLearningLib`
  - `Old8Lang.SerializationLib`

## 3. Building and Running the Project

The project includes a build script, `build_lsp.sh`, which automates the process of building the language server and the VSCode extension.

### To build the project, follow these steps:
1. Make sure you have the .NET SDK and Node.js installed.
2. Run the build script from the root of the project:
```bash
./build_lsp.sh
```
This will build the language server and the VSCode extension and place the compiled files in the `vscode-old8lang/server` directory.

### To run the VSCode extension:
1. Open the `vscode-old8lang` directory in Visual Studio Code.
2. Press `F5` to start the extension development host.

This will launch a new VSCode window with the Old8Lang extension running, allowing you to test and debug the language features in a live environment.

## 4. Development Conventions

While there are no explicit coding style guidelines documented, the project follows standard C# and .NET conventions. It is recommended to maintain a consistent coding style with the existing codebase when contributing to the project. The project also includes a comprehensive test suite, so it is important to add new tests for any new features or bug fixes.
