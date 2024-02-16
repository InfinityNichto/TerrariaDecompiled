using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization;

internal sealed class ReflectionXmlClassWriter : ReflectionClassWriter
{
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected override int ReflectionWriteMembers(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, ClassDataContract classContract, ClassDataContract derivedMostClassContract, int childElementIndex, XmlDictionaryString[] emptyStringArray)
	{
		int num = ((classContract.BaseContract != null) ? ReflectionWriteMembers(xmlWriter, obj, context, classContract.BaseContract, derivedMostClassContract, childElementIndex, emptyStringArray) : 0);
		childElementIndex += num;
		XmlDictionaryString[] memberNames = classContract.MemberNames;
		XmlDictionaryString @namespace = classContract.Namespace;
		context.IncrementItemCount(classContract.Members.Count);
		int num2 = 0;
		while (num2 < classContract.Members.Count)
		{
			DataMember dataMember = classContract.Members[num2];
			Type memberType = dataMember.MemberType;
			if (dataMember.IsGetOnlyCollection)
			{
				context.StoreIsGetOnlyCollection();
			}
			else
			{
				context.ResetIsGetOnlyCollection();
			}
			bool flag = true;
			object obj2 = null;
			if (!dataMember.EmitDefaultValue)
			{
				obj2 = ReflectionGetMemberValue(obj, dataMember);
				object defaultValue = XmlFormatGeneratorStatics.GetDefaultValue(memberType);
				if ((obj2 == null && defaultValue == null) || (obj2 != null && obj2.Equals(defaultValue)))
				{
					flag = false;
					if (dataMember.IsRequired)
					{
						XmlObjectSerializerWriteContext.ThrowRequiredMemberMustBeEmitted(dataMember.Name, classContract.UnderlyingType);
					}
				}
			}
			if (flag)
			{
				bool flag2 = CheckIfMemberHasConflict(dataMember, classContract, derivedMostClassContract);
				if (obj2 == null)
				{
					obj2 = ReflectionGetMemberValue(obj, dataMember);
				}
				PrimitiveDataContract memberPrimitiveContract = dataMember.MemberPrimitiveContract;
				if (flag2 || !ReflectionTryWritePrimitive(xmlWriter, context, memberType, obj2, memberNames[num2 + childElementIndex], @namespace, memberPrimitiveContract))
				{
					ReflectionWriteStartElement(xmlWriter, memberType, @namespace, @namespace.Value, dataMember.Name, 0);
					if (classContract.ChildElementNamespaces[num2 + childElementIndex] != null)
					{
						XmlDictionaryString ns = classContract.ChildElementNamespaces[num2 + childElementIndex];
						xmlWriter.WriteNamespaceDecl(ns);
					}
					ReflectionWriteValue(xmlWriter, context, memberType, obj2, flag2, null);
					ReflectionWriteEndElement(xmlWriter);
				}
				if (classContract.HasExtensionData)
				{
					context.WriteExtensionData(xmlWriter, ((IExtensibleDataObject)obj).ExtensionData, num);
				}
			}
			num2++;
			num++;
		}
		return num;
	}

	public void ReflectionWriteStartElement(XmlWriterDelegator xmlWriter, Type type, XmlDictionaryString ns, string namespaceLocal, string nameLocal, int nameIndex)
	{
		if (NeedsPrefix(type, ns))
		{
			xmlWriter.WriteStartElement("q", nameLocal, namespaceLocal);
		}
		else
		{
			xmlWriter.WriteStartElement(nameLocal, namespaceLocal);
		}
	}

	public void ReflectionWriteEndElement(XmlWriterDelegator xmlWriter)
	{
		xmlWriter.WriteEndElement();
	}

	private bool NeedsPrefix(Type type, XmlDictionaryString ns)
	{
		if (type == Globals.TypeOfXmlQualifiedName)
		{
			if (ns != null && ns.Value != null)
			{
				return ns.Value.Length > 0;
			}
			return false;
		}
		return false;
	}

	private bool CheckIfMemberHasConflict(DataMember member, ClassDataContract classContract, ClassDataContract derivedMostClassContract)
	{
		if (CheckIfConflictingMembersHaveDifferentTypes(member))
		{
			return true;
		}
		string name = member.Name;
		string @namespace = classContract.StableName.Namespace;
		ClassDataContract classDataContract = derivedMostClassContract;
		while (classDataContract != null && classDataContract != classContract)
		{
			if (@namespace == classDataContract.StableName.Namespace)
			{
				List<DataMember> members = classDataContract.Members;
				for (int i = 0; i < members.Count; i++)
				{
					if (name == members[i].Name)
					{
						return CheckIfConflictingMembersHaveDifferentTypes(members[i]);
					}
				}
			}
			classDataContract = classDataContract.BaseContract;
		}
		return false;
	}

	private bool CheckIfConflictingMembersHaveDifferentTypes(DataMember member)
	{
		while (member.ConflictingMember != null)
		{
			if (member.MemberType != member.ConflictingMember.MemberType)
			{
				return true;
			}
			member = member.ConflictingMember;
		}
		return false;
	}
}
