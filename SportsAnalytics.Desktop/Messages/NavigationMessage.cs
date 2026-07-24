using CommunityToolkit.Mvvm.Messaging.Messages;
using SportsAnalytics.Desktop.ViewModels;

namespace SportsAnalytics.Desktop.Messages;

public class NavigationMessage : ValueChangedMessage<ViewModelBase>
{
    public NavigationMessage(ViewModelBase value) : base(value)
    {
    }
}
