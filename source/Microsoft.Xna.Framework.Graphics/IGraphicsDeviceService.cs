using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Xna.Framework.Graphics;

public interface IGraphicsDeviceService
{
	GraphicsDevice GraphicsDevice { get; }

	[SpecialName]
	event EventHandler<EventArgs> DeviceDisposing;

	[SpecialName]
	event EventHandler<EventArgs> DeviceReset;

	[SpecialName]
	event EventHandler<EventArgs> DeviceResetting;

	[SpecialName]
	event EventHandler<EventArgs> DeviceCreated;
}
