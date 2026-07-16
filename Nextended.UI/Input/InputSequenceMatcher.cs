using System;
using System.Collections.Generic;
using System.Linq;

namespace Nextended.UI.Input;

/// <summary>
///     Matches an ORDERED sequence of bindings (each step may itself be a chord), e.g.
///     Ctrl+X → X → J. Registers each distinct step with the given
///     <see cref="InputBindingManager"/> and raises <see cref="SequenceCompleted"/> when the steps
///     are pressed in order, each within <see cref="StepTimeout"/> of the previous one.
/// </summary>
public sealed class InputSequenceMatcher : IDisposable
{
    private readonly InputBindingManager _manager;
    private readonly List<StoredInputBinding> _sequence;
    // Distinct steps registered once each (a duplicate step like X,X reuses the same registration).
    private readonly List<(string Id, StoredInputBinding Binding)> _registered = new();
    private int _position;
    private DateTime _lastAdvance = DateTime.MinValue;

    /// <summary>Maximum gap between two steps before the sequence resets.</summary>
    public TimeSpan StepTimeout { get; set; } = TimeSpan.FromSeconds(2);

    public event Action? SequenceCompleted;

    public InputSequenceMatcher(InputBindingManager manager, IEnumerable<StoredInputBinding> sequence)
    {
        _manager = manager;
        // Per-step MinTime is recording metadata (gap between recorded presses), not a hold
        // requirement — strip it so the manager fires immediately.
        _sequence = sequence.Where(s => s is { IsValid: true }).Select(s => s.WithoutMinTime()).ToList();

        var idPrefix = $"seq_{Guid.NewGuid():N}";
        foreach (var step in _sequence)
        {
            if (_registered.Any(r => r.Binding.Equals(step))) continue;
            var id = $"{idPrefix}_{_registered.Count}";
            _registered.Add((id, step));
            _manager.RegisterBinding(id, step);
        }

        if (_sequence.Count > 0)
            _manager.OnBindingPressed += OnBindingPressed;
    }

    /// <summary>The (valid) steps this matcher watches.</summary>
    public IReadOnlyList<StoredInputBinding> Sequence => _sequence;

    private void OnBindingPressed(string bindingId)
    {
        var hit = _registered.FirstOrDefault(r => r.Id == bindingId);
        if (hit.Id == null) return; // not one of ours

        if (_position > 0 && DateTime.Now - _lastAdvance > StepTimeout)
            _position = 0;

        if (hit.Binding.Equals(_sequence[_position]))
        {
            _position++;
            _lastAdvance = DateTime.Now;
        }
        else
        {
            // ponytail: wrong step resets (restarting at 1 if it matches the first step); presses of
            // keys that are no step at all are ignored entirely — add strict-reset via OnKeyPressed
            // if false positives ever matter.
            _position = hit.Binding.Equals(_sequence[0]) ? 1 : 0;
            _lastAdvance = DateTime.Now;
        }

        if (_position >= _sequence.Count)
        {
            _position = 0;
            SequenceCompleted?.Invoke();
        }
    }

    public void Dispose()
    {
        _manager.OnBindingPressed -= OnBindingPressed;
        foreach (var (id, _) in _registered)
            _manager.RemoveBinding(id);
        _registered.Clear();
    }
}
