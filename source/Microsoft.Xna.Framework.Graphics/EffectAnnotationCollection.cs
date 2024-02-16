using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class EffectAnnotationCollection : IEnumerable<EffectAnnotation>
{
	private unsafe ID3DXBaseEffect* pEffect;

	private List<EffectAnnotation> pAnnotation;

	public EffectAnnotation this[int index]
	{
		get
		{
			if (index >= 0 && index < pAnnotation.Count)
			{
				return pAnnotation[index];
			}
			return null;
		}
	}

	public EffectAnnotation this[string name]
	{
		get
		{
			List<EffectAnnotation>.Enumerator enumerator = pAnnotation.GetEnumerator();
			if (enumerator.MoveNext())
			{
				do
				{
					EffectAnnotation current = enumerator.Current;
					if (current._name == name)
					{
						return current;
					}
				}
				while (enumerator.MoveNext());
			}
			return null;
		}
	}

	public int Count => pAnnotation.Count;

	internal unsafe EffectAnnotationCollection(ID3DXBaseEffect* parent, sbyte* parentHandle, int count)
	{
		pEffect = parent;
		base._002Ector();
		pAnnotation = new List<EffectAnnotation>(count);
		int num = 0;
		if (0 >= count)
		{
			return;
		}
		do
		{
			ID3DXBaseEffect* ptr = pEffect;
			sbyte* handle = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, uint, sbyte*>)(int)(*(uint*)(*(int*)ptr + 72)))((nint)ptr, parentHandle, (uint)num);
			EffectAnnotation effectAnnotation = new EffectAnnotation(pEffect, handle);
			if (effectAnnotation._paramClass >= EffectParameterClass.Scalar && effectAnnotation._paramType >= EffectParameterType.Void)
			{
				pAnnotation.Add(effectAnnotation);
			}
			num++;
		}
		while (num < count);
	}

	internal unsafe void UpdateParent(ID3DXBaseEffect* parent, sbyte* handle)
	{
		pEffect = parent;
		int num = 0;
		if (0 < pAnnotation.Count)
		{
			do
			{
				ID3DXBaseEffect* ptr = pEffect;
				sbyte* handle2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, uint, sbyte*>)(int)(*(uint*)(*(int*)ptr + 72)))((nint)ptr, handle, (uint)num);
				ID3DXBaseEffect* ptr2 = pEffect;
				EffectAnnotation effectAnnotation = pAnnotation[num];
				effectAnnotation.pEffect = ptr2;
				effectAnnotation._handle = handle2;
				num++;
			}
			while (num < pAnnotation.Count);
		}
	}

	public List<EffectAnnotation>.Enumerator GetEnumerator()
	{
		return pAnnotation.GetEnumerator();
	}

	private IEnumerator<EffectAnnotation> GetGenericEnumerator()
	{
		return pAnnotation.GetEnumerator();
	}

	IEnumerator<EffectAnnotation> IEnumerable<EffectAnnotation>.GetEnumerator()
	{
		//ILSpy generated this explicit interface implementation from .override directive in GetGenericEnumerator
		return this.GetGenericEnumerator();
	}

	private IEnumerator GetBaseEnumerator()
	{
		return pAnnotation.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		//ILSpy generated this explicit interface implementation from .override directive in GetBaseEnumerator
		return this.GetBaseEnumerator();
	}
}
