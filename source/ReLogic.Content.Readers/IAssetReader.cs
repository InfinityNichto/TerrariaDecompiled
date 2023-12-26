using System;
using System.IO;
using System.Threading.Tasks;

namespace ReLogic.Content.Readers;

public interface IAssetReader
{
	ValueTask<T> FromStream<T>(Stream stream, MainThreadCreationContext mainThreadCtx) where T : class
	{
		return ValueTask.FromResult(FromStream<T>(stream));
	}

	protected T FromStream<T>(Stream stream) where T : class
	{
		throw new NotImplementedException();
	}
}
