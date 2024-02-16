namespace System.Runtime.Serialization;

public interface ISurrogateSelector
{
	void ChainSelector(ISurrogateSelector selector);

	ISerializationSurrogate? GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector);

	ISurrogateSelector? GetNextSelector();
}
