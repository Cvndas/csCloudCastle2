using Server.src;

namespace Server.src.ServerModules;

internal class DashboardManager : ManagementModule<DashboardHelper>
{
    public DashboardManager()
    {
        HelperList = new List<DashboardHelper>(Capacity);
    }

    override protected List<DashboardHelper> HelperList { get; init; }
    override protected int Capacity { get; } = 2;
}