namespace NEventStore
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    ///     Represents a single element in a stream of events.
    /// </summary>
    [Serializable]
    [DataContract(Namespace="nes",Name="em")]
    public class EventMessage
    {
        /// <summary>
        ///     Initializes a new instance of the EventMessage class.
        /// </summary>
        public EventMessage()
        {
            Headers = new Dictionary<string, object>();
        }

        /// <summary>
        ///     Gets the metadata which provides additional, unstructured information about this message.
        /// </summary>
        [DataMember(Order = 1)]
        public Dictionary<string, object> Headers { get; set; }

        /// <summary>
        ///     Gets or sets the actual event message body.
        /// </summary>
        [DataMember(Order = 2)]
        public object Body { get; set; }
    }
}