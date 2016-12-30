// ReSharper disable CheckNamespace
namespace NEventStore.Serialization.AcceptanceTests
// ReSharper restore CheckNamespace
{
    using NEventStore.Persistence.AcceptanceTests;
    using NEventStore.Serialization;
    using ProtoBuf.Meta;

    public partial class SerializerFixture
    {
        public SerializerFixture()
        {
            _createSerializer = () =>
                new ProtobufSerializer();
            
            RuntimeTypeModel.Default[typeof(SimpleMessage)][5].SupportNull = true;
        }
    }
}