using System;

namespace Nextended.Core.Scopes;

public class ActionScope: IDisposable
{
    private readonly Action actionOut;

    public ActionScope(Action actionIn, Action actionOut)
    {
        this.actionOut = actionOut;
        actionIn();
    }

    public void Dispose()
    {
        actionOut();
    }
}