namespace NEventStore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
#if !PCL
    using System.Transactions;
#endif
    using Newtonsoft.Json;
    using NEventStore.Conversion;
    using NEventStore.Dispatcher;
    using NEventStore.Persistence;
    using NEventStore.Persistence.InMemory;
    using NEventStore.Serialization;
    using JsonSerializer = NEventStore.Serialization.JsonSerializer;

    public class Wireup
    {
        private readonly NanoContainer _container;
        private readonly Wireup _inner;

        protected Wireup(NanoContainer container)
        {
            _container = container;
        }

        protected Wireup(Wireup inner)
        {
            _inner = inner;
        }

        protected NanoContainer Container
        {
            get { return _container ?? _inner.Container; }
        }

        public static Wireup Init()
        {
#if !PCL
            var container = new NanoContainer();

            container.Register(TransactionScopeOption.Suppress);
            container.Register<IPersistStreams>(new InMemoryPersistenceEngine());
            container.Register<IScheduleDispatches>(new NullDispatcher());
            container.Register<ISerialize>(new JsonSerializer());
            container.Register(BuildEventStore);

            return new Wireup(container);
#else
            throw new NotSupportedException("The PCL bait assembly was not switched with platform-specific assemply! Make sure you have referenced NEventStore in your platform project as well");
#endif
        }

        public virtual Wireup With<T>(T instance) where T : class
        {
            Container.Register(instance);
            return this;
        }

        public virtual Wireup HookIntoPipelineUsing(IEnumerable<IPipelineHook> hooks)
        {
            return HookIntoPipelineUsing((hooks ?? new IPipelineHook[0]).ToArray());
        }

        public virtual Wireup HookIntoPipelineUsing(params IPipelineHook[] hooks)
        {
            ICollection<IPipelineHook> collection = (hooks ?? new IPipelineHook[] { }).Where(x => x != null).ToArray();
            Container.Register(collection);
            return this;
        }

        public virtual IStoreEvents Build()
        {
            if (_inner != null)
            {
                return _inner.Build();
            }

            return Container.Resolve<IStoreEvents>();
        }

        private static IStoreEvents BuildEventStore(NanoContainer context)
        {
#if !PCL
            var scopeOption = context.Resolve<TransactionScopeOption>();
            OptimisticPipelineHook concurrency = scopeOption == TransactionScopeOption.Suppress ? new OptimisticPipelineHook() : null;
            var dispatchScheduler = context.Resolve<IScheduleDispatches>();
            var dispatchSchedulerHook = new DispatchSchedulerPipelineHook(dispatchScheduler);
            var upconverter = context.Resolve<EventUpconverterPipelineHook>();

            ICollection<IPipelineHook> hooks = context.Resolve<ICollection<IPipelineHook>>() ?? new IPipelineHook[0];
            hooks = new IPipelineHook[] { concurrency, dispatchSchedulerHook, upconverter }
                .Concat(hooks)
                .Where(x => x != null)
                .ToArray();

            return new OptimisticEventStore(context.Resolve<IPersistStreams>(), hooks, dispatchScheduler.Start);
            
#else
            throw new NotSupportedException("The PCL bait assembly was not switched with platform-specific assemply! Make sure you have referenced NEventStore in your platform project as well");
#endif
        }
    }
}