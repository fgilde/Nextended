using System.Diagnostics;

namespace Nextended.Aspire;

public static class DistributedApplicationExtensions
{
    public static DistributedApplication EnsureDockerRunning(this DistributedApplication application)
    {
        DockerHelper.EnsureDockerIsRunning();
        return application;
    }

    public static DistributedApplication EnsureDockerRunningIf(this DistributedApplication application, bool condition)
        => condition ? application.EnsureDockerRunning() : application;

    public static DistributedApplication EnsureDockerRunningIfLocalDebug(this DistributedApplication application)
        => application.EnsureDockerRunningIf(IsDebug() && Debugger.IsAttached);

    private static bool IsDebug()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }
}
