namespace NEventStore
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    ///     Represents a materialized view of a stream at specific revision.
    /// </summary>
    [DataContract(Namespace = "nes", Name = "sh")]
    [Serializable]
    public class Snapshot : ISnapshot
    {
        /// <summary>
        ///     Initializes a new instance of the Snapshot class for the default bucket.
        /// </summary>
        /// <param name="streamId">The value which uniquely identifies the stream to which the snapshot applies.</param>
        /// <param name="streamRevision">The position at which the snapshot applies.</param>
        /// <param name="payload">The snapshot or materialized view of the stream at the revision indicated.</param>
        public Snapshot(string streamId, int streamRevision, object payload)
            : this(Bucket.Default, streamId, streamRevision, payload)
        {}

        /// <summary>
        ///     Initializes a new instance of the Snapshot class.
        /// </summary>
        /// <param name="bucketId">The value which uniquely identifies bucket the stream belongs to.</param>
        /// <param name="streamId">The value which uniquely identifies the stream to which the snapshot applies.</param>
        /// <param name="streamRevision">The position at which the snapshot applies.</param>
        /// <param name="payload">The snapshot or materialized view of the stream at the revision indicated.</param>
        public Snapshot(string bucketId, string streamId, int streamRevision, object payload)
            : this()
        {
            BucketId = bucketId;
            StreamId = streamId;
            StreamRevision = streamRevision;
            Payload = payload;
        }

        /// <summary>
        ///     Initializes a new instance of the Snapshot class.
        /// </summary>
        protected Snapshot()
        {}

        [DataMember(Order=0)]
        public virtual string BucketId { get; private set; }

        /// <summary>
        ///     Gets the value which uniquely identifies the stream to which the snapshot applies.
        /// </summary>
        [DataMember(Order = 1)]
        public virtual string StreamId { get; private set; }

        /// <summary>
        ///     Gets the position at which the snapshot applies.
        /// </summary>
        [DataMember(Order = 2)]
        public virtual int StreamRevision { get; private set; }

        /// <summary>
        ///     Gets the snapshot or materialized view of the stream at the revision indicated.
        /// </summary>
        [DataMember(Order = 3)]
        public virtual object Payload { get; private set; }
    }
}