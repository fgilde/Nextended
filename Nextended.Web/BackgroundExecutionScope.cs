using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Nextended.Web;

public class BackgroundExecutionScope : IAsyncDisposable
{
    protected readonly IServiceScope Scope;

    public IServiceProvider Services => Scope.ServiceProvider;

    public BackgroundExecutionScope(IServiceScopeFactory scopeFactory, HttpRequestSnapshot? snapshot = null) : this(
        scopeFactory, () => {}, snapshot)
    {}

    public BackgroundExecutionScope(IServiceScopeFactory scopeFactory, Action onIn, HttpRequestSnapshot? snapshot = null)
    {
        Scope = scopeFactory.CreateScope();

        if (snapshot is not null)
        {
            var httpAccessor = Scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
            httpAccessor.HttpContext = snapshot.ToDefaultHttpContext(Scope.ServiceProvider);
        }
        onIn?.Invoke();
        ScopeEnter();
    }

    public virtual BackgroundExecutionScope ScopeEnter()
    {
        return this;
    }
    
    public virtual Task CompleteAsync(CancellationToken ct) => Task.CompletedTask;

    public ValueTask DisposeAsync()
    {
        Scope.Dispose();
        return ValueTask.CompletedTask;
    }
}