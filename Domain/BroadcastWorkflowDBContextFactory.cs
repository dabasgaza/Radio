using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Domain;

public class BroadcastWorkflowDBContextFactory : IDesignTimeDbContextFactory<BroadcastWorkflowDBContext>
{
    public BroadcastWorkflowDBContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Radio"))
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // ⚠️ ملاحظة: هذا المصنع يُستخدم فقط في وقت التصميم (dotnet ef migrations)
        // ولا يحتاج لدعم التشفير لأنه يعمل في بيئة التطوير فقط.
        // في وقت التشغيل، يستخدم App.xaml.cs و DatabaseManagementService
        // SecureConfigurationProvider الذي يدعم DPAPI ومتغيرات البيئة.

        var optionsBuilder = new DbContextOptionsBuilder<BroadcastWorkflowDBContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new BroadcastWorkflowDBContext(optionsBuilder.Options);
    }
}
