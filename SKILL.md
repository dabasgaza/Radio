---
name: dotnet-skill
description: "Reusable skill for .NET development workflows: analyze, modify, debug, and refactor C# projects while preserving existing project conventions."
argument-hint: "What .NET outcome should this skill produce?"
disable-model-invocation: true
---

# dotnet-skill

This skill captures a practical .NET workflow for agent customization. Use it when the goal is to implement or diagnose changes in .NET projects, including C# code, project files, WPF views, Entity Framework Core, and build configuration.

## When to use

- Fix build or runtime issues in a .NET solution
- Add or update application behavior in C# code
- Refactor .NET code with a focus on maintainability and correctness
- Inspect and update project/solution structure or package references
- Provide targeted .NET guidance for WPF, ASP.NET Core, console apps, libraries, or EF Core data access

## Workflow

1. Review the current solution/project context and requested outcome.
2. Identify the specific .NET project type and relevant files.
3. Determine decision points such as target framework, layers involved, and whether the change is UI, data, or service oriented.
4. Apply minimal, compatible changes that preserve existing code style and architecture.
5. Validate the result against build expectations and any explicit quality criteria.

## Quality criteria

- Changes should compile cleanly for the project target framework
- Preserve existing naming conventions, architecture, and dependency patterns
- Keep diffs focused and avoid unnecessary modifications
- Document assumptions, follow-up questions, and required user actions

## Example prompts

- "Add a new authentication method to the .NET service layer and update the related DTO."
- "Fix the WPF data binding error in `MainWindow.xaml` and its view model."
- "Refactor the Entity Framework repository to use async methods and add a unit test for the new behavior."
- "Update the .NET project file to target the latest supported framework and adjust package references."

## Notes

This skill is intended as a workspace-scoped guide for `.NET` tasks. If you want a personal customization, adapt the prompt hints and descriptions to match your own preferred .NET conventions and toolchain.
