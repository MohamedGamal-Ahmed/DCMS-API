using System.Collections.ObjectModel;
using System.Windows.Input;
using DCMS.WPF.Services;

namespace DCMS.WPF.ViewModels;

public class RecentItemsViewModel : ViewModelBase
{
    private readonly RecentItemsService _recentItemsService;
    private ObservableCollection<Services.RecentItem> _items;

    public event Action<Services.RecentItem>? ItemSelected;

    public RecentItemsViewModel(RecentItemsService recentItemsService)
    {
        _recentItemsService = recentItemsService;
        _items = new ObservableCollection<Services.RecentItem>(_recentItemsService.GetRecentItems());

        _recentItemsService.RecentItemsChanged += OnRecentItemsChanged;
        RemoveItemCommand = new RelayCommand(ExecuteRemoveItem);
    }

    public ICommand RemoveItemCommand { get; }

    private void ExecuteRemoveItem(object? parameter)
    {
        if (parameter is Services.RecentItem item)
        {
            _recentItemsService.RemoveFromRecent(item.Id, item.Type);
        }
    }

    public ObservableCollection<Services.RecentItem> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

    private void OnRecentItemsChanged()
    {
        Items = new ObservableCollection<Services.RecentItem>(_recentItemsService.GetRecentItems());
        OnPropertyChanged(nameof(NoItemsMessageVisibility));
    }

    public System.Windows.Visibility NoItemsMessageVisibility => 
        (Items == null || Items.Count == 0) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

    public void SelectItem(Services.RecentItem item)
    {
        ItemSelected?.Invoke(item);
    }

    public void AddToRecent(string id, string title, Services.RecentItemType type)
    {
        _recentItemsService.AddToRecent(id, title, type);
    }
}
