using System;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public class VertexDeclaration : GraphicsResource
{
	internal VertexElement[] _elements;

	internal int _vertexStride;

	internal DeclarationBinding _binding;

	public int VertexStride => _vertexStride;

	public VertexDeclaration(int vertexStride, params VertexElement[] elements)
	{
		try
		{
			if (elements != null && elements.Length != 0)
			{
				VertexElement[] elements2 = (_elements = (VertexElement[])elements.Clone());
				_vertexStride = vertexStride;
				VertexElementValidator.Validate(vertexStride, elements2);
				goto IL_005a;
			}
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
		try
		{
			throw new ArgumentNullException("elements", FrameworkResources.NullNotAllowed);
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
		IL_005a:
		try
		{
			return;
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
	}

	public VertexDeclaration(params VertexElement[] elements)
	{
		try
		{
			if (elements != null && elements.Length != 0)
			{
				VertexElementValidator.Validate(_vertexStride = VertexElementValidator.GetVertexStride(_elements = (VertexElement[])elements.Clone()), _elements);
				goto IL_0066;
			}
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
		try
		{
			throw new ArgumentNullException("elements", FrameworkResources.NullNotAllowed);
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
		IL_0066:
		try
		{
			return;
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
	}

	private void _0021VertexDeclaration()
	{
		Unbind();
	}

	private void _007EVertexDeclaration()
	{
		Unbind();
	}

	public VertexElement[] GetVertexElements()
	{
		return (VertexElement[])_elements.Clone();
	}

	internal void Bind(GraphicsDevice device)
	{
		if (isDisposed)
		{
			throw new ObjectDisposedException(typeof(VertexDeclaration).Name);
		}
		VertexElementValidator.Validate(_vertexStride, _elements, device._profileCapabilities);
		DeclarationBinding binding = _binding;
		if (binding != null)
		{
			_parent.vertexDeclarationManager.ReleaseBinding(binding);
			_binding = null;
		}
		_binding = device.vertexDeclarationManager.CreateBinding(this);
		_parent = device;
	}

	internal void Unbind()
	{
		DeclarationBinding binding = _binding;
		if (binding != null)
		{
			_parent.vertexDeclarationManager.ReleaseBinding(binding);
			_binding = null;
		}
	}

	internal static VertexDeclaration FromType(Type vertexType)
	{
		if (vertexType == null)
		{
			throw new ArgumentNullException("vertexType", FrameworkResources.NullNotAllowed);
		}
		if (!vertexType.IsValueType)
		{
			throw new ArgumentException(string.Format(args: new object[1] { vertexType }, provider: CultureInfo.CurrentCulture, format: FrameworkResources.VertexTypeNotValueType));
		}
		if (!(Activator.CreateInstance(vertexType) is IVertexType { VertexDeclaration: var vertexDeclaration }))
		{
			throw new ArgumentException(string.Format(args: new object[1] { vertexType }, provider: CultureInfo.CurrentCulture, format: FrameworkResources.VertexTypeNotIVertexType));
		}
		if (vertexDeclaration == null)
		{
			throw new InvalidOperationException(string.Format(args: new object[1] { vertexType }, provider: CultureInfo.CurrentCulture, format: FrameworkResources.VertexTypeNullDeclaration));
		}
		if (Marshal.SizeOf(vertexType) != vertexDeclaration._vertexStride)
		{
			throw new InvalidOperationException(string.Format(args: new object[1] { vertexType }, provider: CultureInfo.CurrentCulture, format: FrameworkResources.VertexTypeWrongSize));
		}
		return vertexDeclaration;
	}

	[HandleProcessCorruptedStateExceptions]
	protected override void Dispose([MarshalAs(UnmanagedType.U1)] bool P_0)
	{
		if (P_0)
		{
			try
			{
				_007EVertexDeclaration();
				return;
			}
			finally
			{
				base.Dispose(true);
			}
		}
		try
		{
			_0021VertexDeclaration();
		}
		finally
		{
			base.Dispose(false);
		}
	}
}
