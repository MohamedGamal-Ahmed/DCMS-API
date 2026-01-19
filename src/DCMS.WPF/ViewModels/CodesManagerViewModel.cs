using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using DCMS.WPF.Helpers;
using DCMS.WPF.Services;

namespace DCMS.WPF.ViewModels;

public class CodesManagerViewModel : ViewModelBase
{
    private readonly CodeLookupService _codeLookupService;
    private readonly IServiceProvider _serviceProvider;

    public CodesManagerViewModel(CodeLookupService codeLookupService, IServiceProvider serviceProvider)
    {
        _codeLookupService = codeLookupService;
        _serviceProvider = serviceProvider;
        
        RefreshCommand = new RelayCommand(ExecuteRefresh);
        AddCommand = new RelayCommand(ExecuteAdd);
        EditCommand = new RelayCommand(ExecuteEdit, CanExecuteEdit);
        DeleteCommand = new RelayCommand(ExecuteDelete, CanExecuteEdit);

        // Initial Load
        RefreshCodes();
    }

    public ObservableCollection<CodeEntry> Codes => _codeLookupService.AvailableCodes;

    private CodeEntry? _selectedCode;
    public CodeEntry? SelectedCode
    {
        get => _selectedCode;
        set
        {
            if (SetProperty(ref _selectedCode, value))
            {
                (EditCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (DeleteCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand RefreshCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    private void ExecuteRefresh(object? parameter)
    {
        RefreshCodes();
    }

    private void RefreshCodes()
    {
        // Service updates the collection directly, but we can trigger a reload if needed
        // Assuming the service maintains the single source of truth ObservableCollection
    }

    private void ExecuteAdd(object? parameter)
    {
        var editorViewModel = new CodeEditorViewModel();
        var editorView = new Views.Dialogs.CodeEditorDialog { DataContext = editorViewModel };

        if (editorView.ShowDialog() == true)
        {
            _ = _codeLookupService.AddCodeAsync(editorViewModel.Code, editorViewModel.Entity, editorViewModel.Engineer);
        }
    }

    private void ExecuteEdit(object? parameter)
    {
        if (SelectedCode == null) return;

        var editorViewModel = new CodeEditorViewModel(SelectedCode);
        var editorView = new Views.Dialogs.CodeEditorDialog { DataContext = editorViewModel };

        if (editorView.ShowDialog() == true)
        {
            _ = _codeLookupService.UpdateCodeAsync(SelectedCode.Id, editorViewModel.Code, editorViewModel.Entity, editorViewModel.Engineer);
        }
    }

    private bool CanExecuteEdit(object? parameter)
    {
        return SelectedCode != null;
    }

    private void ExecuteDelete(object? parameter)
    {
        if (SelectedCode == null) return;

        var result = MessageBox.Show($"هل أنت متأكد من حذف الكود: {SelectedCode.Code}؟", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            _ = _codeLookupService.DeleteCodeAsync(SelectedCode.Id);
        }
    }
}
