using System;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class ResourceCreatedEventArgs : EventArgs
{
	internal object _resource;

	public object Resource => _resource;

	internal ResourceCreatedEventArgs(object resource)
	{
		_resource = resource;
	}
}
