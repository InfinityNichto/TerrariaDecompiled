using System;

namespace Microsoft.Xna.Framework.Graphics;

public class PresentationParameters
{
	internal struct Settings
	{
		public int BackBufferWidth;

		public int BackBufferHeight;

		public SurfaceFormat BackBufferFormat;

		public DepthFormat DepthStencilFormat;

		public int MultiSampleCount;

		public DisplayOrientation DisplayOrientation;

		public PresentInterval PresentationInterval;

		public RenderTargetUsage RenderTargetUsage;

		public IntPtr DeviceWindowHandle;

		public int IsFullScreen;
	}

	internal Settings settings;

	public int BackBufferWidth
	{
		get
		{
			return settings.BackBufferWidth;
		}
		set
		{
			settings.BackBufferWidth = value;
		}
	}

	public int BackBufferHeight
	{
		get
		{
			return settings.BackBufferHeight;
		}
		set
		{
			settings.BackBufferHeight = value;
		}
	}

	public SurfaceFormat BackBufferFormat
	{
		get
		{
			return settings.BackBufferFormat;
		}
		set
		{
			settings.BackBufferFormat = value;
		}
	}

	public DepthFormat DepthStencilFormat
	{
		get
		{
			return settings.DepthStencilFormat;
		}
		set
		{
			settings.DepthStencilFormat = value;
		}
	}

	public int MultiSampleCount
	{
		get
		{
			return settings.MultiSampleCount;
		}
		set
		{
			settings.MultiSampleCount = value;
		}
	}

	public DisplayOrientation DisplayOrientation
	{
		get
		{
			return settings.DisplayOrientation;
		}
		set
		{
			settings.DisplayOrientation = value;
		}
	}

	public PresentInterval PresentationInterval
	{
		get
		{
			return settings.PresentationInterval;
		}
		set
		{
			settings.PresentationInterval = value;
		}
	}

	public RenderTargetUsage RenderTargetUsage
	{
		get
		{
			return settings.RenderTargetUsage;
		}
		set
		{
			settings.RenderTargetUsage = value;
		}
	}

	public IntPtr DeviceWindowHandle
	{
		get
		{
			return settings.DeviceWindowHandle;
		}
		set
		{
			settings.DeviceWindowHandle = value;
		}
	}

	public bool IsFullScreen
	{
		get
		{
			return settings.IsFullScreen != 0;
		}
		set
		{
			settings.IsFullScreen = (value ? 1 : 0);
		}
	}

	public Rectangle Bounds => new Rectangle(0, 0, settings.BackBufferWidth, settings.BackBufferHeight);

	public PresentationParameters()
	{
		IsFullScreen = true;
	}

	public PresentationParameters Clone()
	{
		PresentationParameters presentationParameters = new PresentationParameters();
		presentationParameters.settings = settings;
		return presentationParameters;
	}
}
