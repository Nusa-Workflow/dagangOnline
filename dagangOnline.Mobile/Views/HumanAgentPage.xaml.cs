using dagangOnline.Mobile.ViewModels;

namespace dagangOnline.Mobile.Views;

public partial class HumanAgentPage
{
    public HumanAgentPage()
    {
        BindingContext = new HumanAgentViewModel();
    }
}
