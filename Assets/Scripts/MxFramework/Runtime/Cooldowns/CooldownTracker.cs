using System;
using System.Collections.Generic;

namespace MxFramework.Runtime
{
    public sealed class CooldownTracker
    {
        private readonly Dictionary<int, long> _endFrames = new Dictionary<int, long>();

        public bool IsReady(int id, RuntimeFrame frame)
        {
            return !_endFrames.TryGetValue(id, out long endFrame) || frame.Value >= endFrame;
        }

        public void Start(int id, RuntimeFrame frame, long durationFrames)
        {
            ValidateDuration(durationFrames);
            _endFrames[id] = AddFrames(frame.Value, durationFrames);
        }

        public long GetRemainingFrames(int id, RuntimeFrame frame)
        {
            if (!_endFrames.TryGetValue(id, out long endFrame))
            {
                return 0L;
            }

            long remaining = endFrame - frame.Value;
            return remaining > 0L ? remaining : 0L;
        }

        public bool TryConsume(int id, RuntimeFrame frame, long durationFrames)
        {
            ValidateDuration(durationFrames);

            if (!IsReady(id, frame))
            {
                return false;
            }

            _endFrames[id] = AddFrames(frame.Value, durationFrames);
            return true;
        }

        public void Clear()
        {
            _endFrames.Clear();
        }

        public CooldownTrackerSnapshot CreateSnapshot()
        {
            if (_endFrames.Count == 0)
            {
                return CooldownTrackerSnapshot.Empty;
            }

            var entries = new CooldownSnapshotEntry[_endFrames.Count];
            int index = 0;
            foreach (KeyValuePair<int, long> pair in _endFrames)
            {
                entries[index++] = new CooldownSnapshotEntry(pair.Key, new RuntimeFrame(pair.Value));
            }

            Array.Sort(entries, CompareEntries);
            return new CooldownTrackerSnapshot(entries);
        }

        private static int CompareEntries(CooldownSnapshotEntry left, CooldownSnapshotEntry right)
        {
            return left.Id.CompareTo(right.Id);
        }

        private static void ValidateDuration(long durationFrames)
        {
            if (durationFrames < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(durationFrames), "Cooldown duration cannot be negative.");
            }
        }

        private static long AddFrames(long frame, long durationFrames)
        {
            if (durationFrames > long.MaxValue - frame)
            {
                throw new ArgumentOutOfRangeException(nameof(durationFrames), "Cooldown end frame would exceed long.MaxValue.");
            }

            return frame + durationFrames;
        }
    }

    public readonly struct CooldownTrackerSnapshot
    {
        public static readonly CooldownTrackerSnapshot Empty = new CooldownTrackerSnapshot(Array.Empty<CooldownSnapshotEntry>());

        public CooldownTrackerSnapshot(IReadOnlyList<CooldownSnapshotEntry> entries)
        {
            Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }

        public IReadOnlyList<CooldownSnapshotEntry> Entries { get; }
    }

    public readonly struct CooldownSnapshotEntry
    {
        public CooldownSnapshotEntry(int id, RuntimeFrame endFrame)
        {
            Id = id;
            EndFrame = endFrame;
        }

        public int Id { get; }

        public RuntimeFrame EndFrame { get; }
    }
}
