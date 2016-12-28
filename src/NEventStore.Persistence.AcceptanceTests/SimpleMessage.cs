namespace NEventStore.Persistence.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;

    [Serializable]
    [DataContract(Namespace = "AcceptanceTests", Name = "SimpleMessage")]
    public class SimpleMessage
    {
        public SimpleMessage()
        {
            Contents = new List<string>();
        }

        [DataMember(Order = 0)] public Guid Id { get; set; }
        [DataMember(Order = 1)] public DateTime Created { get; set; }
        [DataMember(Order = 2)] public string Value { get; set; }
        [DataMember(Order = 3)] public int Count { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists",
            Justification = "This is an acceptance test DTO and the structure doesn't really matter.")]
        [DataMember(Order = 4)]
        public List<string> Contents { get; private set; }
    }
}