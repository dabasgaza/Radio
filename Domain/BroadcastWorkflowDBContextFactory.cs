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

        var optionsBuilder = new DbContextOptionsBuilder<BroadcastWorkflowDBContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new BroadcastWorkflowDBContext(optionsBuilder.Options);
    }
}