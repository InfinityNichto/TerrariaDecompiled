using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Graphics;

public class DisplayModeCollection : IEnumerable<DisplayMode>, IEnumerable
{
	private List<DisplayMode> _displayModes;

	public IEnumerable<DisplayMode> this[SurfaceFormat format]
	{
		get
		{
			List<DisplayMode> list = new List<DisplayMode>();
			foreach (DisplayMode displayMode in _displayModes)
			{
				if (displayMode.Format == format)
				{
					list.Add(displayMode);
				}
			}
			return list;
		}
	}

	internal DisplayModeCollection(List<DisplayMode> displayModes)
	{
		_displayModes = displayModes;
	}

	public IEnumerator<DisplayMode> GetEnumerator()
	{
		return _displayModes.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
