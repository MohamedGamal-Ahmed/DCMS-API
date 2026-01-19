using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.WPF.Services;
using DCMS.Domain.Models;

namespace DCMS.WPF.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CurrentUserService _currentUserService;
    private readonly NotificationService _notificationService;

    public NotificationService Notifications => _notificationService;
    
    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private string _userName = "المستخدم";

    [ObservableProperty]
    private bool _canManageUsers;

    [ObservableProperty]
    private bool _canManageEngineers;

    [ObservableProperty]
    private bool _canAddInbound;

    [ObservableProperty]
    private bool _canAddOutbound;

    [ObservableProperty]
    private bool _canSeeAuditLog;

    [ObservableProperty]
    private bool _canSeeReporting;

    [ObservableProperty]
    private bool _canSeeBackup;

    [ObservableProperty]
    private string _selectedNavigationItem = "CommandCenter";

    public User CurrentUser => _currentUserService.CurrentUser;

    public ICommand NavigateCommand { get; }
    public ICommand LogoutCommand { get; }

    public MainViewModel(
        IServiceProvider serviceProvider,
        CurrentUserService currentUserService,
        NotificationService notificationService)
    {
        _serviceProvider = serviceProvider;
        _currentUserService = currentUserService;
        _notificationService = notificationService;

        UserName = CurrentUser.FullName ?? CurrentUser.Username;
        ApplyPermissions();

        NavigateCommand = new RelayCommand<string>(ExecuteNavigate);
        LogoutCommand = new RelayCommand(ExecuteLogout);

        // Default view
        ExecuteNavigate("CommandCenter");
    }

    private void ApplyPermissions()
    {
        var user = CurrentUser;
        var role = user.Role;

        CanManageUsers = user.CanManageUsers();
        CanManageEngineers = user.CanManageEngineers();
        CanAddInbound = user.CanAddInbound();
        CanAddOutbound = user.CanAddOutbound();
        
        CanSeeAuditLog = role == UserRole.Admin || role == UserRole.OfficeManager;
        CanSeeReporting = role == UserRole.Admin || role == UserRole.OfficeManager;
        CanSeeBackup = role == UserRole.Admin;
    }

    private void ExecuteNavigate(string? destination)
    {
        if (string.IsNullOrEmpty(destination)) return;

        SelectedNavigationItem = destination;
        object? nextView = null;

        switch (destination)
        {
            case "CommandCenter":
                nextView = _serviceProvider.GetRequiredService<Views.AiChatView>();
                break;
            case "Dashboard":
                var dashboardView = _serviceProvider.GetRequiredService<Views.DashboardView>();
                if (dashboardView.DataContext is DashboardViewModel vm)
                {
                    vm.RequestNavigation += (s, args) => 
                    {
                        NavigateToSearchWithFilters(args);
                    };
                }
                nextView = dashboardView;
                break;
            case "Inbound":
                var typeSelectorView = _serviceProvider.GetRequiredService<Views.InboundTypeSelectorView>();
                typeSelectorView.NavigateToForm += (s, formControl) =>
                {
                    CurrentView = formControl;
                };
                nextView = typeSelectorView;
                break;
            case "Outbound":
                nextView = _serviceProvider.GetRequiredService<Views.PostaOutboundView>();
                break;
            case "Calendar":
                nextView = _serviceProvider.GetRequiredService<Views.MeetingAgendaView>();
                break;
            case "Search":
                nextView = _serviceProvider.GetRequiredService<Views.SearchAndFollowUpView>();
                break;
            case "Users":
                nextView = _serviceProvider.GetRequiredService<Views.UserManagementView>();
                break;
            case "Engineers":
                nextView = _serviceProvider.GetRequiredService<Views.EngineerManagementView>();
                break;
            case "AuditLog":
                var auditView = _serviceProvider.GetRequiredService<Views.AuditLogView>();
                auditView.DataContext = _serviceProvider.GetRequiredService<AuditLogViewModel>();
                nextView = auditView;
                break;
            case "Reporting":
                var reportingView = _serviceProvider.GetRequiredService<Views.ReportingView>();
                reportingView.DataContext = _serviceProvider.GetRequiredService<ReportingViewModel>();
                nextView = reportingView;
                break;
            case "Backup":
                var backupView = _serviceProvider.GetRequiredService<Views.BackupView>();
                backupView.DataContext = _serviceProvider.GetRequiredService<BackupViewModel>();
                nextView = backupView;
                break;
            case "Import":
                nextView = _serviceProvider.GetRequiredService<Views.ImportView>();
                break;
            case "Help":
                nextView = new Views.HelpView();
                break;
        }

        if (nextView != null)
        {
            CurrentView = nextView;
        }
    }

    private void NavigateToSearchWithFilters(SearchNavigationArgs args)
    {
        var searchView = _serviceProvider.GetRequiredService<Views.SearchAndFollowUpView>();
        if (searchView.DataContext is SearchAndFollowUpViewModel searchVm)
        {
            searchVm.ApplyDashboardFilters(args.Status, args.Engineer, args.OnlyOverdue, args.FromDate, args.ToDate, args.Entity, args.OnlyOutbound);
        }
        CurrentView = searchView;
    }

    public event EventHandler? RequestLogout;

    private void ExecuteLogout(object? parameter)
    {
        RequestLogout?.Invoke(this, EventArgs.Empty);
    }
}
