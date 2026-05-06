using DataAccess.Common;
using DataAccess.Services;
using Microsoft.Extensions.DependencyInjection;
using Radio.Views.Admin;
using Radio.Views.Correspondents;
using Radio.Views.Episodes;
using Radio.Views.Guests;
using Radio.Views.Home;
using Radio.Views.Programs;
using Radio.Views.Employees;
using Radio.Views.Reports;
using Radio.Views.StaffRoles;
using Radio.Views.Users;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace Radio.Services
{
    public class NavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly UserSession _session;
        private readonly Stack<string> _history = new();
        private const int MaxHistory = 10;

        public string? CurrentViewName { get; private set; }

        public event Action<string>? ViewChanged;

        public IReadOnlyCollection<string> History => _history.ToList().AsReadOnly();

        public NavigationService(IServiceProvider serviceProvider, UserSession session)
        {
            _serviceProvider = serviceProvider;
            _session = session;
        }

        public UserControl? NavigateTo(string viewName)
        {
            _history.Push(viewName);
            if (_history.Count > MaxHistory)
            {
                var temp = _history.Reverse().Take(MaxHistory).ToList();
                _history.Clear();
                for (int i = temp.Count - 1; i >= 0; i--)
                    _history.Push(temp[i]);
            }

            try
            {
                var view = CreateView(viewName);
                if (view != null)
                {
                    CurrentViewName = viewName;
                    ViewChanged?.Invoke(viewName);
                }
                return view;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public UserControl? GoBack()
        {
            if (_history.Count <= 1) return null;

            _history.Pop();
            var previous = _history.Peek();
            return NavigateTo(previous);
        }

        public string? GetPreviousViewName()
        {
            if (_history.Count <= 1) return null;
            var current = _history.Pop();
            var previous = _history.Peek();
            _history.Push(current);
            return previous;
        }

        private UserControl? CreateView(string viewName)
        {
            var userService = _serviceProvider.GetRequiredService<IUserService>();

            switch (viewName)
            {
                case "Home":
                    return new HomeView();

                case "Users":
                    return new UsersView(userService, _session);

                case "Employees":
                    var empService = _serviceProvider.GetRequiredService<IEmployeeService>();
                    return new EmployeesView(empService, _session);

                case "SocialPlatforms":
                    var platformService = _serviceProvider.GetRequiredService<IPlatformService>();
                    return new SocialPlatformsView(platformService, _session);

                case "StaffRoles":
                    var staffService = _serviceProvider.GetRequiredService<IEmployeeService>();
                    return new StaffRolesView(staffService, _session);

                case "Programs":
                    var progService = _serviceProvider.GetRequiredService<IProgramService>();
                    return new ProgramsView(progService, _session);

                case "Episodes":
                    var epService = _serviceProvider.GetRequiredService<IEpisodeService>();
                    var pService = _serviceProvider.GetRequiredService<IProgramService>();
                    var gService = _serviceProvider.GetRequiredService<IGuestService>();
                    var cService = _serviceProvider.GetRequiredService<ICorrespondentService>();
                    var epEmpService = _serviceProvider.GetRequiredService<IEmployeeService>();
                    return new EpisodesView(epService, pService, _session, _serviceProvider, gService, cService, epEmpService);

                case "Guests":
                    var guestService = _serviceProvider.GetRequiredService<IGuestService>();
                    return new GuestsView(guestService, _session);

                case "Correspondents":
                    var corService = _serviceProvider.GetRequiredService<ICorrespondentService>();
                    return new CorrespondentsView(corService, _session);

                case "Coverage":
                    var covService = _serviceProvider.GetRequiredService<ICoverageService>();
                    return new CoverageView(covService, _session, _serviceProvider);

                case "Reports":
                    var reportService = _serviceProvider.GetRequiredService<IReportsService>();
                    return new ReportsView(reportService);

                case "PublishingRecords":
                    var pubRecService = _serviceProvider.GetRequiredService<IPublishingService>();
                    var execRecService = _serviceProvider.GetRequiredService<IExecutionService>();
                    return new PublishingRecordsView(pubRecService, execRecService, _session, _serviceProvider);

                default:
                    return null;
            }
        }
    }
}
