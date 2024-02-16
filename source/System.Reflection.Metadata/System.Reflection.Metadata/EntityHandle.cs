using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Metadata;

public readonly struct EntityHandle : IEquatable<EntityHandle>
{
	private readonly uint _vToken;

	public static readonly ModuleDefinitionHandle ModuleDefinition = new ModuleDefinitionHandle(1);

	public static readonly AssemblyDefinitionHandle AssemblyDefinition = new AssemblyDefinitionHandle(1);

	internal uint Type => _vToken & 0x7F000000u;

	internal uint VType => _vToken & 0xFF000000u;

	internal bool IsVirtual => (_vToken & 0x80000000u) != 0;

	public bool IsNil => (_vToken & 0x80FFFFFFu) == 0;

	internal int RowId => (int)(_vToken & 0xFFFFFF);

	internal uint SpecificHandleValue => _vToken & 0x80FFFFFFu;

	public HandleKind Kind => (HandleKind)(Type >> 24);

	internal int Token => (int)_vToken;

	internal EntityHandle(uint vToken)
	{
		_vToken = vToken;
	}

	public static implicit operator Handle(EntityHandle handle)
	{
		return Handle.FromVToken(handle._vToken);
	}

	public static explicit operator EntityHandle(Handle handle)
	{
		if (handle.IsHeapHandle)
		{
			Throw.InvalidCast();
		}
		return new EntityHandle(handle.EntityHandleValue);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is EntityHandle other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(EntityHandle other)
	{
		return _vToken == other._vToken;
	}

	public override int GetHashCode()
	{
		return (int)_vToken;
	}

	public static bool operator ==(EntityHandle left, EntityHandle right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(EntityHandle left, EntityHandle right)
	{
		return !left.Equals(right);
	}

	internal static int Compare(EntityHandle left, EntityHandle right)
	{
		return left._vToken.CompareTo(right._vToken);
	}
}
