using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using DCMS.Domain.Entities;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DCMS.WPF.ViewModels;

public class EngineerManagementViewModel : ViewModelBase
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private ObservableCollection<EngineerViewModel> _engineers;
    private ICollectionView _engineersView;
    private string _searchText = string.Empty;
    private bool _showInternal = true;
    private bool _showExternal = true;
    private bool _isBusy;
    private string _statusMessage = string.Empty;

    public EngineerManagementViewModel(IDbContextFactory<DCMSDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
        _engineers = new ObservableCollection<EngineerViewModel>();
        
        LoadCommand = new RelayCommand(async _ => await LoadEngineers());
        AddCommand = new RelayCommand(ExecuteAdd);
        SaveCommand = new RelayCommand(async _ => await SaveChanges());
        DeleteCommand = new RelayCommand(ExecuteDelete);
        
        // Initial Load
        _ = LoadEngineers();
    }

    public ICollectionView EngineersView => _engineersView;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _engineersView.Refresh();
            }
        }
    }

    public bool ShowInternal
    {
        get => _showInternal;
        set
        {
            if (SetProperty(ref _showInternal, value))
            {
                _engineersView.Refresh();
            }
        }
    }

    public bool ShowExternal
    {
        get => _showExternal;
        set
        {
            if (SetProperty(ref _showExternal, value))
            {
                _engineersView.Refresh();
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand LoadCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }

    private async Task LoadEngineers()
    {
        IsBusy = true;
        StatusMessage = "جاري تحميل البيانات...";
        
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var list = await context.Engineers.OrderBy(e => e.FullName).ToListAsync();
            
            _engineers = new ObservableCollection<EngineerViewModel>(
                list.Select(e => new EngineerViewModel(e))
            );

            _engineersView = CollectionViewSource.GetDefaultView(_engineers);
            _engineersView.Filter = FilterEngineers;
            
            OnPropertyChanged(nameof(EngineersView));
            StatusMessage = "تم التحميل بنجاح";
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ في التحميل: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool FilterEngineers(object obj)
    {
        if (obj is not EngineerViewModel vm) return false;

        // Type Filter
        bool typeMatch = (vm.IsResponsibleEngineer && ShowInternal) || (!vm.IsResponsibleEngineer && ShowExternal);
        if (!typeMatch) return false;

        // Search Filter
        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        
        return vm.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    private void ExecuteAdd(object? type)
    {
        // Clear search to ensure the new item is visible
        SearchText = string.Empty;

        bool isResponsible = type?.ToString() == "Internal";
        
        var newEng = new Engineer 
        { 
            FullName = "مهندس جديد", 
            IsResponsibleEngineer = isResponsible,
            IsActive = true 
        };

        var vm = new EngineerViewModel(newEng);
        _engineers.Insert(0, vm); // Add to top
        
        // Ensure the filter allows seeing the new item
        if (isResponsible && !ShowInternal) ShowInternal = true;
        if (!isResponsible && !ShowExternal) ShowExternal = true;
        
        StatusMessage = "تمت إضافة سجل جديد في أعلى القائمة. يرجى تعديل الاسم ثم الحفظ.";
    }

    private void ExecuteDelete(object? parameter)
    {
        if (parameter is EngineerViewModel vm)
        {
            if (MessageBox.Show($"هل أنت متأكد من حذف '{vm.FullName}'؟\nسيتم وضع علامة 'غير نشط' عليه بدلاً من الحذف النهائي للحفاظ على سجلات التاريخ.", 
                "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                vm.IsActive = false;
                StatusMessage = "تم تعطيل المهندس. اضغط حفظ لتطبيق التغييرات.";
            }
        }
    }

    private async Task SaveChanges()
    {
        IsBusy = true;
        StatusMessage = "جاري الحفظ...";

        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            // We need to sync our ViewModel changes back to DB
            // Strategy: Reload from DB, apply changes, add new ones
            
            foreach (var vm in _engineers)
            {
                if (vm.Id == 0) // New
                {
                    // Allow saving if name is changed from default
                    if (string.IsNullOrWhiteSpace(vm.FullName) || vm.FullName == "مهندس جديد")
                    {
                        if (vm.FullName == "مهندس جديد")
                            MessageBox.Show("يرجى تغيير اسم المهندس قبل الحفظ.", "تنبيه");
                        continue;
                    }
                    
                    context.Engineers.Add(vm.ToEntity());
                }
                else // Existing
                {
                    if (vm.IsDirty)
                    {
                        var entity = await context.Engineers.FindAsync(vm.Id);
                        if (entity != null)
                        {
                            entity.FullName = vm.FullName;
                            entity.IsResponsibleEngineer = vm.IsResponsibleEngineer;
                            entity.IsActive = vm.IsActive;
                        }
                    }
                }
            }

            await context.SaveChangesAsync();
            
            StatusMessage = "تم حفظ التغييرات بنجاح";
            await LoadEngineers(); // Reload to get IDs and fresh state
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ في الحفظ: {ex.Message}";
            MessageBox.Show($"خطأ في الحفظ: {ex.Message}", "Error");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class EngineerViewModel : ViewModelBase
{
    private Engineer _engineer;
    private bool _isDirty;

    public EngineerViewModel(Engineer engineer)
    {
        _engineer = engineer;
        _id = engineer.Id;
        _fullName = engineer.FullName;
        _isResponsibleEngineer = engineer.IsResponsibleEngineer;
        _isActive = engineer.IsActive;
    }

    private int _id;
    public int Id => _id;

    private string _fullName;
    public string FullName
    {
        get => _fullName;
        set
        {
            if (SetProperty(ref _fullName, value)) _isDirty = true;
        }
    }

    private bool _isResponsibleEngineer;
    public bool IsResponsibleEngineer
    {
        get => _isResponsibleEngineer;
        set
        {
            if (SetProperty(ref _isResponsibleEngineer, value)) _isDirty = true;
        }
    }

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (SetProperty(ref _isActive, value)) _isDirty = true;
        }
    }

    public bool IsDirty => _isDirty;

    public Engineer ToEntity()
    {
        return new Engineer
        {
            Id = Id,
            FullName = FullName,
            IsResponsibleEngineer = IsResponsibleEngineer,
            IsActive = IsActive
        };
    }
}
