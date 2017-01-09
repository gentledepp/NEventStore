// ReSharper disable CheckNamespace
namespace NEventStore.Serialization.AcceptanceTests
// ReSharper restore CheckNamespace
{
    using System.Linq;
    using NEventStore.Persistence.AcceptanceTests;
    using NEventStore.Serialization;
    using ProtoBuf.Meta;

    public partial class SerializerFixture
    {
        public SerializerFixture()
        {
            var knownTypes = KnownTypesProvider.Provide();

            _createSerializer = () =>
                new ProtobufSerializer(knownTypes.ToArray());

            RuntimeTypeModel.Default[typeof(SimpleMessage)][5].SupportNull = true;
        }
    }
}