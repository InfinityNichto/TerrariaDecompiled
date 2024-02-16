using Microsoft.Xna.Framework.Content;

namespace Microsoft.Xna.Framework.Graphics;

internal static class GraphicsContentHelper
{
	internal static GraphicsDevice GraphicsDeviceFromContentReader(ContentReader contentReader)
	{
		IGraphicsDeviceService graphicsDeviceService = (IGraphicsDeviceService)contentReader.ContentManager.ServiceProvider.GetService(typeof(IGraphicsDeviceService));
		if (graphicsDeviceService == null)
		{
			throw contentReader.CreateContentLoadException(FrameworkResources.NoGraphicsDeviceContent);
		}
		GraphicsDevice graphicsDevice = graphicsDeviceService.GraphicsDevice;
		if (graphicsDevice == null)
		{
			throw contentReader.CreateContentLoadException(FrameworkResources.NoGraphicsDeviceContent);
		}
		GraphicsProfile graphicsProfile = graphicsDevice.GraphicsProfile;
		GraphicsProfile graphicsProfile2 = (GraphicsProfile)contentReader.graphicsProfile;
		if (!IsProfileCompatible(graphicsProfile, graphicsProfile2))
		{
			throw contentReader.CreateContentLoadException(FrameworkResources.BadXnbGraphicsProfile, graphicsProfile2, graphicsProfile);
		}
		return graphicsDevice;
	}

	private static bool IsProfileCompatible(GraphicsProfile deviceProfile, GraphicsProfile contentProfile)
	{
		switch (deviceProfile)
		{
		case GraphicsProfile.Reach:
			return contentProfile == GraphicsProfile.Reach;
		case GraphicsProfile.HiDef:
			if (contentProfile != 0)
			{
				return contentProfile == GraphicsProfile.HiDef;
			}
			return true;
		default:
			return false;
		}
	}
}
