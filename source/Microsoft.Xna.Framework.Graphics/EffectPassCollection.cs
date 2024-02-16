using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class EffectPassCollection : IEnumerable<EffectPass>
{
	private unsafe ID3DXBaseEffect* pEffect;

	private List<EffectPass> pPass;

	public EffectPass this[int index]
	{
		get
		{
			if (index >= 0 && index < pPass.Count)
			{
				return pPass[index];
			}
			return null;
		}
	}

	public EffectPass this[string name]
	{
		get
		{
			List<EffectPass>.Enumerator enumerator = pPass.GetEnumerator();
			if (enumerator.MoveNext())
			{
				do
				{
					EffectPass current = enumerator.Current;
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

	public int Count => pPass.Count;

	internal unsafe EffectPassCollection(ID3DXBaseEffect* parent, EffectTechnique technique, int count)
	{
		pEffect = parent;
		base._002Ector();
		pPass = new List<EffectPass>(count);
		int num = 0;
		if (0 < count)
		{
			do
			{
				ID3DXBaseEffect* ptr = pEffect;
				sbyte* pass = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, uint, sbyte*>)(int)(*(uint*)(*(int*)ptr + 56)))((nint)ptr, technique._handle, (uint)num);
				pPass.Add(new EffectPass(pEffect, technique, pass, num));
				num++;
			}
			while (num < count);
		}
	}

	internal unsafe void UpdateParent(ID3DXBaseEffect* parent, sbyte* technique)
	{
		pEffect = parent;
		int num = 0;
		if (0 < pPass.Count)
		{
			do
			{
				ID3DXBaseEffect* ptr = pEffect;
				sbyte* handle = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, uint, sbyte*>)(int)(*(uint*)(*(int*)ptr + 56)))((nint)ptr, technique, (uint)num);
				ID3DXBaseEffect* parent2 = pEffect;
				EffectPass effectPass = pPass[num];
				effectPass.pEffect = parent2;
				effectPass._handle = handle;
				effectPass.pAnnotations.UpdateParent(parent2, handle);
				num++;
			}
			while (num < pPass.Count);
		}
	}

	public List<EffectPass>.Enumerator GetEnumerator()
	{
		return pPass.GetEnumerator();
	}

	private IEnumerator<EffectPass> GetGenericEnumerator()
	{
		return pPass.GetEnumerator();
	}

	IEnumerator<EffectPass> IEnumerable<EffectPass>.GetEnumerator()
	{
		//ILSpy generated this explicit interface implementation from .override directive in GetGenericEnumerator
		return this.GetGenericEnumerator();
	}

	private IEnumerator GetBaseEnumerator()
	{
		return pPass.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		//ILSpy generated this explicit interface implementation from .override directive in GetBaseEnumerator
		return this.GetBaseEnumerator();
	}
}
