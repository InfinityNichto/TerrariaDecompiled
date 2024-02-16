namespace System.ComponentModel.DataAnnotations.Schema;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class DatabaseGeneratedAttribute : Attribute
{
	public DatabaseGeneratedOption DatabaseGeneratedOption { get; }

	public DatabaseGeneratedAttribute(DatabaseGeneratedOption databaseGeneratedOption)
	{
		if (!Enum.IsDefined(typeof(DatabaseGeneratedOption), databaseGeneratedOption))
		{
			throw new ArgumentOutOfRangeException("databaseGeneratedOption");
		}
		DatabaseGeneratedOption = databaseGeneratedOption;
	}
}
