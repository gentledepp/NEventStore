 
 
using System;
using System.Collections.Generic;

namespace NEventStore.Serialization.AcceptanceTests 
{
	public class KnownTypesProvider
	{
		public static IEnumerable<Type> Provide() 
		{
			yield return typeof(NEventStore.Serialization.AcceptanceTests.when_serializing_an_untyped_payload_on_a_snapshot.TestPayLoad);
			yield return typeof(NEventStore.Persistence.AcceptanceTests.SimpleMessage);

			yield break;
		}
	}
}
