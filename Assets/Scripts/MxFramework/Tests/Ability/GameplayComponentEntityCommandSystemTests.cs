using System.Collections.Generic;
using MxFramework.Gameplay;
using MxFramework.Runtime;
using NUnit.Framework;

namespace MxFramework.Tests.Ability
{
    public sealed class GameplayComponentEntityCommandSystemTests
    {
        [Test]
        public void CreateComponentEntityCommandCreatesEntityAndEmitsGenerationEvent()
        {
            var buffer = new RuntimeCommandBuffer();
            var module = new GameplayRuntimeModule(
                new GameplayWorld(),
                new GameplayAbilityRegistry(),
                buffer,
                tickWorldAutomatically: false);

            buffer.Enqueue(GameplayRuntimeCommandFactory.CreateComponentEntity(RuntimeFrame.Zero, traceId: "create-component"));

            module.Tick(new RuntimeTickContext(0, 0d, 0d, RuntimeTickStage.Simulation));

            GameplayEntityId[] entities = module.ComponentWorld.CreateEntitySnapshot();
            Assert.AreEqual(1, entities.Length);
            Assert.AreEqual(new GameplayEntityId(1, 1), entities[0]);

            var events = new List<GameplayRuntimeEvent>();
            Assert.AreEqual(1, module.DrainEvents(RuntimeFrame.Zero, events));
            Assert.AreEqual(GameplayRuntimeEventType.ComponentEntityCreated, events[0].Type);
            Assert.AreEqual(GameplayRuntimeCommandIds.CreateComponentEntity, events[0].CommandId);
            Assert.AreEqual(entities[0], events[0].ComponentEntityId);
            Assert.AreEqual(entities[0].Index, events[0].TargetEntityId);
            Assert.AreEqual("create-component", events[0].TraceId);
        }

        [Test]
        public void DestroyComponentEntityCommandDestroysEntityAndRemovesRegisteredComponents()
        {
            var buffer = new RuntimeCommandBuffer();
            var componentWorld = new GameplayComponentWorld();
            GameplayComponentStore<TestStatComponent> stats = componentWorld.CreateStore<TestStatComponent>();
            GameplayEntityId entity = componentWorld.CreateEntity();
            stats.Set(entity, new TestStatComponent(10));
            var module = new GameplayRuntimeModule(
                new GameplayWorld(),
                new GameplayAbilityRegistry(),
                buffer,
                tickWorldAutomatically: false,
                componentWorld: componentWorld);

            buffer.Enqueue(GameplayRuntimeCommandFactory.DestroyComponentEntity(RuntimeFrame.Zero, entity, traceId: "destroy-component"));

            module.Tick(new RuntimeTickContext(0, 0d, 0d, RuntimeTickStage.Simulation));

            Assert.IsFalse(componentWorld.IsAlive(entity));
            Assert.IsFalse(stats.Contains(entity));
            var events = new List<GameplayRuntimeEvent>();
            Assert.AreEqual(1, module.DrainEvents(RuntimeFrame.Zero, events));
            Assert.AreEqual(GameplayRuntimeEventType.ComponentEntityDestroyed, events[0].Type);
            Assert.AreEqual(entity, events[0].ComponentEntityId);
            Assert.AreEqual("destroy-component", events[0].TraceId);
        }

        [Test]
        public void DestroyComponentEntityCommandRejectsStaleAndInvalidEntityIds()
        {
            var buffer = new RuntimeCommandBuffer();
            var componentWorld = new GameplayComponentWorld();
            GameplayEntityId entity = componentWorld.CreateEntity();
            Assert.IsTrue(componentWorld.DestroyEntity(entity));
            var module = new GameplayRuntimeModule(
                new GameplayWorld(),
                new GameplayAbilityRegistry(),
                buffer,
                tickWorldAutomatically: false,
                componentWorld: componentWorld);

            buffer.Enqueue(GameplayRuntimeCommandFactory.DestroyComponentEntity(RuntimeFrame.Zero, entity, traceId: "stale"));
            buffer.Enqueue(new RuntimeCommand(
                RuntimeFrame.Zero,
                sourceId: 0,
                commandId: GameplayRuntimeCommandIds.DestroyComponentEntity,
                targetId: 0,
                payload0: 0,
                payload1: 1,
                traceId: "invalid"));

            module.Tick(new RuntimeTickContext(0, 0d, 0d, RuntimeTickStage.Simulation));

            var events = new List<GameplayRuntimeEvent>();
            Assert.AreEqual(2, module.DrainEvents(RuntimeFrame.Zero, events));
            Assert.AreEqual(GameplayRuntimeEventType.CommandRejected, events[0].Type);
            Assert.AreEqual(GameplayComponentEntityCommandSystem.MissingEntityReason, events[0].Reason);
            Assert.AreEqual(entity, events[0].ComponentEntityId);
            Assert.AreEqual(GameplayRuntimeEventType.CommandRejected, events[1].Type);
            Assert.AreEqual(GameplayComponentEntityCommandSystem.InvalidEntityReason, events[1].Reason);
            Assert.AreEqual(default(GameplayEntityId), events[1].ComponentEntityId);
        }

        [Test]
        public void ComponentEntityCommandsAreHandledBeforeUnsupportedSystem()
        {
            var buffer = new RuntimeCommandBuffer();
            var module = new GameplayRuntimeModule(
                new GameplayWorld(),
                new GameplayAbilityRegistry(),
                buffer,
                tickWorldAutomatically: false);

            buffer.Enqueue(GameplayRuntimeCommandFactory.CreateComponentEntity(RuntimeFrame.Zero, traceId: "create"));

            module.Tick(new RuntimeTickContext(0, 0d, 0d, RuntimeTickStage.Simulation));

            var events = new List<GameplayRuntimeEvent>();
            module.DrainEvents(RuntimeFrame.Zero, events);
            Assert.AreEqual(1, events.Count);
            Assert.AreNotEqual(GameplayRuntimeEventType.CommandRejected, events[0].Type);
        }

        private readonly struct TestStatComponent : IGameplayComponent
        {
            public TestStatComponent(int value)
            {
                Value = value;
            }

            public int Value { get; }
        }
    }
}
