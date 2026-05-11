using MxFramework.Runtime;

namespace MxFramework.Gameplay
{
    public enum GameplayRuntimeEventType
    {
        None = 0,
        AbilityCastSucceeded = 1,
        AbilityCastFailed = 2,
        EntityDespawned = 3,
        CommandRejected = 4,
        WorldTicked = 5,
        ComponentEntityCreated = 6,
        ComponentEntityDestroyed = 7
    }

    public readonly struct GameplayRuntimeEvent
    {
        public GameplayRuntimeEvent(
            RuntimeFrame frame,
            GameplayRuntimeEventType type,
            int commandId,
            int casterEntityId,
            int abilityId,
            int targetEntityId,
            GameplayAbilityRuntimeFailureCode failureCode,
            string reason,
            string traceId,
            int componentEntityIndex = 0,
            int componentEntityGeneration = 0)
        {
            Frame = frame;
            Type = type;
            CommandId = commandId;
            CasterEntityId = casterEntityId;
            AbilityId = abilityId;
            TargetEntityId = targetEntityId;
            FailureCode = failureCode;
            Reason = reason ?? string.Empty;
            TraceId = traceId ?? string.Empty;
            ComponentEntityIndex = componentEntityIndex;
            ComponentEntityGeneration = componentEntityGeneration;
        }

        public RuntimeFrame Frame { get; }
        public GameplayRuntimeEventType Type { get; }
        public int CommandId { get; }
        public int CasterEntityId { get; }
        public int AbilityId { get; }
        public int TargetEntityId { get; }
        public GameplayAbilityRuntimeFailureCode FailureCode { get; }
        public string Reason { get; }
        public string TraceId { get; }
        public int ComponentEntityIndex { get; }
        public int ComponentEntityGeneration { get; }
        public GameplayEntityId ComponentEntityId => new GameplayEntityId(ComponentEntityIndex, ComponentEntityGeneration);
    }
}
