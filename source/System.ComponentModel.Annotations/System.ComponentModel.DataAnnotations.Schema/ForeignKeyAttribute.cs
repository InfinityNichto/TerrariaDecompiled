namespace System.ComponentModel.DataAnnotations.Schema;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class ForeignKeyAttribute : Attribute
{
	public string Name { get; }

	public ForeignKeyAttribute(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException(System.SR.Format(System.SR.ArgumentIsNullOrWhitespace, "name"), "name");
		}
		Name = name;
	}
}
