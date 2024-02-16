using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace System.ComponentModel.DataAnnotations;

internal sealed class AssociatedMetadataTypeTypeDescriptor : CustomTypeDescriptor
{
	private static class TypeDescriptorCache
	{
		private static readonly ConcurrentDictionary<Type, Type> s_metadataTypeCache = new ConcurrentDictionary<Type, Type>();

		private static readonly ConcurrentDictionary<(Type, string), Attribute[]> s_typeMemberCache = new ConcurrentDictionary<(Type, string), Attribute[]>();

		private static readonly ConcurrentDictionary<(Type, Type), bool> s_validatedMetadataTypeCache = new ConcurrentDictionary<(Type, Type), bool>();

		public static void ValidateMetadataType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type associatedType)
		{
			(Type, Type) key = (type, associatedType);
			if (!s_validatedMetadataTypeCache.ContainsKey(key))
			{
				CheckAssociatedMetadataType(type, associatedType);
				s_validatedMetadataTypeCache.TryAdd(key, value: true);
			}
		}

		[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
		public static Type GetAssociatedMetadataType(Type type)
		{
			if (s_metadataTypeCache.TryGetValue(type, out var value))
			{
				return value;
			}
			MetadataTypeAttribute metadataTypeAttribute = (MetadataTypeAttribute)Attribute.GetCustomAttribute(type, typeof(MetadataTypeAttribute));
			if (metadataTypeAttribute != null)
			{
				value = metadataTypeAttribute.MetadataClassType;
			}
			s_metadataTypeCache.TryAdd(type, value);
			return value;
		}

		private static void CheckAssociatedMetadataType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type mainType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type associatedMetadataType)
		{
			HashSet<string> other = new HashSet<string>(from p in mainType.GetProperties()
				select p.Name);
			IEnumerable<string> first = from f in associatedMetadataType.GetFields()
				select f.Name;
			IEnumerable<string> second = from p in associatedMetadataType.GetProperties()
				select p.Name;
			HashSet<string> hashSet = new HashSet<string>(first.Concat(second), StringComparer.Ordinal);
			if (!hashSet.IsSubsetOf(other))
			{
				hashSet.ExceptWith(other);
				throw new InvalidOperationException(System.SR.Format(System.SR.AssociatedMetadataTypeTypeDescriptor_MetadataTypeContainsUnknownProperties, mainType.FullName, string.Join(", ", hashSet.ToArray())));
			}
		}

		public static Attribute[] GetAssociatedMetadata([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, string memberName)
		{
			(Type, string) key = (type, memberName);
			if (s_typeMemberCache.TryGetValue(key, out var value))
			{
				return value;
			}
			MemberTypes type2 = MemberTypes.Field | MemberTypes.Property;
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
			MemberInfo memberInfo = type.GetMember(memberName, type2, bindingAttr).FirstOrDefault();
			value = ((!(memberInfo != null)) ? Array.Empty<Attribute>() : Attribute.GetCustomAttributes(memberInfo, inherit: true));
			s_typeMemberCache.TryAdd(key, value);
			return value;
		}
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	private Type AssociatedMetadataType { get; set; }

	private bool IsSelfAssociated { get; set; }

	public AssociatedMetadataTypeTypeDescriptor(ICustomTypeDescriptor parent, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type associatedMetadataType)
		: base(parent)
	{
		AssociatedMetadataType = associatedMetadataType ?? TypeDescriptorCache.GetAssociatedMetadataType(type);
		IsSelfAssociated = type == AssociatedMetadataType;
		if (AssociatedMetadataType != null)
		{
			TypeDescriptorCache.ValidateMetadataType(type, AssociatedMetadataType);
		}
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
	{
		return GetPropertiesWithMetadata(base.GetProperties(attributes));
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
	public override PropertyDescriptorCollection GetProperties()
	{
		return GetPropertiesWithMetadata(base.GetProperties());
	}

	private PropertyDescriptorCollection GetPropertiesWithMetadata(PropertyDescriptorCollection originalCollection)
	{
		if (AssociatedMetadataType == null)
		{
			return originalCollection;
		}
		bool flag = false;
		List<PropertyDescriptor> list = new List<PropertyDescriptor>();
		foreach (PropertyDescriptor item2 in originalCollection)
		{
			Attribute[] associatedMetadata = TypeDescriptorCache.GetAssociatedMetadata(AssociatedMetadataType, item2.Name);
			PropertyDescriptor item = item2;
			if (associatedMetadata.Length != 0)
			{
				item = new MetadataPropertyDescriptorWrapper(item2, associatedMetadata);
				flag = true;
			}
			list.Add(item);
		}
		if (flag)
		{
			return new PropertyDescriptorCollection(list.ToArray(), readOnly: true);
		}
		return originalCollection;
	}

	public override AttributeCollection GetAttributes()
	{
		AttributeCollection attributeCollection = base.GetAttributes();
		if (AssociatedMetadataType != null && !IsSelfAssociated)
		{
			Attribute[] newAttributes = TypeDescriptor.GetAttributes(AssociatedMetadataType).OfType<Attribute>().ToArray();
			attributeCollection = AttributeCollection.FromExisting(attributeCollection, newAttributes);
		}
		return attributeCollection;
	}
}
