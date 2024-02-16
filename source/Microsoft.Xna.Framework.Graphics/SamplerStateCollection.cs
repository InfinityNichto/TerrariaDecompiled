using System;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class SamplerStateCollection
{
	private GraphicsDevice pDevice;

	private SamplerState[] pSamplerList;

	private int samplerOffset;

	public SamplerState this[int index]
	{
		get
		{
			if (index >= 0)
			{
				SamplerState[] array = pSamplerList;
				if (index < (nint)array.LongLength)
				{
					return array[index];
				}
			}
			throw new ArgumentOutOfRangeException("index");
		}
		set
		{
			if (index >= 0)
			{
				SamplerState[] array = pSamplerList;
				if (index < (nint)array.LongLength)
				{
					if (value == null)
					{
						throw new ArgumentNullException("value", FrameworkResources.NullNotAllowed);
					}
					if (value != array[index])
					{
						value.Apply(pDevice, samplerOffset + index);
						pSamplerList[index] = value;
					}
					return;
				}
			}
			throw new ArgumentOutOfRangeException("index");
		}
	}

	internal SamplerStateCollection(GraphicsDevice pParent, int samplerOffset, int maxSamplers)
	{
		pDevice = pParent;
		this.samplerOffset = samplerOffset;
		base._002Ector();
		pSamplerList = new SamplerState[maxSamplers];
	}

	internal void InitializeDeviceState()
	{
		int num = 0;
		SamplerState[] array = pSamplerList;
		if (0 >= (nint)array.LongLength)
		{
			return;
		}
		while (true)
		{
			array[num] = null;
			SamplerState linearWrap = SamplerState.LinearWrap;
			if (num < 0)
			{
				break;
			}
			array = pSamplerList;
			if (num >= (nint)array.LongLength)
			{
				break;
			}
			if (linearWrap != null)
			{
				if (linearWrap != array[num])
				{
					linearWrap.Apply(pDevice, samplerOffset + num);
					pSamplerList[num] = linearWrap;
				}
				num++;
				array = pSamplerList;
				if (num >= (nint)array.LongLength)
				{
					return;
				}
				continue;
			}
			throw new ArgumentNullException("value", FrameworkResources.NullNotAllowed);
		}
		throw new ArgumentOutOfRangeException("index");
	}

	internal void ClearState(int index)
	{
		if (index >= 0)
		{
			SamplerState[] array = pSamplerList;
			if (index < (nint)array.LongLength)
			{
				array[index] = null;
			}
		}
	}
}
