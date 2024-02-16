using System;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class ResourceDestroyedEventArgs : EventArgs
{
	internal object _tag;

	internal string _name;

	public string Name => _name;

	public object Tag => _tag;

	internal ResourceDestroyedEventArgs(string name, object tag)
	{
		_tag = tag;
		_name = name;
	}
}
