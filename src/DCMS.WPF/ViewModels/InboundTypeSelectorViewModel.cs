using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;

namespace DCMS.WPF.ViewModels;

public class InboundTypeSelectorViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;

    public InboundTypeSelectorViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        SelectPostaCommand = new RelayCommand(ExecuteSelectPosta);
        SelectEmailCommand = new RelayCommand(ExecuteSelectEmail);
        SelectRequestCommand = new RelayCommand(ExecuteSelectRequest);
        SelectMissionCommand = new RelayCommand(ExecuteSelectMission);
        SelectContractCommand = new RelayCommand(ExecuteSelectContract);
    }

    public ICommand SelectPostaCommand { get; }
    public ICommand SelectEmailCommand { get; }
    public ICommand SelectRequestCommand { get; }
    public ICommand SelectMissionCommand { get; }
    public ICommand SelectContractCommand { get; }

    public event EventHandler<UserControl>? NavigateToForm;

    private void ExecuteSelectPosta(object? parameter)
    {
        var postaView = _serviceProvider.GetRequiredService<Views.PostaInboundView>();
        NavigateToForm?.Invoke(this, postaView);
    }

    private void ExecuteSelectEmail(object? parameter)
    {
        var emailView = _serviceProvider.GetRequiredService<Views.EmailInboundView>();
        NavigateToForm?.Invoke(this, emailView);
    }

    private void ExecuteSelectRequest(object? parameter)
    {
        var requestView = _serviceProvider.GetRequiredService<Views.RequestInboundView>();
        NavigateToForm?.Invoke(this, requestView);
    }

    private void ExecuteSelectMission(object? parameter)
    {
        var missionView = _serviceProvider.GetRequiredService<Views.MissionInboundView>();
        NavigateToForm?.Invoke(this, missionView);
    }

    private void ExecuteSelectContract(object? parameter)
    {
        var contractView = _serviceProvider.GetRequiredService<Views.ContractInboundView>();
        NavigateToForm?.Invoke(this, contractView);
    }
}
