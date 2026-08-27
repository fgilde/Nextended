using System.Diagnostics;

namespace Nextended.Aspire;

public static class DistributedApplicationExtensions
{
    public static DistributedApplication EnsureDockerRunning(this DistributedApplication application)
    {
        DockerHelper.EnsureDockerIsRunning();
        return application;
    }

    public static IDistributedApplicationBuilder EnsureDockerRunning(this IDistributedApplicationBuilder application)
    {
        DockerHelper.EnsureDockerIsRunning();
        return application;
    }

    public static DistributedApplication EnsureDockerRunningIf(this DistributedApplication application, bool condition)
        => condition ? application.EnsureDockerRunning() : application;

    public static IDistributedApplicationBuilder EnsureDockerRunningIf(this IDistributedApplicationBuilder application, bool condition)
        => condition ? application.EnsureDockerRunning() : application;

    public static DistributedApplication EnsureDockerRunningIfLocalDebug(this DistributedApplication application)
        => application.EnsureDockerRunningIf(IsDebug() && Debugger.IsAttached);

    public static DistributedApplication EnsureDockerRunningIfDebuggerAttached(this DistributedApplication application)
        => application.EnsureDockerRunningIf(Debugger.IsAttached);

    public static IDistributedApplicationBuilder EnsureDockerRunningIfRunMode(this IDistributedApplicationBuilder applicationBuilder)
        => applicationBuilder.EnsureDockerRunningIf(applicationBuilder.ExecutionContext.IsRunMode);

    private static bool IsDebug()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }
}
