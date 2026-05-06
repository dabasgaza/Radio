# 📐 Radio System — Technical Manifest & ERD

This document provides a visual and technical map of the data structures and workflows.

## 1. Entity Relationship Diagram (ERD)

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

## 2. State Machine: Episode Lifecycle

```mermaid
stateDiagram-v2
    [*] --> Planned : Create Episode
    
    Planned --> Executed : Mark Executed
    Planned --> Cancelled : Cancel (with Reason)
    
    Executed --> Published : Publish Social Media
    Executed --> Planned : Revert (with Reason)
    Executed --> Cancelled : Cancel (with Reason)
    
    Published --> WebsitePublished : Publish to Website
    Published --> Executed : Revert (with Reason)
    
    WebsitePublished --> Published : Revert (with Reason)
    
    Cancelled --> [*]
```

## 3. Data Integrity Constraints

| Table | Constraint | Logic |
| :--- | :--- | :--- |
| `Episodes` | `UQ_Episodes_Name_Date` | (Optional) Unique name per day. |
| `EpisodeGuests` | `UQ_Episode_Guest` | A guest cannot be added twice to the same episode. |
| `AuditLogs` | `IX_Table_Record` | Composite index for fast history lookup. |
| `Users` | `UQ_Username` | Unique usernames required. |

## 4. Soft Delete Filter
Applied globally in `BroadcastWorkflowDBContext`:
```csharp
modelBuilder.Entity<T>().HasQueryFilter(e => e.IsActive);
```
**Warning**: To include deleted records, use `.IgnoreQueryFilters()`.

---
*Technical Manifest v1.0*
