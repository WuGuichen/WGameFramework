using System.Collections.Generic;
using MxFramework.Runtime;

namespace MxFramework.Gameplay
{
    public sealed class GameplayUnsupportedCommandSystem : IGameplaySystem
    {
        public const string DefaultSystemId = "mxframework.gameplay.command.unsupported";
        public const string UnsupportedReason = "UnsupportedGameplayCommand";

        public GameplayUnsupportedCommandSystem(
            string systemId = DefaultSystemId,
            int priority = int.MaxValue)
        {
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
                if (IsSupported(command.CommandId))
                    continue;

                context.Events.Enqueue(context.Frame, new GameplayRuntimeEvent(
                    context.Frame,
                    GameplayRuntimeEventType.CommandRejected,
                    command.CommandId,
                    command.TargetId,
                    command.Payload1,
                    command.Payload2,
                    GameplayAbilityRuntimeFailureCode.None,
                    UnsupportedReason,
                    command.TraceId));
            }
        }

        private static bool IsSupported(int commandId)
        {
            return commandId == GameplayRuntimeCommandIds.CastAbility
                || commandId == GameplayRuntimeCommandIds.DespawnEntity;
        }
    }
}
