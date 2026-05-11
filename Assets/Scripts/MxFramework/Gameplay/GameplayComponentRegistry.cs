using System;
using System.Collections.Generic;

namespace MxFramework.Gameplay
{
    /// <summary>Owns ECS-style entity ids and registered component stores for lifecycle cleanup.</summary>
    public sealed class GameplayComponentRegistry
    {
        private readonly GameplayEntityLifecycle _lifecycle;
        private readonly List<IGameplayComponentStore> _stores;
        private readonly Dictionary<Type, IGameplayComponentStore> _storesByType;

        public GameplayComponentRegistry()
            : this(new GameplayEntityLifecycle())
        {
        }

        public GameplayComponentRegistry(GameplayEntityLifecycle lifecycle)
        {
            _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            _stores = new List<IGameplayComponentStore>();
            _storesByType = new Dictionary<Type, IGameplayComponentStore>();
        }

        public GameplayEntityLifecycle Lifecycle => _lifecycle;
        public int CountAlive => _lifecycle.CountAlive;
        public int StoreCount => _stores.Count;

        public GameplayEntityId CreateEntity()
        {
            return _lifecycle.Create();
        }

        public bool DestroyEntity(GameplayEntityId entityId)
        {
            if (!_lifecycle.Destroy(entityId))
                return false;

            RemoveComponents(entityId);
            return true;
        }

        public bool IsAlive(GameplayEntityId entityId)
        {
            return _lifecycle.IsAlive(entityId);
        }

        public GameplayEntityId[] CreateEntitySnapshot()
        {
            return _lifecycle.CreateSnapshot();
        }

        public GameplayComponentStore<T> CreateStore<T>() where T : struct, IGameplayComponent
        {
            var store = new GameplayComponentStore<T>();
            RegisterStore(store);
            return store;
        }

        public GameplayComponentStore<T> GetOrCreateStore<T>() where T : struct, IGameplayComponent
        {
            if (TryGetStore(out GameplayComponentStore<T> store))
                return store;

            return CreateStore<T>();
        }

        public void RegisterStore<T>(GameplayComponentStore<T> store) where T : struct, IGameplayComponent
        {
            RegisterStoreCore(store);
        }

        public bool TryGetStore<T>(out GameplayComponentStore<T> store) where T : struct, IGameplayComponent
        {
            if (_storesByType.TryGetValue(typeof(T), out IGameplayComponentStore rawStore))
            {
                store = (GameplayComponentStore<T>)rawStore;
                return true;
            }

            store = null;
            return false;
        }

        public void Clear()
        {
            _lifecycle.Clear();
            for (int i = 0; i < _stores.Count; i++)
                _stores[i].Clear();
        }

        private void RemoveComponents(GameplayEntityId entityId)
        {
            for (int i = 0; i < _stores.Count; i++)
                _stores[i].Remove(entityId);
        }

        private void RegisterStoreCore(IGameplayComponentStore store)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            Type componentType = store.ComponentType;
            if (componentType == null)
                throw new ArgumentException("Gameplay component store must expose a component type.", nameof(store));
            if (_storesByType.ContainsKey(componentType))
                throw new InvalidOperationException($"Gameplay component store for '{componentType.FullName}' is already registered.");

            _storesByType.Add(componentType, store);
            _stores.Add(store);
        }
    }
}
