using System;
using System.Globalization;

namespace Microsoft.Xna.Framework.Graphics;

internal static class VertexElementValidator
{
	internal static int GetVertexStride(VertexElement[] elements)
	{
		int num = 0;
		for (int i = 0; i < elements.Length; i++)
		{
			int num2 = elements[i].Offset + GetTypeSize(elements[i].VertexElementFormat);
			if (num < num2)
			{
				num = num2;
			}
		}
		return num;
	}

	internal static void Validate(int vertexStride, VertexElement[] elements)
	{
		if (vertexStride <= 0)
		{
			throw new ArgumentOutOfRangeException("vertexStride");
		}
		if (((uint)vertexStride & 3u) != 0)
		{
			throw new ArgumentException(FrameworkResources.VertexElementOffsetNotMultipleFour);
		}
		int[] array = new int[vertexStride];
		for (int i = 0; i < vertexStride; i++)
		{
			array[i] = -1;
		}
		for (int j = 0; j < elements.Length; j++)
		{
			int offset = elements[j].Offset;
			int typeSize = GetTypeSize(elements[j].VertexElementFormat);
			if (elements[j].VertexElementUsage < VertexElementUsage.Position || elements[j].VertexElementUsage > VertexElementUsage.TessellateFactor)
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, FrameworkResources.VertexElementBadUsage, new object[2]
				{
					elements[j].VertexElementUsage,
					string.Empty
				}));
			}
			if (offset < 0 || offset + typeSize > vertexStride)
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, FrameworkResources.VertexElementOutsideStride, new object[2]
				{
					elements[j].VertexElementUsage,
					elements[j].UsageIndex
				}));
			}
			if (((uint)offset & 3u) != 0)
			{
				throw new ArgumentException(FrameworkResources.VertexElementOffsetNotMultipleFour);
			}
			for (int k = 0; k < j; k++)
			{
				if (elements[j].VertexElementUsage == elements[k].VertexElementUsage && elements[j].UsageIndex == elements[k].UsageIndex)
				{
					throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, FrameworkResources.DuplicateVertexElement, new object[2]
					{
						elements[j].VertexElementUsage,
						elements[j].UsageIndex
					}));
				}
			}
			for (int l = offset; l < offset + typeSize; l++)
			{
				if (array[l] >= 0)
				{
					throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, FrameworkResources.VertexElementsOverlap, elements[array[l]].VertexElementUsage, elements[array[l]].UsageIndex, elements[j].VertexElementUsage, elements[j].UsageIndex));
				}
				array[l] = j;
			}
		}
	}

	internal static void Validate(int vertexStride, VertexElement[] elements, ProfileCapabilities profile)
	{
		Validate(vertexStride, elements);
		if (vertexStride > profile.MaxStreamStride)
		{
			profile.ThrowNotSupportedException(FrameworkResources.ProfileMaxVertexStride, profile.MaxStreamStride);
		}
		if (elements.Length > profile.MaxVertexStreams)
		{
			profile.ThrowNotSupportedException(FrameworkResources.ProfileMaxVertexElements, profile.MaxVertexStreams);
		}
		for (int i = 0; i < elements.Length; i++)
		{
			if (!profile.ValidVertexFormats.Contains(elements[i].VertexElementFormat))
			{
				profile.ThrowNotSupportedException(FrameworkResources.ProfileFormatNotSupported, typeof(VertexElement).Name, elements[i].VertexElementFormat);
			}
			if (elements[i].UsageIndex < 0 || elements[i].UsageIndex >= profile.MaxVertexStreams)
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, FrameworkResources.VertexElementBadUsage, new object[2]
				{
					elements[i].VertexElementUsage,
					elements[i].UsageIndex
				}));
			}
		}
	}

	internal static int GetTypeSize(VertexElementFormat format)
	{
		return format switch
		{
			VertexElementFormat.Single => 4, 
			VertexElementFormat.Vector2 => 8, 
			VertexElementFormat.Vector3 => 12, 
			VertexElementFormat.Vector4 => 16, 
			VertexElementFormat.Color => 4, 
			VertexElementFormat.Byte4 => 4, 
			VertexElementFormat.Short2 => 4, 
			VertexElementFormat.Short4 => 8, 
			VertexElementFormat.NormalizedShort2 => 4, 
			VertexElementFormat.NormalizedShort4 => 8, 
			VertexElementFormat.HalfVector2 => 4, 
			VertexElementFormat.HalfVector4 => 8, 
			_ => 0, 
		};
	}
}
