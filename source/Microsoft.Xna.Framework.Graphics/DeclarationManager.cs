using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using std;

namespace Microsoft.Xna.Framework.Graphics;

internal class DeclarationManager : IEqualityComparer<VertexElement[]>
{
	private GraphicsDevice device;

	private Dictionary<VertexElement[], DeclarationBinding> bindings;

	private DeclarationBinding[] currentDeclarations;

	private int currentDeclarationCount;

	internal DeclarationManager(GraphicsDevice device)
	{
		this.device = device;
		base._002Ector();
		bindings = new Dictionary<VertexElement[], DeclarationBinding>(this);
		currentDeclarations = new DeclarationBinding[device._profileCapabilities.MaxVertexStreams];
	}

	internal void ReleaseAllDeclarations()
	{
		bool lockTaken = false;
		try
		{
			Monitor.Enter(this, ref lockTaken);
			ClearCurrent();
			Dictionary<VertexElement[], DeclarationBinding>.ValueCollection.Enumerator enumerator = bindings.Values.GetEnumerator();
			while (enumerator.MoveNext())
			{
				DeclarationBinding current = enumerator.Current;
				current.root.RecursiveRelease();
				current.indirectOffspring.Clear();
			}
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(this);
			}
		}
	}

	internal DeclarationBinding CreateBinding(VertexDeclaration declaration)
	{
		DeclarationBinding value = null;
		bool lockTaken = false;
		try
		{
			Monitor.Enter(this, ref lockTaken);
			VertexElement[] elements = declaration._elements;
			if (bindings.TryGetValue(elements, out value))
			{
				value.referenceCount++;
				return value;
			}
			value = new DeclarationBinding(elements);
			bindings.Add(elements, value);
			return value;
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(this);
			}
		}
	}

	internal void ReleaseBinding(DeclarationBinding binding)
	{
		bool lockTaken = false;
		try
		{
			Monitor.Enter(this, ref lockTaken);
			if (--binding.referenceCount <= 0)
			{
				binding.root.RecursiveRelease();
				List<DeclarationBinding.BindingNode>.Enumerator enumerator = new List<DeclarationBinding.BindingNode>(binding.indirectOffspring.Keys).GetEnumerator();
				while (enumerator.MoveNext())
				{
					enumerator.Current.RemoveChild(binding);
				}
				bindings.Remove(binding.elements);
			}
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(this);
			}
		}
	}

	internal unsafe void SetVertexDeclaration(VertexBufferBinding* vertexBuffers, int count)
	{
		int num = (int)stackalloc byte[_003CModule_003E.__CxxQueryExceptionSize()];
		bool lockTaken = false;
		try
		{
			Monitor.Enter(this, ref lockTaken);
			bool flag = false;
			for (int i = 0; i < count; i++)
			{
				VertexDeclaration vertexDeclaration = ((VertexBufferBinding*)(System.Runtime.CompilerServices.Unsafe.SizeOf<VertexBufferBinding>() * i + (byte*)vertexBuffers))->_vertexBuffer._vertexDeclaration;
				DeclarationBinding binding = vertexDeclaration._binding;
				if (binding != currentDeclarations[i])
				{
					if (binding == null || vertexDeclaration.GraphicsDevice != device)
					{
						vertexDeclaration.Bind(device);
						binding = vertexDeclaration._binding;
					}
					currentDeclarations[i] = binding;
					flag = true;
				}
			}
			for (int j = count; j < currentDeclarationCount; j++)
			{
				currentDeclarations[j] = null;
				flag = true;
			}
			currentDeclarationCount = count;
			if (flag)
			{
				SetNativeDeclaration();
			}
		}
		catch when (((Func<bool>)delegate
		{
			// Could not convert BlockContainer to single expression
			uint exceptionCode = (uint)Marshal.GetExceptionCode();
			return (byte)_003CModule_003E.__CxxExceptionFilter((void*)Marshal.GetExceptionPointers(), null, 0, null) != 0;
		}).Invoke())
		{
			uint num2 = 0u;
			_003CModule_003E.__CxxRegisterExceptionObject((void*)Marshal.GetExceptionPointers(), (void*)num);
			try
			{
				try
				{
					ClearCurrent();
					_003CModule_003E._CxxThrowException(null, null);
					return;
				}
				catch when (((Func<bool>)delegate
				{
					// Could not convert BlockContainer to single expression
					num2 = (uint)_003CModule_003E.__CxxDetectRethrow((void*)Marshal.GetExceptionPointers());
					return (byte)num2 != 0;
				}).Invoke())
				{
				}
				if (num2 != 0)
				{
					throw;
				}
			}
			finally
			{
				_003CModule_003E.__CxxUnregisterExceptionObject((void*)num, (int)num2);
			}
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(this);
			}
		}
	}

	internal unsafe void SetVertexDeclaration(VertexDeclaration declaration)
	{
		int num = (int)stackalloc byte[_003CModule_003E.__CxxQueryExceptionSize()];
		bool lockTaken = false;
		try
		{
			Monitor.Enter(this, ref lockTaken);
			if (currentDeclarationCount != 1 || declaration._binding != currentDeclarations[0])
			{
				if (declaration._binding == null || declaration.GraphicsDevice != device)
				{
					declaration.Bind(device);
				}
				currentDeclarations[0] = declaration._binding;
				for (int i = 1; i < currentDeclarationCount; i++)
				{
					currentDeclarations[i] = null;
				}
				currentDeclarationCount = 1;
				SetNativeDeclaration();
			}
		}
		catch when (((Func<bool>)delegate
		{
			// Could not convert BlockContainer to single expression
			uint exceptionCode = (uint)Marshal.GetExceptionCode();
			return (byte)_003CModule_003E.__CxxExceptionFilter((void*)Marshal.GetExceptionPointers(), null, 0, null) != 0;
		}).Invoke())
		{
			uint num2 = 0u;
			_003CModule_003E.__CxxRegisterExceptionObject((void*)Marshal.GetExceptionPointers(), (void*)num);
			try
			{
				try
				{
					ClearCurrent();
					_003CModule_003E._CxxThrowException(null, null);
					return;
				}
				catch when (((Func<bool>)delegate
				{
					// Could not convert BlockContainer to single expression
					num2 = (uint)_003CModule_003E.__CxxDetectRethrow((void*)Marshal.GetExceptionPointers());
					return (byte)num2 != 0;
				}).Invoke())
				{
				}
				if (num2 != 0)
				{
					throw;
				}
			}
			finally
			{
				_003CModule_003E.__CxxUnregisterExceptionObject((void*)num, (int)num2);
			}
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(this);
			}
		}
	}

	private void ClearCurrent()
	{
		int num = 0;
		if (0 < currentDeclarationCount)
		{
			do
			{
				currentDeclarations[num] = null;
				num++;
			}
			while (num < currentDeclarationCount);
		}
		currentDeclarationCount = 0;
	}

	private unsafe void SetNativeDeclaration()
	{
		DeclarationBinding.BindingNode bindingNode = currentDeclarations[0].root;
		int num = 1;
		if (1 < currentDeclarationCount)
		{
			do
			{
				DeclarationBinding.BindingNode bindingNode2 = bindingNode.GetChild(currentDeclarations[num]);
				if (bindingNode2 == null)
				{
					bindingNode2 = new DeclarationBinding.BindingNode();
					bindingNode.AddChild(currentDeclarations[num], bindingNode2);
					currentDeclarations[num].indirectOffspring.Add(bindingNode, value: true);
				}
				bindingNode = bindingNode2;
				num++;
			}
			while (num < currentDeclarationCount);
		}
		if (bindingNode.pDecl == null)
		{
			CreateNativeDeclaration(bindingNode);
		}
		int num2 = _003CModule_003E.Microsoft_002EXna_002EFramework_002EGraphics_002EStateTrackerDevice_002ESetVertexDeclaration(device.pStateTracker, bindingNode.pDecl, bindingNode.pSemantics);
		if (num2 < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
		}
	}

	private unsafe void CreateNativeDeclaration(DeclarationBinding.BindingNode node)
	{
		int num = 0;
		int num2 = 0;
		int num3 = currentDeclarationCount;
		if (0 < num3)
		{
			DeclarationBinding[] array = currentDeclarations;
			do
			{
				num = (int)((nint)array[num2].elements.LongLength + num);
				num2++;
			}
			while (num2 < num3);
		}
		int maxVertexStreams = device._profileCapabilities.MaxVertexStreams;
		if (num > maxVertexStreams)
		{
			device._profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileMaxVertexElements, maxVertexStreams);
		}
		int num4 = num + 1;
		_D3DVERTEXELEMENT9* ptr = (_D3DVERTEXELEMENT9*)_003CModule_003E.new_005B_005D(((uint)num4 > 536870911u) ? uint.MaxValue : ((uint)(num4 << 3)), (nothrow_t*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E.std_002Enothrow));
		if (ptr == null)
		{
			throw new OutOfMemoryException();
		}
		try
		{
			int num5 = 0;
			for (int i = 0; i < currentDeclarationCount; i++)
			{
				VertexElement[] elements = currentDeclarations[i].elements;
				for (int j = 0; j < (nint)elements.LongLength; j++)
				{
					_D3DVERTEXELEMENT9* ptr2 = (_D3DVERTEXELEMENT9*)(num5 * 8 + (byte*)ptr);
					*(short*)ptr2 = (short)i;
					*(short*)((byte*)ptr2 + 2) = (short)elements[j].Offset;
					((byte*)ptr2)[4] = _003CModule_003E.ConvertXnaVertexElementFormatToDx(elements[j].VertexElementFormat);
					((byte*)ptr2)[5] = 0;
					((byte*)ptr2)[6] = _003CModule_003E.ConvertXnaVertexElementUsageToDx(elements[j].VertexElementUsage);
					((byte*)ptr2)[7] = (byte)elements[j].UsageIndex;
					while (IsDuplicateElement(ptr, num5))
					{
						byte b = (byte)(((byte*)ptr2)[7] + 1);
						((byte*)ptr2)[7] = b;
						if (b >= device._profileCapabilities.MaxVertexStreams)
						{
							throw new ArgumentException(string.Format(args: new object[2]
							{
								elements[j].VertexElementUsage,
								elements[j].UsageIndex
							}, provider: CultureInfo.CurrentCulture, format: FrameworkResources.DuplicateVertexElement));
						}
					}
					num5++;
				}
			}
			System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DVERTEXELEMENT9 d3DVERTEXELEMENT);
			*(short*)(&d3DVERTEXELEMENT) = 255;
			System.Runtime.CompilerServices.Unsafe.As<_D3DVERTEXELEMENT9, short>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DVERTEXELEMENT, 2)) = 0;
			System.Runtime.CompilerServices.Unsafe.As<_D3DVERTEXELEMENT9, sbyte>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DVERTEXELEMENT, 4)) = 17;
			System.Runtime.CompilerServices.Unsafe.As<_D3DVERTEXELEMENT9, sbyte>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DVERTEXELEMENT, 5)) = 0;
			System.Runtime.CompilerServices.Unsafe.As<_D3DVERTEXELEMENT9, sbyte>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DVERTEXELEMENT, 6)) = 0;
			System.Runtime.CompilerServices.Unsafe.As<_D3DVERTEXELEMENT9, sbyte>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DVERTEXELEMENT, 7)) = 0;
			// IL cpblk instruction
			System.Runtime.CompilerServices.Unsafe.CopyBlockUnaligned(num5 * 8 + (byte*)ptr, ref d3DVERTEXELEMENT, 8);
			IDirect3DVertexDeclaration9* ptr3 = null;
			IDirect3DDevice9* pComPtr = device.pComPtr;
			int num6 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DVERTEXELEMENT9*, IDirect3DVertexDeclaration9**, int>)(int)(*(uint*)(*(int*)pComPtr + 344)))((nint)pComPtr, ptr, &ptr3);
			if (num6 < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num6);
			}
			VertexShaderInputSemantics* ptr4 = (VertexShaderInputSemantics*)_003CModule_003E.@new(32u, (nothrow_t*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E.std_002Enothrow));
			VertexShaderInputSemantics* ptr5;
			try
			{
				ptr5 = ((ptr4 == null) ? null : _003CModule_003E.Microsoft_002EXna_002EFramework_002EGraphics_002EVertexShaderInputSemantics_002E_007Bctor_007D(ptr4, ptr, (uint)num));
				VertexShaderInputSemantics* ptr6 = ptr5;
			}
			catch
			{
				//try-fault
				_003CModule_003E.delete(ptr4, (nothrow_t*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E.std_002Enothrow));
				throw;
			}
			node.pSemantics = ptr5;
			if (ptr5 == null)
			{
				IDirect3DVertexDeclaration9* intPtr = ptr3;
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)intPtr + 8)))((nint)intPtr);
				throw new OutOfMemoryException();
			}
			node.pDecl = ptr3;
		}
		finally
		{
			_003CModule_003E.delete_005B_005D(ptr);
		}
	}

	[return: MarshalAs(UnmanagedType.U1)]
	private unsafe static bool IsDuplicateElement(_D3DVERTEXELEMENT9* elements, int pos)
	{
		int num = 0;
		if (0 < pos)
		{
			byte b = (pos * 8 + (byte*)elements)[6];
			_D3DVERTEXELEMENT9* ptr = (_D3DVERTEXELEMENT9*)((byte*)elements + 7);
			do
			{
				if (b != *((byte*)ptr - 1) || (pos * 8 + (byte*)elements)[7] != *(byte*)ptr)
				{
					num++;
					ptr = (_D3DVERTEXELEMENT9*)((byte*)ptr + 8);
					continue;
				}
				return true;
			}
			while (num < pos);
		}
		return false;
	}

	private int GetHashCode(VertexElement[] obj)
	{
		int num = 0;
		int num2 = 0;
		if (0 < (nint)obj.LongLength)
		{
			do
			{
				num ^= obj[num2].GetHashCode();
				num2++;
			}
			while (num2 < (nint)obj.LongLength);
		}
		return num;
	}

	int IEqualityComparer<VertexElement[]>.GetHashCode(VertexElement[] obj)
	{
		//ILSpy generated this explicit interface implementation from .override directive in GetHashCode
		return this.GetHashCode(obj);
	}

	[return: MarshalAs(UnmanagedType.U1)]
	private bool Equals(VertexElement[] x, VertexElement[] y)
	{
		int num = x.Length;
		if ((nint)num != (nint)y.LongLength)
		{
			return false;
		}
		int num2 = 0;
		if (0 < num)
		{
			do
			{
				if (!(x[num2] != y[num2]))
				{
					num2++;
					continue;
				}
				return false;
			}
			while (num2 < (nint)x.LongLength);
		}
		return true;
	}

	bool IEqualityComparer<VertexElement[]>.Equals(VertexElement[] x, VertexElement[] y)
	{
		//ILSpy generated this explicit interface implementation from .override directive in Equals
		return this.Equals(x, y);
	}
}
