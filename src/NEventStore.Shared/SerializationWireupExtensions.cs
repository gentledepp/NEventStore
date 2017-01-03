namespace NEventStore
{
    using System;
    using NEventStore.Serialization;

    public static class SerializationWireupExtensions
    {
        public static SerializationWireup UsingBinarySerialization(this PersistenceWireup wireup)
        {
#if WINDOWS_UWP || PCL
            throw new NotSupportedException("BinarySerializer is not available on UWP");
#else
            return wireup.UsingCustomSerialization(new BinarySerializer());
#endif
        }

        public static SerializationWireup UsingCustomSerialization(this PersistenceWireup wireup, ISerialize serializer)
        {
            return new SerializationWireup(wireup, serializer);
        }

        public static SerializationWireup UsingJsonSerialization(this PersistenceWireup wireup)
        {
            return wireup.UsingCustomSerialization(new JsonSerializer());
        }

        public static SerializationWireup UsingBsonSerialization(this PersistenceWireup wireup)
        {
            return wireup.UsingCustomSerialization(new BsonSerializer());
        }
    }
}