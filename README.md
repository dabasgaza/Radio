# 📡 Radio Broadcast Workflow System — Ultimate Guide & Source of Truth

This document serves as the **Single Source of Truth** for both human developers and AI Agents. It consolidates all system architecture, database constraints, coding standards, UI guidelines, and workflows into a single file.

---

## 1. Project Overview & Structure

The Radio Broadcast Workflow System is a desktop application built with **.NET 10 / WPF** designed to manage the scheduling, execution, and digital publishing of radio episodes.

### Project Architecture Layers
*   **Domain**: Contains EF Core models, configurations (Fluent API), DB contexts, and migrations.
*   **DataAccess**: Business logic (Services), Data Transfer Objects (DTOs), validation, permissions, and auditing.
*   **Radio (UI)**: WPF Views, resources, style dictionaries, and dependency injection startup.

---

## 2. Core Architectural & Coding Rules (Non-Negotiable)

1.  **Result Pattern**: Every service method must return `Result` or `Result<T>` from `DataAccess/Common/Result.cs`. Do not throw business exceptions; return descriptive failures.
2.  **BaseEntity Foundation**: All entity models representing database tables (except pure join tables and static lists) must inherit from `BaseEntity` (defining `IsActive`, `CreatedAt`, `CreatedByUserId`, etc.).
3.  **Soft Delete Only**: Physical deletion of records is forbidden. Set `IsActive = false`. Global query filters in `BroadcastWorkflowDBContext` automatically filter active records.
4.  **Auto-Auditing**: Timestamps (`CreatedAt`, `UpdatedAt`) and user IDs (`CreatedByUserId`, `UpdatedByUserId`) are handled automatically by `AuditInterceptor`. Do not update these fields manually in service code.
5.  **Thread Safety & DbContextFactory**: Do not inject `DbContext` directly. Always use `IDbContextFactory<BroadcastWorkflowDBContext>` and create a new context instance inside each service operation (`using var context = await contextFactory.CreateDbContextAsync()`).
6.  **No InverseProperty on User**: Minimize navigation properties pointing back to `User` in other entities to avoid EF Core circular relationship conflicts.
7.  **Service Registration**: Register all new services in `Radio/App.xaml.cs` using `AddTransient` (unless singleton is explicitly needed).
8.  **UI/Service Separation**: WPF views retrieve services from the DI container (`ServiceProvider`) and keep logic strictly inside services.

---

## 3. Database Schema & State Machine

### 📊 Entity Relationships (ERD)
```mermaid
erDiagram
    PROGRAM ||--o{ EPISODE : "contains"
    EPISODE ||--o{ EPISODE_GUEST : "has"
    EPISODE ||--o{ EPISODE_CORRESPONDENT : "has"
    EPISODE ||--o{ EPISODE_EMPLOYEE : "has"
    EPISODE ||--o{ EXECUTION_LOG : "recorded_at"
    EPISODE ||--o{ WEBSITE_PUBLISHING_LOG : "published_at"
    
    EPISODE_STATUS ||--o{ EPISODE : "defines"
    
    GUEST ||--o{ EPISODE_GUEST : "participates"
    CORRESPONDENT ||--o{ EPISODE_CORRESPONDENT : "reports"
    EMPLOYEE ||--o{ EPISODE_EMPLOYEE : "works"
    STAFF_ROLE ||--o{ EMPLOYEE : "defines"
    
    EPISODE_GUEST ||--o{ SOCIAL_MEDIA_PUBLISHING_LOG : "source_for"
    SOCIAL_MEDIA_PUBLISHING_LOG ||--o{ SOCIAL_MEDIA_PUBLISHING_PLATFORM : "targets"
    SOCIAL_MEDIA_PLATFORM ||--o{ SOCIAL_MEDIA_PUBLISHING_PLATFORM : "defined_in"
    
    USER ||--o{ EXECUTION_LOG : "performed_by"
    USER ||--o{ SOCIAL_MEDIA_PUBLISHING_LOG : "published_by"
    USER ||--o{ WEBSITE_PUBLISHING_LOG : "published_by"
    ROLE ||--o{ USER : "assigned_to"
    PERMISSION ||--o{ ROLE_PERMISSION : "granted_to"
    ROLE ||--o{ ROLE_PERMISSION : "has"
```

### 🔄 Episode Status Lifecycle
Episodes transition through the following states:
1.  **Planned (0)**: Initial state for scheduling (Guests, Correspondents, Production Staff).
2.  **Executed (1)**: Episode has been broadcasted. Transitioning to this state writes to `ExecutionLogs`.
3.  **Published (2)**: Digital clips published to social media. Transitioning to this state writes to `SocialMediaPublishingLogs`.
4.  **WebsitePublished (3)**: Episode is live on the official website. Transitioning to this state writes to `WebsitePublishingLogs`.
5.  **Cancelled (4)**: Terminal state. Requires a reason (stored in `CancellationReason`).

*   **Reversion (Undo)**: States can be reverted (e.g., Executed → Planned) using `ReasonInputDialog` to specify a reason, which is logged to `CancellationReason`.

---

## 4. Coding Patterns

### Standard Service Method Template (Command)
```csharp
public async Task<Result<int>> UpdateSomethingAsync(MyDto dto, UserSession session)
{
    var permCheck = session.EnsurePermission(AppPermissions.SomePermission);
    if (!permCheck.IsSuccess)
        return Result<int>.Fail(permCheck.ErrorMessage!);

    using var context = await contextFactory.CreateDbContextAsync();
    var entity = await context.MyEntities.FindAsync(dto.Id);
    if (entity == null)
        return Result<int>.Fail("Record not found.");

    // Map DTO to Entity fields...
    
    await context.SaveChangesAsync();
    return Result<int>.Success(entity.Id);
}
```

### Collection Sync Pattern
When updating nested collections (such as updating guests in an episode):
1.  Load the entity including its child collection.
2.  Identify deleted items (present in DB but missing from DTO) → set `IsActive = false`.
3.  Identify new items (`Id == 0`) → Add to database.
4.  Identify updated items (`Id != 0`) → Update fields.

---

## 5. WPF UI Design & Styling Standards

The UI leverages **MaterialDesignInXamlToolkit** and **MahApps.Metro** for a unified theme, utilizing Tajawal/Cairo fonts for Arabic layouts.

### Standard XAML Style Keys
*   **TextBox Inputs**: `Style="{StaticResource Input.Text}"` (Regular), `Input.Text.Multiline` (Multiline), `Input.Search` (Search Box).
*   **ComboBox / DatePicker / TimePicker**: `Input.ComboBox`, `Input.DatePicker`, `Input.TimePicker`.
*   **Buttons**: `Btn.Primary` (Action), `Btn.Cancel` (Dismiss), `Btn.AddNew` (Plus/Add actions), `Btn.Icon.Edit` (Edit icon button), `Btn.Icon.Delete` (Delete icon button).
*   **DataGrid**: `Style="{StaticResource DataGrid.Main}"` for tables, with `DataGrid.ColumnHeader.Center`, `DataGrid.Row`, and `DataGrid.Cell`.
*   **Stats Cards**: `Style="{StaticResource Card.Stat}"` for numeric dashboard widgets.
*   **Colors & Brushes**: `PrimaryMainBrush`, `SuccessBrush`, `WarningBrush`, `ErrorBrush`, `SurfaceBrush`, `HeaderGradientBrush`.

### Dialog Window Pattern
All modals and entry dialogs should be shown using `DialogHost`:
```csharp
var view = new FormDialog(services...);
var result = await DialogHost.Show(view, "RootDialog");
if (result is true) await LoadDataAsync();
```
Close the dialog and pass the result back using:
```csharp
DialogHost.Close("RootDialog", true); // To save/confirm
DialogHost.Close("RootDialog", null); // To cancel/close
```

---

## 6. Permissions & Security System

Permissions are defined in `DataAccess/Common/AppPermissions.cs`. Check permissions using `UserSession.HasPermission` or `EnsurePermission`:
*   `USER_MANAGE`: Managing users and system permissions.
*   `PROGRAM_MANAGE`: Managing radio programs.
*   `EPISODE_MANAGE`: Creating and editing episodes.
*   `EPISODE_EXECUTE`: Recording execution logs.
*   `EPISODE_PUBLISH`: Publishing social media clips.
*   `EPISODE_WEB_PUBLISH`: Publishing to the website.
*   `EPISODE_EDIT` / `EPISODE_DELETE` / `EPISODE_REVERT`: Managing lifecycle states.
*   `STAFF_MANAGE`: Reused for employees, roles, and social media platforms.
*   `VIEW_REPORTS`: Accessing database statistics and reports.

---

## 7. CLI Development Commands

### Project Build & Run
```powershell
# Build application
dotnet build Radio/Radio.csproj

# Run application with watch mode
dotnet watch run --project Radio/Radio.csproj
```

### EF Core Migrations
```powershell
# Add a new migration
dotnet ef migrations add <MigrationName> --project Domain --startup-project Radio

# Apply migrations to database
dotnet ef database update --project Domain --startup-project Radio

# Remove last migration
dotnet ef migrations remove --project Domain --startup-project Radio
```

---

## 8. Common Troubleshooting

*   **XAML Namespace Errors**: Use `xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"`. Do not use `xmlns:md`.
*   **Binding Errors**: Bindings must point to the fields/properties in the DTO, not internal entities. Ensure `ItemsSource` is set in the code-behind, not in XAML.
*   **Thread Safety Exceptions**: Ensure every service operation instantiates its own `DbContext` scope via `IDbContextFactory`.
