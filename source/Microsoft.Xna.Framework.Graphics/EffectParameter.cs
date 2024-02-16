using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class EffectParameter
{
	internal EffectAnnotationCollection pAnnotations;

	internal Effect _parent;

	internal unsafe ID3DXBaseEffect* pEffect;

	internal unsafe sbyte* _handle;

	internal int _index;

	internal string _name;

	internal string _semantic;

	internal int _rows;

	internal int _columns;

	internal EffectParameterClass _paramClass;

	internal EffectParameterType _paramType;

	internal EffectParameterCollection pParamCollection;

	internal EffectParameterCollection pElementCollection;

	internal object savedValue;

	public EffectParameterType ParameterType => _paramType;

	public EffectParameterClass ParameterClass => _paramClass;

	public EffectParameterCollection Elements => pElementCollection;

	public EffectParameterCollection StructureMembers => pParamCollection;

	public int ColumnCount => _columns;

	public int RowCount => _rows;

	public EffectAnnotationCollection Annotations => pAnnotations;

	public string Semantic => _semantic;

	public string Name => _name;

	internal int ElementCount => pElementCollection.Count;

	internal unsafe EffectParameter(ID3DXBaseEffect* parent, Effect effect, sbyte* handle, int index)
	{
		_parent = effect;
		pEffect = parent;
		_handle = handle;
		_index = index;
		base._002Ector();
		ID3DXBaseEffect* ptr = pEffect;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DXPARAMETER_DESC d3DXPARAMETER_DESC);
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, _D3DXPARAMETER_DESC*, int>)(int)(*(uint*)(*(int*)ptr + 16)))((nint)ptr, _handle, &d3DXPARAMETER_DESC);
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
		IntPtr ptr2 = (IntPtr)(void*)(int)(*(uint*)(&d3DXPARAMETER_DESC));
		_name = Marshal.PtrToStringAnsi(ptr2);
		_semantic = ((System.Runtime.CompilerServices.Unsafe.As<_D3DXPARAMETER_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPARAMETER_DESC, 4)) == 0) ? string.Empty : Marshal.PtrToStringAnsi((IntPtr)(void*)(int)System.Runtime.CompilerServices.Unsafe.As<_D3DXPARAMETER_DESC, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPARAMETER_DESC, 4))));
		_rows = System.Runtime.CompilerServices.Unsafe.As<_D3DXPARAMETER_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPARAMETER_DESC, 16));
		_columns = System.Runtime.CompilerServices.Unsafe.As<_D3DXPARAMETER_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPARAMETER_DESC, 20));
		_paramClass = _003CModule_003E.ConvertDxParameterClassToXna(System.Runtime.CompilerServices.Unsafe.As<_D3DXPARAMETER_DESC, _D3DXPARAMETER_CLASS>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPARAMETER_DESC, 8)));
		_paramType = _003CModule_003E.ConvertDxParameterTypeToXna(System.Runtime.CompilerServices.Unsafe.As<_D3DXPARAMETER_DESC, _D3DXPARAMETER_TYPE>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPARAMETER_DESC, 12)));
		pAnnotations = new EffectAnnotationCollection(pEffect, _handle, System.Runtime.CompilerServices.Unsafe.As<_D3DXPARAMETER_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPARAMETER_DESC, 28)));
		uint count = ((System.Runtime.CompilerServices.Unsafe.As<_D3DXPARAMETER_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPARAMETER_DESC, 24)) == 0) ? System.Runtime.CompilerServices.Unsafe.As<_D3DXPARAMETER_DESC, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPARAMETER_DESC, 32)) : 0u);
		pParamCollection = new EffectParameterCollection(parent, effect, _handle, (int)count, arrayElements: false);
		pElementCollection = new EffectParameterCollection(parent, effect, _handle, System.Runtime.CompilerServices.Unsafe.As<_D3DXPARAMETER_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPARAMETER_DESC, 24)), arrayElements: true);
	}

	internal unsafe void UpdateHandle(ID3DXBaseEffect* parent, sbyte* handle)
	{
		pEffect = parent;
		_handle = handle;
		pAnnotations.UpdateParent(parent, handle);
		pParamCollection.UpdateParent(parent, handle, arrayElements: false);
		pElementCollection.UpdateParent(parent, handle, arrayElements: true);
		SetLastValue();
	}

	internal void SetLastValue()
	{
		object obj = savedValue;
		if (obj == null)
		{
			return;
		}
		if (obj is float[] value)
		{
			SetValue(value);
			savedValue = null;
			return;
		}
		if (obj is string value2)
		{
			SetValue(value2);
			savedValue = null;
			return;
		}
		if (obj is Texture value3)
		{
			SetValue(value3);
			return;
		}
		savedValue = null;
		throw new NotSupportedException();
	}

	internal void SaveDataForRecreation()
	{
		pElementCollection.SaveDataForRecreation();
		pParamCollection.SaveDataForRecreation();
		if (pElementCollection.Count > 0)
		{
			return;
		}
		switch (_paramType)
		{
		case EffectParameterType.String:
			savedValue = GetValueString();
			break;
		case EffectParameterType.Bool:
		case EffectParameterType.Int32:
		case EffectParameterType.Single:
		{
			int num = _columns * _rows;
			if (num > 0)
			{
				savedValue = GetValueSingleArray(num);
			}
			break;
		}
		}
	}

	public unsafe void SetValue(Texture value)
	{
		IDirect3DBaseTexture9* ptr = null;
		if (value != null)
		{
			ptr = (IDirect3DBaseTexture9*)value.pStateTracker;
			IntPtr pComPtr = (IntPtr)ptr;
			Helpers.CheckDisposed(value, pComPtr);
			if (value.isActiveRenderTarget)
			{
				throw new InvalidOperationException(FrameworkResources.MustResolveRenderTarget);
			}
		}
		EffectParameterType paramType = _paramType;
		if (paramType != EffectParameterType.Texture && paramType != EffectParameterType.Texture1D && paramType != EffectParameterType.Texture2D && paramType != EffectParameterType.Texture3D && paramType != EffectParameterType.TextureCube)
		{
			throw new InvalidCastException();
		}
		ID3DXBaseEffect* ptr2 = pEffect;
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, IDirect3DBaseTexture9*, int>)(int)(*(uint*)(*(int*)ptr2 + 208)))((nint)ptr2, _handle, ptr);
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
		savedValue = value;
	}

	public unsafe void SetValue(string value)
	{
		EffectParameterType paramType = _paramType;
		if (paramType != EffectParameterType.String)
		{
			throw new InvalidCastException();
		}
		sbyte* ptr = (sbyte*)((!(value != null)) ? null : Marshal.StringToHGlobalAnsi(value).ToPointer());
		ID3DXBaseEffect* ptr2 = pEffect;
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, sbyte*, int>)(int)(*(uint*)(*(int*)ptr2 + 200)))((nint)ptr2, _handle, ptr);
		if (ptr != null)
		{
			Marshal.FreeHGlobal((IntPtr)ptr);
		}
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
	}

	public unsafe void SetValue(Matrix[] value)
	{
		if (_paramClass == EffectParameterClass.Matrix)
		{
			EffectParameterCollection effectParameterCollection = pElementCollection;
			if ((nint)value.LongLength <= effectParameterCollection.Count && pElementCollection.Count != 0)
			{
				if (value != null && (nint)value.LongLength > 0)
				{
					fixed (void* ptr = &System.Runtime.CompilerServices.Unsafe.As<Matrix, void>(ref value[0]))
					{
						try
						{
							int num = *(int*)pEffect + 160;
							int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, D3DXMATRIX*, uint, int>)(int)(*(uint*)num))((nint)pEffect, _handle, (D3DXMATRIX*)ptr, (uint)value.Length);
							if (num2 < 0)
							{
								throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
							}
						}
						catch
						{
							//try-fault
							ptr = null;
							throw;
						}
					}
				}
				else
				{
					ID3DXBaseEffect* ptr2 = pEffect;
					int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, D3DXMATRIX*, uint, int>)(int)(*(uint*)(*(int*)ptr2 + 160)))((nint)ptr2, _handle, null, 0u);
					if (num3 < 0)
					{
						throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
					}
				}
				try
				{
					return;
				}
				catch
				{
					//try-fault
					throw;
				}
			}
		}
		throw new InvalidCastException();
	}

	public unsafe void SetValue(Matrix value)
	{
		if (_paramClass == EffectParameterClass.Matrix && pElementCollection.Count == 0)
		{
			ID3DXBaseEffect* ptr = pEffect;
			int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, D3DXMATRIX*, int>)(int)(*(uint*)(*(int*)ptr + 152)))((nint)ptr, _handle, (D3DXMATRIX*)(&value));
			if (num < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num);
			}
			return;
		}
		throw new InvalidCastException();
	}

	public unsafe void SetValue(Quaternion[] value)
	{
		EffectParameterClass paramClass = _paramClass;
		if (paramClass == EffectParameterClass.Vector && pElementCollection.Count != 0)
		{
			if (value != null && (nint)value.LongLength > 0)
			{
				fixed (void* ptr = &System.Runtime.CompilerServices.Unsafe.As<Quaternion, void>(ref value[0]))
				{
					try
					{
						int num = *(int*)pEffect + 128;
						int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, uint, int>)(int)(*(uint*)num))((nint)pEffect, _handle, (float*)ptr, (uint)((nint)value.LongLength << 2));
						if (num2 < 0)
						{
							throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
						}
					}
					catch
					{
						//try-fault
						ptr = null;
						throw;
					}
				}
			}
			else
			{
				ID3DXBaseEffect* ptr2 = pEffect;
				int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, D3DXVECTOR4*, uint, int>)(int)(*(uint*)(*(int*)ptr2 + 144)))((nint)ptr2, _handle, null, 0u);
				if (num3 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
				}
			}
			try
			{
				return;
			}
			catch
			{
				//try-fault
				throw;
			}
		}
		throw new InvalidCastException();
	}

	public unsafe void SetValue(Quaternion value)
	{
		if (pElementCollection.Count != 0)
		{
			throw new InvalidCastException();
		}
		EffectParameterClass paramClass = _paramClass;
		if (paramClass == EffectParameterClass.Vector && _columns == 4 && _rows == 1)
		{
			ID3DXBaseEffect* ptr = pEffect;
			int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, D3DXVECTOR4*, int>)(int)(*(uint*)(*(int*)ptr + 136)))((nint)ptr, _handle, (D3DXVECTOR4*)(&value));
			if (num < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num);
			}
			return;
		}
		throw new InvalidCastException();
	}

	public unsafe void SetValue(Vector4[] value)
	{
		EffectParameterClass paramClass = _paramClass;
		if (paramClass == EffectParameterClass.Vector && pElementCollection.Count != 0)
		{
			if (value != null && (nint)value.LongLength > 0)
			{
				fixed (void* ptr = &System.Runtime.CompilerServices.Unsafe.As<Vector4, void>(ref value[0]))
				{
					try
					{
						int num = *(int*)pEffect + 128;
						int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, uint, int>)(int)(*(uint*)num))((nint)pEffect, _handle, (float*)ptr, (uint)((nint)value.LongLength << 2));
						if (num2 < 0)
						{
							throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
						}
					}
					catch
					{
						//try-fault
						ptr = null;
						throw;
					}
				}
			}
			else
			{
				ID3DXBaseEffect* ptr2 = pEffect;
				int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, D3DXVECTOR4*, uint, int>)(int)(*(uint*)(*(int*)ptr2 + 144)))((nint)ptr2, _handle, null, 0u);
				if (num3 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
				}
			}
			try
			{
				return;
			}
			catch
			{
				//try-fault
				throw;
			}
		}
		throw new InvalidCastException();
	}

	public unsafe void SetValue(Vector4 value)
	{
		if (pElementCollection.Count != 0)
		{
			throw new InvalidCastException();
		}
		EffectParameterClass paramClass = _paramClass;
		if (paramClass == EffectParameterClass.Vector && _columns == 4 && _rows == 1)
		{
			ID3DXBaseEffect* ptr = pEffect;
			int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, D3DXVECTOR4*, int>)(int)(*(uint*)(*(int*)ptr + 136)))((nint)ptr, _handle, (D3DXVECTOR4*)(&value));
			if (num < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num);
			}
			return;
		}
		throw new InvalidCastException();
	}

	public unsafe void SetValue(Vector3[] value)
	{
		EffectParameterClass paramClass = _paramClass;
		if (paramClass == EffectParameterClass.Vector && pElementCollection.Count != 0)
		{
			if (value != null && (nint)value.LongLength > 0)
			{
				fixed (void* ptr = &System.Runtime.CompilerServices.Unsafe.As<Vector3, void>(ref value[0]))
				{
					try
					{
						int num = *(int*)pEffect + 128;
						int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, uint, int>)(int)(*(uint*)num))((nint)pEffect, _handle, (float*)ptr, (uint)((nint)value.LongLength * 3));
						if (num2 < 0)
						{
							throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
						}
					}
					catch
					{
						//try-fault
						ptr = null;
						throw;
					}
				}
			}
			else
			{
				ID3DXBaseEffect* ptr2 = pEffect;
				int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, uint, int>)(int)(*(uint*)(*(int*)ptr2 + 128)))((nint)ptr2, _handle, null, 0u);
				if (num3 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
				}
			}
			try
			{
				return;
			}
			catch
			{
				//try-fault
				throw;
			}
		}
		throw new InvalidCastException();
	}

	public unsafe void SetValue(Vector3 value)
	{
		if (pElementCollection.Count != 0)
		{
			throw new InvalidCastException();
		}
		EffectParameterClass paramClass = _paramClass;
		if (paramClass == EffectParameterClass.Vector && _columns == 3 && _rows == 1)
		{
			ID3DXBaseEffect* ptr = pEffect;
			int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, void*, uint, int>)(int)(*(uint*)(*(int*)ptr + 80)))((nint)ptr, _handle, &value, (uint)sizeof(Vector3));
			if (num < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num);
			}
			return;
		}
		throw new InvalidCastException();
	}

	public unsafe void SetValue(Vector2[] value)
	{
		EffectParameterClass paramClass = _paramClass;
		if (paramClass == EffectParameterClass.Vector && pElementCollection.Count != 0)
		{
			if (value != null && (nint)value.LongLength > 0)
			{
				fixed (void* ptr = &System.Runtime.CompilerServices.Unsafe.As<Vector2, void>(ref value[0]))
				{
					try
					{
						int num = *(int*)pEffect + 128;
						int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, uint, int>)(int)(*(uint*)num))((nint)pEffect, _handle, (float*)ptr, (uint)((nint)value.LongLength << 1));
						if (num2 < 0)
						{
							throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
						}
					}
					catch
					{
						//try-fault
						ptr = null;
						throw;
					}
				}
			}
			else
			{
				ID3DXBaseEffect* ptr2 = pEffect;
				int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, uint, int>)(int)(*(uint*)(*(int*)ptr2 + 128)))((nint)ptr2, _handle, null, 0u);
				if (num3 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
				}
			}
			try
			{
				return;
			}
			catch
			{
				//try-fault
				throw;
			}
		}
		throw new InvalidCastException();
	}

	public unsafe void SetValue(Vector2 value)
	{
		if (pElementCollection.Count != 0)
		{
			throw new InvalidCastException();
		}
		EffectParameterClass paramClass = _paramClass;
		if (paramClass == EffectParameterClass.Vector && _columns == 2 && _rows == 1)
		{
			ID3DXBaseEffect* ptr = pEffect;
			int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, void*, uint, int>)(int)(*(uint*)(*(int*)ptr + 80)))((nint)ptr, _handle, &value, (uint)sizeof(Vector2));
			if (num < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num);
			}
			return;
		}
		throw new InvalidCastException();
	}

	public unsafe void SetValue(float[] value)
	{
		EffectParameterClass paramClass = _paramClass;
		if (paramClass != 0 && paramClass != EffectParameterClass.Vector && paramClass != EffectParameterClass.Matrix)
		{
			throw new InvalidCastException();
		}
		if (value != null && (nint)value.LongLength > 0)
		{
			if (paramClass == EffectParameterClass.Matrix)
			{
				int columns = _columns;
				int num = _rows * columns;
				int count = pElementCollection.Count;
				if (count == 0)
				{
					Matrix valueMatrix = GetValueMatrix();
					int num2 = Math.Min(value.Length, num);
					fixed (float* ptr = &System.Runtime.CompilerServices.Unsafe.AsRef<float>(&valueMatrix.M11))
					{
						try
						{
							int num3 = 0;
							int num4 = 0;
							int rows = _rows;
							if (0 < rows)
							{
								while (num3 < num2)
								{
									int num5 = 0;
									int columns2 = _columns;
									if (0 < columns2)
									{
										while (num3 < num2)
										{
											*(float*)((ref *(_003F*)((num4 * 4 + num5) * 4)) + (ref *(_003F*)ptr)) = value[num3];
											num3++;
											num5++;
											columns2 = _columns;
											if (num5 >= columns2)
											{
												break;
											}
										}
									}
									num4++;
									rows = _rows;
									if (num4 >= rows)
									{
										break;
									}
								}
							}
							SetValue(valueMatrix);
						}
						catch
						{
							//try-fault
							ptr = null;
							throw;
						}
					}
				}
				else
				{
					Matrix[] valueMatrixArray = GetValueMatrixArray(count);
					fixed (float* ptr2 = &valueMatrixArray[0].M11)
					{
						try
						{
							int num6 = Math.Min(value.Length, count * num);
							int num7 = 0;
							int num8 = 0;
							if (0 < count)
							{
								while (num7 < num6)
								{
									int num9 = 0;
									int rows2 = _rows;
									if (0 < rows2)
									{
										while (num7 < num6)
										{
											int num10 = 0;
											int columns3 = _columns;
											if (0 < columns3)
											{
												while (num7 < num6)
												{
													*(float*)((ref *(_003F*)(((num8 * 4 + num9) * 4 + num10) * 4)) + (ref *(_003F*)ptr2)) = value[num7];
													num7++;
													num10++;
													columns3 = _columns;
													if (num10 >= columns3)
													{
														break;
													}
												}
											}
											num9++;
											rows2 = _rows;
											if (num9 >= rows2)
											{
												break;
											}
										}
									}
									num8++;
									if (num8 >= count)
									{
										break;
									}
								}
							}
							SetValue(valueMatrixArray);
						}
						catch
						{
							//try-fault
							ptr2 = null;
							throw;
						}
					}
				}
			}
			else
			{
				fixed (void* ptr3 = &System.Runtime.CompilerServices.Unsafe.As<float, void>(ref value[0]))
				{
					try
					{
						int num11 = *(int*)pEffect + 128;
						int num12 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, uint, int>)(int)(*(uint*)num11))((nint)pEffect, _handle, (float*)ptr3, (uint)value.Length);
						if (num12 < 0)
						{
							throw GraphicsHelpers.GetExceptionFromResult((uint)num12);
						}
					}
					catch
					{
						//try-fault
						ptr3 = null;
						throw;
					}
				}
			}
		}
		else
		{
			ID3DXBaseEffect* ptr4 = pEffect;
			int num13 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, uint, int>)(int)(*(uint*)(*(int*)ptr4 + 128)))((nint)ptr4, _handle, null, 0u);
			if (num13 < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num13);
			}
		}
		try
		{
			return;
		}
		catch
		{
			//try-fault
			throw;
		}
	}

	public unsafe void SetValue(float value)
	{
		if (pElementCollection.Count != 0)
		{
			throw new InvalidCastException();
		}
		EffectParameterClass paramClass = _paramClass;
		if (paramClass == EffectParameterClass.Vector && _rows == 1)
		{
			switch (_columns)
			{
			default:
				throw new InvalidCastException();
			case 4:
			{
				System.Runtime.CompilerServices.Unsafe.SkipInit(out D3DXVECTOR4 d3DXVECTOR2);
				System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR4, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR2, 12)) = value;
				System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR4, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR2, 8)) = value;
				System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR4, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR2, 4)) = value;
				*(float*)(&d3DXVECTOR2) = value;
				ID3DXBaseEffect* ptr = pEffect;
				int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, D3DXVECTOR4*, int>)(int)(*(uint*)(*(int*)ptr + 136)))((nint)ptr, _handle, &d3DXVECTOR2);
				if (num2 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
				}
				break;
			}
			case 3:
			{
				System.Runtime.CompilerServices.Unsafe.SkipInit(out D3DXVECTOR3 d3DXVECTOR3);
				System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR3, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR3, 8)) = value;
				System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR3, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR3, 4)) = value;
				*(float*)(&d3DXVECTOR3) = value;
				ID3DXBaseEffect* ptr = pEffect;
				int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, uint, int>)(int)(*(uint*)(*(int*)ptr + 128)))((nint)ptr, _handle, (float*)(&d3DXVECTOR3), 3u);
				if (num3 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
				}
				break;
			}
			case 2:
			{
				System.Runtime.CompilerServices.Unsafe.SkipInit(out D3DXVECTOR2 d3DXVECTOR);
				System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR2, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR, 4)) = value;
				*(float*)(&d3DXVECTOR) = value;
				ID3DXBaseEffect* ptr = pEffect;
				int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, uint, int>)(int)(*(uint*)(*(int*)ptr + 128)))((nint)ptr, _handle, (float*)(&d3DXVECTOR), 2u);
				if (num < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num);
				}
				break;
			}
			}
		}
		else if (paramClass == EffectParameterClass.Matrix)
		{
			System.Runtime.CompilerServices.Unsafe.SkipInit(out D3DXMATRIX d3DXMATRIX);
			System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 12)) = value;
			System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 8)) = value;
			System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 4)) = value;
			*(float*)(&d3DXMATRIX) = value;
			System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 28)) = value;
			System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 24)) = value;
			System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 20)) = value;
			System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 16)) = value;
			System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 44)) = value;
			System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 40)) = value;
			System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 36)) = value;
			System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 32)) = value;
			System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 60)) = value;
			System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 56)) = value;
			System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 52)) = value;
			System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 48)) = value;
			ID3DXBaseEffect* ptr = pEffect;
			int num4 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, D3DXMATRIX*, int>)(int)(*(uint*)(*(int*)ptr + 152)))((nint)ptr, _handle, &d3DXMATRIX);
			if (num4 < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num4);
			}
		}
		else
		{
			if (paramClass != 0)
			{
				throw new InvalidCastException();
			}
			ID3DXBaseEffect* ptr = pEffect;
			int num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float, int>)(int)(*(uint*)(*(int*)ptr + 120)))((nint)ptr, _handle, value);
			if (num5 < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num5);
			}
		}
	}

	public unsafe void SetValue(int[] value)
	{
		EffectParameterClass paramClass = _paramClass;
		if (paramClass != 0 && paramClass != EffectParameterClass.Vector && paramClass != EffectParameterClass.Matrix)
		{
			throw new InvalidCastException();
		}
		if (value != null && (nint)value.LongLength > 0)
		{
			if (paramClass == EffectParameterClass.Matrix)
			{
				int columns = _columns;
				int num = _rows * columns;
				int count = pElementCollection.Count;
				if (count == 0)
				{
					Matrix valueMatrix = GetValueMatrix();
					int num2 = Math.Min(value.Length, num);
					fixed (float* ptr = &System.Runtime.CompilerServices.Unsafe.AsRef<float>(&valueMatrix.M11))
					{
						try
						{
							int num3 = 0;
							int num4 = 0;
							int rows = _rows;
							if (0 < rows)
							{
								while (num3 < num2)
								{
									int num5 = 0;
									int columns2 = _columns;
									if (0 < columns2)
									{
										while (num3 < num2)
										{
											float num6 = value[num3];
											*(float*)((ref *(_003F*)((num4 * 4 + num5) * 4)) + (ref *(_003F*)ptr)) = num6;
											num3++;
											num5++;
											columns2 = _columns;
											if (num5 >= columns2)
											{
												break;
											}
										}
									}
									num4++;
									rows = _rows;
									if (num4 >= rows)
									{
										break;
									}
								}
							}
							SetValue(valueMatrix);
						}
						catch
						{
							//try-fault
							ptr = null;
							throw;
						}
					}
				}
				else
				{
					Matrix[] valueMatrixArray = GetValueMatrixArray(count);
					int num7 = Math.Min(value.Length, count * num);
					fixed (float* ptr2 = &valueMatrixArray[0].M11)
					{
						try
						{
							int num8 = 0;
							int num9 = 0;
							if (0 < count)
							{
								while (num8 < num7)
								{
									int num10 = 0;
									int rows2 = _rows;
									if (0 < rows2)
									{
										while (num8 < num7)
										{
											int num11 = 0;
											int columns3 = _columns;
											if (0 < columns3)
											{
												while (num8 < num7)
												{
													float num12 = value[num8];
													*(float*)((ref *(_003F*)(((num9 * 4 + num10) * 4 + num11) * 4)) + (ref *(_003F*)ptr2)) = num12;
													num8++;
													num11++;
													columns3 = _columns;
													if (num11 >= columns3)
													{
														break;
													}
												}
											}
											num10++;
											rows2 = _rows;
											if (num10 >= rows2)
											{
												break;
											}
										}
									}
									num9++;
									if (num9 >= count)
									{
										break;
									}
								}
							}
							SetValue(valueMatrixArray);
						}
						catch
						{
							//try-fault
							ptr2 = null;
							throw;
						}
					}
				}
			}
			else
			{
				fixed (void* ptr3 = &System.Runtime.CompilerServices.Unsafe.As<int, void>(ref value[0]))
				{
					try
					{
						int num13 = *(int*)pEffect + 112;
						int num14 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, int*, uint, int>)(int)(*(uint*)num13))((nint)pEffect, _handle, (int*)ptr3, (uint)value.Length);
						if (num14 < 0)
						{
							throw GraphicsHelpers.GetExceptionFromResult((uint)num14);
						}
					}
					catch
					{
						//try-fault
						ptr3 = null;
						throw;
					}
				}
			}
		}
		else
		{
			ID3DXBaseEffect* ptr4 = pEffect;
			int num15 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, int*, uint, int>)(int)(*(uint*)(*(int*)ptr4 + 112)))((nint)ptr4, _handle, null, 0u);
			if (num15 < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num15);
			}
		}
		try
		{
			return;
		}
		catch
		{
			//try-fault
			throw;
		}
	}

	public unsafe void SetValue(int value)
	{
		if (pElementCollection.Count != 0)
		{
			throw new InvalidCastException();
		}
		EffectParameterClass paramClass = _paramClass;
		if (paramClass == EffectParameterClass.Vector && _rows == 1)
		{
			switch (_columns)
			{
			default:
				throw new InvalidCastException();
			case 4:
			{
				System.Runtime.CompilerServices.Unsafe.SkipInit(out D3DXVECTOR4 d3DXVECTOR2);
				System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR4, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR2, 12)) = value;
				System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR4, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR2, 8)) = System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR4, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR2, 12));
				System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR4, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR2, 4)) = System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR4, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR2, 12));
				*(float*)(&d3DXVECTOR2) = System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR4, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR2, 12));
				ID3DXBaseEffect* ptr = pEffect;
				int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, D3DXVECTOR4*, int>)(int)(*(uint*)(*(int*)ptr + 136)))((nint)ptr, _handle, &d3DXVECTOR2);
				if (num2 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
				}
				break;
			}
			case 3:
			{
				System.Runtime.CompilerServices.Unsafe.SkipInit(out D3DXVECTOR3 d3DXVECTOR3);
				System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR3, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR3, 8)) = value;
				System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR3, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR3, 4)) = System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR3, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR3, 8));
				*(float*)(&d3DXVECTOR3) = System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR3, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR3, 8));
				ID3DXBaseEffect* ptr = pEffect;
				int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, uint, int>)(int)(*(uint*)(*(int*)ptr + 128)))((nint)ptr, _handle, (float*)(&d3DXVECTOR3), 3u);
				if (num3 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
				}
				break;
			}
			case 2:
			{
				System.Runtime.CompilerServices.Unsafe.SkipInit(out D3DXVECTOR2 d3DXVECTOR);
				System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR2, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR, 4)) = value;
				*(float*)(&d3DXVECTOR) = System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR2, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR, 4));
				ID3DXBaseEffect* ptr = pEffect;
				int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, uint, int>)(int)(*(uint*)(*(int*)ptr + 128)))((nint)ptr, _handle, (float*)(&d3DXVECTOR), 2u);
				if (num < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num);
				}
				break;
			}
			}
			return;
		}
		switch (paramClass)
		{
		case EffectParameterClass.Matrix:
		{
			System.Runtime.CompilerServices.Unsafe.SkipInit(out D3DXMATRIX d3DXMATRIX);
			System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 48)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 52)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 56)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 60)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 32)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 36)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 40)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 44)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 16)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 20)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 24)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 28)) = (*(float*)(&d3DXMATRIX) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 4)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 8)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 12)) = value)))))))))))))));
			ID3DXBaseEffect* ptr = pEffect;
			int num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, D3DXMATRIX*, int>)(int)(*(uint*)(*(int*)ptr + 152)))((nint)ptr, _handle, &d3DXMATRIX);
			if (num5 < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num5);
			}
			break;
		}
		default:
			throw new InvalidCastException();
		case EffectParameterClass.Scalar:
		{
			ID3DXBaseEffect* ptr = pEffect;
			int num4 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, int, int>)(int)(*(uint*)(*(int*)ptr + 104)))((nint)ptr, _handle, value);
			if (num4 < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num4);
			}
			break;
		}
		}
	}

	public unsafe void SetValue(bool[] value)
	{
		EffectParameterClass paramClass = _paramClass;
		if (paramClass != 0 && paramClass != EffectParameterClass.Vector && paramClass != EffectParameterClass.Matrix)
		{
			throw new InvalidCastException();
		}
		if (value != null)
		{
			int num = value.Length;
			if (num > 0)
			{
				if (paramClass == EffectParameterClass.Matrix)
				{
					int columns = _columns;
					int val = _rows * columns;
					int count = pElementCollection.Count;
					if (count == 0)
					{
						Matrix valueMatrix = GetValueMatrix();
						int num2 = Math.Min(value.Length, val);
						fixed (float* ptr = &System.Runtime.CompilerServices.Unsafe.AsRef<float>(&valueMatrix.M11))
						{
							try
							{
								int num3 = 0;
								int num4 = 0;
								int rows = _rows;
								if (0 < rows)
								{
									while (num3 < num2)
									{
										int num5 = 0;
										int columns2 = _columns;
										if (0 < columns2)
										{
											while (num3 < num2)
											{
												bool flag = value[num3];
												num3++;
												float num6 = ((!flag) ? 0f : 1f);
												*(float*)((ref *(_003F*)((num4 * 4 + num5) * 4)) + (ref *(_003F*)ptr)) = num6;
												num5++;
												columns2 = _columns;
												if (num5 >= columns2)
												{
													break;
												}
											}
										}
										num4++;
										rows = _rows;
										if (num4 >= rows)
										{
											break;
										}
									}
								}
								SetValue(valueMatrix);
							}
							catch
							{
								//try-fault
								ptr = null;
								throw;
							}
						}
					}
					else
					{
						Matrix[] valueMatrixArray = GetValueMatrixArray(count);
						int num7 = Math.Min(value.Length, count * 16);
						fixed (float* ptr2 = &valueMatrixArray[0].M11)
						{
							try
							{
								int num8 = 0;
								int num9 = 0;
								if (0 < count)
								{
									while (num8 < num7)
									{
										int num10 = 0;
										int rows2 = _rows;
										if (0 < rows2)
										{
											while (num8 < num7)
											{
												int num11 = 0;
												int columns3 = _columns;
												if (0 < columns3)
												{
													while (num8 < num7)
													{
														bool flag2 = value[num8];
														num8++;
														float num12 = ((!flag2) ? 0f : 1f);
														*(float*)((ref *(_003F*)(((num9 * 4 + num10) * 4 + num11) * 4)) + (ref *(_003F*)ptr2)) = num12;
														num11++;
														columns3 = _columns;
														if (num11 >= columns3)
														{
															break;
														}
													}
												}
												num10++;
												rows2 = _rows;
												if (num10 >= rows2)
												{
													break;
												}
											}
										}
										num9++;
										if (num9 >= count)
										{
											break;
										}
									}
								}
								SetValue(valueMatrixArray);
							}
							catch
							{
								//try-fault
								ptr2 = null;
								throw;
							}
						}
					}
				}
				else
				{
					int[] array = new int[num];
					int num13 = 0;
					if (0 < (nint)array.LongLength)
					{
						do
						{
							int num14 = (value[num13] ? 1 : 0);
							array[num13] = num14;
							num13++;
						}
						while (num13 < (nint)array.LongLength);
					}
					fixed (void* ptr3 = &System.Runtime.CompilerServices.Unsafe.As<int, void>(ref array[0]))
					{
						try
						{
							int num15 = *(int*)pEffect + 96;
							int num16 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, int*, uint, int>)(int)(*(uint*)num15))((nint)pEffect, _handle, (int*)ptr3, (uint)value.Length);
							if (num16 < 0)
							{
								throw GraphicsHelpers.GetExceptionFromResult((uint)num16);
							}
						}
						catch
						{
							//try-fault
							ptr3 = null;
							throw;
						}
					}
				}
				goto IL_0296;
			}
		}
		ID3DXBaseEffect* ptr4 = pEffect;
		int num17 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, int*, uint, int>)(int)(*(uint*)(*(int*)ptr4 + 96)))((nint)ptr4, _handle, null, 0u);
		if (num17 < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num17);
		}
		goto IL_0296;
		IL_0296:
		try
		{
			return;
		}
		catch
		{
			//try-fault
			throw;
		}
	}

	public unsafe void SetValue([MarshalAs(UnmanagedType.U1)] bool value)
	{
		if (pElementCollection.Count != 0)
		{
			throw new InvalidCastException();
		}
		EffectParameterClass paramClass = _paramClass;
		if (paramClass == EffectParameterClass.Vector && _rows == 1)
		{
			switch (_columns)
			{
			default:
				throw new InvalidCastException();
			case 4:
			{
				System.Runtime.CompilerServices.Unsafe.SkipInit(out D3DXVECTOR4 d3DXVECTOR3);
				*(float*)(&d3DXVECTOR3) = (System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR4, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR3, 4)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR4, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR3, 8)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR4, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR3, 12)) = ((!value) ? 0f : 1f))));
				ID3DXBaseEffect* ptr = pEffect;
				int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, D3DXVECTOR4*, int>)(int)(*(uint*)(*(int*)ptr + 136)))((nint)ptr, _handle, &d3DXVECTOR3);
				if (num3 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
				}
				break;
			}
			case 3:
			{
				System.Runtime.CompilerServices.Unsafe.SkipInit(out D3DXVECTOR3 d3DXVECTOR2);
				*(float*)(&d3DXVECTOR2) = (System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR3, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR2, 4)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR3, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR2, 8)) = ((!value) ? 0f : 1f)));
				ID3DXBaseEffect* ptr = pEffect;
				int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, uint, int>)(int)(*(uint*)(*(int*)ptr + 128)))((nint)ptr, _handle, (float*)(&d3DXVECTOR2), 3u);
				if (num2 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
				}
				break;
			}
			case 2:
			{
				System.Runtime.CompilerServices.Unsafe.SkipInit(out D3DXVECTOR2 d3DXVECTOR);
				*(float*)(&d3DXVECTOR) = (System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR2, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR, 4)) = ((!value) ? 0f : 1f));
				ID3DXBaseEffect* ptr = pEffect;
				int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, uint, int>)(int)(*(uint*)(*(int*)ptr + 128)))((nint)ptr, _handle, (float*)(&d3DXVECTOR), 2u);
				if (num < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num);
				}
				break;
			}
			}
			return;
		}
		switch (paramClass)
		{
		case EffectParameterClass.Matrix:
		{
			System.Runtime.CompilerServices.Unsafe.SkipInit(out D3DXMATRIX d3DXMATRIX);
			System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 48)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 52)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 56)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 60)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 32)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 36)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 40)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 44)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 16)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 20)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 24)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 28)) = (*(float*)(&d3DXMATRIX) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 4)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 8)) = (System.Runtime.CompilerServices.Unsafe.As<D3DXMATRIX, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXMATRIX, 12)) = ((!value) ? 0f : 1f))))))))))))))));
			ID3DXBaseEffect* ptr = pEffect;
			int num6 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, D3DXMATRIX*, int>)(int)(*(uint*)(*(int*)ptr + 152)))((nint)ptr, _handle, &d3DXMATRIX);
			if (num6 < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num6);
			}
			break;
		}
		default:
			throw new InvalidCastException();
		case EffectParameterClass.Scalar:
		{
			int num4 = (value ? 1 : 0);
			ID3DXBaseEffect* ptr = pEffect;
			int num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, int, int>)(int)(*(uint*)(*(int*)ptr + 88)))((nint)ptr, _handle, num4);
			if (num5 < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num5);
			}
			break;
		}
		}
	}

	public unsafe void SetValueTranspose(Matrix[] value)
	{
		if (_paramClass == EffectParameterClass.Matrix)
		{
			EffectParameterCollection effectParameterCollection = pElementCollection;
			if ((nint)value.LongLength <= effectParameterCollection.Count && pElementCollection.Count != 0)
			{
				if (value != null && (nint)value.LongLength > 0)
				{
					fixed (void* ptr = &System.Runtime.CompilerServices.Unsafe.As<Matrix, void>(ref value[0]))
					{
						try
						{
							int num = *(int*)pEffect + 184;
							int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, D3DXMATRIX*, uint, int>)(int)(*(uint*)num))((nint)pEffect, _handle, (D3DXMATRIX*)ptr, (uint)value.Length);
							if (num2 < 0)
							{
								throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
							}
						}
						catch
						{
							//try-fault
							ptr = null;
							throw;
						}
					}
				}
				else
				{
					ID3DXBaseEffect* ptr2 = pEffect;
					int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, D3DXMATRIX*, uint, int>)(int)(*(uint*)(*(int*)ptr2 + 184)))((nint)ptr2, _handle, null, 0u);
					if (num3 < 0)
					{
						throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
					}
				}
				try
				{
					return;
				}
				catch
				{
					//try-fault
					throw;
				}
			}
		}
		throw new InvalidCastException();
	}

	public unsafe void SetValueTranspose(Matrix value)
	{
		if (_paramClass == EffectParameterClass.Matrix && pElementCollection.Count == 0)
		{
			ID3DXBaseEffect* ptr = pEffect;
			int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, D3DXMATRIX*, int>)(int)(*(uint*)(*(int*)ptr + 176)))((nint)ptr, _handle, (D3DXMATRIX*)(&value));
			if (num < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num);
			}
			return;
		}
		throw new InvalidCastException();
	}

	[return: MarshalAs(UnmanagedType.U1)]
	public unsafe bool GetValueBoolean()
	{
		EffectParameterClass paramClass = _paramClass;
		if (paramClass != 0 && pElementCollection.Count == 0)
		{
			throw new InvalidCastException();
		}
		ID3DXBaseEffect* ptr = pEffect;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out int num);
		int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, int*, int>)(int)(*(uint*)(*(int*)ptr + 92)))((nint)ptr, _handle, &num);
		if (num2 < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
		}
		return (byte)((num != 0) ? 1u : 0u) != 0;
	}

	public unsafe bool[] GetValueBooleanArray(int count)
	{
		//Discarded unreachable code: IL_0277
		if (count <= 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		bool[] array = new bool[count];
		if (_paramClass == EffectParameterClass.Matrix)
		{
			int columns = _columns;
			int val = _rows * columns;
			int count2 = pElementCollection.Count;
			if (count2 == 0)
			{
				Matrix valueMatrix = GetValueMatrix();
				int num = Math.Min(count, val);
				fixed (float* ptr = &System.Runtime.CompilerServices.Unsafe.AsRef<float>(&valueMatrix.M11))
				{
					try
					{
						int num2 = 0;
						int num3 = 0;
						int rows = _rows;
						if (0 < rows)
						{
							while (num2 < num)
							{
								int num4 = 0;
								int columns2 = _columns;
								if (0 < columns2)
								{
									while (num2 < num)
									{
										int num5 = ((*(float*)((ref *(_003F*)((num3 * 4 + num4) * 4)) + (ref *(_003F*)ptr)) != 0f) ? 1 : 0);
										array[num2] = (byte)num5 != 0;
										num2++;
										num4++;
										columns2 = _columns;
										if (num4 >= columns2)
										{
											break;
										}
									}
								}
								num3++;
								rows = _rows;
								if (num3 >= rows)
								{
									break;
								}
							}
						}
					}
					catch
					{
						//try-fault
						ptr = null;
						throw;
					}
				}
			}
			else
			{
				Matrix[] valueMatrixArray = GetValueMatrixArray(count2);
				int num6 = Math.Min(count, count2 * 16);
				fixed (float* ptr2 = &valueMatrixArray[0].M11)
				{
					try
					{
						int num7 = 0;
						int num8 = 0;
						if (0 < count2)
						{
							while (num7 < num6)
							{
								int num9 = 0;
								int rows2 = _rows;
								if (0 < rows2)
								{
									while (num7 < num6)
									{
										int num10 = 0;
										int columns3 = _columns;
										if (0 < columns3)
										{
											while (num7 < num6)
											{
												int num11 = ((*(float*)((ref *(_003F*)(((num8 * 4 + num9) * 4 + num10) * 4)) + (ref *(_003F*)ptr2)) != 0f) ? 1 : 0);
												array[num7] = (byte)num11 != 0;
												num7++;
												num10++;
												columns3 = _columns;
												if (num10 >= columns3)
												{
													break;
												}
											}
										}
										num9++;
										rows2 = _rows;
										if (num9 >= rows2)
										{
											break;
										}
									}
								}
								num8++;
								if (num8 >= count2)
								{
									break;
								}
							}
						}
					}
					catch
					{
						//try-fault
						ptr2 = null;
						throw;
					}
				}
			}
		}
		else
		{
			ID3DXBaseEffect* ptr3 = pEffect;
			System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DXPARAMETER_DESC d3DXPARAMETER_DESC);
			int num12 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, _D3DXPARAMETER_DESC*, int>)(int)(*(uint*)(*(int*)ptr3 + 16)))((nint)ptr3, _handle, &d3DXPARAMETER_DESC);
			if (num12 < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num12);
			}
			int num13 = System.Runtime.CompilerServices.Unsafe.As<_D3DXPARAMETER_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPARAMETER_DESC, 40)) >>> 2;
			int[] array2 = new int[num13];
			fixed (void* ptr4 = &System.Runtime.CompilerServices.Unsafe.As<int, void>(ref array2[0]))
			{
				try
				{
					int num14 = ((count >= num13) ? num13 : count);
					int num15 = *(int*)pEffect + 100;
					int num16 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, int*, uint, int>)(int)(*(uint*)num15))((nint)pEffect, _handle, (int*)ptr4, (uint)num14);
					if (num16 < 0)
					{
						throw GraphicsHelpers.GetExceptionFromResult((uint)num16);
					}
					int num17 = 0;
					if (0 < num14)
					{
						do
						{
							int num18 = ((array2[num17] != 0) ? 1 : 0);
							array[num17] = (byte)num18 != 0;
							num17++;
						}
						while (num17 < num14);
					}
				}
				catch
				{
					//try-fault
					ptr4 = null;
					throw;
				}
			}
		}
		return array;
	}

	public unsafe int GetValueInt32()
	{
		EffectParameterClass paramClass = _paramClass;
		if (paramClass != 0 && pElementCollection.Count == 0)
		{
			throw new InvalidCastException();
		}
		ID3DXBaseEffect* ptr = pEffect;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out int result);
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, int*, int>)(int)(*(uint*)(*(int*)ptr + 108)))((nint)ptr, _handle, &result);
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
		return result;
	}

	public unsafe int[] GetValueInt32Array(int count)
	{
		//Discarded unreachable code: IL_0240
		if (count <= 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		int[] array = new int[count];
		if (_paramClass == EffectParameterClass.Matrix)
		{
			int columns = _columns;
			int val = _rows * columns;
			int count2 = pElementCollection.Count;
			if (count2 == 0)
			{
				Matrix valueMatrix = GetValueMatrix();
				int num = Math.Min(count, val);
				fixed (float* ptr = &System.Runtime.CompilerServices.Unsafe.AsRef<float>(&valueMatrix.M11))
				{
					try
					{
						int num2 = 0;
						int num3 = 0;
						int rows = _rows;
						if (0 < rows)
						{
							while (num2 < num)
							{
								int num4 = 0;
								int columns2 = _columns;
								if (0 < columns2)
								{
									while (num2 < num)
									{
										array[num2] = (int)(double)(*(float*)((ref *(_003F*)((num3 * 4 + num4) * 4)) + (ref *(_003F*)ptr)));
										num2++;
										num4++;
										columns2 = _columns;
										if (num4 >= columns2)
										{
											break;
										}
									}
								}
								num3++;
								rows = _rows;
								if (num3 >= rows)
								{
									break;
								}
							}
						}
					}
					catch
					{
						//try-fault
						ptr = null;
						throw;
					}
				}
			}
			else
			{
				Matrix[] valueMatrixArray = GetValueMatrixArray(count2);
				int num5 = Math.Min(count, count2 * 16);
				fixed (float* ptr2 = &valueMatrixArray[0].M11)
				{
					try
					{
						int num6 = 0;
						int num7 = 0;
						if (0 < count2)
						{
							while (num6 < num5)
							{
								int num8 = 0;
								int rows2 = _rows;
								if (0 < rows2)
								{
									while (num6 < num5)
									{
										int num9 = 0;
										int columns3 = _columns;
										if (0 < columns3)
										{
											while (num6 < num5)
											{
												array[num6] = (int)(double)(*(float*)((ref *(_003F*)(((num7 * 4 + num8) * 4 + num9) * 4)) + (ref *(_003F*)ptr2)));
												num6++;
												num9++;
												columns3 = _columns;
												if (num9 >= columns3)
												{
													break;
												}
											}
										}
										num8++;
										rows2 = _rows;
										if (num8 >= rows2)
										{
											break;
										}
									}
								}
								num7++;
								if (num7 >= count2)
								{
									break;
								}
							}
						}
					}
					catch
					{
						//try-fault
						ptr2 = null;
						throw;
					}
				}
			}
		}
		else
		{
			ID3DXBaseEffect* ptr3 = pEffect;
			System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DXPARAMETER_DESC d3DXPARAMETER_DESC);
			int num10 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, _D3DXPARAMETER_DESC*, int>)(int)(*(uint*)(*(int*)ptr3 + 16)))((nint)ptr3, _handle, &d3DXPARAMETER_DESC);
			if (num10 < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num10);
			}
			int num11 = System.Runtime.CompilerServices.Unsafe.As<_D3DXPARAMETER_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPARAMETER_DESC, 40)) >>> 2;
			fixed (void* ptr4 = &System.Runtime.CompilerServices.Unsafe.As<int, void>(ref (new int[num11])[0]))
			{
				try
				{
					int num12 = ((count >= num11) ? num11 : count);
					int num13 = *(int*)pEffect + 116;
					int num14 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, int*, uint, int>)(int)(*(uint*)num13))((nint)pEffect, _handle, (int*)ptr4, (uint)num12);
					if (num14 < 0)
					{
						throw GraphicsHelpers.GetExceptionFromResult((uint)num14);
					}
					IntPtr source = new IntPtr(ptr4);
					Marshal.Copy(source, array, 0, num12);
				}
				catch
				{
					//try-fault
					ptr4 = null;
					throw;
				}
			}
		}
		return array;
	}

	public unsafe float GetValueSingle()
	{
		EffectParameterClass paramClass = _paramClass;
		if (paramClass != 0 && pElementCollection.Count == 0)
		{
			throw new InvalidCastException();
		}
		ID3DXBaseEffect* ptr = pEffect;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out float result);
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, int>)(int)(*(uint*)(*(int*)ptr + 124)))((nint)ptr, _handle, &result);
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
		return result;
	}

	public unsafe float[] GetValueSingleArray(int count)
	{
		//Discarded unreachable code: IL_023f
		if (count <= 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		float[] array = new float[count];
		if (_paramClass == EffectParameterClass.Matrix)
		{
			int columns = _columns;
			int val = _rows * columns;
			int count2 = pElementCollection.Count;
			if (count2 == 0)
			{
				Matrix valueMatrix = GetValueMatrix();
				int num = Math.Min(count, val);
				fixed (float* ptr = &System.Runtime.CompilerServices.Unsafe.AsRef<float>(&valueMatrix.M11))
				{
					try
					{
						int num2 = 0;
						int num3 = 0;
						int rows = _rows;
						if (0 < rows)
						{
							while (num2 < num)
							{
								int num4 = 0;
								int columns2 = _columns;
								if (0 < columns2)
								{
									while (num2 < num)
									{
										array[num2] = *(float*)((ref *(_003F*)((num3 * 4 + num4) * 4)) + (ref *(_003F*)ptr));
										num2++;
										num4++;
										columns2 = _columns;
										if (num4 >= columns2)
										{
											break;
										}
									}
								}
								num3++;
								rows = _rows;
								if (num3 >= rows)
								{
									break;
								}
							}
						}
					}
					catch
					{
						//try-fault
						ptr = null;
						throw;
					}
				}
			}
			else
			{
				Matrix[] valueMatrixArray = GetValueMatrixArray(count2);
				int num5 = Math.Min(count, count2 * 16);
				fixed (float* ptr2 = &valueMatrixArray[0].M11)
				{
					try
					{
						int num6 = 0;
						int num7 = 0;
						if (0 < count2)
						{
							while (num6 < num5)
							{
								int num8 = 0;
								int rows2 = _rows;
								if (0 < rows2)
								{
									while (num6 < num5)
									{
										int num9 = 0;
										int columns3 = _columns;
										if (0 < columns3)
										{
											while (num6 < num5)
											{
												array[num6] = *(float*)((ref *(_003F*)(((num7 * 4 + num8) * 4 + num9) * 4)) + (ref *(_003F*)ptr2));
												num6++;
												num9++;
												columns3 = _columns;
												if (num9 >= columns3)
												{
													break;
												}
											}
										}
										num8++;
										rows2 = _rows;
										if (num8 >= rows2)
										{
											break;
										}
									}
								}
								num7++;
								if (num7 >= count2)
								{
									break;
								}
							}
						}
					}
					catch
					{
						//try-fault
						ptr2 = null;
						throw;
					}
				}
			}
		}
		else
		{
			ID3DXBaseEffect* ptr3 = pEffect;
			System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DXPARAMETER_DESC d3DXPARAMETER_DESC);
			int num10 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, _D3DXPARAMETER_DESC*, int>)(int)(*(uint*)(*(int*)ptr3 + 16)))((nint)ptr3, _handle, &d3DXPARAMETER_DESC);
			if (num10 < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num10);
			}
			int num11 = System.Runtime.CompilerServices.Unsafe.As<_D3DXPARAMETER_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPARAMETER_DESC, 40)) >>> 2;
			fixed (void* ptr4 = &System.Runtime.CompilerServices.Unsafe.As<float, void>(ref (new float[num11])[0]))
			{
				try
				{
					int num12 = ((count >= num11) ? num11 : count);
					int num13 = *(int*)pEffect + 132;
					int num14 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, uint, int>)(int)(*(uint*)num13))((nint)pEffect, _handle, (float*)ptr4, (uint)num12);
					if (num14 < 0)
					{
						throw GraphicsHelpers.GetExceptionFromResult((uint)num14);
					}
					IntPtr source = new IntPtr(ptr4);
					Marshal.Copy(source, array, 0, num12);
				}
				catch
				{
					//try-fault
					ptr4 = null;
					throw;
				}
			}
		}
		return array;
	}

	public unsafe Vector2 GetValueVector2()
	{
		Vector2 result = default(Vector2);
		ID3DXBaseEffect* ptr;
		if (pElementCollection.Count == 0)
		{
			switch (_paramClass)
			{
			case EffectParameterClass.Scalar:
			{
				ptr = pEffect;
				System.Runtime.CompilerServices.Unsafe.SkipInit(out float num);
				int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, int>)(int)(*(uint*)(*(int*)ptr + 124)))((nint)ptr, _handle, &num);
				if (num2 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
				}
				result.Y = num;
				result.X = num;
				return result;
			}
			case EffectParameterClass.Vector:
				break;
			default:
				throw new InvalidCastException();
			}
			if (_columns != 2 || _rows != 1)
			{
				throw new InvalidCastException();
			}
		}
		ptr = pEffect;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out D3DXVECTOR4 d3DXVECTOR);
		int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, D3DXVECTOR4*, int>)(int)(*(uint*)(*(int*)ptr + 140)))((nint)ptr, _handle, &d3DXVECTOR);
		if (num3 < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
		}
		result.X = *(float*)(&d3DXVECTOR);
		result.Y = System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR4, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR, 4));
		return result;
	}

	public unsafe Vector2[] GetValueVector2Array(int count)
	{
		//Discarded unreachable code: IL_00d8
		if (count <= 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		ID3DXBaseEffect* ptr = pEffect;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DXPARAMETER_DESC d3DXPARAMETER_DESC);
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, _D3DXPARAMETER_DESC*, int>)(int)(*(uint*)(*(int*)ptr + 16)))((nint)ptr, _handle, &d3DXPARAMETER_DESC);
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
		int num2 = System.Runtime.CompilerServices.Unsafe.As<_D3DXPARAMETER_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPARAMETER_DESC, 40)) >>> 2;
		int num3 = count * 2;
		int num4 = ((num3 >= num2) ? num2 : num3);
		float[] array;
		if (_paramClass == EffectParameterClass.Matrix)
		{
			array = GetValueSingleArray(num4);
		}
		else
		{
			array = new float[num2];
			fixed (float* ptr2 = &array[0])
			{
				try
				{
					int num5 = *(int*)pEffect + 132;
					int num6 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, uint, int>)(int)(*(uint*)num5))((nint)pEffect, _handle, ptr2, (uint)num4);
					if (num6 < 0)
					{
						throw GraphicsHelpers.GetExceptionFromResult((uint)num6);
					}
				}
				catch
				{
					//try-fault
					ptr2 = null;
					throw;
				}
			}
		}
		Vector2[] array2 = new Vector2[count];
		fixed (void* value = &System.Runtime.CompilerServices.Unsafe.As<Vector2, void>(ref array2[0]))
		{
			IntPtr destination = new IntPtr(value);
			Marshal.Copy(array, 0, destination, num4);
			return array2;
		}
	}

	public unsafe Vector3 GetValueVector3()
	{
		Vector3 result = default(Vector3);
		ID3DXBaseEffect* ptr;
		if (pElementCollection.Count == 0)
		{
			switch (_paramClass)
			{
			case EffectParameterClass.Scalar:
			{
				ptr = pEffect;
				System.Runtime.CompilerServices.Unsafe.SkipInit(out float num);
				int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, int>)(int)(*(uint*)(*(int*)ptr + 124)))((nint)ptr, _handle, &num);
				if (num2 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
				}
				result.Z = num;
				result.Y = num;
				result.X = num;
				return result;
			}
			case EffectParameterClass.Vector:
				break;
			default:
				throw new InvalidCastException();
			}
			if (_columns != 3 || _rows != 1)
			{
				throw new InvalidCastException();
			}
		}
		ptr = pEffect;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out D3DXVECTOR4 d3DXVECTOR);
		int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, D3DXVECTOR4*, int>)(int)(*(uint*)(*(int*)ptr + 140)))((nint)ptr, _handle, &d3DXVECTOR);
		if (num3 < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
		}
		result.X = *(float*)(&d3DXVECTOR);
		result.Y = System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR4, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR, 4));
		result.Z = System.Runtime.CompilerServices.Unsafe.As<D3DXVECTOR4, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXVECTOR, 8));
		return result;
	}

	public unsafe Vector3[] GetValueVector3Array(int count)
	{
		//Discarded unreachable code: IL_00d8
		if (count <= 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		ID3DXBaseEffect* ptr = pEffect;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DXPARAMETER_DESC d3DXPARAMETER_DESC);
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, _D3DXPARAMETER_DESC*, int>)(int)(*(uint*)(*(int*)ptr + 16)))((nint)ptr, _handle, &d3DXPARAMETER_DESC);
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
		int num2 = System.Runtime.CompilerServices.Unsafe.As<_D3DXPARAMETER_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPARAMETER_DESC, 40)) >>> 2;
		int num3 = count * 3;
		int num4 = ((num3 >= num2) ? num2 : num3);
		float[] array;
		if (_paramClass == EffectParameterClass.Matrix)
		{
			array = GetValueSingleArray(num4);
		}
		else
		{
			array = new float[num2];
			fixed (float* ptr2 = &array[0])
			{
				try
				{
					int num5 = *(int*)pEffect + 132;
					int num6 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, uint, int>)(int)(*(uint*)num5))((nint)pEffect, _handle, ptr2, (uint)num4);
					if (num6 < 0)
					{
						throw GraphicsHelpers.GetExceptionFromResult((uint)num6);
					}
				}
				catch
				{
					//try-fault
					ptr2 = null;
					throw;
				}
			}
		}
		Vector3[] array2 = new Vector3[count];
		fixed (void* value = &System.Runtime.CompilerServices.Unsafe.As<Vector3, void>(ref array2[0]))
		{
			IntPtr destination = new IntPtr(value);
			Marshal.Copy(array, 0, destination, num4);
			return array2;
		}
	}

	public unsafe Vector4 GetValueVector4()
	{
		Vector4 result = default(Vector4);
		ID3DXBaseEffect* ptr;
		if (pElementCollection.Count == 0)
		{
			switch (_paramClass)
			{
			case EffectParameterClass.Scalar:
			{
				ptr = pEffect;
				System.Runtime.CompilerServices.Unsafe.SkipInit(out float num);
				int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, int>)(int)(*(uint*)(*(int*)ptr + 124)))((nint)ptr, _handle, &num);
				if (num2 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
				}
				result.W = num;
				result.Z = num;
				result.Y = num;
				result.X = num;
				return result;
			}
			case EffectParameterClass.Vector:
				break;
			default:
				throw new InvalidCastException();
			}
			if (_columns != 4 || _rows != 1)
			{
				throw new InvalidCastException();
			}
		}
		ptr = pEffect;
		int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, D3DXVECTOR4*, int>)(int)(*(uint*)(*(int*)ptr + 140)))((nint)ptr, _handle, (D3DXVECTOR4*)(&result));
		if (num3 < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
		}
		return result;
	}

	public unsafe Vector4[] GetValueVector4Array(int count)
	{
		//Discarded unreachable code: IL_00d8
		if (count <= 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		ID3DXBaseEffect* ptr = pEffect;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DXPARAMETER_DESC d3DXPARAMETER_DESC);
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, _D3DXPARAMETER_DESC*, int>)(int)(*(uint*)(*(int*)ptr + 16)))((nint)ptr, _handle, &d3DXPARAMETER_DESC);
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
		int num2 = System.Runtime.CompilerServices.Unsafe.As<_D3DXPARAMETER_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPARAMETER_DESC, 40)) >>> 2;
		int num3 = count * 4;
		int num4 = ((num3 >= num2) ? num2 : num3);
		float[] array;
		if (_paramClass == EffectParameterClass.Matrix)
		{
			array = GetValueSingleArray(num4);
		}
		else
		{
			array = new float[num2];
			fixed (void* ptr2 = &System.Runtime.CompilerServices.Unsafe.As<float, void>(ref array[0]))
			{
				try
				{
					int num5 = *(int*)pEffect + 132;
					int num6 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, uint, int>)(int)(*(uint*)num5))((nint)pEffect, _handle, (float*)ptr2, (uint)num4);
					if (num6 < 0)
					{
						throw GraphicsHelpers.GetExceptionFromResult((uint)num6);
					}
				}
				catch
				{
					//try-fault
					ptr2 = null;
					throw;
				}
			}
		}
		Vector4[] array2 = new Vector4[count];
		fixed (void* value = &System.Runtime.CompilerServices.Unsafe.As<Vector4, void>(ref array2[0]))
		{
			IntPtr destination = new IntPtr(value);
			Marshal.Copy(array, 0, destination, num4);
			return array2;
		}
	}

	public unsafe Quaternion GetValueQuaternion()
	{
		Quaternion result = default(Quaternion);
		ID3DXBaseEffect* ptr;
		if (pElementCollection.Count == 0)
		{
			switch (_paramClass)
			{
			case EffectParameterClass.Scalar:
			{
				ptr = pEffect;
				System.Runtime.CompilerServices.Unsafe.SkipInit(out float num);
				int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, int>)(int)(*(uint*)(*(int*)ptr + 124)))((nint)ptr, _handle, &num);
				if (num2 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
				}
				result.W = num;
				result.Z = num;
				result.Y = num;
				result.X = num;
				return result;
			}
			case EffectParameterClass.Vector:
				break;
			default:
				throw new InvalidCastException();
			}
			if (_columns != 4 || _rows != 1)
			{
				throw new InvalidCastException();
			}
		}
		ptr = pEffect;
		int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, D3DXVECTOR4*, int>)(int)(*(uint*)(*(int*)ptr + 140)))((nint)ptr, _handle, (D3DXVECTOR4*)(&result));
		if (num3 < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
		}
		return result;
	}

	public unsafe Quaternion[] GetValueQuaternionArray(int count)
	{
		//Discarded unreachable code: IL_00d8
		if (count <= 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		ID3DXBaseEffect* ptr = pEffect;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DXPARAMETER_DESC d3DXPARAMETER_DESC);
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, _D3DXPARAMETER_DESC*, int>)(int)(*(uint*)(*(int*)ptr + 16)))((nint)ptr, _handle, &d3DXPARAMETER_DESC);
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
		int num2 = System.Runtime.CompilerServices.Unsafe.As<_D3DXPARAMETER_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPARAMETER_DESC, 40)) >>> 2;
		int num3 = count * 4;
		int num4 = ((num3 >= num2) ? num2 : num3);
		float[] array;
		if (_paramClass == EffectParameterClass.Matrix)
		{
			array = GetValueSingleArray(num4);
		}
		else
		{
			array = new float[num2];
			fixed (void* ptr2 = &System.Runtime.CompilerServices.Unsafe.As<float, void>(ref array[0]))
			{
				try
				{
					int num5 = *(int*)pEffect + 132;
					int num6 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, uint, int>)(int)(*(uint*)num5))((nint)pEffect, _handle, (float*)ptr2, (uint)num4);
					if (num6 < 0)
					{
						throw GraphicsHelpers.GetExceptionFromResult((uint)num6);
					}
				}
				catch
				{
					//try-fault
					ptr2 = null;
					throw;
				}
			}
		}
		Quaternion[] array2 = new Quaternion[count];
		fixed (void* value = &System.Runtime.CompilerServices.Unsafe.As<Quaternion, void>(ref array2[0]))
		{
			IntPtr destination = new IntPtr(value);
			Marshal.Copy(array, 0, destination, num4);
			return array2;
		}
	}

	public unsafe Matrix GetValueMatrix()
	{
		Matrix result = default(Matrix);
		ID3DXBaseEffect* ptr;
		if (pElementCollection.Count == 0)
		{
			switch (_paramClass)
			{
			case EffectParameterClass.Scalar:
			{
				ptr = pEffect;
				System.Runtime.CompilerServices.Unsafe.SkipInit(out float num);
				int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, int>)(int)(*(uint*)(*(int*)ptr + 124)))((nint)ptr, _handle, &num);
				if (num2 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
				}
				result.M14 = num;
				result.M13 = num;
				result.M12 = num;
				result.M11 = num;
				result.M24 = num;
				result.M23 = num;
				result.M22 = num;
				result.M21 = num;
				result.M34 = num;
				result.M33 = num;
				result.M32 = num;
				result.M31 = num;
				result.M44 = num;
				result.M43 = num;
				result.M42 = num;
				result.M41 = num;
				return result;
			}
			default:
				throw new InvalidCastException();
			case EffectParameterClass.Matrix:
				break;
			}
		}
		ptr = pEffect;
		int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, D3DXMATRIX*, int>)(int)(*(uint*)(*(int*)ptr + 156)))((nint)ptr, _handle, (D3DXMATRIX*)(&result));
		if (num3 < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
		}
		return result;
	}

	public unsafe Matrix[] GetValueMatrixArray(int count)
	{
		if (count <= 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (_paramClass == EffectParameterClass.Matrix && pElementCollection.Count != 0)
		{
			int num = pElementCollection.Count * 16;
			float[] array = new float[num];
			fixed (void* ptr = &System.Runtime.CompilerServices.Unsafe.As<float, void>(ref array[0]))
			{
				int num2 = count * 16;
				int num3 = ((num2 >= num) ? num : num2);
				int num4 = *(int*)pEffect + 164;
				int num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, D3DXMATRIX*, uint, int>)(int)(*(uint*)num4))((nint)pEffect, _handle, (D3DXMATRIX*)ptr, (uint)((num3 + 15) / 16));
				if (num5 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num5);
				}
				Matrix[] array2 = new Matrix[count];
				fixed (void* value = &System.Runtime.CompilerServices.Unsafe.As<Matrix, void>(ref array2[0]))
				{
					IntPtr destination = new IntPtr(value);
					Marshal.Copy(array, 0, destination, num3);
					return array2;
				}
			}
		}
		throw new InvalidCastException();
	}

	public unsafe Matrix GetValueMatrixTranspose()
	{
		Matrix result = default(Matrix);
		ID3DXBaseEffect* ptr;
		if (pElementCollection.Count == 0)
		{
			switch (_paramClass)
			{
			case EffectParameterClass.Scalar:
			{
				ptr = pEffect;
				System.Runtime.CompilerServices.Unsafe.SkipInit(out float num);
				int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, float*, int>)(int)(*(uint*)(*(int*)ptr + 124)))((nint)ptr, _handle, &num);
				if (num2 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
				}
				result.M14 = num;
				result.M13 = num;
				result.M12 = num;
				result.M11 = num;
				result.M24 = num;
				result.M23 = num;
				result.M22 = num;
				result.M21 = num;
				result.M34 = num;
				result.M33 = num;
				result.M32 = num;
				result.M31 = num;
				result.M44 = num;
				result.M43 = num;
				result.M42 = num;
				result.M41 = num;
				return result;
			}
			default:
				throw new InvalidCastException();
			case EffectParameterClass.Matrix:
				break;
			}
		}
		ptr = pEffect;
		int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, D3DXMATRIX*, int>)(int)(*(uint*)(*(int*)ptr + 180)))((nint)ptr, _handle, (D3DXMATRIX*)(&result));
		if (num3 < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
		}
		return result;
	}

	public unsafe Matrix[] GetValueMatrixTransposeArray(int count)
	{
		if (count <= 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (_paramClass == EffectParameterClass.Matrix && pElementCollection.Count != 0)
		{
			int num = pElementCollection.Count * 16;
			float[] array = new float[num];
			fixed (void* ptr = &System.Runtime.CompilerServices.Unsafe.As<float, void>(ref array[0]))
			{
				int num2 = count * 16;
				int num3 = ((num2 >= num) ? num : num2);
				int num4 = *(int*)pEffect + 188;
				int num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, D3DXMATRIX*, uint, int>)(int)(*(uint*)num4))((nint)pEffect, _handle, (D3DXMATRIX*)ptr, (uint)((num3 + 15) / 16));
				if (num5 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num5);
				}
				Matrix[] array2 = new Matrix[count];
				fixed (void* value = &System.Runtime.CompilerServices.Unsafe.As<Matrix, void>(ref array2[0]))
				{
					IntPtr destination = new IntPtr(value);
					Marshal.Copy(array, 0, destination, num3);
					return array2;
				}
			}
		}
		throw new InvalidCastException();
	}

	public unsafe string GetValueString()
	{
		EffectParameterType paramType = _paramType;
		if (paramType != EffectParameterType.String)
		{
			throw new InvalidCastException();
		}
		sbyte* ptr = null;
		ID3DXBaseEffect* ptr2 = pEffect;
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, sbyte**, int>)(int)(*(uint*)(*(int*)ptr2 + 204)))((nint)ptr2, _handle, &ptr);
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
		return Marshal.PtrToStringAnsi((IntPtr)ptr);
	}

	public unsafe Texture2D GetValueTexture2D()
	{
		EffectParameterType paramType = _paramType;
		if (paramType != EffectParameterType.Texture && paramType != EffectParameterType.Texture2D)
		{
			throw new InvalidCastException();
		}
		IDirect3DBaseTexture9* ptr = null;
		IDirect3DTexture9* ptr2 = null;
		Texture2D result = null;
		ID3DXBaseEffect* ptr3 = pEffect;
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, IDirect3DBaseTexture9**, int>)(int)(*(uint*)(*(int*)ptr3 + 212)))((nint)ptr3, _handle, &ptr);
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
		if (ptr != null)
		{
			IDirect3DBaseTexture9* ptr4 = (IDirect3DBaseTexture9*)(int)(*(uint*)((byte*)ptr + 8));
			num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _GUID*, void**, int>)(int)(*(uint*)(int)(*(uint*)ptr4)))((nint)ptr4, (_GUID*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E.IID_IDirect3DTexture9), (void**)(&ptr2));
			if (num >= 0)
			{
				System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DSURFACE_DESC d3DSURFACE_DESC);
				num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DSURFACE_DESC*, int>)(int)(*(uint*)(*(int*)ptr2 + 68)))((nint)ptr2, 0u, &d3DSURFACE_DESC);
				if (num >= 0)
				{
					result = Texture2D.GetManagedObject(ptr2, _parent.GraphicsDevice, System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC, 12)));
				}
			}
			if (ptr != null)
			{
				IDirect3DBaseTexture9* intPtr = ptr;
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)intPtr + 8)))((nint)intPtr);
				ptr = null;
			}
			if (num < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num);
			}
		}
		return result;
	}

	public unsafe TextureCube GetValueTextureCube()
	{
		EffectParameterType paramType = _paramType;
		if (paramType != EffectParameterType.Texture && paramType != EffectParameterType.TextureCube)
		{
			throw new InvalidCastException();
		}
		IDirect3DBaseTexture9* ptr = null;
		IDirect3DCubeTexture9* ptr2 = null;
		TextureCube result = null;
		ID3DXBaseEffect* ptr3 = pEffect;
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, IDirect3DBaseTexture9**, int>)(int)(*(uint*)(*(int*)ptr3 + 212)))((nint)ptr3, _handle, &ptr);
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
		if (ptr != null)
		{
			IDirect3DBaseTexture9* ptr4 = (IDirect3DBaseTexture9*)(int)(*(uint*)((byte*)ptr + 8));
			num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _GUID*, void**, int>)(int)(*(uint*)(int)(*(uint*)ptr4)))((nint)ptr4, (_GUID*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E.IID_IDirect3DCubeTexture9), (void**)(&ptr2));
			if (num >= 0)
			{
				System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DSURFACE_DESC d3DSURFACE_DESC);
				num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DSURFACE_DESC*, int>)(int)(*(uint*)(*(int*)ptr2 + 68)))((nint)ptr2, 0u, &d3DSURFACE_DESC);
				if (num >= 0)
				{
					result = TextureCube.GetManagedObject(ptr2, _parent.GraphicsDevice, System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC, 12)));
				}
			}
			if (ptr != null)
			{
				IDirect3DBaseTexture9* intPtr = ptr;
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)intPtr + 8)))((nint)intPtr);
				ptr = null;
			}
			if (num < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num);
			}
		}
		return result;
	}

	public unsafe Texture3D GetValueTexture3D()
	{
		EffectParameterType paramType = _paramType;
		if (paramType != EffectParameterType.Texture && paramType != EffectParameterType.Texture3D)
		{
			throw new InvalidCastException();
		}
		IDirect3DBaseTexture9* ptr = null;
		IDirect3DVolumeTexture9* ptr2 = null;
		Texture3D result = null;
		ID3DXBaseEffect* ptr3 = pEffect;
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, IDirect3DBaseTexture9**, int>)(int)(*(uint*)(*(int*)ptr3 + 212)))((nint)ptr3, _handle, &ptr);
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
		if (ptr != null)
		{
			IDirect3DBaseTexture9* ptr4 = (IDirect3DBaseTexture9*)(int)(*(uint*)((byte*)ptr + 8));
			num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _GUID*, void**, int>)(int)(*(uint*)(int)(*(uint*)ptr4)))((nint)ptr4, (_GUID*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E.IID_IDirect3DVolumeTexture9), (void**)(&ptr2));
			if (num >= 0)
			{
				System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DVOLUME_DESC d3DVOLUME_DESC);
				num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DVOLUME_DESC*, int>)(int)(*(uint*)(*(int*)ptr2 + 68)))((nint)ptr2, 0u, &d3DVOLUME_DESC);
				if (num >= 0)
				{
					result = Texture3D.GetManagedObject(ptr2, _parent.GraphicsDevice, System.Runtime.CompilerServices.Unsafe.As<_D3DVOLUME_DESC, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DVOLUME_DESC, 12)));
				}
			}
			if (ptr != null)
			{
				IDirect3DBaseTexture9* intPtr = ptr;
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)intPtr + 8)))((nint)intPtr);
				ptr = null;
			}
			if (num < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num);
			}
		}
		return result;
	}
}
