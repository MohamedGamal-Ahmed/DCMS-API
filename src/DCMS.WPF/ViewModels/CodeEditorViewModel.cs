using DCMS.WPF.Helpers;
using DCMS.WPF.Services;

namespace DCMS.WPF.ViewModels;

public class CodeEditorViewModel : ViewModelBase
{
    private string _code = string.Empty;
    private string _entity = string.Empty;
    private string _engineer = string.Empty;
    private string _title = "إضافة كود جديد";

    public CodeEditorViewModel()
    {
        Title = "إضافة كود جديد";
    }

    public CodeEditorViewModel(CodeEntry existing)
    {
        Title = "تعديل الكود";
        Code = existing.Code;
        Entity = existing.Entity;
        Engineer = existing.Engineer;
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Code
    {
        get => _code;
        set => SetProperty(ref _code, value);
    }

    public string Entity
    {
        get => _entity;
        set => SetProperty(ref _entity, value);
    }

    public string Engineer
    {
        get => _engineer;
        set => SetProperty(ref _engineer, value);
    }
}
