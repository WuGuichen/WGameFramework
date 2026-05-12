using System;
using System.Collections.Generic;

namespace MxFramework.Gameplay
{
    public sealed class GameplayComponentSchemaRegistry
    {
        private readonly Dictionary<string, SchemaEntry> _entriesByStableId;
        private readonly Dictionary<Type, SchemaEntry> _entriesByType;
        private readonly List<SchemaEntry> _entries;

        public GameplayComponentSchemaRegistry()
        {
            _entriesByStableId = new Dictionary<string, SchemaEntry>(StringComparer.Ordinal);
            _entriesByType = new Dictionary<Type, SchemaEntry>();
            _entries = new List<SchemaEntry>();
        }

        public int Count => _entries.Count;

        public void Register(IGameplayComponentSchemaDescriptor descriptor)
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));

            GameplayComponentSchema schema = descriptor.Schema;
            SchemaEntry entry = GetOrCreateEntry(schema);
            entry.Attach(descriptor);
        }

        public bool TryGetByStableId(string stableId, out GameplayComponentSchema schema)
        {
            if (!string.IsNullOrEmpty(stableId) && _entriesByStableId.TryGetValue(stableId, out SchemaEntry entry))
            {
                schema = entry.Schema;
                return true;
            }

            schema = default;
            return false;
        }

        public bool TryGetByType(Type componentType, out GameplayComponentSchema schema)
        {
            if (componentType != null && _entriesByType.TryGetValue(componentType, out SchemaEntry entry))
            {
                schema = entry.Schema;
                return true;
            }

            schema = default;
            return false;
        }

        public bool TryGetDiagnosticWriter<T>(out IGameplayComponentDiagnosticWriter<T> writer)
            where T : struct, IGameplayComponent
        {
            if (_entriesByType.TryGetValue(typeof(T), out SchemaEntry entry))
                return entry.TryGetDiagnosticWriter(out writer);

            writer = null;
            return false;
        }

        public bool TryGetHashWriter<T>(out IGameplayComponentHashWriter<T> writer)
            where T : struct, IGameplayComponent
        {
            if (_entriesByType.TryGetValue(typeof(T), out SchemaEntry entry))
                return entry.TryGetHashWriter(out writer);

            writer = null;
            return false;
        }

        public bool TryGetSaveStateAdapter<T>(out IGameplayComponentSaveStateAdapter<T> adapter)
            where T : struct, IGameplayComponent
        {
            if (_entriesByType.TryGetValue(typeof(T), out SchemaEntry entry))
                return entry.TryGetSaveStateAdapter(out adapter);

            adapter = null;
            return false;
        }

        public GameplayComponentSchema[] CreateSnapshot()
        {
            if (_entries.Count == 0)
                return Array.Empty<GameplayComponentSchema>();

            var snapshot = new GameplayComponentSchema[_entries.Count];
            for (int i = 0; i < _entries.Count; i++)
                snapshot[i] = _entries[i].Schema;

            Array.Sort(snapshot, CompareSchemas);
            return snapshot;
        }

        public void Clear()
        {
            _entriesByStableId.Clear();
            _entriesByType.Clear();
            _entries.Clear();
        }

        private SchemaEntry GetOrCreateEntry(GameplayComponentSchema schema)
        {
            bool hasStableId = _entriesByStableId.TryGetValue(schema.StableId, out SchemaEntry stableEntry);
            bool hasType = _entriesByType.TryGetValue(schema.ComponentType, out SchemaEntry typeEntry);

            if (hasStableId && hasType)
            {
                if (!ReferenceEquals(stableEntry, typeEntry))
                    throw new InvalidOperationException("Gameplay component schema stable id and component type belong to different entries.");
                if (!stableEntry.Schema.Equals(schema))
                    throw new InvalidOperationException("Gameplay component schema entry cannot be registered with conflicting metadata.");

                return stableEntry;
            }

            if (hasStableId)
                throw new InvalidOperationException("Gameplay component schema stable id is already registered: " + schema.StableId);
            if (hasType)
                throw new InvalidOperationException("Gameplay component schema component type is already registered: " + schema.ComponentType.FullName);

            var entry = new SchemaEntry(schema);
            _entriesByStableId.Add(schema.StableId, entry);
            _entriesByType.Add(schema.ComponentType, entry);
            _entries.Add(entry);
            return entry;
        }

        private static int CompareSchemas(GameplayComponentSchema left, GameplayComponentSchema right)
        {
            return string.CompareOrdinal(left.StableId, right.StableId);
        }

        private sealed class SchemaEntry
        {
            private object _diagnosticWriter;
            private object _hashWriter;
            private object _saveStateAdapter;
            private bool _schemaOnlyRegistered;

            public SchemaEntry(GameplayComponentSchema schema)
            {
                Schema = schema;
            }

            public GameplayComponentSchema Schema { get; }

            public void Attach(IGameplayComponentSchemaDescriptor descriptor)
            {
                bool attached = false;
                Type componentType = Schema.ComponentType;

                Type diagnosticType = typeof(IGameplayComponentDiagnosticWriter<>).MakeGenericType(componentType);
                if (diagnosticType.IsInstanceOfType(descriptor))
                {
                    AttachCapability(ref _diagnosticWriter, descriptor, "diagnostic writer");
                    attached = true;
                }

                Type hashType = typeof(IGameplayComponentHashWriter<>).MakeGenericType(componentType);
                if (hashType.IsInstanceOfType(descriptor))
                {
                    AttachCapability(ref _hashWriter, descriptor, "hash writer");
                    attached = true;
                }

                Type saveType = typeof(IGameplayComponentSaveStateAdapter<>).MakeGenericType(componentType);
                if (saveType.IsInstanceOfType(descriptor))
                {
                    AttachCapability(ref _saveStateAdapter, descriptor, "save state adapter");
                    attached = true;
                }

                if (!attached)
                {
                    if (_schemaOnlyRegistered)
                        throw new InvalidOperationException("Gameplay component schema already has a schema-only descriptor.");

                    _schemaOnlyRegistered = true;
                    return;
                }
            }

            public bool TryGetDiagnosticWriter<T>(out IGameplayComponentDiagnosticWriter<T> writer)
                where T : struct, IGameplayComponent
            {
                writer = _diagnosticWriter as IGameplayComponentDiagnosticWriter<T>;
                return writer != null;
            }

            public bool TryGetHashWriter<T>(out IGameplayComponentHashWriter<T> writer)
                where T : struct, IGameplayComponent
            {
                writer = _hashWriter as IGameplayComponentHashWriter<T>;
                return writer != null;
            }

            public bool TryGetSaveStateAdapter<T>(out IGameplayComponentSaveStateAdapter<T> adapter)
                where T : struct, IGameplayComponent
            {
                adapter = _saveStateAdapter as IGameplayComponentSaveStateAdapter<T>;
                return adapter != null;
            }

            private static void AttachCapability(ref object target, object capability, string capabilityName)
            {
                if (target != null && !ReferenceEquals(target, capability))
                    throw new InvalidOperationException("Gameplay component schema already has a " + capabilityName + ".");

                target = capability;
            }
        }
    }
}
