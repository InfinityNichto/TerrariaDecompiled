using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class EffectParameterCollection : IEnumerable<EffectParameter>
{
	private Effect _parent;

	private unsafe ID3DXBaseEffect* pEffect;

	private List<EffectParameter> pParameter;

	public EffectParameter this[int index]
	{
		get
		{
			if (index >= 0 && index < pParameter.Count)
			{
				return pParameter[index];
			}
			return null;
		}
	}

	public EffectParameter this[string name]
	{
		get
		{
			List<EffectParameter>.Enumerator enumerator = pParameter.GetEnumerator();
			if (enumerator.MoveNext())
			{
				do
				{
					EffectParameter current = enumerator.Current;
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

	public int Count => pParameter.Count;

	internal unsafe EffectParameterCollection(ID3DXBaseEffect* parent, Effect effect, sbyte* parameter, int count, [MarshalAs(UnmanagedType.U1)] bool arrayElements)
	{
		_parent = effect;
		pEffect = parent;
		base._002Ector();
		pParameter = new List<EffectParameter>(count);
		int num = 0;
		if (0 >= count)
		{
			return;
		}
		do
		{
			sbyte* handle;
			if (arrayElements)
			{
				ID3DXBaseEffect* ptr = pEffect;
				handle = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, uint, sbyte*>)(int)(*(uint*)(*(int*)ptr + 44)))((nint)ptr, parameter, (uint)num);
			}
			else
			{
				ID3DXBaseEffect* ptr = pEffect;
				handle = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, uint, sbyte*>)(int)(*(uint*)(*(int*)ptr + 32)))((nint)ptr, parameter, (uint)num);
			}
			EffectParameter effectParameter = new EffectParameter(pEffect, _parent, handle, num);
			if (effectParameter._paramClass >= EffectParameterClass.Scalar && effectParameter._paramType >= EffectParameterType.Void && effect.WantParameter(effectParameter))
			{
				pParameter.Add(effectParameter);
			}
			num++;
		}
		while (num < count);
	}

	internal unsafe void UpdateParent(ID3DXBaseEffect* parent, sbyte* parameter, [MarshalAs(UnmanagedType.U1)] bool arrayElements)
	{
		pEffect = parent;
		int num = 0;
		if (0 >= pParameter.Count)
		{
			return;
		}
		do
		{
			sbyte* handle;
			if (arrayElements)
			{
				int num2 = *(int*)pEffect + 44;
				handle = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, uint, sbyte*>)(int)(*(uint*)num2))((nint)pEffect, parameter, (uint)pParameter[num]._index);
			}
			else
			{
				int num3 = *(int*)pEffect + 32;
				handle = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, uint, sbyte*>)(int)(*(uint*)num3))((nint)pEffect, parameter, (uint)pParameter[num]._index);
			}
			pParameter[num].UpdateHandle(pEffect, handle);
			num++;
		}
		while (num < pParameter.Count);
	}

	internal void SaveDataForRecreation()
	{
		int num = 0;
		if (0 >= pParameter.Count)
		{
			return;
		}
		do
		{
			EffectParameter effectParameter = pParameter[num];
			effectParameter.pElementCollection.SaveDataForRecreation();
			effectParameter.pParamCollection.SaveDataForRecreation();
			if (effectParameter.pElementCollection.pParameter.Count <= 0)
			{
				switch (effectParameter._paramType)
				{
				case EffectParameterType.String:
					effectParameter.savedValue = effectParameter.GetValueString();
					break;
				case EffectParameterType.Bool:
				case EffectParameterType.Int32:
				case EffectParameterType.Single:
				{
					int num2 = effectParameter._columns * effectParameter._rows;
					if (num2 > 0)
					{
						effectParameter.savedValue = effectParameter.GetValueSingleArray(num2);
					}
					break;
				}
				}
			}
			num++;
		}
		while (num < pParameter.Count);
	}

	public EffectParameter GetParameterBySemantic(string semantic)
	{
		List<EffectParameter>.Enumerator enumerator = pParameter.GetEnumerator();
		if (enumerator.MoveNext())
		{
			do
			{
				EffectParameter current = enumerator.Current;
				if (string.Compare(current._semantic, semantic, StringComparison.OrdinalIgnoreCase) == 0)
				{
					return current;
				}
			}
			while (enumerator.MoveNext());
		}
		return null;
	}

	public List<EffectParameter>.Enumerator GetEnumerator()
	{
		return pParameter.GetEnumerator();
	}

	private IEnumerator<EffectParameter> GetGenericEnumerator()
	{
		return pParameter.GetEnumerator();
	}

	IEnumerator<EffectParameter> IEnumerable<EffectParameter>.GetEnumerator()
	{
		//ILSpy generated this explicit interface implementation from .override directive in GetGenericEnumerator
		return this.GetGenericEnumerator();
	}

	private IEnumerator GetBaseEnumerator()
	{
		return pParameter.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		//ILSpy generated this explicit interface implementation from .override directive in GetBaseEnumerator
		return this.GetBaseEnumerator();
	}
}
