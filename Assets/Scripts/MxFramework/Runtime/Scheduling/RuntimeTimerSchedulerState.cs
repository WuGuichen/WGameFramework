using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MxFramework.Runtime
{
    public sealed class RuntimeTimerSchedulerState
    {
        private readonly ReadOnlyCollection<RuntimeTimerState> _timers;

        public RuntimeTimerSchedulerState(
            int schemaVersion,
            long currentFrame,
            long nextTimerId,
            long nextSequence,
            IReadOnlyList<RuntimeTimerState> timers)
        {
            SchemaVersion = schemaVersion;
            CurrentFrame = currentFrame;
            NextTimerId = nextTimerId;
            NextSequence = nextSequence;
            _timers = new ReadOnlyCollection<RuntimeTimerState>(
                timers != null ? new List<RuntimeTimerState>(timers) : new List<RuntimeTimerState>());
        }

        public int SchemaVersion { get; }
        public long CurrentFrame { get; }
        public long NextTimerId { get; }
        public long NextSequence { get; }
        public IReadOnlyList<RuntimeTimerState> Timers => _timers;
    }

    public sealed class RuntimeTimerState
    {
        public RuntimeTimerState(
            long timerId,
            long sequence,
            RuntimeTimerKind kind,
            RuntimeTimerHandle handle,
            long targetFrame,
            long remainingFrames,
            double remainingSeconds,
            long intervalFrames,
            double intervalSeconds,
            bool isRepeating,
            string traceId,
            string commandSummary)
        {
            TimerId = timerId;
            Sequence = sequence;
            Kind = kind;
            Handle = handle;
            TargetFrame = targetFrame;
            RemainingFrames = remainingFrames;
            RemainingSeconds = remainingSeconds;
            IntervalFrames = intervalFrames;
            IntervalSeconds = intervalSeconds;
            IsRepeating = isRepeating;
            TraceId = traceId ?? string.Empty;
            CommandSummary = commandSummary ?? string.Empty;
        }

        public long TimerId { get; }
        public long Sequence { get; }
        public RuntimeTimerKind Kind { get; }
        public RuntimeTimerHandle Handle { get; }
        public long TargetFrame { get; }
        public long RemainingFrames { get; }
        public double RemainingSeconds { get; }
        public long IntervalFrames { get; }
        public double IntervalSeconds { get; }
        public bool IsRepeating { get; }
        public string TraceId { get; }
        public string CommandSummary { get; }
    }
}
