using System;
using System.Collections.Generic;

namespace MxFramework.Gameplay
{
    /// <summary>Creates and destroys generation-based gameplay entity ids.</summary>
    public sealed class GameplayEntityLifecycle
    {
        private readonly List<Slot> _slots;
        private readonly Queue<int> _freeIndices;

        public GameplayEntityLifecycle()
        {
            _slots = new List<Slot>();
            _freeIndices = new Queue<int>();
        }

        public int CountAlive { get; private set; }
        public int CountAllocated => _slots.Count;

        public GameplayEntityId Create()
        {
            int index;
            Slot slot;
            if (_freeIndices.Count > 0)
            {
                index = _freeIndices.Dequeue();
                slot = _slots[index - 1];
            }
            else
            {
                index = _slots.Count + 1;
                slot = new Slot(1, false);
                _slots.Add(slot);
            }

            slot.IsAlive = true;
            _slots[index - 1] = slot;
            CountAlive++;

            return new GameplayEntityId(index, slot.Generation);
        }

        public bool Destroy(GameplayEntityId entityId)
        {
            if (!TryGetAliveSlotIndex(entityId, out int slotIndex))
                return false;

            Slot slot = _slots[slotIndex];
            if (slot.Generation == int.MaxValue)
                throw new InvalidOperationException("Gameplay entity generation overflow.");

            slot.IsAlive = false;
            slot.Generation++;
            _slots[slotIndex] = slot;
            _freeIndices.Enqueue(slotIndex + 1);
            CountAlive--;

            return true;
        }

        public bool IsAlive(GameplayEntityId entityId)
        {
            return TryGetAliveSlotIndex(entityId, out _);
        }

        public GameplayEntityId[] CreateSnapshot()
        {
            if (CountAlive == 0)
                return Array.Empty<GameplayEntityId>();

            var snapshot = new GameplayEntityId[CountAlive];
            int snapshotIndex = 0;
            for (int slotIndex = 0; slotIndex < _slots.Count; slotIndex++)
            {
                Slot slot = _slots[slotIndex];
                if (!slot.IsAlive)
                    continue;

                snapshot[snapshotIndex++] = new GameplayEntityId(slotIndex + 1, slot.Generation);
            }

            return snapshot;
        }

        public void Clear()
        {
            _freeIndices.Clear();
            CountAlive = 0;

            for (int i = 0; i < _slots.Count; i++)
            {
                Slot slot = _slots[i];
                if (slot.Generation == int.MaxValue)
                    throw new InvalidOperationException("Gameplay entity generation overflow.");

                slot.IsAlive = false;
                slot.Generation++;
                _slots[i] = slot;
                _freeIndices.Enqueue(i + 1);
            }
        }

        private bool TryGetAliveSlotIndex(GameplayEntityId entityId, out int slotIndex)
        {
            slotIndex = entityId.Index - 1;
            if (!entityId.IsValid || slotIndex < 0 || slotIndex >= _slots.Count)
                return false;

            Slot slot = _slots[slotIndex];
            return slot.IsAlive && slot.Generation == entityId.Generation;
        }

        private struct Slot
        {
            public Slot(int generation, bool isAlive)
            {
                Generation = generation;
                IsAlive = isAlive;
            }

            public int Generation;
            public bool IsAlive;
        }
    }
}
