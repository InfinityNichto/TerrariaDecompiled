using System;

namespace Microsoft.Xna.Framework.Graphics;

public struct RenderTargetBinding
{
	internal Texture _renderTarget;

	internal CubeMapFace _cubeMapFace;

	public CubeMapFace CubeMapFace => _cubeMapFace;

	public Texture RenderTarget => _renderTarget;

	public RenderTargetBinding(RenderTargetCube renderTarget, CubeMapFace cubeMapFace)
	{
		if (renderTarget == null)
		{
			throw new ArgumentNullException("renderTarget", FrameworkResources.NullNotAllowed);
		}
		_renderTarget = renderTarget;
		_cubeMapFace = cubeMapFace;
	}

	public RenderTargetBinding(RenderTarget2D renderTarget)
	{
		if (renderTarget == null)
		{
			throw new ArgumentNullException("renderTarget", FrameworkResources.NullNotAllowed);
		}
		_renderTarget = renderTarget;
		_cubeMapFace = CubeMapFace.PositiveX;
	}

	public static implicit operator RenderTargetBinding(RenderTarget2D renderTarget)
	{
		return new RenderTargetBinding(renderTarget);
	}
}
