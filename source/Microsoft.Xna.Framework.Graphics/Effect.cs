using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Xna.Framework.Graphics;

public class Effect : GraphicsResource, IGraphicsResource
{
	private EffectTechniqueCollection pTechniqueCollection;

	private EffectParameterCollection pParamCollection;

	internal byte[] pCachedEffectData;

	internal WeakReference pParentEffect;

	internal List<WeakReference> pClonedEffects;

	internal EffectTechnique _currentTechnique;

	internal static object pSyncObject = new object();

	internal unsafe ID3DXEffect* pComPtr;

	public EffectParameterCollection Parameters => pParamCollection;

	public EffectTechniqueCollection Techniques => pTechniqueCollection;

	public unsafe EffectTechnique CurrentTechnique
	{
		get
		{
			return _currentTechnique;
		}
		set
		{
			IntPtr intPtr = (IntPtr)pComPtr;
			Helpers.CheckDisposed(this, intPtr);
			if (value == null)
			{
				throw new ArgumentNullException("value", FrameworkResources.NullNotAllowed);
			}
			if (value != _currentTechnique)
			{
				if (value._parent != this)
				{
					throw new InvalidOperationException();
				}
				EffectPass activePass = _parent.activePass;
				if (activePass != null)
				{
					activePass.EndPass();
					_parent.activePass = null;
				}
				ID3DXEffect* ptr = pComPtr;
				int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, int>)(int)(*(uint*)(*(int*)ptr + 232)))((nint)ptr, value._handle);
				if (num < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num);
				}
				_currentTechnique = value;
			}
		}
	}

	private unsafe void InitializeHelpers()
	{
		IntPtr intPtr = (IntPtr)pComPtr;
		Helpers.CheckDisposed(this, intPtr);
		ID3DXEffect* ptr = pComPtr;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DXEFFECT_DESC d3DXEFFECT_DESC);
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DXEFFECT_DESC*, int>)(int)(*(uint*)(*(int*)ptr + 12)))((nint)ptr, &d3DXEFFECT_DESC);
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
		EffectTechniqueCollection effectTechniqueCollection = pTechniqueCollection;
		if (effectTechniqueCollection != null && pParamCollection != null)
		{
			effectTechniqueCollection.UpdateParent((ID3DXBaseEffect*)pComPtr);
			pParamCollection.UpdateParent((ID3DXBaseEffect*)pComPtr, null, arrayElements: false);
		}
		else
		{
			pTechniqueCollection = new EffectTechniqueCollection((ID3DXBaseEffect*)pComPtr, this, System.Runtime.CompilerServices.Unsafe.As<_D3DXEFFECT_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXEFFECT_DESC, 8)));
			pParamCollection = new EffectParameterCollection((ID3DXBaseEffect*)pComPtr, this, null, System.Runtime.CompilerServices.Unsafe.As<_D3DXEFFECT_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXEFFECT_DESC, 4)), arrayElements: false);
		}
	}

	private unsafe void CreateEffectFromCode(GraphicsDevice graphicsDevice, byte[] effectCode)
	{
		//The blocks IL_0203 are reachable both inside and outside the pinned region starting at IL_0057. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_0203 are reachable both inside and outside the pinned region starting at IL_00ac. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		if (effectCode != null)
		{
			int num = effectCode.Length;
			if (num != 0)
			{
				if (num % 4 != 0)
				{
					string text = "effectCode";
					throw new ArgumentException(string.Format(FrameworkResources.ArrayMultipleFour, text), text);
				}
				if (graphicsDevice == null)
				{
					throw new ArgumentNullException("graphicsDevice", FrameworkResources.DeviceCannotBeNullOnResourceCreate);
				}
				if ((uint)num >= 8u)
				{
					ID3DXBuffer* ptr = null;
					fixed (void* ptr2 = &System.Runtime.CompilerServices.Unsafe.As<byte, void>(ref effectCode[0]))
					{
						uint* ptr3 = (uint*)ptr2;
						if (*ptr3 == 3169848271u)
						{
							ptr3++;
							uint num2 = *ptr3;
							if ((nint)effectCode.LongLength >= (int)(num2 + 4))
							{
								void* ptr4 = System.Runtime.CompilerServices.Unsafe.AsPointer(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref *ptr2, num2));
								if (*(int*)ptr4 == -16840447)
								{
									StateTrackerDevice* pStateTracker = graphicsDevice.pStateTracker;
									*(int*)((byte*)pStateTracker + 92) = 0;
									*(int*)((byte*)pStateTracker + 96) = 0;
									*(int*)((byte*)pStateTracker + 100) = 0;
									fixed (ID3DXEffect** ptr5 = &pComPtr)
									{
										int num3 = _003CModule_003E.D3DXCreateEffectEx((IDirect3DDevice9*)pStateTracker, ptr4, (uint)((nint)effectCode.LongLength - (int)num2), null, null, null, 131072u, null, ptr5, &ptr);
										if (ptr != null)
										{
											ID3DXBuffer* intPtr = ptr;
											((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)intPtr + 8)))((nint)intPtr);
											ptr = null;
										}
										uint num4 = *(uint*)((byte*)pStateTracker + 92);
										if (num4 != 0)
										{
											graphicsDevice._profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileVertexShaderModel, FormatShaderModel(num4));
										}
										uint num5 = *(uint*)((byte*)pStateTracker + 96);
										if (num5 != 0)
										{
											graphicsDevice._profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfilePixelShaderModel, FormatShaderModel(num5));
										}
										if (num3 < 0)
										{
											throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
										}
										int num6 = *(int*)((byte*)pStateTracker + 100);
										if (num6 < 0)
										{
											throw GraphicsHelpers.GetExceptionFromResult((uint)num6);
										}
										_parent = graphicsDevice;
										pCachedEffectData = effectCode;
										graphicsDevice.Resources.AddTrackedObject(this, pComPtr, 0u, _internalHandle, ref _internalHandle);
										InitializeHelpers();
										CurrentTechnique = pTechniqueCollection[0];
										List<EffectTechnique>.Enumerator enumerator = pTechniqueCollection.GetEnumerator();
										if (!enumerator.MoveNext())
										{
											return;
										}
										do
										{
											List<EffectPass>.Enumerator enumerator2 = enumerator.Current.pPasses.GetEnumerator();
											if (!enumerator2.MoveNext())
											{
												continue;
											}
											uint* ptr6 = ptr3 + 1;
											do
											{
												EffectPass current = enumerator2.Current;
												if (ptr6 < ptr4)
												{
													ptr3++;
													ptr6++;
													current._stateFlags = (EffectStateFlags)(*ptr3);
													ptr3++;
													ptr6++;
													current._textureFlags = *ptr3;
													continue;
												}
												throw new InvalidOperationException(FrameworkResources.MustUserShaderCode);
											}
											while (enumerator2.MoveNext());
										}
										while (enumerator.MoveNext());
										return;
									}
								}
							}
						}
						throw new InvalidOperationException(FrameworkResources.MustUserShaderCode);
					}
				}
				throw new InvalidOperationException(FrameworkResources.MustUserShaderCode);
			}
		}
		throw new ArgumentNullException("effectCode", FrameworkResources.NullNotAllowed);
	}

	private static string FormatShaderModel(uint shaderModel)
	{
		if (shaderModel == 513)
		{
			return "2.x";
		}
		return string.Format(args: new object[2]
		{
			shaderModel >> 8,
			shaderModel & 0xFFu
		}, provider: CultureInfo.CurrentCulture, format: "{0}.{1}");
	}

	private unsafe Effect(ID3DXEffect* pInterface, GraphicsDevice pDevice)
	{
		pComPtr = pInterface;
		((object)this)._002Ector();
		try
		{
			_parent = pDevice;
			InitializeHelpers();
			return;
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
	}

	public Effect(GraphicsDevice graphicsDevice, byte[] effectCode)
	{
		try
		{
			CreateEffectFromCode(graphicsDevice, effectCode);
			return;
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
	}

	protected unsafe Effect(Effect cloneSource)
	{
		try
		{
			if (cloneSource == null)
			{
				throw new ArgumentNullException("cloneSource", FrameworkResources.NullNotAllowed);
			}
			IntPtr intPtr = (IntPtr)cloneSource.pComPtr;
			Helpers.CheckDisposed(cloneSource, intPtr);
			GraphicsDevice parent = cloneSource._parent;
			if (parent == null)
			{
				throw new ArgumentNullException("graphicsDevice", FrameworkResources.DeviceCannotBeNullOnResourceCreate);
			}
			ID3DXEffect* ptr = null;
			ID3DXEffect* ptr2 = cloneSource.pComPtr;
			int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, IDirect3DDevice9*, ID3DXEffect**, int>)(int)(*(uint*)(*(int*)ptr2 + 308)))((nint)ptr2, (IDirect3DDevice9*)parent.pStateTracker, &ptr);
			if (num < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num);
			}
			_parent = parent;
			pComPtr = ptr;
			pParentEffect = new WeakReference(cloneSource);
			cloneSource.AddClonedEffect(this);
			parent.Resources.AddTrackedObject(this, ptr, 0u, _internalHandle, ref _internalHandle);
			InitializeHelpers();
			CurrentTechnique = pTechniqueCollection[0];
			int num2 = 0;
			if (0 >= pParamCollection.Count)
			{
				return;
			}
			do
			{
				EffectParameter effectParameter = pParamCollection[num2];
				if (effectParameter.ElementCount > 1)
				{
					EffectParameterType paramType = effectParameter._paramType;
					EffectParameterType effectParameterType = paramType;
					if (effectParameterType != EffectParameterType.Bool)
					{
						EffectParameterType effectParameterType2 = paramType;
						if (effectParameterType2 != EffectParameterType.Int32)
						{
							EffectParameterType effectParameterType3 = paramType;
							if (effectParameterType3 != EffectParameterType.Single)
							{
								goto IL_015f;
							}
						}
					}
					int rows = effectParameter._rows;
					int columns = effectParameter._columns;
					int num3 = effectParameter.ElementCount * columns * rows;
					if (num3 > 0)
					{
						float[] valueSingleArray = cloneSource.pParamCollection[num2].GetValueSingleArray(num3);
						effectParameter.SetValue(valueSingleArray);
					}
				}
				goto IL_015f;
				IL_015f:
				num2++;
			}
			while (num2 < pParamCollection.Count);
			return;
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
	}

	public virtual Effect Clone()
	{
		return new Effect(this);
	}

	protected internal virtual void OnApply()
	{
	}

	internal unsafe void OnLostDevice()
	{
		IntPtr intPtr = (IntPtr)pComPtr;
		Helpers.CheckDisposed(this, intPtr);
		ID3DXEffect* intPtr2 = pComPtr;
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr2 + 276)))((nint)intPtr2);
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
	}

	internal unsafe void OnResetDevice()
	{
		IntPtr intPtr = (IntPtr)pComPtr;
		Helpers.CheckDisposed(this, intPtr);
		ID3DXEffect* intPtr2 = pComPtr;
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr2 + 280)))((nint)intPtr2);
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
	}

	internal void AddClonedEffect(Effect effect)
	{
		if (pClonedEffects == null)
		{
			pClonedEffects = new List<WeakReference>();
		}
		pClonedEffects.Add(new WeakReference(effect));
	}

	internal virtual int SaveDataForRecreation()
	{
		pParamCollection.SaveDataForRecreation();
		ReleaseNativeObject(disposeManagedResource: false);
		return 0;
	}

	int IGraphicsResource.SaveDataForRecreation()
	{
		//ILSpy generated this explicit interface implementation from .override directive in SaveDataForRecreation
		return this.SaveDataForRecreation();
	}

	internal unsafe virtual int RecreateAndPopulateObject()
	{
		if (pComPtr != null)
		{
			return 0;
		}
		GraphicsDevice parent = _parent;
		if (parent == null)
		{
			return -2147467259;
		}
		int num = 0;
		WeakReference weakReference = pParentEffect;
		if (weakReference == null)
		{
			CreateEffectFromCode(parent, pCachedEffectData);
		}
		else if (weakReference.Target is Effect { pComPtr: var ptr } && ptr != null)
		{
			ID3DXEffect* ptr2 = null;
			num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, IDirect3DDevice9*, ID3DXEffect**, int>)(int)(*(uint*)(*(int*)ptr + 308)))((nint)ptr, (IDirect3DDevice9*)_parent.pStateTracker, &ptr2);
			pComPtr = ptr2;
			_parent.Resources.AddTrackedObject(this, ptr2, 0u, _internalHandle, ref _internalHandle);
			InitializeHelpers();
		}
		List<WeakReference> list = pClonedEffects;
		if (list != null)
		{
			int num2 = 0;
			if (0 < list.Count)
			{
				do
				{
					if (num >= 0 && pClonedEffects[num2].Target is Effect effect2 && effect2.pComPtr == null)
					{
						num = effect2.RecreateAndPopulateObject();
					}
					num2++;
				}
				while (num2 < pClonedEffects.Count);
			}
		}
		EffectTechnique currentTechnique = _currentTechnique;
		if (currentTechnique != null && pComPtr != null)
		{
			EffectTechnique currentTechnique2 = currentTechnique;
			_currentTechnique = null;
			CurrentTechnique = currentTechnique2;
		}
		return num;
	}

	int IGraphicsResource.RecreateAndPopulateObject()
	{
		//ILSpy generated this explicit interface implementation from .override directive in RecreateAndPopulateObject
		return this.RecreateAndPopulateObject();
	}

	internal unsafe virtual void ReleaseNativeObject([MarshalAs(UnmanagedType.U1)] bool disposeManagedResource)
	{
		bool lockTaken = false;
		try
		{
			Monitor.Enter(pSyncObject, ref lockTaken);
			GraphicsDevice parent = _parent;
			if (parent != null && pComPtr != null)
			{
				parent.Resources.ReleaseAllReferences(_internalHandle, disposeManagedResource);
			}
			pComPtr = null;
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(pSyncObject);
			}
		}
	}

	void IGraphicsResource.ReleaseNativeObject([MarshalAs(UnmanagedType.U1)] bool disposeManagedResource)
	{
		//ILSpy generated this explicit interface implementation from .override directive in ReleaseNativeObject
		this.ReleaseNativeObject(disposeManagedResource);
	}

	[return: MarshalAs(UnmanagedType.U1)]
	internal virtual bool WantParameter(EffectParameter P_0)
	{
		return true;
	}

	internal unsafe static Effect GetManagedObject(ID3DXEffect* pInterface, GraphicsDevice pDevice, uint pool)
	{
		Effect effect = pDevice.Resources.GetCachedObject(pInterface) as Effect;
		if (effect != null)
		{
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)pInterface + 8)))((nint)pInterface);
			effect.isDisposed = false;
			GC.ReRegisterForFinalize(effect);
		}
		else
		{
			effect = new Effect(pInterface, pDevice);
			pDevice.Resources.AddTrackedObject(effect, pInterface, pool, 0uL, ref effect._internalHandle);
		}
		return effect;
	}

	private void OnObjectCreation()
	{
		InitializeHelpers();
	}

	private void _0021Effect()
	{
		if (!isDisposed)
		{
			isDisposed = true;
			ReleaseNativeObject(disposeManagedResource: true);
		}
	}

	private void _007EEffect()
	{
		_0021Effect();
	}

	[HandleProcessCorruptedStateExceptions]
	protected override void Dispose([MarshalAs(UnmanagedType.U1)] bool P_0)
	{
		if (P_0)
		{
			try
			{
				_007EEffect();
				return;
			}
			finally
			{
				base.Dispose(true);
			}
		}
		try
		{
			_0021Effect();
		}
		finally
		{
			base.Dispose(false);
		}
	}
}
