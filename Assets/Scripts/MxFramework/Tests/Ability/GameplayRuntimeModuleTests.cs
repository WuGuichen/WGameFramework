using System.Collections.Generic;
using MxFramework.Gameplay;
using MxFramework.Runtime;
using NUnit.Framework;

namespace MxFramework.Tests.Ability
{
    public sealed class GameplayRuntimeModuleTests
    {
        private const int AttrHp = 1;
        private const int AttrAttack = 2;
        private const int AttrDefense = 3;
        private const int AbilityStrike = 300001;

        [Test]
        public void Tick_DrainsCastAbilityCommandAndEmitsStableEvents()
        {
            RuntimeEntity player = CreateEntity(1, 1, 1000, 120, 20);
            RuntimeEntity enemy = CreateEntity(2, 2, 600, 80, 10);
            var world = new GameplayWorld();
            world.Register(player);
            world.Register(enemy);
            var abilities = new GameplayAbilityRegistry();
            Assert.IsTrue(abilities.TryRegister(CreateStrikeAbility(), out string failure), failure);
            var buffer = new RuntimeCommandBuffer();
            var module = new GameplayRuntimeModule(world, abilities, buffer);

            RuntimeCommandValidationResult accepted = buffer.Enqueue(GameplayRuntimeCommandFactory.CastAbility(
                new RuntimeFrame(4),
                player.EntityId,
                AbilityStrike,
                traceId: "strike"));
            Assert.IsTrue(accepted.Success, accepted.Error.ToString());

            module.Tick(new RuntimeTickContext(4, 0d, 0d, RuntimeTickStage.Simulation));

            Assert.AreEqual(490, enemy.Store.GetAttribute(AttrHp));
            Assert.AreEqual(1, module.AbilityResults.Count);
            Assert.IsTrue(module.AbilityResults[0].Success);
            Assert.AreEqual(1L, world.TickCount);

            var events = new List<GameplayRuntimeEvent>();
            Assert.AreEqual(2, module.DrainEvents(new RuntimeFrame(4), events));
            Assert.AreEqual(GameplayRuntimeEventType.AbilityCastSucceeded, events[0].Type);
            Assert.AreEqual(new RuntimeFrame(4), events[0].Frame);
            Assert.AreEqual(player.EntityId, events[0].CasterEntityId);
            Assert.AreEqual(AbilityStrike, events[0].AbilityId);
            Assert.AreEqual(enemy.EntityId, events[0].TargetEntityId);
            Assert.AreEqual("strike", events[0].TraceId);
            Assert.AreEqual(GameplayRuntimeEventType.WorldTicked, events[1].Type);
        }

        [Test]
        public void Tick_CastAbilityFailureEmitsStructuredFailureEvent()
        {
            RuntimeEntity player = CreateEntity(1, 1, 1000, 120, 20);
            var world = new GameplayWorld();
            world.Register(player);
            var abilities = new GameplayAbilityRegistry();
            Assert.IsTrue(abilities.TryRegister(CreateStrikeAbility(), out string failure), failure);
            var buffer = new RuntimeCommandBuffer();
            var module = new GameplayRuntimeModule(world, abilities, buffer, tickWorldAutomatically: false);

            buffer.Enqueue(GameplayRuntimeCommandFactory.CastAbility(
                RuntimeFrame.Zero,
                player.EntityId,
                AbilityStrike,
                candidateEntityId: 99,
                traceId: "missing-candidate"));

            module.Tick(new RuntimeTickContext(0, 0d, 0d, RuntimeTickStage.Simulation));

            var events = new List<GameplayRuntimeEvent>();
            Assert.AreEqual(1, module.DrainEvents(RuntimeFrame.Zero, events));
            Assert.AreEqual(GameplayRuntimeEventType.AbilityCastFailed, events[0].Type);
            Assert.AreEqual(GameplayAbilityRuntimeFailureCode.EmptyCandidates, events[0].FailureCode);
            Assert.AreEqual(GameplayAbilityRuntimeService.EmptyCandidatesFailureReason, events[0].Reason);
            Assert.AreEqual("missing-candidate", events[0].TraceId);
            Assert.AreEqual(0L, world.TickCount);
        }

        [Test]
        public void Tick_DespawnEntityCommandRemovesEntity()
        {
            RuntimeEntity entity = CreateEntity(10, 1, 100, 10, 5);
            var world = new GameplayWorld();
            world.Register(entity);
            var buffer = new RuntimeCommandBuffer();
            var module = new GameplayRuntimeModule(world, new GameplayAbilityRegistry(), buffer, tickWorldAutomatically: false);

            buffer.Enqueue(GameplayRuntimeCommandFactory.DespawnEntity(RuntimeFrame.Zero, entity.EntityId, traceId: "despawn"));

            module.Tick(new RuntimeTickContext(0, 0d, 0d, RuntimeTickStage.Simulation));

            Assert.IsFalse(world.Entities.TryGet(entity.EntityId, out _));
            var events = new List<GameplayRuntimeEvent>();
            module.DrainEvents(RuntimeFrame.Zero, events);
            Assert.AreEqual(GameplayRuntimeEventType.EntityDespawned, events[0].Type);
            Assert.AreEqual(entity.EntityId, events[0].TargetEntityId);
        }

        [Test]
        public void RuntimeHost_TicksGameplayModuleAfterEarlierModules()
        {
            RuntimeEntity player = CreateEntity(1, 1, 1000, 120, 20);
            RuntimeEntity enemy = CreateEntity(2, 2, 600, 80, 10);
            var world = new GameplayWorld();
            world.Register(player);
            world.Register(enemy);
            var abilities = new GameplayAbilityRegistry();
            Assert.IsTrue(abilities.TryRegister(CreateStrikeAbility(), out string failure), failure);
            var buffer = new RuntimeCommandBuffer();
            buffer.Enqueue(GameplayRuntimeCommandFactory.CastAbility(new RuntimeFrame(1), player.EntityId, AbilityStrike));
            var module = new GameplayRuntimeModule(world, abilities, buffer);
            var host = new RuntimeHost();
            host.RegisterModule(module);
            host.Initialize();
            host.Start();

            host.Tick(1, 0d);

            Assert.AreEqual(490, enemy.Store.GetAttribute(AttrHp));
            Assert.AreEqual(1L, world.TickCount);
        }

        [Test]
        public void AbilityResults_KeepRecentResultsAndCanBeCleared()
        {
            RuntimeEntity player = CreateEntity(1, 1, 1000, 120, 20);
            RuntimeEntity enemy = CreateEntity(2, 2, 600, 80, 10);
            var world = new GameplayWorld();
            world.Register(player);
            world.Register(enemy);
            var abilities = new GameplayAbilityRegistry();
            Assert.IsTrue(abilities.TryRegister(CreateStrikeAbility(), out string failure), failure);
            var buffer = new RuntimeCommandBuffer();
            var module = new GameplayRuntimeModule(
                world,
                abilities,
                buffer,
                tickWorldAutomatically: false,
                abilityResultCapacity: 2);

            buffer.Enqueue(GameplayRuntimeCommandFactory.CastAbility(RuntimeFrame.Zero, player.EntityId, AbilityStrike, traceId: "one"));
            buffer.Enqueue(GameplayRuntimeCommandFactory.CastAbility(RuntimeFrame.Zero, player.EntityId, AbilityStrike, traceId: "two"));
            buffer.Enqueue(GameplayRuntimeCommandFactory.CastAbility(RuntimeFrame.Zero, player.EntityId, AbilityStrike, traceId: "three"));

            module.Tick(new RuntimeTickContext(0, 0d, 0d, RuntimeTickStage.Simulation));

            Assert.AreEqual(2, module.AbilityResults.Count);
            Assert.AreEqual("two", module.AbilityResults[0].TraceId);
            Assert.AreEqual("three", module.AbilityResults[1].TraceId);

            var copied = new List<GameplayAbilityRuntimeResult>();
            Assert.AreEqual(2, module.CopyAbilityResults(copied));
            Assert.AreEqual("two", copied[0].TraceId);
            Assert.AreEqual("three", copied[1].TraceId);

            module.ClearAbilityResults();

            Assert.AreEqual(0, module.AbilityResults.Count);
        }

        private static RuntimeEntity CreateEntity(int id, int team, int hp, int attack, int defense)
        {
            var entity = new RuntimeEntity(id, team, AttrHp);
            entity.Store.RegisterAttribute(AttrHp, hp);
            entity.Store.RegisterAttribute(AttrAttack, attack);
            entity.Store.RegisterAttribute(AttrDefense, defense);
            return entity;
        }

        private static IAbility CreateStrikeAbility()
        {
            return new SimpleAbility(
                AbilityStrike,
                new SingleEnemyTargetSelector(),
                new IAbilityEffect[]
                {
                    new DamageEffect(AttrAttack, AttrDefense, AttrHp)
                });
        }
    }
}
