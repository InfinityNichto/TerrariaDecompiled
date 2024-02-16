using System.Text;

namespace System.Runtime.Versioning;

public static class VersioningHelper
{
	public static string MakeVersionSafeName(string? name, ResourceScope from, ResourceScope to)
	{
		return MakeVersionSafeName(name, from, to, null);
	}

	public static string MakeVersionSafeName(string? name, ResourceScope from, ResourceScope to, Type? type)
	{
		ResourceScope resourceScope = from & (ResourceScope.Machine | ResourceScope.Process | ResourceScope.AppDomain | ResourceScope.Library);
		ResourceScope resourceScope2 = to & (ResourceScope.Machine | ResourceScope.Process | ResourceScope.AppDomain | ResourceScope.Library);
		if (resourceScope > resourceScope2)
		{
			throw new ArgumentException(SR.Format(SR.Argument_ResourceScopeWrongDirection, resourceScope, resourceScope2), "from");
		}
		SxSRequirements requirements = GetRequirements(to, from);
		if ((requirements & (SxSRequirements.AssemblyName | SxSRequirements.TypeName)) != 0 && type == null)
		{
			throw new ArgumentNullException("type", SR.ArgumentNull_TypeRequiredByResourceScope);
		}
		StringBuilder stringBuilder = new StringBuilder(name);
		char value = '_';
		if ((requirements & SxSRequirements.ProcessID) != 0)
		{
			stringBuilder.Append(value);
			stringBuilder.Append('p');
			stringBuilder.Append(Environment.ProcessId);
		}
		if ((requirements & SxSRequirements.CLRInstanceID) != 0)
		{
			string cLRInstanceString = GetCLRInstanceString();
			stringBuilder.Append(value);
			stringBuilder.Append('r');
			stringBuilder.Append(cLRInstanceString);
		}
		if ((requirements & SxSRequirements.AppDomainID) != 0)
		{
			stringBuilder.Append(value);
			stringBuilder.Append("ad");
			stringBuilder.Append(AppDomain.CurrentDomain.Id);
		}
		if ((requirements & SxSRequirements.TypeName) != 0)
		{
			stringBuilder.Append(value);
			stringBuilder.Append(type.Name);
		}
		if ((requirements & SxSRequirements.AssemblyName) != 0)
		{
			stringBuilder.Append(value);
			stringBuilder.Append(type.Assembly.FullName);
		}
		return stringBuilder.ToString();
	}

	private static string GetCLRInstanceString()
	{
		return "3";
	}

	private static SxSRequirements GetRequirements(ResourceScope consumeAsScope, ResourceScope calleeScope)
	{
		SxSRequirements sxSRequirements = SxSRequirements.None;
		switch (calleeScope & (ResourceScope.Machine | ResourceScope.Process | ResourceScope.AppDomain | ResourceScope.Library))
		{
		case ResourceScope.Machine:
			switch (consumeAsScope & (ResourceScope.Machine | ResourceScope.Process | ResourceScope.AppDomain | ResourceScope.Library))
			{
			case ResourceScope.Process:
				sxSRequirements |= SxSRequirements.ProcessID;
				break;
			case ResourceScope.AppDomain:
				sxSRequirements |= SxSRequirements.AppDomainID | SxSRequirements.ProcessID | SxSRequirements.CLRInstanceID;
				break;
			default:
				throw new ArgumentException(SR.Format(SR.Argument_BadResourceScopeTypeBits, consumeAsScope), "consumeAsScope");
			case ResourceScope.Machine:
				break;
			}
			break;
		case ResourceScope.Process:
			if ((consumeAsScope & ResourceScope.AppDomain) != 0)
			{
				sxSRequirements |= SxSRequirements.AppDomainID | SxSRequirements.CLRInstanceID;
			}
			break;
		default:
			throw new ArgumentException(SR.Format(SR.Argument_BadResourceScopeTypeBits, calleeScope), "calleeScope");
		case ResourceScope.AppDomain:
			break;
		}
		switch (calleeScope & (ResourceScope.Private | ResourceScope.Assembly))
		{
		case ResourceScope.None:
			switch (consumeAsScope & (ResourceScope.Private | ResourceScope.Assembly))
			{
			case ResourceScope.Assembly:
				sxSRequirements |= SxSRequirements.AssemblyName;
				break;
			case ResourceScope.Private:
				sxSRequirements |= SxSRequirements.AssemblyName | SxSRequirements.TypeName;
				break;
			default:
				throw new ArgumentException(SR.Format(SR.Argument_BadResourceScopeVisibilityBits, consumeAsScope), "consumeAsScope");
			case ResourceScope.None:
				break;
			}
			break;
		case ResourceScope.Assembly:
			if ((consumeAsScope & ResourceScope.Private) != 0)
			{
				sxSRequirements |= SxSRequirements.TypeName;
			}
			break;
		default:
			throw new ArgumentException(SR.Format(SR.Argument_BadResourceScopeVisibilityBits, calleeScope), "calleeScope");
		case ResourceScope.Private:
			break;
		}
		return sxSRequirements;
	}
}
