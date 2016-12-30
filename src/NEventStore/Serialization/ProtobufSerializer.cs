namespace NEventStore.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using NEventStore.Logging;
    using ProtoBuf;
    using ProtoBuf.Meta;


    // see: https://lostechies.com/gabrielschenker/2012/06/30/how-we-got-rid-of-the-databasepart-6/

    public class ProtobufSerializer : ISerialize
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof(ProtobufSerializer));
        private readonly IEnumerable<Type> _knownTypes = new[] { typeof(List<EventMessage>), typeof(Dictionary<string, object>) };


        readonly IDictionary<Type, Formatter> _type2Contract = new Dictionary<Type, Formatter>();
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

        public ProtobufSerializer(params Type[] knownEventTypes)
        {
            if (knownEventTypes == null || knownEventTypes.Length == 0)
            {
                knownEventTypes = MessagesProvider.GetKnownEventTypes();
            }

            foreach (var type in knownEventTypes)
            {
                Logger.Debug(Messages.RegisteringKnownType, type);
            }
            
            _type2Contract = knownEventTypes.ToDictionary
            (t => t,
                t =>
                {
                    var formatter = RuntimeTypeModel.Default.CreateFormatter(t);
                    return new Formatter(t.GetContractName(), formatter.Deserialize,
                        (o, stream) => formatter.Serialize(stream, o));
                });
            _contractName2Type = knownEventTypes.ToDictionary(
                t => t.GetContractName(),
                t => t);
        }

        public virtual void Serialize<T>(Stream output, T graph)
        {
            Logger.Verbose(Messages.SerializingGraph, typeof(T));
            Formatter formatter;
            Type t = typeof(T);
            if (!_type2Contract.TryGetValue(t, out formatter))
            {
                var s = $"Can't find a serializer for unknown object type '{t.FullName}'.Have you passed all known types to the constructor?";
                throw new InvalidOperationException(s);
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

            Formatter value;
            Type t = typeof(T);
            if (!_type2Contract.TryGetValue(t, out value))
            {
                var s = $"Can't find a serializer for unknown object type '{t.FullName}'.Have you passed all known types to the constructor?";
                throw new InvalidOperationException(s);
            }

            return (T)value.DeserializerDelegate(input);
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

        public object DeserializeEvent(byte[] buffer)
        {
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

    public static class MessagesProvider
    {
        public static Type[] GetKnownEventTypes()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetExportedTypes()
                    .Where(t => (t.GetCustomAttributes(typeof(DataContractAttribute), true).Any() 
                            /*|| t.GetCustomAttributes(typeof(ProtoContractAttribute), true).Any()*/) 
                         && t.IsAbstract == false)
                         )
                .Union(new[] { typeof(MessageContract), typeof(EventMessage) })
                .ToArray();
            return types;
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
        public static string GetContractName(this Type self)
        {
            var attr = (DataContractAttribute) self.GetCustomAttributes(typeof(DataContractAttribute), false).FirstOrDefault();

            if(attr == null)
                throw new InvalidOperationException($"The type '{self.FullName}' cannot be serialized as it does not have a '[DataContract]' attribute");

            if(string.IsNullOrWhiteSpace(attr.Namespace) && string.IsNullOrWhiteSpace(attr.Name))
                throw new InvalidOperationException($"Please specify Name and (optionally) Namespace in the DataContract of '{self.FullName}'");

            return $"{attr.Namespace}.{attr.Name}";
        }        
    }
}
