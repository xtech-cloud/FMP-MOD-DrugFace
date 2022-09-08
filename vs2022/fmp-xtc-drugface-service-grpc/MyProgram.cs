
public static class MyProgram
{
    public static void PreBuild(WebApplicationBuilder? _builder)
    {
        //_builder?.Services.AddSingleton<YourDAO>();
    }

    public static void PreRun(WebApplication? _app)
    {
    }
}
