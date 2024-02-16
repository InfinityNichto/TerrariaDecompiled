using System.Reflection;

namespace System.Runtime.Serialization;

internal sealed class ValueTypeFixupInfo
{
	private readonly long _containerID;

	private readonly FieldInfo _parentField;

	private readonly int[] _parentIndex;

	public long ContainerID => _containerID;

	public FieldInfo ParentField => _parentField;

	public int[] ParentIndex => _parentIndex;

	public ValueTypeFixupInfo(long containerID, FieldInfo member, int[] parentIndex)
	{
		if (member == null && parentIndex == null)
		{
			throw new ArgumentException(System.SR.Argument_MustSupplyParent);
		}
		if (containerID == 0L && member == null)
		{
			_containerID = containerID;
			_parentField = member;
			_parentIndex = parentIndex;
		}
		if (member != null)
		{
			if (parentIndex != null)
			{
				throw new ArgumentException(System.SR.Argument_MemberAndArray);
			}
			if (member.FieldType.IsValueType && containerID == 0L)
			{
				throw new ArgumentException(System.SR.Argument_MustSupplyContainer);
			}
		}
		_containerID = containerID;
		_parentField = member;
		_parentIndex = parentIndex;
	}
}
