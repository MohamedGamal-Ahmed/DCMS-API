using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using DCMS.Domain.Entities;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DCMS.WPF.ViewModels;

public class EngineerItemViewModel : INotifyPropertyChanged
{
    public Engineer Engineer { get; set; } = null!;
    
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class EngineerSelectorViewModel : ViewModelBase
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private readonly bool _responsibleEngineersOnly;
    private string _searchText = string.Empty;
    private string _statusMessage = string.Empty;
    private ObservableCollection<EngineerItemViewModel> _allEngineers = new();
    private ObservableCollection<EngineerItemViewModel> _filteredEngineers = new();
    private ObservableCollection<Engineer> _selectedEngineers = new();

    public EngineerSelectorViewModel(IDbContextFactory<DCMSDbContext> contextFactory, bool responsibleEngineersOnly = false)
    {
        _contextFactory = contextFactory;
        _responsibleEngineersOnly = responsibleEngineersOnly;
        
        AddEngineerCommand = new RelayCommand(ExecuteAddEngineer, CanExecuteAddEngineer);
        RemoveEngineerCommand = new RelayCommand(ExecuteRemoveEngineer);
        
        LoadEngineers();
    }

    #region Properties

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                FilterEngineers();
                ((RelayCommand)AddEngineerCommand).RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(CanAddNewEngineer));
            }
        }
    }

    public ObservableCollection<EngineerItemViewModel> FilteredEngineers
    {
        get => _filteredEngineers;
        set => SetProperty(ref _filteredEngineers, value);
    }

    public ObservableCollection<Engineer> SelectedEngineers
    {
        get => _selectedEngineers;
        set => SetProperty(ref _selectedEngineers, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }
    }

    public bool CanAddNewEngineer
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return false;
            
            var normalizedSearch = NormalizeName(SearchText);
            
            // Allow command ONLY if the engineer EXISTS
            return _allEngineers.Any(e => 
                NormalizeName(e.Engineer.FullName).Equals(normalizedSearch, StringComparison.OrdinalIgnoreCase));
        }
    }
    
    private string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        
        var normalized = name.Trim();
        // Remove common prefixes
        var prefixes = new[] { "Eng.", "Eng ", "م.", "م ", "مهندس" };
        foreach (var prefix in prefixes)
        {
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(prefix.Length).Trim();
            }
        }
        return normalized;
    }

    public bool HasSelectedEngineers => SelectedEngineers.Count > 0;
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    #endregion

    #region Commands

    public ICommand AddEngineerCommand { get; }
    public ICommand RemoveEngineerCommand { get; }

    private bool CanExecuteAddEngineer(object? parameter) => CanAddNewEngineer;

    private async void ExecuteAddEngineer(object? parameter)
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;

        try
        {
            StatusMessage = "جاري الإضافة...";

            var trimmedName = SearchText.Trim();

            using var context = await _contextFactory.CreateDbContextAsync();
            
            // Check if engineer already exists in database
            var existingEngineer = await context.Engineers
                .FirstOrDefaultAsync(e => e.FullName == trimmedName);

            if (existingEngineer != null)
            {
                StatusMessage = $"الاسم '{trimmedName}' موجود بالفعل في قاعدة البيانات";
                
                // If the engineer exists but is not in the local list (different filter), add to local
                if (!_allEngineers.Any(e => e.Engineer.Id == existingEngineer.Id))
                {
                    var engineerItem = new EngineerItemViewModel { Engineer = existingEngineer };
                    engineerItem.PropertyChanged += EngineerItem_PropertyChanged;
                    _allEngineers.Add(engineerItem);
                }
                
                // Add to selected (Modify to just select, not create)
                 var itemToSelect = _allEngineers.First(e => e.Engineer.Id == existingEngineer.Id);
                 itemToSelect.IsSelected = true;
                 if (!SelectedEngineers.Any(se => se.Id == existingEngineer.Id))
                 {
                     SelectedEngineers.Add(existingEngineer);
                 }

                SearchText = string.Empty;
                StatusMessage = "تم اختيار المهندس"; 
                await Task.Delay(2000);
                StatusMessage = string.Empty;
                return;
            }

            // If we get here, the engineer does not exist.
            // WE DO NOT ALLOW CREATION anymore.
            StatusMessage = "عذراً، هذا المهندس غير مسجل في النظام. يرجى التواصل مع المدير لإضافته.";
            await Task.Delay(3000);
            StatusMessage = string.Empty;
            return;

            /*
            // Creation Logic Removed
            ...
            */
        }
        catch (DbUpdateException)
        {
            // Handle unique constraint violation
            StatusMessage = $"خطأ: الاسم '{SearchText.Trim()}' موجود بالفعل في قاعدة البيانات";
            
            // Clear status after 3 seconds
            await Task.Delay(3000);
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ: {ex.Message}";
            
            // Clear status after 3 seconds
            await Task.Delay(3000);
            StatusMessage = string.Empty;
        }
    }

    private void ExecuteRemoveEngineer(object? parameter)
    {
        if (parameter is Engineer engineer)
        {
            var item = _allEngineers.FirstOrDefault(e => e.Engineer.Id == engineer.Id);
            if (item != null)
            {
                item.IsSelected = false;
            }
            SelectedEngineers.Remove(engineer);
        }
    }

    #endregion

    #region Methods

    private async void LoadEngineers()
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var query = context.Engineers.Where(e => e.IsActive);

            if (_responsibleEngineersOnly)
            {
                query = query.Where(e => e.IsResponsibleEngineer);
            }

            var engineers = await query.OrderBy(e => e.FullName).ToListAsync();

            _allEngineers.Clear();
            foreach (var engineer in engineers)
            {
                var item = new EngineerItemViewModel { Engineer = engineer };
                item.PropertyChanged += EngineerItem_PropertyChanged;
                _allEngineers.Add(item);
            }

            FilterEngineers();
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ في تحميل المهندسين: {ex.Message}";
        }
    }

    private void FilterEngineers()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            FilteredEngineers = new ObservableCollection<EngineerItemViewModel>(_allEngineers);
        }
        else
        {
            var filtered = _allEngineers.Where(e => 
                e.Engineer.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            FilteredEngineers = new ObservableCollection<EngineerItemViewModel>(filtered);
        }
    }

    private void EngineerItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EngineerItemViewModel.IsSelected))
        {
            SetSelectedEngineers();
        }
    }

    private void SetSelectedEngineers()
    {
        SelectedEngineers.Clear();
        foreach (var item in _allEngineers.Where(e => e.IsSelected))
        {
            SelectedEngineers.Add(item.Engineer);
        }
        OnPropertyChanged(nameof(HasSelectedEngineers));
    }

    #endregion
}
