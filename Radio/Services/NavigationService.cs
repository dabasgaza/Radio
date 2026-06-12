using DataAccess.Common;
using DataAccess.Services;
using Microsoft.Extensions.DependencyInjection;
using Radio.Views.Correspondents;
using Radio.Views.Database;
using Radio.Views.Employees;
using Radio.Views.Episodes;
using Radio.Views.Guests;
using Radio.Views.Home;
using Radio.Views.Programs;
using Radio.Views.Reports;
using Radio.Views.SocialPlatforms;
using Radio.Views.StaffRoles;
using Radio.Views.Users;
using System.Windows.Controls;

namespace Radio.Services
{
    public class NavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly UserSession _session;

        // ✨ ذاكرة مؤقتة محدودة الحجم مع آلية LRU (الأقل استعمالاً يُحذف أولاً)
        private readonly Dictionary<string, UserControl> _viewCache = new();
        private readonly LinkedList<string> _cacheAccessOrder = new(); // تتبع ترتيب الوصول لـ LRU
        private const int MaxCacheSize = 8; // الحد الأقصى للعروض المخزنة

        private readonly Stack<string> _history = new();
        private const int MaxHistory = 10;

        private static readonly Dictionary<string, string> RoutePermissions = new()
        {
            ["Users"] = AppPermissions.UserManage,
            ["SecurityRoles"] = AppPermissions.UserManage,
            ["PermissionMatrix"] = AppPermissions.UserManage,
            ["Permissions"] = AppPermissions.UserManage,
            ["Programs"] = AppPermissions.ProgramManage,
            ["Employees"] = AppPermissions.StaffManage,
            ["StaffRoles"] = AppPermissions.StaffManage,
            ["SocialPlatforms"] = AppPermissions.StaffManage,
            ["Episodes"] = AppPermissions.EpisodeManage,
            ["Guests"] = AppPermissions.GuestManage,
            ["Correspondents"] = AppPermissions.CoordinationManage,
            ["Coverages"] = AppPermissions.CoordinationManage,
            ["Reports"] = AppPermissions.ViewReports,
            ["PublishingRecords"] = AppPermissions.EpisodePublish,
            ["Database"] = AppPermissions.DatabaseManage,
            ["AuditLogs"] = AppPermissions.ViewAuditLogs,
            ["Diagnostics"] = AppPermissions.DatabaseManage,
            ["AdminHub"] = AppPermissions.UserManage,
        };

        public string? CurrentViewName { get; private set; }

        public event Action<string>? ViewChanged;

        // ✨ حدث طلب التنقل — يُستخدم من Views الفرعية لطلب تنقل فعلي
        // يحل مشكلة SecurityRolesView → PermissionMatrix حيث كان NavigateTo
        // يُعيد View لكن لا يُعرضه لأن MainWindow لا يعرف بالطلب
        public event Action<string>? NavigationRequested;

        public IReadOnlyCollection<string> History => _history.ToList().AsReadOnly();

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _session = serviceProvider.GetRequiredService<CurrentSessionProvider>().CurrentSession!;
        }

        public UserControl? NavigateTo(string viewName)
        {
            try
            {
                var view = GetOrCreateView(viewName);
                if (view == null) return null;

                _history.Push(viewName);
                if (_history.Count > MaxHistory)
                {
                    var temp = _history.Reverse().Take(MaxHistory).ToList();
                    _history.Clear();
                    for (int i = temp.Count - 1; i >= 0; i--)
                        _history.Push(temp[i]);
                }

                CurrentViewName = viewName;
                ViewChanged?.Invoke(viewName);
                return view;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error navigating to {viewName}: {ex}");
                DataAccess.Services.Messaging.MessageService.Current.ShowError($"خطأ أثناء تحميل شاشة {viewName}: {ex.Message}\n{ex.InnerException?.Message}", "خطأ تنقل");
                return null;
            }
        }

        public UserControl? GoBack()
        {
            if (_history.Count <= 1) return null;

            _history.Pop();
            var previous = _history.Peek();

            var view = GetOrCreateView(previous);
            if (view == null) return null;

            CurrentViewName = previous;
            ViewChanged?.Invoke(previous);
            return view;
        }

        public string? GetPreviousViewName()
        {
            if (_history.Count <= 1) return null;
            var current = _history.Pop();
            var previous = _history.Peek();
            _history.Push(current);
            return previous;
        }

        public bool CanGoBack => _history.Count > 1;

        /// <summary>
        /// إزالة عرض من الذاكرة المؤقتة لإجبار إعادة إنشائه في المرة القادمة.
        /// </summary>
        public void RefreshView(string viewName)
        {
            if (_viewCache.Remove(viewName))
            {
                _cacheAccessOrder.Remove(viewName);
            }
        }

        /// <summary>
        /// الحصول على عرض من الذاكرة المؤقتة أو إنشائه.
        /// متاح للعروض الفرعية (مثل AdminHubView) لتجنب إنشاء نسخ مكررة.
        /// </summary>
        public UserControl? GetOrCreateView(string viewName)
        {
            if (RoutePermissions.TryGetValue(viewName, out var permission))
            {
                if (!_session.HasPermission(permission))
                {
                    DataAccess.Services.Messaging.MessageService.Current.ShowWarning(
                        $"عذراً، لا تملك صلاحية الوصول إلى هذه الشاشة.");
                    return null;
                }
            }

            if (_viewCache.TryGetValue(viewName, out var cached))
            {
                // ✨ تحديث ترتيب LRU — نقل العنصر إلى المقدمة (الأكثر استعمالاً)
                _cacheAccessOrder.Remove(viewName);
                _cacheAccessOrder.AddFirst(viewName);
                return cached;
            }

            var view = CreateView(viewName);
            if (view != null)
            {
                // ✨ إزالة الأقل استعمالاً إذا تجاوزنا الحد الأقصى
                if (_viewCache.Count >= MaxCacheSize && _cacheAccessOrder.Count > 0)
                {
                    var lruViewName = _cacheAccessOrder.Last!.Value;
                    _viewCache.Remove(lruViewName);
                    _cacheAccessOrder.RemoveLast();
                }

                _viewCache[viewName] = view;
                _cacheAccessOrder.AddFirst(viewName);
            }

            return view;
        }

        public void RequestNavigation(string viewName)
        {
            // ✨ إطلاق حدث التنقل بدلاً من استدعاء NavigateTo مباشرة
            // MainWindow مشترك في هذا الحدث ويُنفذ التنقل الفعلي
            NavigationRequested?.Invoke(viewName);
        }

        /// <summary>
        /// تحديث اسم العرض الحالي دون إنشاء أو تخزين View جديد.
        /// يُستخدم من لوحات الحاويات (مثل AdminHubView) لتحديث حالة الشريط الجانبي
        /// ومسار التنقل دون إنشاء نسخة مكررة من العرض في الذاكرة المؤقتة.
        /// </summary>
        public void NotifySubViewChanged(string viewName)
        {
            CurrentViewName = viewName;
            ViewChanged?.Invoke(viewName);
        }

        /// <summary>
        /// تفريغ الذاكرة المؤقتة بالكامل — يُستخدم عند تسجيل الخروج
        /// أو عند الحاجة لتحرير الذاكرة.
        /// </summary>
        public void ClearCache()
        {
            _viewCache.Clear();
            _cacheAccessOrder.Clear();
        }

        private UserControl? CreateView(string viewName)
        {
            // ✨ DialogHelper — خدمة موحدة لعرض الحوارات بدون اقتران بـ MainWindow
            var dialogHelper = _serviceProvider.GetRequiredService<DialogHelper>();

            switch (viewName)
            {
                case "Home":
                    var homeReportService = _serviceProvider.GetRequiredService<IReportsService>();
                    var homeSession = _serviceProvider.GetRequiredService<CurrentSessionProvider>();
                    return new HomeView(homeReportService, homeSession);

                case "Programs":
                    var programService = _serviceProvider.GetRequiredService<IProgramService>();
                    var progSession = _serviceProvider.GetRequiredService<CurrentSessionProvider>().CurrentSession!;
                    return new ProgramsView(programService, progSession, dialogHelper);

                case "Users":
                    var userService = _serviceProvider.GetRequiredService<IUserService>();
                    var session = _serviceProvider.GetRequiredService<CurrentSessionProvider>().CurrentSession!;
                    return new UsersView(userService, session, dialogHelper);

                case "Employees":
                    var empService = _serviceProvider.GetRequiredService<IEmployeeService>();
                    var empSession = _serviceProvider.GetRequiredService<CurrentSessionProvider>().CurrentSession!;
                    return new EmployeesView(empService, empSession, dialogHelper);

                case "SocialPlatforms":
                    var platformService = _serviceProvider.GetRequiredService<IPlatformService>();
                    var platSession = _serviceProvider.GetRequiredService<CurrentSessionProvider>().CurrentSession!;
                    return new SocialPlatformsView(platformService, platSession, dialogHelper);

                case "StaffRoles":
                    var staffService = _serviceProvider.GetRequiredService<IEmployeeService>();
                    var staffSession = _serviceProvider.GetRequiredService<CurrentSessionProvider>().CurrentSession!;
                    return new StaffRolesView(staffService, staffSession, dialogHelper);

                case "Episodes":
                    var epService = _serviceProvider.GetRequiredService<IEpisodeService>();
                    var pService = _serviceProvider.GetRequiredService<IProgramService>();
                    var gService = _serviceProvider.GetRequiredService<IGuestService>();
                    var cService = _serviceProvider.GetRequiredService<ICorrespondentService>();
                    var epEmpService = _serviceProvider.GetRequiredService<IEmployeeService>();
                    var epSession = _serviceProvider.GetRequiredService<CurrentSessionProvider>().CurrentSession!;
                    return new EpisodesView(epService, pService, epSession, _serviceProvider, gService, cService, epEmpService);

                case "Guests":
                    var guestService = _serviceProvider.GetRequiredService<IGuestService>();
                    var gstSession = _serviceProvider.GetRequiredService<CurrentSessionProvider>().CurrentSession!;
                    return new GuestsView(guestService, gstSession, dialogHelper);

                case "Correspondents":
                    var corService = _serviceProvider.GetRequiredService<ICorrespondentService>();
                    var corSession = _serviceProvider.GetRequiredService<CurrentSessionProvider>().CurrentSession!;
                    return new CorrespondentsView(corService, corSession, dialogHelper);

                case "Coverages":
                    var covService = _serviceProvider.GetRequiredService<ICoverageService>();
                    var covSession = _serviceProvider.GetRequiredService<CurrentSessionProvider>().CurrentSession!;
                    return new CoverageView(covService, covSession, _serviceProvider);

                case "Reports":
                    var reportService = _serviceProvider.GetRequiredService<IReportsService>();
                    return new ReportsView(reportService);

                case "PublishingRecords":
                    var pubRecService = _serviceProvider.GetRequiredService<IPublishingService>();
                    var execRecService = _serviceProvider.GetRequiredService<IExecutionService>();
                    var pubSession = _serviceProvider.GetRequiredService<CurrentSessionProvider>().CurrentSession!;
                    return new PublishingRecordsView(pubRecService, execRecService, pubSession, _serviceProvider);

                case "PermissionMatrix":
                    var pmUserService = _serviceProvider.GetRequiredService<IUserService>();
                    var pmSession = _serviceProvider.GetRequiredService<CurrentSessionProvider>().CurrentSession!;
                    return new PermissionMatrixView(pmUserService, pmSession);

                case "SecurityRoles":
                    var srUserService = _serviceProvider.GetRequiredService<IUserService>();
                    var srSession = _serviceProvider.GetRequiredService<CurrentSessionProvider>().CurrentSession!;
                    return new SecurityRolesView(srUserService, srSession, this, dialogHelper);

                case "Permissions":
                    var permService = _serviceProvider.GetRequiredService<IPermissionService>();
                    return new PermissionsView(permService);

                case "Database":
                    var dbMgmtService = _serviceProvider.GetRequiredService<IDatabaseManagementService>();
                    var dbSession = _serviceProvider.GetRequiredService<CurrentSessionProvider>().CurrentSession!;
                    return new DatabaseManagementView(dbMgmtService, dbSession);

                case "AuditLogs":
                    var auditLogService = _serviceProvider.GetRequiredService<IAuditLogService>();
                    var alSession = _serviceProvider.GetRequiredService<CurrentSessionProvider>().CurrentSession!;
                    return new AuditLogsView(auditLogService, alSession);

                case "Diagnostics":
                    var diagService = _serviceProvider.GetRequiredService<ISystemDiagnosticsService>();
                    var diagSession = _serviceProvider.GetRequiredService<CurrentSessionProvider>().CurrentSession!;
                    return new SystemDiagnosticsView(diagService, diagSession);

                case "AdminHub":
                    var ahSession = _serviceProvider.GetRequiredService<CurrentSessionProvider>().CurrentSession!;
                    return new AdminHubView(_serviceProvider, ahSession, this);

                default:
                    return null;
            }
        }
    }
}
