# 🛠 Radio Project AI Skills

This document serves as the central directory for AI skills used in the Radio project. These skills are designed to assist AI coding agents (like Antigravity) in performing tasks more efficiently and safely, following the [.NET Agent Skills](https://github.com/dotnet/skills) standard.

## 🚀 Available Skills

The following skills are integrated into the workspace and can be used by AI agents:

### 1. [EF Core Query Optimization](.cursor/skills/optimizing-ef-core-queries/SKILL.md)
**Purpose**: Fix N+1 problems, choose correct tracking modes, and avoid common EF Core performance traps.
**Use when**: Working on DataAccess services, fixing slow queries, or loading complex entity graphs.

### 2. [C# Scripting & Execution](.cursor/skills/csharp-scripts/SKILL.md)
**Purpose**: Run lightweight C# scripts to verify logic, process data, or test service methods in isolation.
**Use when**: You need to validate a logic snippet without running the entire WPF application.

### 3. [WPF & XAML Design Standards](.cursor/skills/wpf-xaml-standards/SKILL.md)
**Purpose**: Ensure UI consistency using the project's standard style keys and Material Design patterns.
**Use when**: Creating or modifying Views, Dialogs, or Styles in the `Radio` project.

### 4. [Context Mode Optimization](.cursor/skills/context-mode/SKILL.md)
**Purpose**: Drastically reduce context window usage and maintain session continuity.
**Use when**: Performing large-scale analysis, fetching web data, or in very long conversations.

---

## 🛠 How to Use Skills
AI Agents should automatically detect these skills in the `.cursor/skills/` directory. If you are an agent:
1. **Read the SKILL.md** in the relevant directory.
2. **Follow the Workflow** defined in the skill.
3. **Verify results** against the Validation checklist.

## 📚 Reference
These skills are based on and extended from the [dotnet/skills](https://github.com/dotnet/skills) repository.

---
*Generated for Radio Project | May 2026*
