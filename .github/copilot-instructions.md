# GitHub Copilot Instructions for Radio

This repository uses `AGENTS.md` as the primary agent guidance file. Use the root-level `AGENTS.md` first for project architecture, conventions, and common patterns.

## Key points for Copilot
- This is a **.NET 10 / WPF** desktop application with layered architecture.
- Business logic lives in `DataAccess/Services/`, and each service returns `Result` or `Result<T>`.
- Audit metadata is handled automatically by `DataAccess/Data/AuditInterceptor.cs`.
- Permission checks use `AppPermissions` and `UserSession` from `DataAccess/Common/`.
- Avoid manual audit/timestamp updates in service code.
- Use `IDbContextFactory<BroadcastWorkflowDBContext>` rather than direct `DbContext` injection.

## Important files
- `DataAccess/Common/Result.cs`
- `DataAccess/Data/AuditInterceptor.cs`
- `DataAccess/Common/AppPermissions.cs`
- `DataAccess/Validation/ValidationPipeline.cs`
- `DataAccess/Services/EpisodeService.cs`
- `Domain/Models/BroadcastWorkflowDBContext.cs`
- `Radio/App.xaml.cs`

## Notes
- Prefer targeted changes and preserve existing conventions.
- If a requested change impacts data schema, update EF models, configurations, and migrations together.
- There is no dedicated test suite in this repository; focus on keeping behavior consistent.
