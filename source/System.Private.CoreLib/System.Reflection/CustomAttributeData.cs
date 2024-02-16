using System.Collections.Generic;
using System.Text;

namespace System.Reflection;

public class CustomAttributeData
{
	public virtual Type AttributeType => Constructor.DeclaringType;

	public virtual ConstructorInfo Constructor => null;

	public virtual IList<CustomAttributeTypedArgument> ConstructorArguments => null;

	public virtual IList<CustomAttributeNamedArgument> NamedArguments => null;

	public static IList<CustomAttributeData> GetCustomAttributes(MemberInfo target)
	{
		if ((object)target == null)
		{
			throw new ArgumentNullException("target");
		}
		return target.GetCustomAttributesData();
	}

	public static IList<CustomAttributeData> GetCustomAttributes(Module target)
	{
		if ((object)target == null)
		{
			throw new ArgumentNullException("target");
		}
		return target.GetCustomAttributesData();
	}

	public static IList<CustomAttributeData> GetCustomAttributes(Assembly target)
	{
		if ((object)target == null)
		{
			throw new ArgumentNullException("target");
		}
		return target.GetCustomAttributesData();
	}

	public static IList<CustomAttributeData> GetCustomAttributes(ParameterInfo target)
	{
		if (target == null)
		{
			throw new ArgumentNullException("target");
		}
		return target.GetCustomAttributesData();
	}

	protected CustomAttributeData()
	{
	}

	public override string ToString()
	{
		Span<char> initialBuffer = stackalloc char[256];
		ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer);
		valueStringBuilder.Append('[');
		valueStringBuilder.Append(Constructor.DeclaringType.FullName);
		valueStringBuilder.Append('(');
		bool flag = true;
		IList<CustomAttributeTypedArgument> constructorArguments = ConstructorArguments;
		int count = constructorArguments.Count;
		for (int i = 0; i < count; i++)
		{
			if (!flag)
			{
				valueStringBuilder.Append(", ");
			}
			valueStringBuilder.Append(constructorArguments[i].ToString());
			flag = false;
		}
		IList<CustomAttributeNamedArgument> namedArguments = NamedArguments;
		int count2 = namedArguments.Count;
		for (int j = 0; j < count2; j++)
		{
			if (!flag)
			{
				valueStringBuilder.Append(", ");
			}
			valueStringBuilder.Append(namedArguments[j].ToString());
			flag = false;
		}
		valueStringBuilder.Append(")]");
		return valueStringBuilder.ToString();
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override bool Equals(object? obj)
	{
		return obj == this;
	}
}
