using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

internal interface IGraphicsResource
{
	void ReleaseNativeObject([MarshalAs(UnmanagedType.U1)] bool disposeManagedResource);

	int SaveDataForRecreation();

	int RecreateAndPopulateObject();
}
