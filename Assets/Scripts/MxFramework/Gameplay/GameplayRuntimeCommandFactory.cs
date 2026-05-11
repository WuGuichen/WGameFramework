using MxFramework.Runtime;

namespace MxFramework.Gameplay
{
    public static class GameplayRuntimeCommandFactory
    {
        public static RuntimeCommand CastAbility(
            RuntimeFrame frame,
            int casterEntityId,
            int abilityId,
            int candidateEntityId = 0,
            int sourceId = 0,
            string traceId = "")
        {
            return new RuntimeCommand(
                frame,
                sourceId,
                GameplayRuntimeCommandIds.CastAbility,
                targetId: casterEntityId,
                payload0: casterEntityId,
                payload1: abilityId,
                payload2: candidateEntityId,
                traceId: traceId);
        }

        public static RuntimeCommand DespawnEntity(
            RuntimeFrame frame,
            int entityId,
            int sourceId = 0,
            string traceId = "")
        {
            return new RuntimeCommand(
                frame,
                sourceId,
                GameplayRuntimeCommandIds.DespawnEntity,
                targetId: entityId,
                payload0: entityId,
                traceId: traceId);
        }
    }
}
