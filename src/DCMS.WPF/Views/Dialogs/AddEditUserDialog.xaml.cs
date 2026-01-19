using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DCMS.WPF.Views.Dialogs;

public partial class AddEditUserDialog : Window
{
    private readonly User? _user;
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;

    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole SelectedRole { get; set; } = UserRole.FollowUpStaff;
    public bool UserIsActive { get; set; } = true;

    public AddEditUserDialog(User? user, IDbContextFactory<DCMSDbContext> contextFactory)
    {
        InitializeComponent();
        _user = user;
        _contextFactory = contextFactory;

        // Populate roles ComboBox
        cmbRole.ItemsSource = Enum.GetValues(typeof(UserRole)).Cast<UserRole>()
            .Select(r => new { Value = r, DisplayName = GetRoleDisplayName(r) });
        cmbRole.DisplayMemberPath = "DisplayName";
        cmbRole.SelectedValuePath = "Value";

        if (_user != null)
        {
            // Edit mode
            Title = "تعديل مستخدم";
            Username = _user.Username;
            FullName = _user.FullName ?? string.Empty;
            Email = _user.Email ?? string.Empty;
            SelectedRole = _user.Role;
            UserIsActive = _user.IsActive;

            chkChangePassword.Visibility = Visibility.Visible;
            lblPassword.Visibility = Visibility.Collapsed;
        }
        else
        {
            // Add mode
            Title = "إضافة مستخدم جديد";
            cmbRole.SelectedValue = SelectedRole; // Set default selection
        }

        DataContext = this;
    }

    private string GetRoleDisplayName(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => "مدير النظام",
            UserRole.TechnicalManager => "مدير فني",
            UserRole.FollowUpStaff => "موظف متابعة",
            _ => role.ToString()
        };
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtUsername.Text))
        {
            MessageBox.Show("اسم المستخدم مطلوب", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if ((_user == null || chkChangePassword.IsChecked == true) && string.IsNullOrWhiteSpace(txtPassword.Password))
        {
            MessageBox.Show("كلمة المرور مطلوبة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            using var context = _contextFactory.CreateDbContext();

            if (_user == null)
            {
                // Add new user
                var newUser = new User
                {
                    Username = txtUsername.Text.Trim(),
                    FullName = txtFullName.Text.Trim(),
                    Email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim(),
                    PasswordHash = HashPassword(txtPassword.Password),
                    Role = (UserRole)cmbRole.SelectedValue,
                    IsActive = chkIsActive.IsChecked ?? true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Users.Add(newUser);
            }
            else
            {
                // Edit existing user
                var dbUser = context.Users.Find(_user.Id);
                if (dbUser != null)
                {
                    dbUser.Username = txtUsername.Text.Trim();
                    dbUser.FullName = txtFullName.Text.Trim();
                    dbUser.Email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim();
                    dbUser.Role = (UserRole)cmbRole.SelectedValue;
                    dbUser.IsActive = chkIsActive.IsChecked ?? true;
                    if (chkChangePassword.IsChecked == true)
                    {
                        dbUser.PasswordHash = HashPassword(txtPassword.Password);
                    }
                    dbUser.UpdatedAt = DateTime.UtcNow;
                }
            }

            context.SaveChanges();
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ChkChangePassword_CheckedChanged(object sender, RoutedEventArgs e)
    {
        if (lblPassword != null)
        {
            lblPassword.Visibility = chkChangePassword.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
