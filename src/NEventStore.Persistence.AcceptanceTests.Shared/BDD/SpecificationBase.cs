namespace NEventStore.Persistence.AcceptanceTests.BDD
{
    using System;
    using System.CodeDom;
    using NEventStore.Dispatcher;
    using Xunit;
    
    public abstract class SpecificationBase : IDisposable
    {
        public SpecificationBase()
        {
            OnStart();
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
            OnFinish();
        }
    }


    public abstract class SpecificationBase2 : IDisposable
    {
        protected virtual void Because()
        { }

        protected virtual void Cleanup()
        { }

        protected virtual void Context()
        { }

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
            OnFinish();
        }
    }
}