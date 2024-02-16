using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization.Json;

internal sealed class ReflectionJsonClassWriter : ReflectionClassWriter
{
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected override int ReflectionWriteMembers(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, ClassDataContract classContract, ClassDataContract derivedMostClassContract, int childElementIndex, XmlDictionaryString[] memberNames)
	{
		int num = ((classContract.BaseContract != null) ? ReflectionWriteMembers(xmlWriter, obj, context, classContract.BaseContract, derivedMostClassContract, childElementIndex, memberNames) : 0);
		childElementIndex += num;
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
				if (obj2 == null)
				{
					obj2 = ReflectionGetMemberValue(obj, dataMember);
				}
				bool flag2 = DataContractJsonSerializer.CheckIfXmlNameRequiresMapping(classContract.MemberNames[num2]);
				PrimitiveDataContract memberPrimitiveContract = dataMember.MemberPrimitiveContract;
				if (flag2 || !ReflectionTryWritePrimitive(xmlWriter, context, memberType, obj2, memberNames[num2 + childElementIndex], null, memberPrimitiveContract))
				{
					if (flag2)
					{
						XmlObjectSerializerWriteContextComplexJson.WriteJsonNameWithMapping(xmlWriter, memberNames, num2 + childElementIndex);
					}
					else
					{
						ReflectionWriteStartElement(xmlWriter, memberNames[num2 + childElementIndex]);
					}
					ReflectionWriteValue(xmlWriter, context, memberType, obj2, writeXsiType: false, null);
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

	public void ReflectionWriteStartElement(XmlWriterDelegator xmlWriter, XmlDictionaryString name)
	{
		xmlWriter.WriteStartElement(name, null);
	}

	public void ReflectionWriteStartElement(XmlWriterDelegator xmlWriter, string name)
	{
		xmlWriter.WriteStartElement(name, null);
	}

	public void ReflectionWriteEndElement(XmlWriterDelegator xmlWriter)
	{
		xmlWriter.WriteEndElement();
	}
}
