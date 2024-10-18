using Server.src;

namespace Server.src.ServerModules;

internal class DashboardManager : ManagerModule<DashboardHelper>
{
    public DashboardManager(Region region, int id) : base(id)
    {
        Region = region;
    }

    override protected int Capacity { get; init; } = 2;
    private int capa = 2;

    public Region Region { get; init;}
    private readonly Region _region;
}