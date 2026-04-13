using BroadcastWorkflow.Services;
using DataAccess.Services;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Radio.Forms;
using System.IO;
using System.Windows;

namespace Radio
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;
        public IConfiguration Configuration { get; private set; } = null!;

        public App()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // 1. Database Context Factory
            string connectionString = Configuration.GetConnectionString("DefaultConnection")!;
            services.AddDbContextFactory<BroadcastWorkflowDBContext>(options =>
                options.UseSqlServer(connectionString));

            // 2. Services (To be implemented in Phase 3)
            services.AddTransient<IAuditService, AuditService>();
            services.AddTransient<IAuthService, AuthService>();
            services.AddTransient<IGuestService, GuestService>();
            services.AddTransient<ICorrespondentService, CorrespondentService>();
            services.AddTransient<IEpisodeService, EpisodeService>();
            services.AddTransient<IProgramService, ProgramService>();
            services.AddTransient<IEpisodeService, EpisodeService>();
            services.AddTransient<IExecutionService, ExecutionService>();
            services.AddTransient<IPublishingService, PublishingService>();
            services.AddTransient<IReportsService, ReportsService>();
            services.AddTransient<IUserService, UserService>();



            // 3. Windows (To be implemented in Phase 4+)
            services.AddTransient<MainWindow>();
            services.AddTransient<LoginWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // For now, we'll just show a placeholder or the first window when ready
            //var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            //mainWindow.Show();

            var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
            loginWindow.Show();
            base.OnStartup(e);
        }

    }

}
