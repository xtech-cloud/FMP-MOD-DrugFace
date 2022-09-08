
using XTC.FMP.MOD.DrugFace.App.Service;

/// <summary>
/// 测试上下文，用于共享测试资源
/// </summary>
public class TestFixture : TestFixtureBase
{
    public TestFixture()
        : base()
    {
    }

    public override void Dispose()
    {
        base.Dispose();
    }


    protected override void newHealthyService()
    {
        throw new NotImplementedException();
        //serviceHealthy_ = new HealthyService(new HealthyDAO(new DatabaseOptions()));
    }

}
