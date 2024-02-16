using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization;

internal static class SerializationEventsCache
{
	private static readonly ConcurrentDictionary<Type, SerializationEvents> s_cache = new ConcurrentDictionary<Type, SerializationEvents>();

	internal static SerializationEvents GetSerializationEventsForType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type t)
	{
		return s_cache.GetOrAdd(t, (Type type) => CreateSerializationEvents(type));
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern", Justification = "The Type is annotated correctly, it just can't pass through the lambda method.")]
	private static SerializationEvents CreateSerializationEvents(Type t)
	{
		return new SerializationEvents(t);
	}
}
