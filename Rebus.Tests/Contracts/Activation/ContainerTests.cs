﻿using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Transport;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Contracts.Activation
{
    public class ContainerTests<TFactory> : FixtureBase where TFactory : IHandlerActivatorFactory, new()
    {
        TFactory _factory;

        protected override void SetUp()
        {
            _factory = new TFactory();

            DisposableHandler.WasCalledAllright = false;
            DisposableHandler.WasDisposedAllright = false;
        }

        [Test]
        public async Task ResolvingWithoutRegistrationYieldsEmptySequenec()
        {
            var handlerActivator = _factory.GetActivator();

            using (var transactionContext = new DefaultTransactionContext())
            {
                var handlers = (await handlerActivator.GetHandlers("hej", transactionContext)).ToList();

                Assert.That(handlers.Count, Is.EqualTo(0));
            }
        }

        [Test]
        public async Task CanRegisterHandler()
        {
            _factory.RegisterHandlerType<SomeStringHandler>();
            var handlerActivator = _factory.GetActivator();

            using (var transactionContext = new DefaultTransactionContext())
            {
                var handlers = (await handlerActivator.GetHandlers("hej", transactionContext)).ToList();

                Assert.That(handlers.Count, Is.EqualTo(1));
                Assert.That(handlers[0], Is.TypeOf<SomeStringHandler>());
            }
        }

        [Test]
        public async Task ResolvedHandlerIsDisposed()
        {
            _factory.RegisterHandlerType<DisposableHandler>();

            var bus = Configure.With(_factory.GetActivator())
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(true), "somequeue"))
                .Start();

            Using(bus);

            await bus.SendLocal("hej med dig");

            await Task.Delay(500);

            Assert.That(DisposableHandler.WasCalledAllright, Is.True, "The handler was apparently not called");
            Assert.That(DisposableHandler.WasDisposedAllright, Is.True, "The handler was apparently not disposed");
        }

        class SomeStringHandler : IHandleMessages<string>
        {
            public async Task Handle(string message)
            {
            }
        }

        class DisposableHandler : IHandleMessages<string>, IDisposable
        {
            public static bool WasCalledAllright;
            public static bool WasDisposedAllright;

            public async Task Handle(string message)
            {
                WasCalledAllright = true;
            }

            public void Dispose()
            {
                WasDisposedAllright = true;
            }
        }
    }

    public interface IHandlerActivatorFactory
    {
        IHandlerActivator GetActivator();
        void RegisterHandlerType<THandler>() where THandler : class;
    }
}