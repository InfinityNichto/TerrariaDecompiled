using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

internal class SavedDeviceState
{
	private GraphicsDevice device;

	private VertexBufferBinding[] vertexBuffers;

	private IndexBuffer indices;

	private Texture[] textures;

	private SamplerState[] samplerStates;

	private Texture[] vertexTextures;

	private SamplerState[] vertexSamplerStates;

	private BlendState blendState;

	private Color blendFactor;

	private int multiSampleMask;

	private DepthStencilState depthStencilState;

	private int referenceStencil;

	private RasterizerState rasterizerState;

	private Viewport? viewport;

	private Rectangle? scissorRectangle;

	public SavedDeviceState(GraphicsDevice device, [MarshalAs(UnmanagedType.U1)] bool saveViewport)
	{
		this.device = device;
		base._002Ector();
		vertexBuffers = device.GetVertexBuffers();
		indices = device.Indices;
		int maxSamplers = device._profileCapabilities.MaxSamplers;
		textures = new Texture[maxSamplers];
		samplerStates = new SamplerState[maxSamplers];
		int num = 0;
		if (0 < maxSamplers)
		{
			do
			{
				textures[num] = device.Textures[num];
				samplerStates[num] = device.SamplerStates[num];
				num++;
			}
			while (num < maxSamplers);
		}
		int maxVertexSamplers = device._profileCapabilities.MaxVertexSamplers;
		vertexTextures = new Texture[maxVertexSamplers];
		vertexSamplerStates = new SamplerState[maxVertexSamplers];
		int num2 = 0;
		if (0 < maxVertexSamplers)
		{
			do
			{
				vertexTextures[num2] = device.VertexTextures[num2];
				vertexSamplerStates[num2] = device.VertexSamplerStates[num2];
				num2++;
			}
			while (num2 < maxVertexSamplers);
		}
		blendState = device.BlendState;
		Color color = device.BlendFactor;
		blendFactor = color;
		multiSampleMask = device.MultiSampleMask;
		depthStencilState = device.DepthStencilState;
		referenceStencil = device.ReferenceStencil;
		rasterizerState = device.RasterizerState;
		if (saveViewport)
		{
			Viewport? viewport = device.Viewport;
			this.viewport = viewport;
			Rectangle? rectangle = device.ScissorRectangle;
			scissorRectangle = rectangle;
		}
	}

	public void Restore()
	{
		device.SetVertexBuffers(vertexBuffers);
		device.Indices = indices;
		int num = 0;
		GraphicsDevice graphicsDevice = device;
		if (0 < graphicsDevice._profileCapabilities.MaxSamplers)
		{
			do
			{
				Texture[] array = textures;
				if (array[num] != null)
				{
					graphicsDevice.Textures[num] = array[num];
				}
				SamplerState[] array2 = samplerStates;
				if (array2[num] != null)
				{
					device.SamplerStates[num] = array2[num];
				}
				num++;
				graphicsDevice = device;
			}
			while (num < graphicsDevice._profileCapabilities.MaxSamplers);
		}
		int num2 = 0;
		GraphicsDevice graphicsDevice2 = device;
		if (0 < graphicsDevice2._profileCapabilities.MaxVertexSamplers)
		{
			do
			{
				Texture[] array3 = vertexTextures;
				if (array3[num2] != null)
				{
					graphicsDevice2.VertexTextures[num2] = array3[num2];
				}
				SamplerState[] array4 = vertexSamplerStates;
				if (array4[num2] != null)
				{
					device.VertexSamplerStates[num2] = array4[num2];
				}
				num2++;
				graphicsDevice2 = device;
			}
			while (num2 < graphicsDevice2._profileCapabilities.MaxVertexSamplers);
		}
		BlendState blendState = this.blendState;
		if (blendState != null)
		{
			device.BlendState = blendState;
			device.BlendFactor = blendFactor;
			device.MultiSampleMask = multiSampleMask;
		}
		DepthStencilState depthStencilState = this.depthStencilState;
		if (depthStencilState != null)
		{
			device.DepthStencilState = depthStencilState;
			device.ReferenceStencil = referenceStencil;
		}
		RasterizerState rasterizerState = this.rasterizerState;
		if (rasterizerState != null)
		{
			device.RasterizerState = rasterizerState;
		}
		if (viewport.HasValue)
		{
			Viewport value = viewport.Value;
			device.Viewport = value;
		}
		if (scissorRectangle.HasValue)
		{
			Rectangle value2 = scissorRectangle.Value;
			device.ScissorRectangle = value2;
		}
	}
}
