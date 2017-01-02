namespace NEventStore.Serialization.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using NEventStore.Persistence.AcceptanceTests;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;
    using XunitShould;

    public class when_serializing_a_simple_message : SerializationConcern
    {
        private readonly SimpleMessage _message = new SimpleMessage().Populate();
        private SimpleMessage _deserialized;
        private byte[] _serialized;

        protected override void Context()
        {
            _serialized = Serializer.Serialize(_message);
        }

        protected override void Because()
        {
            _deserialized = Serializer.Deserialize<SimpleMessage>(_serialized);
        }

        [Fact]
        public void should_deserialize_a_message_which_contains_the_same_Id_as_the_serialized_message()
        {
            _deserialized.Id.ShouldEqual(_message.Id);
        }

        [Fact]
        public void should_deserialize_a_message_which_contains_the_same_Value_as_the_serialized_message()
        {
            _deserialized.Value.ShouldEqual(_message.Value);
        }

        [Fact]
        public void should_deserialize_a_message_which_contains_the_same_Created_value_as_the_serialized_message()
        {
            _deserialized.Created.ShouldEqual(_message.Created);
        }

        [Fact]
        public void should_deserialize_a_message_which_contains_the_same_Count_as_the_serialized_message()
        {
            _deserialized.Count.ShouldEqual(_message.Count);
        }

        [Fact]
        public void should_deserialize_a_message_which_contains_the_number_of_elements_as_the_serialized_message()
        {
            _deserialized.Contents.Count.ShouldEqual(_message.Contents.Count);
        }

        [Fact]
        public void should_deserialize_a_message_which_contains_the_same_Contents_as_the_serialized_message()
        {
            _deserialized.Contents.SequenceEqual(_message.Contents).ShouldBeTrue();
        }

        public when_serializing_a_simple_message(SerializerFixture data) : base(data)
        {}
    }

    public class when_serializing_a_list_of_event_messages : SerializationConcern
    {
        private readonly List<EventMessage> Messages = new List<EventMessage>
        {
            new EventMessage {Body = "some value"},
            new EventMessage {Body = 42},
            new EventMessage {Body = new SimpleMessage()}
        };

        private List<EventMessage> _deserialized;
        private byte[] _serialized;

        protected override void Context()
        {
            _serialized = Serializer.Serialize(Messages);
        }

        protected override void Because()
        {
            _deserialized = Serializer.Deserialize<List<EventMessage>>(_serialized);
        }

        [Fact]
        public void should_deserialize_the_same_number_of_event_messages_as_it_serialized()
        {
            Messages.Count.ShouldEqual(_deserialized.Count);
        }

        [Fact]
        public void should_deserialize_the_the_complex_types_within_the_event_messages()
        {
            _deserialized.Last().Body.ShouldBeInstanceOf<SimpleMessage>();
        }

        public when_serializing_a_list_of_event_messages(SerializerFixture data) : base(data)
        {}
    }

    public class when_serializing_a_list_of_commit_headers : SerializationConcern
    {
        private readonly Dictionary<string, object> _headers = new Dictionary<string, object>
        {
            {"HeaderKey", "SomeValue"},
            {"AnotherKey", 42},
            {"AndAnotherKey", Guid.NewGuid()},
            {"LastKey", new SimpleMessage()}
        };

        private EventMessage _message;
        private EventMessage _deserialized;
        private byte[] _serialized;

        protected override void Context()
        {
            _message = new EventMessage() {Headers = _headers};
            _serialized = Serializer.Serialize(_message);
        }

        protected override void Because()
        {
            _deserialized = Serializer.Deserialize<EventMessage>(_serialized);
        }

        [Fact]
        public void should_deserialize_the_same_number_of_event_messages_as_it_serialized()
        {
            _headers.Count.ShouldEqual(_deserialized.Headers.Count);
        }

        [Fact]
        public void should_deserialize_the_the_complex_types_within_the_event_messages()
        {
            _deserialized.Headers.Last().Value.ShouldBeInstanceOf<SimpleMessage>();
        }

        public when_serializing_a_list_of_commit_headers(SerializerFixture data) : base(data)
        {}
    }

    public class when_serializing_an_untyped_payload_on_a_snapshot : SerializationConcern
    {
        private Snapshot _deserialized;
        private TestPayLoad _payload;
        private byte[] _serialized;
        private Snapshot _snapshot;

        [Serializable]
        [DataContract(Name="testpayload")]
        public class TestPayLoad
        {
            public TestPayLoad()
                : this(new Dictionary<string, List<int>>())
            {
                
            }

            public TestPayLoad(IDictionary<string, List<int>> data)
            {
                Data = data;
            }

            [DataMember(Order=1)]
            public IDictionary<string, List<int>> Data { get; set; }
        }

        protected override void Context()
        {
            _payload = new TestPayLoad();
            _snapshot = new Snapshot(Guid.NewGuid().ToString(), 42, _payload);
            _serialized = Serializer.Serialize(_snapshot);
        }

        protected override void Because()
        {
            _deserialized = Serializer.Deserialize<Snapshot>(_serialized);
        }

        [Fact]
        public void should_correctly_deserialize_the_untyped_payload_contents()
        {
            var actual = (TestPayLoad)_deserialized.Payload;
            var expected = (TestPayLoad)_snapshot.Payload;
            actual.Data.ShouldEqual(expected.Data);
        }

        [Fact]
        public void should_correctly_deserialize_the_untyped_payload_type()
        {
            _deserialized.Payload.ShouldBeInstanceOf(_snapshot.Payload.GetType());
        }

        public when_serializing_an_untyped_payload_on_a_snapshot(SerializerFixture data) : base(data)
        {}
    }

    public class SerializationConcern : SpecificationBase2, IClassFixture<SerializerFixture>
    {
        private SerializerFixture _data;

        public ISerialize Serializer
        {
            get { return _data.Serializer; }
        }

        public SerializationConcern(SerializerFixture data)
        {
            _data = data;
            OnStart();
        }
    }

    public partial class SerializerFixture
    {
        private readonly Func<ISerialize> _createSerializer;
        private ISerialize _serializer;

        public ISerialize Serializer
        {
            get { return _serializer ?? (_serializer = _createSerializer()); }
        }
    }
}