namespace MxFramework.Gameplay
{
    public static class GameplayCoreComponentSchemaDescriptors
    {
        public const string IdentityStableId = "mxframework.gameplay.identity";
        public const string TeamStableId = "mxframework.gameplay.team";
        public const string LifecycleStableId = "mxframework.gameplay.lifecycle";
        public const string TagsStableId = "mxframework.gameplay.tags";
        public const string StatusesStableId = "mxframework.gameplay.statuses";

        public static void RegisterDiagnostics(GameplayComponentSchemaRegistry registry)
        {
            if (registry == null)
                throw new System.ArgumentNullException(nameof(registry));

            registry.Register(new IdentityDiagnostics());
            registry.Register(new TeamDiagnostics());
            registry.Register(new LifecycleDiagnostics());
            registry.Register(new TagsDiagnostics());
            registry.Register(new StatusesDiagnostics());
        }

        public static void RegisterRuntimeHash(GameplayComponentSchemaRegistry registry)
        {
            if (registry == null)
                throw new System.ArgumentNullException(nameof(registry));

            registry.Register(new IdentityHash());
            registry.Register(new TeamHash());
            registry.Register(new LifecycleHash());
            registry.Register(new TagsHash());
            registry.Register(new StatusesHash());
        }

        private sealed class IdentityDiagnostics : IGameplayComponentDiagnosticWriter<GameplayIdentityComponent>
        {
            public GameplayComponentSchema Schema => new GameplayComponentSchema(
                IdentityStableId,
                1,
                typeof(GameplayIdentityComponent),
                "Gameplay Identity",
                supportsDiagnostics: true,
                supportsHash: true);

            public void WriteDiagnostics(
                GameplayEntityId entityId,
                in GameplayIdentityComponent component,
                GameplayComponentDiagnosticWriter writer)
            {
                WriteEntity(writer, entityId);
                writer.AddInt("definitionId", component.DefinitionId);
                writer.AddInt("variantId", component.VariantId);
                writer.AddBool("isNone", component.IsNone);
            }
        }

        private sealed class TeamDiagnostics : IGameplayComponentDiagnosticWriter<GameplayTeamComponent>
        {
            public GameplayComponentSchema Schema => new GameplayComponentSchema(
                TeamStableId,
                1,
                typeof(GameplayTeamComponent),
                "Gameplay Team",
                supportsDiagnostics: true,
                supportsHash: true);

            public void WriteDiagnostics(
                GameplayEntityId entityId,
                in GameplayTeamComponent component,
                GameplayComponentDiagnosticWriter writer)
            {
                WriteEntity(writer, entityId);
                writer.AddInt("teamId", component.TeamId);
                writer.AddBool("isNeutral", component.IsNeutral);
            }
        }

        private sealed class LifecycleDiagnostics : IGameplayComponentDiagnosticWriter<GameplayLifecycleComponent>
        {
            public GameplayComponentSchema Schema => new GameplayComponentSchema(
                LifecycleStableId,
                1,
                typeof(GameplayLifecycleComponent),
                "Gameplay Lifecycle",
                supportsDiagnostics: true,
                supportsHash: true);

            public void WriteDiagnostics(
                GameplayEntityId entityId,
                in GameplayLifecycleComponent component,
                GameplayComponentDiagnosticWriter writer)
            {
                WriteEntity(writer, entityId);
                writer.AddInt("state", (int)component.State);
                writer.AddBool("isAlive", component.IsAlive);
                writer.AddBool("isTerminal", component.IsTerminal);
            }
        }

        private sealed class TagsDiagnostics : IGameplayComponentDiagnosticWriter<GameplayTagComponent>
        {
            public GameplayComponentSchema Schema => new GameplayComponentSchema(
                TagsStableId,
                1,
                typeof(GameplayTagComponent),
                "Gameplay Tags",
                supportsDiagnostics: true,
                supportsHash: true);

            public void WriteDiagnostics(
                GameplayEntityId entityId,
                in GameplayTagComponent component,
                GameplayComponentDiagnosticWriter writer)
            {
                WriteEntity(writer, entityId);
                GameplayTagId[] ids = component.ToArray();
                writer.AddInt("count", ids.Length);
                for (int i = 0; i < ids.Length; i++)
                    writer.AddInt("id." + i, ids[i].Value);
            }
        }

        private sealed class StatusesDiagnostics : IGameplayComponentDiagnosticWriter<GameplayStatusComponent>
        {
            public GameplayComponentSchema Schema => new GameplayComponentSchema(
                StatusesStableId,
                1,
                typeof(GameplayStatusComponent),
                "Gameplay Statuses",
                supportsDiagnostics: true,
                supportsHash: true);

            public void WriteDiagnostics(
                GameplayEntityId entityId,
                in GameplayStatusComponent component,
                GameplayComponentDiagnosticWriter writer)
            {
                WriteEntity(writer, entityId);
                GameplayStatusId[] ids = component.ToArray();
                writer.AddInt("count", ids.Length);
                for (int i = 0; i < ids.Length; i++)
                    writer.AddInt("id." + i, ids[i].Value);
            }
        }

        private static void WriteEntity(GameplayComponentDiagnosticWriter writer, GameplayEntityId entityId)
        {
            writer.AddInt("entity.index", entityId.Index);
            writer.AddInt("entity.generation", entityId.Generation);
        }

        private sealed class IdentityHash : IGameplayComponentHashWriter<GameplayIdentityComponent>
        {
            public GameplayComponentSchema Schema => new GameplayComponentSchema(
                IdentityStableId,
                1,
                typeof(GameplayIdentityComponent),
                "Gameplay Identity",
                supportsDiagnostics: true,
                supportsHash: true);

            public void WriteHash(
                GameplayEntityId entityId,
                in GameplayIdentityComponent component,
                MxFramework.Runtime.RuntimeHashAccumulator accumulator)
            {
                accumulator.AddInt("definitionId", component.DefinitionId);
                accumulator.AddInt("variantId", component.VariantId);
            }
        }

        private sealed class TeamHash : IGameplayComponentHashWriter<GameplayTeamComponent>
        {
            public GameplayComponentSchema Schema => new GameplayComponentSchema(
                TeamStableId,
                1,
                typeof(GameplayTeamComponent),
                "Gameplay Team",
                supportsDiagnostics: true,
                supportsHash: true);

            public void WriteHash(
                GameplayEntityId entityId,
                in GameplayTeamComponent component,
                MxFramework.Runtime.RuntimeHashAccumulator accumulator)
            {
                accumulator.AddInt("teamId", component.TeamId);
            }
        }

        private sealed class LifecycleHash : IGameplayComponentHashWriter<GameplayLifecycleComponent>
        {
            public GameplayComponentSchema Schema => new GameplayComponentSchema(
                LifecycleStableId,
                1,
                typeof(GameplayLifecycleComponent),
                "Gameplay Lifecycle",
                supportsDiagnostics: true,
                supportsHash: true);

            public void WriteHash(
                GameplayEntityId entityId,
                in GameplayLifecycleComponent component,
                MxFramework.Runtime.RuntimeHashAccumulator accumulator)
            {
                accumulator.AddInt("state", (int)component.State);
            }
        }

        private sealed class TagsHash : IGameplayComponentHashWriter<GameplayTagComponent>
        {
            public GameplayComponentSchema Schema => new GameplayComponentSchema(
                TagsStableId,
                1,
                typeof(GameplayTagComponent),
                "Gameplay Tags",
                supportsDiagnostics: true,
                supportsHash: true);

            public void WriteHash(
                GameplayEntityId entityId,
                in GameplayTagComponent component,
                MxFramework.Runtime.RuntimeHashAccumulator accumulator)
            {
                GameplayTagId[] ids = component.ToArray();
                accumulator.AddInt("count", ids.Length);
                for (int i = 0; i < ids.Length; i++)
                    accumulator.AddInt("id", ids[i].Value);
            }
        }

        private sealed class StatusesHash : IGameplayComponentHashWriter<GameplayStatusComponent>
        {
            public GameplayComponentSchema Schema => new GameplayComponentSchema(
                StatusesStableId,
                1,
                typeof(GameplayStatusComponent),
                "Gameplay Statuses",
                supportsDiagnostics: true,
                supportsHash: true);

            public void WriteHash(
                GameplayEntityId entityId,
                in GameplayStatusComponent component,
                MxFramework.Runtime.RuntimeHashAccumulator accumulator)
            {
                GameplayStatusId[] ids = component.ToArray();
                accumulator.AddInt("count", ids.Length);
                for (int i = 0; i < ids.Length; i++)
                    accumulator.AddInt("id", ids[i].Value);
            }
        }
    }
}
