using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class EffectTechniqueCollection : IEnumerable<EffectTechnique>
{
	private Effect _parent;

	private unsafe ID3DXBaseEffect* pEffect;

	private List<EffectTechnique> pTechniques;

	public EffectTechnique this[int index]
	{
		get
		{
			if (index >= 0 && index < pTechniques.Count)
			{
				return pTechniques[index];
			}
			return null;
		}
	}

	public EffectTechnique this[string name]
	{
		get
		{
			List<EffectTechnique>.Enumerator enumerator = pTechniques.GetEnumerator();
			if (enumerator.MoveNext())
			{
				do
				{
					EffectTechnique current = enumerator.Current;
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

	public int Count => pTechniques.Count;

	internal unsafe EffectTechniqueCollection(ID3DXBaseEffect* parent, Effect effect, int count)
	{
		_parent = effect;
		pEffect = parent;
		base._002Ector();
		pTechniques = new List<EffectTechnique>(count);
		int num = 0;
		if (0 < count)
		{
			do
			{
				ID3DXBaseEffect* ptr = pEffect;
				sbyte* technique = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, sbyte*>)(int)(*(uint*)(*(int*)ptr + 48)))((nint)ptr, (uint)num);
				pTechniques.Add(new EffectTechnique(pEffect, _parent, technique));
				num++;
			}
			while (num < count);
		}
	}

	internal unsafe EffectTechnique GetTechniqueFromHandle(sbyte* tech)
	{
		List<EffectTechnique>.Enumerator enumerator = pTechniques.GetEnumerator();
		if (enumerator.MoveNext())
		{
			do
			{
				EffectTechnique current = enumerator.Current;
				if (current._handle == tech)
				{
					return current;
				}
			}
			while (enumerator.MoveNext());
		}
		return null;
	}

	internal unsafe void UpdateParent(ID3DXBaseEffect* parent)
	{
		pEffect = parent;
		int num = 0;
		if (0 < pTechniques.Count)
		{
			do
			{
				ID3DXBaseEffect* ptr = pEffect;
				sbyte* ptr2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, sbyte*>)(int)(*(uint*)(*(int*)ptr + 48)))((nint)ptr, (uint)num);
				ID3DXBaseEffect* parent2 = pEffect;
				EffectTechnique effectTechnique = pTechniques[num];
				effectTechnique.pEffect = parent2;
				effectTechnique._handle = ptr2;
				effectTechnique.pPasses.UpdateParent(parent2, ptr2);
				effectTechnique.pAnnotations.UpdateParent(effectTechnique.pEffect, effectTechnique._handle);
				num++;
			}
			while (num < pTechniques.Count);
		}
	}

	public List<EffectTechnique>.Enumerator GetEnumerator()
	{
		return pTechniques.GetEnumerator();
	}

	private IEnumerator<EffectTechnique> GetGenericEnumerator()
	{
		return pTechniques.GetEnumerator();
	}

	IEnumerator<EffectTechnique> IEnumerable<EffectTechnique>.GetEnumerator()
	{
		//ILSpy generated this explicit interface implementation from .override directive in GetGenericEnumerator
		return this.GetGenericEnumerator();
	}

	private IEnumerator GetBaseEnumerator()
	{
		return pTechniques.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		//ILSpy generated this explicit interface implementation from .override directive in GetBaseEnumerator
		return this.GetBaseEnumerator();
	}
}
