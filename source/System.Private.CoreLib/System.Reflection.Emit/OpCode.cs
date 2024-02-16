using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Reflection.Emit;

public readonly struct OpCode : IEquatable<OpCode>
{
	private readonly OpCodeValues m_value;

	private readonly int m_flags;

	private static volatile string[] g_nameCache;

	public OperandType OperandType => (OperandType)(m_flags & 0x1F);

	public FlowControl FlowControl => (FlowControl)((m_flags >> 5) & 0xF);

	public OpCodeType OpCodeType => (OpCodeType)((m_flags >> 9) & 7);

	public StackBehaviour StackBehaviourPop => (StackBehaviour)((m_flags >> 12) & 0x1F);

	public StackBehaviour StackBehaviourPush => (StackBehaviour)((m_flags >> 17) & 0x1F);

	public int Size => (m_flags >> 22) & 3;

	public short Value => (short)m_value;

	public string? Name
	{
		get
		{
			if (Size == 0)
			{
				return null;
			}
			string[] array = g_nameCache;
			if (array == null)
			{
				array = (g_nameCache = new string[287]);
			}
			OpCodeValues opCodeValues = (OpCodeValues)(ushort)Value;
			int num = (int)opCodeValues;
			if (num > 255)
			{
				if (num < 65024 || num > 65054)
				{
					return null;
				}
				num = 256 + (num - 65024);
			}
			string text = Volatile.Read(ref array[num]);
			if (text != null)
			{
				return text;
			}
			text = Enum.GetName(typeof(OpCodeValues), opCodeValues).ToLowerInvariant().Replace('_', '.');
			Volatile.Write(ref array[num], text);
			return text;
		}
	}

	internal OpCode(OpCodeValues value, int flags)
	{
		m_value = value;
		m_flags = flags;
	}

	internal bool EndsUncondJmpBlk()
	{
		return (m_flags & 0x1000000) != 0;
	}

	internal int StackChange()
	{
		return m_flags >> 28;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is OpCode obj2)
		{
			return Equals(obj2);
		}
		return false;
	}

	public bool Equals(OpCode obj)
	{
		return obj.Value == Value;
	}

	public static bool operator ==(OpCode a, OpCode b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(OpCode a, OpCode b)
	{
		return !(a == b);
	}

	public override int GetHashCode()
	{
		return Value;
	}

	public override string? ToString()
	{
		return Name;
	}
}
