using System;
using System.Collections.Generic;
using MxFramework.Runtime;

namespace MxFramework.Gameplay
{
    public sealed class GameplayComponentAbilityCommandSystem : IGameplaySystem
    {
        public const string DefaultSystemId = "mxframework.gameplay.command.component_ability";

        private readonly GameplayComponentAbilityRegistry _abilityRegistry;

        public GameplayComponentAbilityCommandSystem(
            GameplayComponentAbilityRegistry abilityRegistry,
            string systemId = DefaultSystemId,
            int priority = 50)
        {
            _abilityRegistry = abilityRegistry ?? throw new ArgumentNullException(nameof(abilityRegistry));
            SystemId = systemId ?? string.Empty;
            Priority = priority;
        }

        public string SystemId { get; }
        public GameplaySystemPhase Phase => GameplaySystemPhase.Command;
        public int Priority { get; }
        public bool IsEnabled { get; private set; } = true;

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
        }

        public void Tick(GameplaySystemContext context)
        {
            IReadOnlyList<RuntimeCommand> commands = context.Commands;
            for (int i = 0; i < commands.Count; i++)
            {
                RuntimeCommand command = commands[i];
                if (command.CommandId != GameplayRuntimeCommandIds.CastComponentAbility)
                    continue;

                ExecuteCast(context, command);
                context.CommandState.MarkHandled(command);
            }
        }

        private void ExecuteCast(GameplaySystemContext context, RuntimeCommand command)
        {
            GameplayComponentWorld componentWorld = context.ComponentWorld;
            if (componentWorld == null)
            {
                EnqueueFailure(
                    context,
                    command,
                    default,
                    command.Payload1,
                    GameplayComponentAbilityEvents.MissingComponentWorldReason,
                    GameplayAbilityRuntimeFailureCode.AbilityCastFailed);
                return;
            }

            if (!TryReadCaster(command, out GameplayEntityId casterEntityId))
            {
                EnqueueFailure(
                    context,
                    command,
                    default,
                    command.Payload1,
                    GameplayComponentAbilityEvents.InvalidCasterReason,
                    GameplayAbilityRuntimeFailureCode.MissingCaster);
                return;
            }

            if (!componentWorld.IsAlive(casterEntityId))
            {
                EnqueueFailure(
                    context,
                    command,
                    casterEntityId,
                    command.Payload1,
                    GameplayComponentAbilityEvents.MissingCasterReason,
                    GameplayAbilityRuntimeFailureCode.MissingCaster);
                return;
            }

            int abilityId = command.Payload1;
            if (command.Payload2 != 0)
            {
                EnqueueFailure(
                    context,
                    command,
                    casterEntityId,
                    abilityId,
                    GameplayComponentAbilityEvents.InvalidCommandPayloadReason,
                    GameplayAbilityRuntimeFailureCode.AbilityCastFailed);
                return;
            }

            if (!_abilityRegistry.TryGet(abilityId, out IGameplayComponentAbility ability))
            {
                EnqueueFailure(
                    context,
                    command,
                    casterEntityId,
                    abilityId,
                    GameplayComponentAbilityEvents.MissingAbilityReason,
                    GameplayAbilityRuntimeFailureCode.MissingAbility);
                return;
            }

            GameplayComponentAbilityResult result = ability.Cast(new GameplayComponentAbilityContext(
                context.Frame,
                componentWorld,
                casterEntityId,
                new[] { casterEntityId },
                command.TraceId));
            if (result == null)
            {
                EnqueueFailure(
                    context,
                    command,
                    casterEntityId,
                    abilityId,
                    GameplayComponentAbilityEvents.EffectFailedReason,
                    GameplayAbilityRuntimeFailureCode.AbilityCastFailed);
                return;
            }

            GameplayEntityId eventEntityId = ResolveEventEntity(result, casterEntityId);
            context.Events.Enqueue(context.Frame, new GameplayRuntimeEvent(
                context.Frame,
                result.Success ? GameplayRuntimeEventType.AbilityCastSucceeded : GameplayRuntimeEventType.AbilityCastFailed,
                command.CommandId,
                casterEntityId: 0,
                abilityId: abilityId,
                targetEntityId: eventEntityId.Index,
                failureCode: result.Success ? GameplayAbilityRuntimeFailureCode.None : MapFailureCode(result.FailureCode),
                reason: result.Success ? GameplayComponentAbilityEvents.CastComponentAbilityReason : result.FailureReason,
                traceId: command.TraceId,
                componentEntityIndex: eventEntityId.Index,
                componentEntityGeneration: eventEntityId.Generation));
        }

        private static GameplayEntityId ResolveEventEntity(
            GameplayComponentAbilityResult result,
            GameplayEntityId casterEntityId)
        {
            if (result.TargetEntityIds.Count > 0 && result.TargetEntityIds[0].IsValid)
                return result.TargetEntityIds[0];

            return casterEntityId;
        }

        private static bool TryReadCaster(RuntimeCommand command, out GameplayEntityId casterEntityId)
        {
            int index = command.TargetId;
            int generation = command.Payload0;
            if (index <= 0 || generation <= 0)
            {
                casterEntityId = default;
                return false;
            }

            casterEntityId = new GameplayEntityId(index, generation);
            return true;
        }

        private static GameplayAbilityRuntimeFailureCode MapFailureCode(GameplayComponentAbilityFailureCode failureCode)
        {
            switch (failureCode)
            {
                case GameplayComponentAbilityFailureCode.MissingCaster:
                    return GameplayAbilityRuntimeFailureCode.MissingCaster;
                case GameplayComponentAbilityFailureCode.MissingAbility:
                    return GameplayAbilityRuntimeFailureCode.MissingAbility;
                default:
                    return GameplayAbilityRuntimeFailureCode.AbilityCastFailed;
            }
        }

        private static void EnqueueFailure(
            GameplaySystemContext context,
            RuntimeCommand command,
            GameplayEntityId entityId,
            int abilityId,
            string reason,
            GameplayAbilityRuntimeFailureCode failureCode)
        {
            context.Events.Enqueue(context.Frame, new GameplayRuntimeEvent(
                context.Frame,
                GameplayRuntimeEventType.AbilityCastFailed,
                command.CommandId,
                casterEntityId: 0,
                abilityId: abilityId,
                targetEntityId: entityId.Index,
                failureCode: failureCode,
                reason: reason,
                traceId: command.TraceId,
                componentEntityIndex: entityId.Index,
                componentEntityGeneration: entityId.Generation));
        }
    }
}
