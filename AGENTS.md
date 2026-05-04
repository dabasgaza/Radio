# AI Agent Guide for Radio Project

## Purpose
This file helps AI coding agents understand the Radio broadcast workflow system quickly and safely.

## Project overview
- Desktop application built with **.NET 10 / WPF**.
- Layered architecture:
  - `Domain/` contains EF Core entities, configurations, `BroadcastWorkflowDBContext`, and migrations.
  - `DataAccess/` contains business logic services, DTOs, validation, permissions, and audit behavior.
  - `Radio/` contains the WPF UI, application startup, views, resources, and converters.
- Primary domain: broadcast episode workflow, publishing, execution logging, user/role permissions, and audit tracking.

## Build and run
- Build: `dotnet build Radio/Radio.csproj`
- Publish: `dotnet publish Radio/Radio.csproj`
- Run/watch: `dotnet watch run --project Radio/Radio.csproj`
- Solution file: `Radio.slnx`

## Key conventions
- Every service method returns `Result` or `Result<T>` from `DataAccess/Common/Result.cs`.
- Use `AppPermissions` in `DataAccess/Common/AppPermissions.cs` and `UserSession` in `DataAccess/Common/UserSession.cs` for security checks.
- `BaseEntity` is the audited base class for most tables. It defines `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`, and `RowVersion`.
- `AuditInterceptor` in `DataAccess/Data/AuditInterceptor.cs` handles automatic audit metadata and soft deletes on `SaveChangesAsync()`.
- Validation is centralized in `DataAccess/Validation/ValidationPipeline.cs`.
- Do not add manual audit or timestamp logic in services; use the interceptor and entity base class.
- The application uses `IDbContextFactory<BroadcastWorkflowDBContext>` and creates `DbContext` instances per service operation to avoid threading issues.

## Important files
- `DataAccess/Common/Result.cs` — result/validation pattern
- `DataAccess/Data/AuditInterceptor.cs` — automatic auditing and soft-delete logic
- `DataAccess/Common/AppPermissions.cs` — permission constants
- `DataAccess/Validation/ValidationPipeline.cs` — validation rules for DTOs and entities
- `DataAccess/Services/EpisodeService.cs` — core episode workflow and status transitions
- `Domain/Models/BaseEntity.cs` — audited base entity
- `Domain/Models/BroadcastWorkflowDBContext.cs` — EF Core model configuration and seed data
- `Radio/App.xaml.cs` — DI configuration and application startup

## What to avoid
- Avoid direct `DbContext` injection into UI code.
- Avoid bypassing the `Result` pattern when returning errors.
- Avoid manual audit metadata updates in service code.
- Avoid using `InverseProperty` on `User` entities because the project intentionally minimizes ambiguous EF Core navigation relationships.

## Documentation links
- [Project README](README.md)
- [Project skill document](SKILL.md)
- [Radio project documentation](RADIO_PROJECT_DOCUMENTATION.md)

## Notes for agents
- Prefer small, targeted edits over broad refactors unless the user explicitly asks for architecture change.
- If adding a new entity, update `Domain/Models/`, `Domain/Models/Configurations/`, migrations, and any seed data if needed.
- If changing workflow state, inspect `EpisodeService.cs`, status constants, and publishing/logging tables.
- There is no dedicated tests folder in this repository; focus on preserving current behavior.
