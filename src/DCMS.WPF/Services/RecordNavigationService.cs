using DCMS.Application.Interfaces;
using DCMS.Infrastructure.Data;
using DCMS.WPF.ViewModels;
using DCMS.WPF.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace DCMS.WPF.Services;

public class RecordNavigationService : IRecordNavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;

    public RecordNavigationService(IServiceProvider serviceProvider, IDbContextFactory<DCMSDbContext> contextFactory)
    {
        _serviceProvider = serviceProvider;
        _contextFactory = contextFactory;
    }

    public async Task OpenRecordAsync(string url)
    {
        if (string.IsNullOrEmpty(url)) return;

        try
        {
            var uri = new Uri(url);
            if (uri.Scheme != "record") return;

            var type = uri.Host.ToLower();
            var path = uri.AbsolutePath.Trim('/');
            
            // If host is a code like "in-1234", we need to handle it
            string? codeToSearch = null;
            int? idToSearch = null;

            if (int.TryParse(path, out int parsedId))
            {
                idToSearch = parsedId;
            }
            else if (type.StartsWith("in-") || type.StartsWith("out-"))
            {
                codeToSearch = type.ToUpper();
                type = type.Split('-')[0]; // "in" or "out"
            }
            else if (!string.IsNullOrEmpty(path))
            {
                codeToSearch = path.ToUpper();
            }
            else
            {
                codeToSearch = type.ToUpper();
            }

            await System.Windows.Application.Current.Dispatcher.Invoke(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
                var currentUserService = scope.ServiceProvider.GetRequiredService<CurrentUserService>();
                var correspondenceService = scope.ServiceProvider.GetRequiredService<ICorrespondenceService>();
                
                using var context = await _contextFactory.CreateDbContextAsync();

                if (type == "inbound" || type == "in")
                {
                    var recordDto = (DCMS.Application.DTOs.AiSearchResultDto?)null;
                    if (idToSearch.HasValue)
                        recordDto = await correspondenceService.GetByIdAsync(idToSearch.Value, "Inbound");
                    else if (!string.IsNullOrEmpty(codeToSearch))
                        recordDto = await correspondenceService.GetByIdAsync(codeToSearch, "Inbound");

                    if (recordDto != null && int.TryParse(recordDto.Id, out int entityId))
                    {
                        // We need the actual entity to open the details view
                        var entity = await context.Inbounds.FindAsync(entityId);
                        if (entity != null)
                        {
                            var detailsVm = new InboundDetailsViewModel(_contextFactory, _serviceProvider, notificationService, currentUserService, currentUserService.CurrentUser, entity);
                            var detailsView = new InboundDetailsView { DataContext = detailsVm };
                            detailsVm.RequestClose += () => detailsView.Close();
                            detailsView.Show();
                        }
                    }
                }
                else if (type == "outbound" || type == "out")
                {
                    var recordDto = (DCMS.Application.DTOs.AiSearchResultDto?)null;
                    if (idToSearch.HasValue)
                        recordDto = await correspondenceService.GetByIdAsync(idToSearch.Value, "Outbound");
                    else if (!string.IsNullOrEmpty(codeToSearch))
                        recordDto = await correspondenceService.GetByIdAsync(codeToSearch, "Outbound");

                    if (recordDto != null && int.TryParse(recordDto.Id, out int entityId))
                    {
                        var entity = await context.Outbounds.FindAsync(entityId);
                        if (entity != null)
                        {
                            var detailsVm = new OutboundDetailsViewModel(_contextFactory, _serviceProvider, currentUserService, entity);
                            var detailsView = new OutboundDetailsView { DataContext = detailsVm };
                            detailsVm.RequestClose += () => detailsView.Close();
                            detailsView.Show();
                        }
                    }
                }
                else if (type == "meeting")
                {
                    var record = await context.Meetings.FindAsync(idToSearch ?? 0);
                    if (record != null)
                    {
                        var editVm = _serviceProvider.GetRequiredService<AddMeetingDialogViewModel>();
                        editVm.LoadMeeting(record);
                        var editDialog = new AddMeetingDialog(editVm);
                        editDialog.Show();
                    }
                }
                else
                {
                    MessageBox.Show($"فتح سجل من نوع {type} غير مدعوم حالياً بشكل مباشر من الشات.", "تنبيه");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening record: {ex.Message}");
        }
    }
}
