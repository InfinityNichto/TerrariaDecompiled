using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.Design;

public class CommandID
{
	public virtual int ID { get; }

	public virtual Guid Guid { get; }

	public CommandID(Guid menuGroup, int commandID)
	{
		Guid = menuGroup;
		ID = commandID;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is CommandID { Guid: var guid } commandID && guid.Equals(Guid))
		{
			return commandID.ID == ID;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (Guid.GetHashCode() << 2) | ID;
	}

	public override string ToString()
	{
		return $"{Guid} : {ID}";
	}
}
