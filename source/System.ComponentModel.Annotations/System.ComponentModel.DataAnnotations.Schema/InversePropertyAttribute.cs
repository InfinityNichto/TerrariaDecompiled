namespace System.ComponentModel.DataAnnotations.Schema;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class InversePropertyAttribute : Attribute
{
	public string Property { get; }

	public InversePropertyAttribute(string property)
	{
		if (string.IsNullOrWhiteSpace(property))
		{
			throw new ArgumentException(System.SR.Format(System.SR.ArgumentIsNullOrWhitespace, "property"), "property");
		}
		Property = property;
	}
}
