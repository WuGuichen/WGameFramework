using System;
using System.Collections.Generic;
using MxFramework.Runtime;

namespace MxFramework.Gameplay
{
    public sealed class GameplayRuntimeModule : RuntimeModule
    {
        public const string DefaultModuleId = "mxframework.gameplay.runtime";

        private readonly List<RuntimeCommand> _drainedCommands = new List<RuntimeCommand>();
        private readonly List<GameplayAbilityRuntimeResult> _abilityResults = new List<GameplayAbilityRuntimeResult>();
        private readonly RuntimeEventQueue<GameplayRuntimeEvent> _events = new RuntimeEventQueue<GameplayRuntimeEvent>();

        public GameplayRuntimeModule(
            GameplayWorld world,
            GameplayAbilityRegistry abilityRegistry,
            RuntimeCommandBuffer commandBuffer,
            bool tickWorldAutomatically = true,
            string moduleId = DefaultModuleId,
            RuntimeTickStage tickStage = RuntimeTickStage.Simulation,
            int priority = 100)
            : base(moduleId, tickStage, priority)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            AbilityRegistry = abilityRegistry ?? throw new ArgumentNullException(nameof(abilityRegistry));
            CommandBuffer = commandBuffer ?? throw new ArgumentNullException(nameof(commandBuffer));
            TickWorldAutomatically = tickWorldAutomatically;
        }

        public GameplayWorld World { get; }
        public GameplayAbilityRegistry AbilityRegistry { get; }
        public RuntimeCommandBuffer CommandBuffer { get; }
        public bool TickWorldAutomatically { get; }
        public RuntimeEventQueue<GameplayRuntimeEvent> Events => _events;
        public IReadOnlyList<GameplayAbilityRuntimeResult> AbilityResults => _abilityResults;

        public override void Tick(RuntimeTickContext context)
        {
            RuntimeFrame frame = new RuntimeFrame(context.FrameIndex);
            DrainCommands(frame);

            if (TickWorldAutomatically)
            {
                World.Tick(context.DeltaTime);
                EnqueueEvent(new GameplayRuntimeEvent(
                    frame,
                    GameplayRuntimeEventType.WorldTicked,
                    commandId: 0,
                    casterEntityId: 0,
                    abilityId: 0,
                    targetEntityId: 0,
                    failureCode: GameplayAbilityRuntimeFailureCode.None,
                    reason: string.Empty,
                    traceId: string.Empty));
            }
        }

        public int DrainEvents(RuntimeFrame frame, List<GameplayRuntimeEvent> output)
        {
            return _events.Drain(frame, output);
        }

        private void DrainCommands(RuntimeFrame frame)
        {
            _drainedCommands.Clear();
            IReadOnlyList<RuntimeCommand> commands = CommandBuffer.DrainForFrame(frame);
            for (int i = 0; i < commands.Count; i++)
            {
                _drainedCommands.Add(commands[i]);
            }

            for (int i = 0; i < _drainedCommands.Count; i++)
            {
                ExecuteCommand(frame, _drainedCommands[i]);
            }

            _drainedCommands.Clear();
        }

        private void ExecuteCommand(RuntimeFrame frame, RuntimeCommand command)
        {
            switch (command.CommandId)
            {
                case GameplayRuntimeCommandIds.CastAbility:
                    ExecuteCastAbility(frame, command);
                    return;
                case GameplayRuntimeCommandIds.DespawnEntity:
                    ExecuteDespawnEntity(frame, command);
                    return;
                default:
                    EnqueueRejected(frame, command, "UnsupportedGameplayCommand");
                    return;
            }
        }

        private void ExecuteCastAbility(RuntimeFrame frame, RuntimeCommand command)
        {
            int casterEntityId = command.Payload0 != 0 ? command.Payload0 : command.TargetId;
            int abilityId = command.Payload1;
            int candidateEntityId = command.Payload2;
            IReadOnlyList<int> candidates = candidateEntityId > 0
                ? new[] { candidateEntityId }
                : null;

            var service = new GameplayAbilityRuntimeService(World.Entities.CreateSnapshot(), AbilityRegistry);
            GameplayAbilityRuntimeResult result = service.Cast(new GameplayAbilityCastRequest(
                casterEntityId,
                abilityId,
                candidates,
                command.TraceId));

            _abilityResults.Add(result);

            int firstTargetId = result.TargetEntityIds.Count == 0 ? 0 : result.TargetEntityIds[0];
            EnqueueEvent(new GameplayRuntimeEvent(
                frame,
                result.Success ? GameplayRuntimeEventType.AbilityCastSucceeded : GameplayRuntimeEventType.AbilityCastFailed,
                command.CommandId,
                casterEntityId,
                abilityId,
                firstTargetId,
                result.FailureCode,
                result.FailureReason,
                command.TraceId));
        }

        private void ExecuteDespawnEntity(RuntimeFrame frame, RuntimeCommand command)
        {
            int entityId = command.Payload0 != 0 ? command.Payload0 : command.TargetId;
            bool removed = World.Remove(entityId);
            EnqueueEvent(new GameplayRuntimeEvent(
                frame,
                removed ? GameplayRuntimeEventType.EntityDespawned : GameplayRuntimeEventType.CommandRejected,
                command.CommandId,
                casterEntityId: 0,
                abilityId: 0,
                targetEntityId: entityId,
                failureCode: GameplayAbilityRuntimeFailureCode.None,
                reason: removed ? string.Empty : "MissingEntity",
                traceId: command.TraceId));
        }

        private void EnqueueRejected(RuntimeFrame frame, RuntimeCommand command, string reason)
        {
            EnqueueEvent(new GameplayRuntimeEvent(
                frame,
                GameplayRuntimeEventType.CommandRejected,
                command.CommandId,
                command.TargetId,
                command.Payload1,
                command.Payload2,
                GameplayAbilityRuntimeFailureCode.None,
                reason,
                command.TraceId));
        }

        private void EnqueueEvent(in GameplayRuntimeEvent evt)
        {
            _events.Enqueue(evt.Frame, evt);
        }
    }
}
