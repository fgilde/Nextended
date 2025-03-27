using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using System;
using YamlDotNet.Core.Tokens;

namespace Nextended.Core;


public class PausableCancellationTokenSource : CancellationTokenSource
{
    private BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.GetProperty;
    public bool IsPaused { get; private set; }

    public void Pause()
    {
        SetPaused(true);
    }

    public void Resume()
    {
        SetPaused(false);
    }

    public void PauseAfter(TimeSpan delay)
    {
        var context = TaskScheduler.FromCurrentSynchronizationContext();
        Task.Delay(delay).ContinueWith(task => Pause(), context);
    }

    public void ResumeAfter(TimeSpan delay)
    {
        var context = TaskScheduler.FromCurrentSynchronizationContext();
        Task.Delay(delay).ContinueWith(_ => Resume(), context);
    }

    public new static PausableCancellationTokenSource CreateLinkedTokenSource(params CancellationToken[] tokens)
    {
        if (tokens == null || tokens.Length == 0) throw new ArgumentException(nameof(tokens));
        var cancellationTokenSource = new PausableCancellationTokenSource { linkingRegistrations = new CancellationTokenRegistration[tokens.Length] };
        for (int index = 0; index < tokens.Length; ++index)
        {
            if (tokens[index].CanBeCanceled)
                cancellationTokenSource.linkingRegistrations[index] = tokens[index].RegisterWithoutExecutionContext(LinkedTokenCancelDelegate, cancellationTokenSource);
            tokens[index].RegisterPaused((token, b) => cancellationTokenSource.SetPaused(b));
        }
        return cancellationTokenSource;
    }

    public new static PausableCancellationTokenSource CreateLinkedTokenSource(CancellationToken token1, CancellationToken token2)
    {
        return CreateLinkedTokenSource([token1, token2]);
    }

    private void SetPaused(bool pause)
    {
        IsPaused = Token.SetAsPaused(pause);
    }

    private CancellationTokenRegistration[] linkingRegistrations
    {
        get => typeof(CancellationTokenSource).GetField("m_linkingRegistrations", flags)?.GetValue(this) as CancellationTokenRegistration[];
        set => typeof(CancellationTokenSource).GetField("m_linkingRegistrations", flags)?.SetValue(this, value);
    }

    private static Action<object> LinkedTokenCancelDelegate
    {
        get
        {
            var fieldInfo = typeof(CancellationTokenSource).GetField("s_LinkedTokenCancelDelegate", BindingFlags.Static | BindingFlags.NonPublic);
            return fieldInfo?.GetValue(null) as Action<object>;
        }
    }

}

public static class PausableCancellationToken
{
    private static readonly ConcurrentDictionary<CancellationToken, bool> pausedTokens = new ConcurrentDictionary<CancellationToken, bool>();
    private static readonly ConcurrentDictionary<CancellationToken, ConcurrentBag<Action<CancellationToken, bool>>> onPausedActions = new ConcurrentDictionary<CancellationToken, ConcurrentBag<Action<CancellationToken, bool>>>();

    internal static bool SetAsPaused(this CancellationToken token, bool shouldPause)
    {
        if (shouldPause && !token.IsCancellationRequested && !token.IsPaused())
        {
            pausedTokens.AddOrUpdate(token, true, (cancellationToken, b) => b);
            token.Register(() => SetAsPaused(token, false), true);
        }
        if (!shouldPause && pausedTokens.ContainsKey(token))
        {
            pausedTokens[token] = false;
            pausedTokens.TryRemove(token, out bool r);
        }
        return CallRegisteredActions(token, token.IsPaused());
    }

    internal static CancellationTokenRegistration RegisterWithoutExecutionContext(this CancellationToken token, Action<object> callback, object state)
    {
        var methodInfo = typeof(CancellationToken).GetMethod("InternalRegisterWithoutEC", BindingFlags.Instance | BindingFlags.NonPublic);
        return (CancellationTokenRegistration)methodInfo.Invoke(token, new[] { callback, state });
    }

    public static async Task WaitWhenPaused(this CancellationToken token)
    {
        while (token.IsPaused()) await Task.Delay(100, token);
    }

    private static bool CallRegisteredActions(CancellationToken token, bool isPaused)
    {
        foreach (var action in onPausedActions.GetOrAdd(token, new ConcurrentBag<Action<CancellationToken, bool>>()))
            action(token, isPaused);
        return isPaused;
    }

    public static void RegisterPaused(this CancellationToken token,
        Action<CancellationToken, bool> pausedChangedAction)
    {
        onPausedActions.GetOrAdd(token, new ConcurrentBag<Action<CancellationToken, bool>>()).Add(pausedChangedAction);
    }

    public static bool IsPaused(this CancellationToken token)
    {
        return pausedTokens.ContainsKey(token) && pausedTokens[token] && !token.IsCancellationRequested;
    }
    
}