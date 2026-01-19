using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;

namespace DCMS.WPF.ViewModels;

public class GlobalSearchViewModel : ViewModelBase
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private string _searchText = string.Empty;
    private bool _isResultsPopupOpen;
    private bool _hasNoResults;
    private ObservableCollection<GlobalSearchResultItem> _searchResults;

    public event Action<GlobalSearchResultItem>? ResultSelected;

    public GlobalSearchViewModel(IDbContextFactory<DCMSDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
        _searchResults = new ObservableCollection<GlobalSearchResultItem>();
        ClearSearchCommand = new RelayCommand(_ => ClearSearch());
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                OnPropertyChanged(nameof(IsSearchActive));
                if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
                {
                    IsResultsPopupOpen = false;
                }
                else
                {
                    PerformSearch(value);
                }
            }
        }
    }

    public bool IsSearchActive => !string.IsNullOrEmpty(SearchText);

    public bool IsResultsPopupOpen
    {
        get => _isResultsPopupOpen;
        set => SetProperty(ref _isResultsPopupOpen, value);
    }

    public bool HasNoResults
    {
        get => _hasNoResults;
        set => SetProperty(ref _hasNoResults, value);
    }

    public ObservableCollection<GlobalSearchResultItem> SearchResults
    {
        get => _searchResults;
        set => SetProperty(ref _searchResults, value);
    }

    public ICommand ClearSearchCommand { get; }

    private void ClearSearch()
    {
        SearchText = string.Empty;
        IsResultsPopupOpen = false;
    }

    private async void PerformSearch(string query)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var results = new List<GlobalSearchResultItem>();

            // Search Inbounds
            var inbounds = await context.Inbounds
                .Where(i => i.Subject.Contains(query) || i.SubjectNumber.Contains(query) || (i.Code != null && i.Code.Contains(query)))
                .OrderByDescending(i => i.InboundDate)
                .Take(5)
                .AsNoTracking()
                .ToListAsync();

            results.AddRange(inbounds.Select(i => new GlobalSearchResultItem
            {
                Id = i.Id,
                Type = SearchResultType.Inbound,
                Subject = i.Subject,
                Subtitle = $"{i.SubjectNumber} | {i.InboundDate:d/M/yyyy}",
                Icon = "📥",
                IconBgColor = "#3498DB",
                OriginalObject = i
            }));

            // Search Outbounds
            var outbounds = await context.Outbounds
                .Where(o => o.Subject.Contains(query) || (o.Code != null && o.Code.Contains(query)))
                .OrderByDescending(o => o.OutboundDate)
                .Take(5)
                .AsNoTracking()
                .ToListAsync();

            results.AddRange(outbounds.Select(o => new GlobalSearchResultItem
            {
                Id = o.Id,
                Type = SearchResultType.Outbound,
                Subject = o.Subject,
                Subtitle = $"{o.Code ?? "No Code"} | {o.OutboundDate:d/M/yyyy}",
                Icon = "📤",
                IconBgColor = "#E67E22",
                OriginalObject = o
            }));

            SearchResults = new ObservableCollection<GlobalSearchResultItem>(results);
            HasNoResults = results.Count == 0;
            IsResultsPopupOpen = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Global Search Error: {ex.Message}");
        }
    }

    public void SelectItem(GlobalSearchResultItem item)
    {
        ResultSelected?.Invoke(item);
        IsResultsPopupOpen = false;
        SearchText = string.Empty; // Reset after selection
    }
}

public class GlobalSearchResultItem
{
    public int Id { get; set; }
    public SearchResultType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string IconBgColor { get; set; } = string.Empty;
    public object? OriginalObject { get; set; }
}

public enum SearchResultType
{
    Inbound,
    Outbound
}
