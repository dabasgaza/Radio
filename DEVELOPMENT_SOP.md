# 🚀 Standard Operating Procedure (SOP) for Radio Project Development

This document defines the strict workflow and architectural rules for the Radio project. Any AI assistant or developer working on this codebase **MUST** adhere to these steps to ensure consistency and prevent architectural drift.

---

## 🏗️ Core Architectural Principles (Non-Negotiable)

1.  **Result Pattern**: Every service method MUST return `Result` or `Result<T>`. Never throw exceptions for expected business errors.
2.  **BaseEntity**: All entity models (except lookup tables) MUST inherit from `BaseEntity`.
3.  **Audit System**: Audit fields (`CreatedAt`, `CreatedBy`, etc.) and `AuditLogs` are handled automatically by `AuditInterceptor`. **Do not** manually set these in services.
4.  **Primary Key Dependency**: When syncing child collections (e.g., guests), always use the primary key (`Id`) to distinguish between Update and Insert.
5.  **Soft Delete**: Never use `context.Remove()`. Set `IsActive = false` instead. Global Query Filters are already configured in `BroadcastWorkflowDBContext`.

---

## 🛠️ Step-by-Step Workflow for New Features

Follow these steps in exact order to avoid dependency errors.

### Phase 1: Domain & Data Layer
1.  **Define Model**: Create the entity in `Domain/Models/`. Inherit from `BaseEntity`.
2.  **Configure**: Create a configuration file in `Domain/Models/Configurations/` using Fluent API.
3.  **Register**: Add the `DbSet` and the configuration call in `BroadcastWorkflowDBContext.cs`.
4.  **Migration**: 
    - Run `dotnet ef migrations add <Name> --project Domain --startup-project Radio`.
    - Run `dotnet ef database update --project Domain --startup-project Radio`.

### Phase 2: DataAccess Layer (Business Logic)
1.  **Create DTOs**: Define the data transfer objects in `DataAccess/DTOs/`.
2.  **Define Interface**: Create `IService` in `DataAccess/Services/`.
3.  **Implement Service**: 
    - Use `IDbContextFactory<BroadcastWorkflowDBContext>`.
    - Implement `Result` pattern.
    - Check permissions using `session.EnsurePermission(AppPermissions.X)`.
4.  **Validation**: Add validation logic in `DataAccess/Validation/ValidationPipeline.cs` if needed.
5.  **DI Registration**: Register the service in `Radio/App.xaml.cs` in the `ConfigureServices` method.

### Phase 3: UI Layer (WPF)
1.  **Create Control**: Create a `UserControl` in `Radio/Views/`.
2.  **Design**: Use `MaterialDesign` components and `MahApps` window styles.
3.  **Integration**: Use `DialogHost` for forms.
4.  **Binding**: Ensure all interactive elements have unique `x:Name` or are properly bound to the context.

---

## 🛡️ Anti-Hallucination Checklist (For AI)

Before performing any edit, verify the following:

- [ ] **File Verification**: I have read the current content of the file I am about to edit.
- [ ] **Context Awareness**: I have checked the `README.md` for the latest architectural changes.
- [ ] **Consistency Check**: I am using the same naming conventions as existing services (e.g., `Async` suffix, `Result` return type).
- [ ] **Permission Check**: Does this operation require a new permission in `AppPermissions.cs`?
- [ ] **Audit Check**: Am I letting the `AuditInterceptor` handle the audit fields?
- [ ] **Inverse Collection Check**: Did I avoid adding `ICollection<...>` to the `User` entity? (Inverse collections are forbidden in this project).

---

## 🔄 Database Migration Protocol

1.  **Always** use `--project Domain --startup-project Radio`.
2.  **Never** manually edit migration files unless strictly necessary for data seeding.
3.  **If a migration fails**: 
    - Run `dotnet ef migrations remove`.
    - Fix the model/config.
    - Try again.

---

## 📝 Reporting & Documentation

- After every major change, update the `Changelog` in `README.md`.
- If a decision deviates from standard patterns, document the "Why" in the `Architecture Decisions` section of the `README.md`.

---

**By following this SOP, you ensure the Radio project remains stable, maintainable, and professionally engineered.**
