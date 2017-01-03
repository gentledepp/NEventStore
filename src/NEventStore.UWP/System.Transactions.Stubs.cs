using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Transactions
{
    /// <summary>Provides additional options for creating a transaction scope.</summary>
    public enum TransactionScopeOption
    {
        Required,
        RequiresNew,
        Suppress,
    }

    /// <summary>
    /// This is just a stup, as NEventStore only uses the "Suppress" option on transactionscopes
    /// which means that tey are never really used anyways (??)
    /// </summary>
    public class TransactionScope : IDisposable
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Transactions.TransactionScope" /> class. </summary>
        public TransactionScope()
        {
            
        }
        /// <summary>Initializes a new instance of the <see cref="T:System.Transactions.TransactionScope" /> class with the specified requirements.</summary>
        /// <param name="scopeOption">An instance of the <see cref="T:System.Transactions.TransactionScopeOption" /> enumeration that describes the transaction requirements associated with this transaction scope.</param>
        public TransactionScope(TransactionScopeOption scopeOption)
        {
            
        }
        /// <summary>Initializes a new instance of the <see cref="T:System.Transactions.TransactionScope" /> class with the specified timeout value and requirements.</summary>
        /// <param name="scopeOption">An instance of the <see cref="T:System.Transactions.TransactionScopeOption" /> enumeration that describes the transaction requirements associated with this transaction scope.</param>
        /// <param name="scopeTimeout">The <see cref="T:System.TimeSpan" /> after which the transaction scope times out and aborts the transaction.</param>
        public TransactionScope(TransactionScopeOption scopeOption, TimeSpan scopeTimeout)
        {
            
        }
        public void Dispose() { }
        /// <summary>Indicates that all operations within the scope are completed successfully.</summary>
        /// <exception cref="T:System.InvalidOperationException">This method has already been called once.</exception>
        public void Complete() { }
    }
}
