namespace System.Diagnostics;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Delegate, AllowMultiple = true)]
public sealed class DebuggerDisplayAttribute : Attribute
{
	private Type _target;

	public string Value { get; }

	public string? Name { get; set; }

	public string? Type { get; set; }

	public Type? Target
	{
		get
		{
			return _target;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			TargetTypeName = value.AssemblyQualifiedName;
			_target = value;
		}
	}

	public string? TargetTypeName { get; set; }

	public DebuggerDisplayAttribute(string? value)
	{
		Value = value ?? "";
		Name = "";
		Type = "";
	}
}
