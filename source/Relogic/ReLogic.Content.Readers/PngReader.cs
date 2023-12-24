using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ReLogic.Content.Readers;

public class PngReader : IAssetReader, IDisposable
{
	private readonly GraphicsDevice _graphicsDevice;

	private readonly ThreadLocal<Color[]> _colorProcessingCache;

	private bool _disposedValue;

	public PngReader(GraphicsDevice graphicsDevice)
	{
		_graphicsDevice = graphicsDevice;
		_colorProcessingCache = new ThreadLocal<Color[]>();
	}

	public Type[] GetAssociatedTypes()
	{
		return new Type[1] { typeof(Texture2D) };
	}

	public async ValueTask<T> FromStream<T>(Stream stream, MainThreadCreationContext mainThreadCtx) where T : class
	{
		if (typeof(T) != typeof(Texture2D))
		{
			throw AssetLoadException.FromInvalidReader<PngReader, T>();
		}
		int width = default(int);
		int height = default(int);
		int len = default(int);
		IntPtr img = FNA3D.ReadImageStream(stream, ref width, ref height, ref len, -1, -1, false);
		PreMultiplyAlpha(img, len);
		await mainThreadCtx;
		Texture2D val = new Texture2D(_graphicsDevice, width, height);
		val.SetDataPointerEXT(0, (Rectangle?)null, img, len);
		FNA3D.FNA3D_Image_Free(img);
		return val as T;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_colorProcessingCache.Dispose();
			}
			_disposedValue = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	private unsafe static void PreMultiplyAlpha(IntPtr img, int len)
	{
		byte* colors = (byte*)img.ToPointer();
		for (int i = 0; i < len; i += 4)
		{
			int a = colors[i + 3];
			colors[i] = (byte)(colors[i] * a / 255);
			colors[i + 1] = (byte)(colors[i + 1] * a / 255);
			colors[i + 2] = (byte)(colors[i + 2] * a / 255);
		}
	}
}
