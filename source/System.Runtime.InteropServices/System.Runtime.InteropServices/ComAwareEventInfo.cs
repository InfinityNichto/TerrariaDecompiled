using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices;

public class ComAwareEventInfo : EventInfo
{
	private readonly EventInfo _innerEventInfo;

	public override EventAttributes Attributes => _innerEventInfo.Attributes;

	public override Type? DeclaringType => _innerEventInfo.DeclaringType;

	public override int MetadataToken => _innerEventInfo.MetadataToken;

	public override Module Module => _innerEventInfo.Module;

	public override string Name => _innerEventInfo.Name;

	public override Type? ReflectedType => _innerEventInfo.ReflectedType;

	public ComAwareEventInfo([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)] Type type, string eventName)
	{
		_innerEventInfo = type.GetEvent(eventName);
	}

	[SupportedOSPlatform("windows")]
	public override void AddEventHandler(object target, Delegate handler)
	{
		if (Marshal.IsComObject(target))
		{
			GetDataForComInvocation(_innerEventInfo, out var sourceIid, out var dispid);
			ComEventsHelper.Combine(target, sourceIid, dispid, handler);
		}
		else
		{
			_innerEventInfo.AddEventHandler(target, handler);
		}
	}

	[SupportedOSPlatform("windows")]
	public override void RemoveEventHandler(object target, Delegate handler)
	{
		if (Marshal.IsComObject(target))
		{
			GetDataForComInvocation(_innerEventInfo, out var sourceIid, out var dispid);
			ComEventsHelper.Remove(target, sourceIid, dispid, handler);
		}
		else
		{
			_innerEventInfo.RemoveEventHandler(target, handler);
		}
	}

	public override MethodInfo? GetAddMethod(bool nonPublic)
	{
		return _innerEventInfo.GetAddMethod(nonPublic);
	}

	public override MethodInfo[] GetOtherMethods(bool nonPublic)
	{
		return _innerEventInfo.GetOtherMethods(nonPublic);
	}

	public override MethodInfo? GetRaiseMethod(bool nonPublic)
	{
		return _innerEventInfo.GetRaiseMethod(nonPublic);
	}

	public override MethodInfo? GetRemoveMethod(bool nonPublic)
	{
		return _innerEventInfo.GetRemoveMethod(nonPublic);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		return _innerEventInfo.GetCustomAttributes(attributeType, inherit);
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return _innerEventInfo.GetCustomAttributes(inherit);
	}

	public override IList<CustomAttributeData> GetCustomAttributesData()
	{
		return _innerEventInfo.GetCustomAttributesData();
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		return _innerEventInfo.IsDefined(attributeType, inherit);
	}

	private static void GetDataForComInvocation(EventInfo eventInfo, out Guid sourceIid, out int dispid)
	{
		object[] customAttributes = eventInfo.DeclaringType.GetCustomAttributes(typeof(ComEventInterfaceAttribute), inherit: false);
		if (customAttributes == null || customAttributes.Length == 0)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_NoComEventInterfaceAttribute);
		}
		if (customAttributes.Length > 1)
		{
			throw new AmbiguousMatchException(System.SR.AmbiguousMatch_MultipleEventInterfaceAttributes);
		}
		Type sourceInterface = ((ComEventInterfaceAttribute)customAttributes[0]).SourceInterface;
		Guid gUID = sourceInterface.GUID;
		MethodInfo method = sourceInterface.GetMethod(eventInfo.Name);
		Attribute customAttribute = Attribute.GetCustomAttribute(method, typeof(DispIdAttribute));
		if (customAttribute == null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_NoDispIdAttribute);
		}
		sourceIid = gUID;
		dispid = ((DispIdAttribute)customAttribute).Value;
	}
}
