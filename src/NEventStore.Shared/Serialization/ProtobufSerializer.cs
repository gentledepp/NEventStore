namespace NEventStore.Serialization
{
    using System;
    using System.IO;

#if !PCL
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using NEventStore.Logging;
    using ProtoBuf;
    using ProtoBuf.Meta;
    // see: https://lostechies.com/gabrielschenker/2012/06/30/how-we-got-rid-of-the-databasepart-6/
    public class ProtobufSerializer : ISerialize
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof(ProtobufSerializer));

        readonly IDictionary<Type, Formatter> _type2Contract = new Dictionary<Type, Formatter>();
        readonly IDictionary<Type, string> _type2ContractName = new Dictionary<Type, string>();
        readonly IDictionary<string, Type> _contractName2Type = new Dictionary<string, Type>();

        protected sealed class Formatter
        {
            public Action<object, Stream> SerializeDelegate;
            public Func<Stream, object> DeserializerDelegate;
            public string ContractName;

            public Formatter(string contractName, Func<Stream, object> deserializerDelegate, Action<object, Stream> serializeDelegate)
            {
                SerializeDelegate = serializeDelegate;
                DeserializerDelegate = deserializerDelegate;
                ContractName = contractName;
            }
        }

        internal static ProtobufSerializer Instance { get; set; }

        static ProtobufSerializer()
        {
            RuntimeTypeModel.Default.Add(typeof(Snapshot), false).SetSurrogate(typeof(SnapshotSurrogate));
            RuntimeTypeModel.Default.Add(typeof(EventMessage), false).SetSurrogate(typeof(EventMessageSurrogate));
        }

        public ProtobufSerializer(params Type[] knownEventTypes)
        {
            var eventTypes = new List<Type>();
            if (knownEventTypes == null || knownEventTypes.Length == 0)
            {
#if FRAMEWORK
                eventTypes = MessageTypeProvider.GetKnownEventTypes().ToList();
#else
                throw new ArgumentOutOfRangeException($"You must provide {nameof(knownEventTypes)}!");
#endif
            }
            else
            {
                eventTypes.AddRange(knownEventTypes);
            }
            
            var simpleTypes = new Type[] { typeof(string), typeof(int), typeof(long), typeof(double), typeof(Guid), typeof(short)};

            foreach (var type in simpleTypes.Concat(eventTypes))
            {
                Logger.Debug(Messages.RegisteringKnownType, type);
            }

            // add simple types as well
            AddSimpleTypes();

            var t2c = eventTypes.ToDictionary
            (t => t,
                t =>  new Formatter(t.GetContractName(_type2ContractName),
                        (stream) => ProtoBuf.Serializer.Deserialize(t, stream),
                        (o, stream) => ProtoBuf.Serializer.Serialize(stream, o))
                );
            foreach (var t in t2c)
            {
                _type2Contract.Add(t.Key, t.Value);
            }

            var c2t = eventTypes
                .Where(t => t.GetContractName(_type2ContractName) != null)
                .ToDictionary(
                    t => t.GetContractName(_type2ContractName),
                    t => t);
            foreach (var t in c2t)
            {
                _contractName2Type.Add(t.Key, t.Value);
            }

            // ugly hack!!
            Instance = this;
        }

        protected virtual void AddSimpleTypes()
        {
            AddFormatter(typeof(string), "s");
            AddFormatter(typeof(char), "c");
            AddFormatter(typeof(short), "i16");
            AddFormatter(typeof(int), "i32");
            AddFormatter(typeof(long), "i64");
            AddFormatter(typeof(ushort), "u16");
            AddFormatter(typeof(uint), "u32");
            AddFormatter(typeof(ulong), "u64");
            AddFormatter(typeof(double), "dbl");
            AddFormatter(typeof(float), "sng");
            AddFormatter(typeof(decimal), "dec");
            AddFormatter(typeof(bool), "b");
            AddFormatter(typeof(Guid), "g");
        }

        private void AddFormatter(Type type, string contractName)
        {
            var f = new Formatter(contractName,
                        (stream) => ProtoBuf.Serializer.Deserialize(type, stream),
                        (o, stream) => ProtoBuf.Serializer.Serialize(stream, o));

            _type2Contract.Add(type, f);
            _type2ContractName.Add(type, contractName);
            _contractName2Type.Add(contractName, type);
        }

        public virtual void Serialize<T>(Stream output, T graph)
        {
            Logger.Verbose(Messages.SerializingGraph, typeof(T));
            Formatter formatter;
            Type t = typeof(T);
            if (!_type2Contract.TryGetValue(t, out formatter))
            {
                //var s = $"Can't find a serializer for unknown object type '{t.FullName}'.Have you passed all known types to the constructor?";
                //throw new InvalidOperationException(s);
                formatter = new Formatter(string.Empty,
                        (stream) => ProtoBuf.Serializer.Deserialize(t, stream),
                        (o, stream) => ProtoBuf.Serializer.Serialize(stream, o));
            }

            formatter.SerializeDelegate(graph, output);
        }

        public Type GetContentType(string contractName)
        {
            return _contractName2Type[contractName];
        }

        public virtual T Deserialize<T>(Stream input)
        {
            Logger.Verbose(Messages.DeserializingStream, typeof(T));

            Formatter f;
            Type t = typeof(T);
            if (!_type2Contract.TryGetValue(t, out f))
            {
                f = new Formatter(string.Empty,
                        (stream) => ProtoBuf.Serializer.Deserialize(t, stream),
                        (o, stream) => ProtoBuf.Serializer.Serialize(stream, o));
            }

            return (T)f.DeserializerDelegate(input);
        }
        
        public virtual object Deserialize(Stream input, Type t)
        {
            Logger.Verbose(Messages.DeserializingStream, t);

            Formatter value;
            if (!_type2Contract.TryGetValue(t, out value))
            {
                var s = $"Can't find a serializer for unknown object type '{t.FullName}'.Have you passed all known types to the constructor?";
                throw new InvalidOperationException(s);
            }

            return value.DeserializerDelegate(input);
        }

        private void Serialize(object instance, Type type, Stream destinationStream)
        {
            Formatter formatter;

            if (type.GetTypeInfo().IsValueType || type == typeof(string))
            {
                formatter = new Formatter(type.GetContractName(_type2ContractName),
                        (stream) => ProtoBuf.Serializer.Deserialize(type, stream),
                        (o, stream) => ProtoBuf.Serializer.Serialize(stream, o));
            }
            else if (!_type2Contract.TryGetValue(type, out formatter))
            {
                var s = $"Can't find a serializer for unknown object type '{type.FullName}'.Have you passed all known types to the constructor?";
                throw new InvalidOperationException(s);
            }

            formatter.SerializeDelegate(instance, destinationStream);
        }

        public byte[] SerializeEvent(object e)
        {
            if(e == null)
                return new byte[0];

            byte[] content;
            using (var ms = new MemoryStream())
            {
                Serialize(e, e.GetType(), ms);
                content = ms.ToArray();
            }
            byte[] messageContractBuffer;
            using (var ms = new MemoryStream())
            {
                var name = e.GetType().GetContractName(_type2ContractName);
                var messageContract = new MessageContract(name, content.Length, 0);
                Serialize(messageContract, typeof(MessageContract), ms);
                messageContractBuffer = ms.ToArray();
            }
            using (var ms = new MemoryStream())
            {
                var headerContract = new MessageHeaderContract(messageContractBuffer.Length);
                headerContract.WriteHeader(ms);
                ms.Write(messageContractBuffer, 0, messageContractBuffer.Length);
                ms.Write(content, 0, content.Length);
                return ms.ToArray();
            }
        }

        public object DeserializeEvent(byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0)
                return null;

            using (var ms = new MemoryStream(buffer))
            {
                var header = MessageHeaderContract.ReadHeader(buffer);
                ms.Seek(MessageHeaderContract.FixedSize, SeekOrigin.Begin);

                var headerBuffer = new byte[header.HeaderBytes];
                ms.Read(headerBuffer, 0, (int) header.HeaderBytes);
                MessageContract contract;

                using (var headerStream = new MemoryStream(headerBuffer))
                    contract = Deserialize<MessageContract>(headerStream);

                var contentBuffer = new byte[contract.ContentSize];
                ms.Read(contentBuffer, 0, (int) contract.ContentSize);
                var contentType = GetContentType(contract.ContractName);

                using (var contentStream = new MemoryStream(contentBuffer))
                {
                    return Deserialize(contentStream, contentType);
                }
            }
        }
    }

    [DataContract(Namespace="nes.proto", Name="mc")]
    public sealed class MessageContract
    {
        [DataMember(Order = 1)] public readonly string ContractName;
        [DataMember(Order = 2)] public readonly long ContentSize;
        [DataMember(Order = 3)] public readonly long ContentPosition;

        MessageContract()
        {
            
        }

        public MessageContract(string contractName, long contentSize, long contentPosition)
        {
            ContractName = contractName;
            ContentSize = contentSize;
            ContentPosition = contentPosition;
        }
    }

    public sealed class MessageHeaderContract
    {
        public static int FixedSize = 8;
        public readonly long HeaderBytes;

        public MessageHeaderContract(long headerBytes)
        {
            HeaderBytes = headerBytes;
        }

        public static MessageHeaderContract ReadHeader(byte[] buffer)
        {
            var headerBytes = BitConverter.ToInt64(buffer, 0);
            return new MessageHeaderContract(headerBytes);
        }

        public void WriteHeader(Stream stream)
        {
            stream.Write(BitConverter.GetBytes(HeaderBytes), 0, 8);
        }
    }

    internal static class ComponentModelExtensions
    {
        public static string GetContractName(this Type self, IDictionary<Type, string> type2Contract = null)
        {
            string n = null;
            string ns = null;

            // as eventmessage and snapshot are not serialized directly, but by surrogates, ignore their contract
            if (self == typeof(EventMessage) ||
                self == typeof(Snapshot))
                return null;

            if (type2Contract != null && (self == typeof(string) || self.GetTypeInfo().IsValueType))
            {
                n = type2Contract[self];
            }
            else
            {
                var attr = (DataContractAttribute)self.GetTypeInfo().GetCustomAttributes(typeof(DataContractAttribute), false).FirstOrDefault();

                if (attr == null)
                    throw new InvalidOperationException($"The type '{self.FullName}' cannot be serialized as it does not have a '[DataContract]' attribute");

                n = attr.Name;
                ns = attr.Namespace;
            }

            if (string.IsNullOrWhiteSpace(n))
                throw new InvalidOperationException($"Please specify Name and (optionally) Namespace in the DataContract of '{self.FullName}'");

            var @namespace = string.IsNullOrWhiteSpace(ns) ? null : $"{ns}.";
            return $"{@namespace}{n}";
        }
    }

    [DataContract(Namespace = "nes", Name = "sh")]
    public sealed class SnapshotSurrogate
    {
        [DataMember(Order = 1)]
        public string BucketId { get; set; }

        /// <summary>
        ///     Gets the value which uniquely identifies the stream to which the snapshot applies.
        /// </summary>
        [DataMember(Order = 2)]
        public string StreamId { get; set; }

        /// <summary>
        ///     Gets the position at which the snapshot applies.
        /// </summary>
        [DataMember(Order = 3)]
        public int StreamRevision { get; set; }

        /// <summary>
        ///     Gets the snapshot or materialized view of the stream at the revision indicated.
        /// </summary>
        [DataMember(Order = 4)]
        public byte[] Payload { get; set; }

        public static implicit operator Snapshot(SnapshotSurrogate suggorage)
        {
            if (suggorage == null)
                return null;
            //object payload = Deserialize(suggorage.Payload);
            object payload = ProtobufSerializer.Instance.DeserializeEvent(suggorage.Payload);
            return new Snapshot(suggorage.BucketId, suggorage.StreamId, suggorage.StreamRevision, payload);
        }

        public static implicit operator SnapshotSurrogate(Snapshot source)
        {
            return source == null ? null : new SnapshotSurrogate
            {
                BucketId = source.BucketId,
                StreamId = source.StreamId,
                StreamRevision = source.StreamRevision,
                //Payload = Serialize(source.Payload)
                Payload = ProtobufSerializer.Instance.SerializeEvent(source.Payload)
            };
        }
    }

    [DataContract(Namespace = "nes", Name = "em")]
    public class EventMessageSurrogate
    {
        public EventMessageSurrogate()
        {
            Headers = new List<KeyValuePairSurrogate>();
        }

        /// <summary>
        ///     Gets the metadata which provides additional, unstructured information about this message.
        /// </summary>
        [DataMember(Order = 1)]
        public List<KeyValuePairSurrogate> Headers { get; set; }

        /// <summary>
        ///     Gets or sets the actual event message body.
        /// </summary>
        [DataMember(Order = 2)]
        public byte[] Body { get; set; }
        
        public static implicit operator EventMessage(EventMessageSurrogate suggorage)
        {
            return suggorage == null ? null : new EventMessage
            {
                Headers = Deserialize(suggorage.Headers),
                Body = ProtobufSerializer.Instance.DeserializeEvent(suggorage.Body)
            };
        }

        public static implicit operator EventMessageSurrogate(EventMessage source)
        {
            return source == null ? null : new EventMessageSurrogate
            {
                Headers = Serialize(source.Headers),
                Body = ProtobufSerializer.Instance.SerializeEvent(source.Body)
            };
        }

        private static List<KeyValuePairSurrogate> Serialize(Dictionary<string, object> o)
        {
            var list = new List<KeyValuePairSurrogate>();

            if (o == null)
                return list;
            
            foreach (var kvp in o)
            {
                list.Add(new KeyValuePairSurrogate(kvp.Key, ProtobufSerializer.Instance.SerializeEvent(kvp.Value)));
            }

            return list;
        }

        private static Dictionary<string, object> Deserialize(List<KeyValuePairSurrogate> b)
        {
            var dict = new Dictionary<string, object>();
            if (b == null)
                return dict;

            foreach (var kvp in b)
            {
                dict.Add(kvp.Key, ProtobufSerializer.Instance.DeserializeEvent(kvp.Value));
            }

            return dict;
        }

        [ProtoContract]
        public class KeyValuePairSurrogate
        {
            public KeyValuePairSurrogate()
            {
                
            }

            public KeyValuePairSurrogate(string key, byte[] value)
            {
                Key = key;
                Value = value;
            }

            [ProtoMember(1)]
            public string Key { get; set; }
            [ProtoMember(2)]
            public byte[] Value { get; set; }
        }
    }

#else
    public class ProtobufSerializer : ISerialize
    {
        public ProtobufSerializer(params Type[] knownEventTypes)
        {
            throw new NotSupportedException("you called the constructor of protobuf in PCL which is not supported. Did you forget to reference a platform-specific version of NEventStore?");       
        }

        public void Serialize<T>(Stream output, T graph)
        {
            throw new NotImplementedException();
        }

        public T Deserialize<T>(Stream input)
        {
            throw new NotImplementedException();
        }
    }
#endif
#if FRAMEWORK
    public static class MessageTypeProvider
    {
        public static Type[] GetKnownEventTypes()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetExportedTypes()
                    .Where(t => (t.GetCustomAttributes(typeof(DataContractAttribute), true).Any())
                         && t.IsAbstract == false)
                         )
                .Union(new[] { typeof(MessageContract) })
                .ToArray();
            return types;
        }
    }
#endif
}
