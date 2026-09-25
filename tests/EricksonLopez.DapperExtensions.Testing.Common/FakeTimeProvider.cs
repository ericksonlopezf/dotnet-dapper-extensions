// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.DapperExtensions.Testing.Common;

#pragma warning disable CS8765, CS8767, CS8766, CS8769, CA1010, CA1816, CA1034, CA1711

/// <summary>
/// Reusable test TimeProvider for virtualizing time and executing Polly retries/delays instantly.
/// </summary>
public sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;
    private readonly List<SchedulingTimer> _timers = new();
    private readonly object _lock = new();

    public override DateTimeOffset GetUtcNow() => _utcNow;
    public override long GetTimestamp() => _utcNow.Ticks;
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public void Advance(TimeSpan duration)
    {
        lock (_lock)
        {
            _utcNow += duration;
            foreach (var timer in _timers.ToArray())
            {
                timer.OnTimeAdvanced(_utcNow);
            }
        }
    }

    /// <summary>
    /// When set to true (default for fast retry loops), timers with small delay (<= 15s) trigger automatically upon creation.
    /// When set to false, timers only trigger when Advance(TimeSpan) is explicitly called, enabling deterministic timeout testing.
    /// </summary>
    public bool AutoTriggerDelays { get; set; } = true;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        lock (_lock)
        {
            var timer = new SchedulingTimer(this, callback, state, dueTime, period, _utcNow);
            _timers.Add(timer);
            if (AutoTriggerDelays && dueTime > TimeSpan.Zero && dueTime <= TimeSpan.FromSeconds(15))
            {
                timer.Trigger();
            }
            return timer;
        }
    }

    internal void RemoveTimer(SchedulingTimer timer)
    {
        lock (_lock)
        {
            _timers.Remove(timer);
        }
    }

    internal sealed class SchedulingTimer : ITimer
    {
        private readonly FakeTimeProvider _parent;
        private readonly TimerCallback _callback;
        private readonly object? _state;
        private DateTimeOffset _dueTimeUtc;
        private TimeSpan _period;
        private bool _triggered;

        public SchedulingTimer(FakeTimeProvider parent, TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period, DateTimeOffset currentUtc)
        {
            _parent = parent;
            _callback = callback;
            _state = state;
            _dueTimeUtc = dueTime == Timeout.InfiniteTimeSpan ? DateTimeOffset.MaxValue : currentUtc + dueTime;
            _period = period;
        }

        public void Trigger(bool synchronous = false)
        {
            if (!_triggered)
            {
                _triggered = true;
                if (synchronous)
                {
                    _callback(_state);
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(_ => _callback(_state));
                }
            }
        }

        public void OnTimeAdvanced(DateTimeOffset newUtc)
        {
            if (!_triggered && newUtc >= _dueTimeUtc)
            {
                Trigger(synchronous: true);
            }
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            _period = period;
            _dueTimeUtc = dueTime == Timeout.InfiniteTimeSpan ? DateTimeOffset.MaxValue : _parent.GetUtcNow() + dueTime;
            _triggered = false;
            if (_parent.AutoTriggerDelays && dueTime > TimeSpan.Zero && dueTime <= TimeSpan.FromSeconds(15))
            {
                Trigger();
            }
            return true;
        }

        public void Dispose()
        {
            _parent.RemoveTimer(this);
        }

        public ValueTask DisposeAsync()
        {
            _parent.RemoveTimer(this);
            return ValueTask.CompletedTask;
        }
    }
}
