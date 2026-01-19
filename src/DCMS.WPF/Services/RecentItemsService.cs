using System.Configuration; // Or use Settings if available, but simple JSON/File approach is often better for portability
using System.IO;
using System.Text.Json;
using DCMS.Domain.Enums;

namespace DCMS.WPF.Services;

public class RecentItemsService
{
    private const string FileName = "recent_items.json";
    private readonly string _filePath;
    private List<RecentItem> _items;

    public event Action? RecentItemsChanged;

    public RecentItemsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, "DCMS");
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        
        _filePath = Path.Combine(folder, FileName);
        _items = LoadItems();
    }

    public void AddToRecent(string id, string title, RecentItemType type)
    {
        // Remove if exists to move to top
        var existing = _items.FirstOrDefault(i => i.Id == id && i.Type == type);
        if (existing != null)
        {
            _items.Remove(existing);
        }

        // Add to top
        _items.Insert(0, new RecentItem 
        { 
            Id = id, 
            Title = title, 
            Type = type, 
            AccessedAt = DateTime.Now 
        });

        // Keep max 10
        if (_items.Count > 10)
        {
            _items = _items.Take(10).ToList();
        }

        SaveItems();
        RecentItemsChanged?.Invoke();
    }

    public void RemoveFromRecent(string id, RecentItemType type)
    {
        var existing = _items.FirstOrDefault(i => i.Id == id && i.Type == type);
        if (existing != null)
        {
            _items.Remove(existing);
            SaveItems();
            RecentItemsChanged?.Invoke();
        }
    }

    public List<RecentItem> GetRecentItems()
    {
        return _items;
    }

    private List<RecentItem> LoadItems()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<RecentItem>>(json) ?? new List<RecentItem>();
            }
        }
        catch 
        {
            // Ignore errors
        }
        return new List<RecentItem>();
    }

    private void SaveItems()
    {
        try
        {
            var json = JsonSerializer.Serialize(_items);
            File.WriteAllText(_filePath, json);
        }
        catch 
        {
            // Ignore
        }
    }
}

public class RecentItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public RecentItemType Type { get; set; }
    public DateTime AccessedAt { get; set; }
}

public enum RecentItemType
{
    Inbound,
    Outbound
}
