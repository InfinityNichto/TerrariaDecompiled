using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class FSharpSetConverter<TSet, TElement> : IEnumerableDefaultConverter<TSet, TElement> where TSet : IEnumerable<TElement>
{
	private readonly Func<IEnumerable<TElement>, TSet> _setConstructor;

	[RequiresUnreferencedCode("Uses Reflection to access FSharp.Core components at runtime.")]
	public FSharpSetConverter()
	{
		_setConstructor = FSharpCoreReflectionProxy.Instance.CreateFSharpSetConstructor<TSet, TElement>();
	}

	protected override void Add(in TElement value, ref ReadStack state)
	{
		((List<TElement>)state.Current.ReturnValue).Add(value);
	}

	protected override void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
	{
		state.Current.ReturnValue = new List<TElement>();
	}

	protected override void ConvertCollection(ref ReadStack state, JsonSerializerOptions options)
	{
		state.Current.ReturnValue = _setConstructor((List<TElement>)state.Current.ReturnValue);
	}
}
