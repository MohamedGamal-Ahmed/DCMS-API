using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DCMS.WPF.ViewModels;

public class UserManagementViewModel : INotifyPropertyChanged
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private string _searchText = string.Empty;
    private User? _selectedUser;

    public ObservableCollection<User> Users { get; set; } = new();
    public ObservableCollection<User> FilteredUsers { get; set; } = new();

    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged();
            FilterUsers();
        }
    }

    public User? SelectedUser
    {
        get => _selectedUser;
        set
        {
            _selectedUser = value;
            OnPropertyChanged();
        }
    }

    public ICommand LoadUsersCommand { get; }
    public ICommand AddUserCommand { get; }
    public ICommand EditUserCommand { get; }
    public ICommand DeleteUserCommand { get; }
    public ICommand ToggleActiveCommand { get; }

    public UserManagementViewModel(IDbContextFactory<DCMSDbContext> contextFactory)
    {
        _contextFactory = contextFactory;

        LoadUsersCommand = new RelayCommand(_ => _ = LoadUsers());
        AddUserCommand = new RelayCommand(_ => AddUser());
        EditUserCommand = new RelayCommand(p => EditUser(p as User), p => p is User);
        DeleteUserCommand = new RelayCommand(async p => await DeleteUser(p as User), p => p is User);
        ToggleActiveCommand = new RelayCommand(async p => await ToggleActive(p as User), p => p is User);

        _ = LoadUsers();
    }

    private async Task LoadUsers()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var users = await context.Users
            .OrderBy(u => u.Username)
            .ToListAsync();

        Users.Clear();
        foreach (var user in users)
        {
            Users.Add(user);
        }

        FilterUsers();
    }

    private void FilterUsers()
    {
        FilteredUsers.Clear();
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? Users
            : Users.Where(u =>
                (u.Username?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.FullName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.Email?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));

        foreach (var user in filtered)
        {
            FilteredUsers.Add(user);
        }
    }

    private void AddUser()
    {
        var dialog = new Views.Dialogs.AddEditUserDialog(null, _contextFactory);
        if (dialog.ShowDialog() == true)
        {
            _ = LoadUsers();
        }
    }

    private void EditUser(User? user)
    {
        if (user == null) return;

        var dialog = new Views.Dialogs.AddEditUserDialog(user, _contextFactory);
        if (dialog.ShowDialog() == true)
        {
            _ = LoadUsers();
        }
    }

    private async Task DeleteUser(User? user)
    {
        if (user == null) return;

        var result = System.Windows.MessageBox.Show(
            $"هل أنت متأكد من حذف المستخدم '{user.Username}'؟\nهذا الإجراء لا يمكن التراجع عنه.",
            "تأكيد الحذف",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        using var context = await _contextFactory.CreateDbContextAsync();
        var dbUser = await context.Users.FindAsync(user.Id);
        if (dbUser != null)
        {
            context.Users.Remove(dbUser);
            await context.SaveChangesAsync();
            await LoadUsers();

            System.Windows.MessageBox.Show(
                "تم حذف المستخدم بنجاح",
                "نجح",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
    }

    private async Task ToggleActive(User? user)
    {
        if (user == null) return;

        using var context = await _contextFactory.CreateDbContextAsync();
        var dbUser = await context.Users.FindAsync(user.Id);
        if (dbUser != null)
        {
            dbUser.IsActive = !dbUser.IsActive;
            dbUser.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            await LoadUsers();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
