using System.Globalization;

namespace Microsoft.Xna.Framework.Graphics;

public class DisplayMode
{
	internal int _width;

	internal int _height;

	internal SurfaceFormat _format;

	public SurfaceFormat Format => _format;

	public int Height => _height;

	public int Width => _width;

	public float AspectRatio
	{
		get
		{
			if (_height == 0 || _width == 0)
			{
				return 0f;
			}
			return (float)_width / (float)_height;
		}
	}

	public Rectangle TitleSafeArea => Viewport.GetTitleSafeArea(0, 0, _width, _height);

	internal DisplayMode(int width, int height, SurfaceFormat format)
	{
		_width = width;
		_height = height;
		_format = format;
	}

	public override string ToString()
	{
		return string.Format(CultureInfo.CurrentCulture, "{{Width:{0} Height:{1} Format:{2} AspectRatio:{3}}}", _width, _height, Format, AspectRatio);
	}
}
