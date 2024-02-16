using System.Collections.Generic;

namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
[Obsolete("AssociationAttribute has been deprecated and is not supported.")]
public sealed class AssociationAttribute : Attribute
{
	public string Name { get; }

	public string ThisKey { get; }

	public string OtherKey { get; }

	public bool IsForeignKey { get; set; }

	public IEnumerable<string> ThisKeyMembers => GetKeyMembers(ThisKey);

	public IEnumerable<string> OtherKeyMembers => GetKeyMembers(OtherKey);

	public AssociationAttribute(string name, string thisKey, string otherKey)
	{
		Name = name;
		ThisKey = thisKey;
		OtherKey = otherKey;
	}

	private static string[] GetKeyMembers(string key)
	{
		if (key == null)
		{
			return Array.Empty<string>();
		}
		return key.Replace(" ", string.Empty).Split(',');
	}
}
