namespace NEventStore.Persistence.AcceptanceTests.BDD
{
    using System;
    using NEventStore.Dispatcher;
    using Xunit;
    
    public abstract class SpecificationBase : IDisposable
    {
        public SpecificationBase()
        {
            
        }

        protected virtual void Because()
        {}

        protected virtual void Cleanup()
        {}

        protected virtual void Context()
        {}

        public void OnFinish()
        {
            Cleanup();
        }

        public void OnStart()
        {
            Context();
            Because();
        }

        public virtual void Dispose()
        {
            
        }
    }
}