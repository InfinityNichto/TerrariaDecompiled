using System.Collections.Generic;

namespace System.Text.Json.Serialization.Metadata;

internal abstract class GenericMethodHolder
{
	public abstract object DefaultValue { get; }

	public abstract bool IsDefaultValue(object value);

	public static GenericMethodHolder CreateHolder(Type type)
	{
		Type type2 = typeof(GenericMethodHolder<>).MakeGenericType(type);
		return (GenericMethodHolder)Activator.CreateInstance(type2);
	}
}
internal sealed class GenericMethodHolder<T> : GenericMethodHolder
{
	public override object DefaultValue => default(T);

	public override bool IsDefaultValue(object value)
	{
		return EqualityComparer<T>.Default.Equals(default(T), (T)value);
	}
}
