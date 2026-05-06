# 🧠 Radio Broadcast Workflow System — AI Knowledge Base (v1.0)

> [!IMPORTANT]
> This is the **Primary Source of Truth** for AI Agents. It is optimized for high-density context and token efficiency.

---

## 🛠 1. System Blueprint
- **Framework**: .NET 10 + WPF (Material Design).
- **Core Architecture**: Layered (Domain → DataAccess → Radio/UI).
- **Database**: EF Core (SQL Server/LocalDB) with `IDbContextFactory` per operation.
- **Workflow**: Episode-centric lifecycle management (Planned → Executed → Published → Web).

---

## ⚖️ 2. The Golden Rules (Non-Negotiable)
1. **Result Pattern**: All services return `Result` or `Result<T>`. Never throw business exceptions.
2. **BaseEntity Foundation**: All audited tables must inherit from `BaseEntity`.
3. **Soft Delete Only**: Use `IsActive = false`. Physical deletion is strictly forbidden (except for pure join tables).
4. **Auto-Auditing**: `AuditInterceptor` handles all timestamps and user IDs. Do not set them manually.
5. **No Direct DbContext**: Always use `IDbContextFactory<BroadcastWorkflowDBContext>`.
6. **Thread Safety**: Create a new `DbContext` scope inside each service method (`using var context = ...`).
7. **No InverseProperty on User**: Avoid `ICollection<T>` in the `User` entity to prevent circular dependency conflicts.
8. **UI/Service Separation**: View code-behind injects services; logic stays in services.

---

## 📂 3. Directory Taxonomy
| Path | Responsibility |
|:---|:---|
| `Domain/Models/` | EF Core Entities & Enums. |
| `Domain/Models/Configurations/` | Fluent API (Constraints, FKs, Query Filters). |
| `DataAccess/Services/` | Business Logic (Implementation of Interfaces). |
| `DataAccess/DTOs/` | Data Transfer Objects (Immutable records preferred). |
| `DataAccess/Common/` | `Result.cs`, `AppPermissions.cs`, `UserSession.cs`. |
| `Radio/Views/` | WPF UserControls (Pages & Dialogs). |
| `Radio/Resources/` | `Styles/` (Standardized XAML look and feel). |

---

## 📊 4. Domain Schema Reference

### Core Entities
- **Program**: Radio program (e.g., "Morning Show").
- **Episode**: The main unit of work. Has `StatusId`.
- **Guest / Correspondent / Employee**: People involved in episodes.
- **Episode[Guest/Correspondent/Employee]**: Join tables for episode assignments.
- **[SocialMedia/Website]PublishingLog**: Tracking where content was published.

### Episode Lifecycle (Status Codes)
| ID | Status | Allowed Transitions |
|:---|:---|:---|
| `0` | **Planned** | → Executed, Cancelled |
| `1` | **Executed** | → Published, Planned (Revert), Cancelled |
| `2` | **Published** | → WebsitePublished, Executed (Revert) |
| `3` | **WebPublished** | → Published (Revert) |
| `4` | **Cancelled** | (Terminal State) |

---

## ⚙️ 5. Coding Patterns

### Service Method (Query)
```csharp
public async Task<List<Dto>> GetDataAsync() {
    using var context = await contextFactory.CreateDbContextAsync();
    return await context.Entities.AsNoTracking().Select(e => new Dto(...)).ToListAsync();
}
```

### Service Method (Command)
```csharp
public async Task<Result> UpdateAsync(Dto dto, UserSession session) {
    var check = session.EnsurePermission(AppPermissions.Manage);
    if (!check.IsSuccess) return Result.Fail(check.ErrorMessage!);

    using var context = await contextFactory.CreateDbContextAsync();
    var entity = await context.Entities.FindAsync(dto.Id);
    if (entity == null) return Result.Fail("Not Found");
    
    // Map DTO to Entity...
    await context.SaveChangesAsync();
    return Result.Success();
}
```

### Collection Sync Pattern (The "Sync" Way)
When updating nested collections (like Guests in Episode):
1. Load existing items with `Include`.
2. Identify items to remove (`existing` not in `dto`).
3. Identify items to add (`dto.Id == 0`).
4. Identify items to update (`dto.Id != 0`).
5. **Note**: Use `IsActive = false` for removals if they are BaseEntities.

---

## 🎨 6. UI/WPF Design Standards

### Standard Style Keys
- **Inputs**: `Input.Text`, `Input.ComboBox`, `Input.DatePicker`, `Input.Search`.
- **Buttons**: `Btn.Primary`, `Btn.Cancel`, `Btn.AddNew`, `Btn.Icon.Edit`, `Btn.Icon.Delete`.
- **Containers**: `DataGrid.Main`, `Zone.Header.Primary`, `Window.Footer`, `View.Base`.

### Dialog Pattern
Use `DialogHost.Show(view, "RootDialog")`. Close via `DialogHost.Close("RootDialog", result)`.

---

## 🚀 7. Common Tasks Checklist

### Adding a New Entity
1. [ ] Create model in `Domain/Models/` (inherit `BaseEntity`).
2. [ ] Add `DbSet` to `BroadcastWorkflowDBContext`.
3. [ ] Add Configuration in `Domain/Models/Configurations/`.
4. [ ] Run `dotnet ef migrations add Name --project Domain --startup-project Radio`.
5. [ ] Update `DbSeeder.cs` if initial data is needed.

### Adding a New Service
1. [ ] Define Interface in `DataAccess/Services/`.
2. [ ] Implement class using `IDbContextFactory`.
3. [ ] Register in `Radio/App.xaml.cs`: `services.AddTransient<IInterface, Implementation>();`.

### Adding a New Tab/View
1. [ ] Add RadioButton to `MainWindow.xaml` (Tag: "MyView").
2. [ ] Update `LoadView` in `MainWindow.xaml.cs`.
3. [ ] Update `ApplyPermissionSecurity` in `MainWindow.xaml.cs`.

---

## 🚨 8. Token-Saving Tips for Agents
- **Avoid reading whole service files** if you only need the interface.
- **Reference `AGENTS.md`** for project-wide conventions.
- **Don't ask for architecture** — it's already defined here.
- **Check `AppPermissions.cs`** before adding new permission strings.
- **Use `BaseEntity` properties** (CreatedAt, etc.) instead of adding custom ones for auditing.

---
*Generated for Radio Project | May 2026*
