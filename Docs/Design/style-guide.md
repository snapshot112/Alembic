# Alembic Project C# Style Guide

## 1. Introduction

This document outlines the coding style, conventions, and best practices to be followed for all C# development within Project Alembic. Adhering to these guidelines ensures that our codebase remains consistent, readable, and maintainable, reflecting the project's core values of rigor and robust design.

## 2. Naming Conventions

Consistency in naming is crucial for readability. We will adhere to the standard .NET naming conventions.

* **Classes, Interfaces, Enums, etc.**: Use `PascalCase`.
* **Interface Names**: Prefix interfaces with `I` (e.g., `IActionHandler`).
* **Local Variables & private Fields**: Use `camelCase`.
* **Private Fields**: Prefix with an underscore `_` (e.g., `_rootDirectory`).

## 3. Formatting & Modern C# Features

Code formatting should be consistent and leverage modern C# features to enhance clarity and reduce boilerplate.

* **File-Scoped Namespaces**: Always use file-scoped namespaces to reduce indentation.
  ```csharp
  namespace Alembic.Agent.Core;

  public class MyClass { /* ... */ }
  ```

* **Primary Constructors**: Prefer primary constructors for classes where the constructor's main purpose is to capture dependencies or data. This is our default for new classes.
  ```csharp
  public class ActionDispatcher(string rootDirectory)
  {
      // ...
  }
  ```

* **Braces**: Use the Allman style (braces on their own line).

* **Indentation**: Use 4 spaces for indentation.

## 4. Commenting and Documentation

Clear documentation is mandatory for all public-facing APIs and complex internal logic.

* **Multi-line Comments**: Use multi-line block comments (`/* */`) for all public and protected members to provide detailed explanations. This is the official project standard.
  ```csharp
  /*
   * A generalized, secure process executor.
   * @param executable The command or application to run (e.g., 'dotnet', 'git').
   * @param arguments The arguments to pass to the executable.
   */
  private static async Task ExecuteProcessAsync(string executable, string arguments) { /* ... */ }
  ```

* **Single-line Comments**: Use `//` for inline comments to explain specific, non-obvious lines of code.

## 5. General Principles

* **Single Responsibility Principle (SRP)**: Each class and method should have one clear purpose.
* **Immutability**: Prefer immutable types (like `record`) for data transfer objects.
* **Asynchronous Programming**: Use `async` and `await` for all I/O-bound operations.

## 6. File and Namespace Organization

* **Namespaces**: Namespaces must follow the project's folder structure.
* **File Names**: C# files should be named using `PascalCase` to match the primary public class they contain (e.g., `ActionDispatcher.cs`).