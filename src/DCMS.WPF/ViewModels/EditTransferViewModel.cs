using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.Infrastructure.Data;
using DCMS.WPF.Helpers;
using DCMS.WPF.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DCMS.WPF.ViewModels;

public class EditTransferViewModel : ViewModelBase
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private readonly NotificationService _notificationService;
    private readonly CurrentUserService _currentUserService;
    private readonly InboundTransfer _transfer;
    
    private DateTime _transferDate;
    private string _transferAttachmentUrl;
    private string _response;
    private DateTime? _responseDate;
    private string _responseAttachmentUrl;
    private bool _isBusy;

    public EditTransferViewModel(IDbContextFactory<DCMSDbContext> contextFactory, NotificationService notificationService, CurrentUserService currentUserService, InboundTransfer transfer)
    {
        _contextFactory = contextFactory;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _transfer = transfer;

        _transferDate = transfer.TransferDate;
        _transferAttachmentUrl = transfer.TransferAttachmentUrl ?? string.Empty;
        _response = transfer.Response ?? string.Empty;
        _responseDate = transfer.ResponseDate;
        _responseAttachmentUrl = transfer.ResponseAttachmentUrl ?? string.Empty;

        SaveCommand = new RelayCommand(ExecuteSave);
        DeleteCommand = new RelayCommand(ExecuteDelete);
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke());
    }

    public string EngineerName => _transfer.Engineer?.FullName ?? "Unknown";

    public DateTime TransferDate
    {
        get => _transferDate;
        set => SetProperty(ref _transferDate, value);
    }

    public string TransferAttachmentUrl
    {
        get => _transferAttachmentUrl;
        set => SetProperty(ref _transferAttachmentUrl, value);
    }

    public string Response
    {
        get => _response;
        set => SetProperty(ref _response, value);
    }

    public DateTime? ResponseDate
    {
        get => _responseDate;
        set => SetProperty(ref _responseDate, value);
    }

    public string ResponseAttachmentUrl
    {
        get => _responseAttachmentUrl;
        set => SetProperty(ref _responseAttachmentUrl, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand CancelCommand { get; }

    public event Action? RequestClose;

    private async void ExecuteSave(object? parameter)
    {
        IsBusy = true;
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var transferToUpdate = await context.InboundTransfers
                .FirstOrDefaultAsync(t => t.InboundId == _transfer.InboundId && t.EngineerId == _transfer.EngineerId);

            if (transferToUpdate != null)
            {
                transferToUpdate.TransferDate = TransferDate.ToUniversalTime();
                transferToUpdate.TransferAttachmentUrl = TransferAttachmentUrl;
                transferToUpdate.Response = Response;
                transferToUpdate.ResponseDate = ResponseDate?.ToUniversalTime();
                transferToUpdate.ResponseAttachmentUrl = ResponseAttachmentUrl;

                await context.SaveChangesAsync();
                
                _notificationService.AddNotification($"تم تعديل سجل التحويل للمهندس: {EngineerName}", _transfer.InboundId.ToString(), NotificationType.Success);
                
                MessageBox.Show("تم حفظ التعديلات بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                RequestClose?.Invoke();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"حدث خطأ أثناء الحفظ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    private async void ExecuteDelete(object? parameter)
    {
        var result = MessageBox.Show($"هل أنت متأكد من حذف سجل التحويل للمهندس: {EngineerName}؟\nسيتم حذف هذا الإجراء من سجل الأحداث نهائياً.", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        
        if (result == MessageBoxResult.Yes)
        {
            IsBusy = true;
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var transferToDelete = await context.InboundTransfers
                    .FirstOrDefaultAsync(t => t.InboundId == _transfer.InboundId && t.EngineerId == _transfer.EngineerId);

                if (transferToDelete != null)
                {
                    context.InboundTransfers.Remove(transferToDelete);
                    await context.SaveChangesAsync();

                    _notificationService.AddNotification($"تم حذف سجل التحويل للمهندس: {EngineerName}", _transfer.InboundId.ToString(), NotificationType.Warning);
                    
                    MessageBox.Show("تم حذف السجل بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    RequestClose?.Invoke();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء الحذف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
