namespace NEventStore
{
#if !PCL
    using System.Transactions;
#endif
    using System;
    using NEventStore.Dispatcher;
    using NEventStore.Logging;
    using NEventStore.Persistence;

    public class AsynchronousDispatchSchedulerWireup : Wireup
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (AsynchronousDispatchSchedulerWireup));

        public AsynchronousDispatchSchedulerWireup(Wireup wireup, IDispatchCommits dispatcher, DispatcherSchedulerStartup startup)
            : base(wireup)
        {
#if !PCL
            var option = Container.Resolve<TransactionScopeOption>();
            if (option != TransactionScopeOption.Suppress)
            {
                Logger.Warn(Messages.SynchronousDispatcherTwoPhaseCommits);
            }

            Logger.Debug(Messages.AsyncDispatchSchedulerRegistered);
            Startup(startup);
            DispatchTo(dispatcher ?? new NullDispatcher());
            Container.Register<IScheduleDispatches>(c =>
            {
                var dispatchScheduler = new AsynchronousDispatchScheduler(
                    c.Resolve<IDispatchCommits>(),
                    c.Resolve<IPersistStreams>());
                if (c.Resolve<DispatcherSchedulerStartup>() == DispatcherSchedulerStartup.Auto)
                {
                    dispatchScheduler.Start();
                }
                return dispatchScheduler;
            });
#else
            throw new NotSupportedException("The PCL bait assembly was not switched with platform-specific assemply! Make sure you have referenced NEventStore in your platform project as well");
#endif
        }

        public AsynchronousDispatchSchedulerWireup Startup(DispatcherSchedulerStartup startup)
        {
            Container.Register(startup);
            return this;
        }

        public AsynchronousDispatchSchedulerWireup DispatchTo(IDispatchCommits instance)
        {
            Logger.Debug(Messages.DispatcherRegistered, instance.GetType());
            Container.Register(instance);
            return this;
        }
    }
}