using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization;

internal sealed class ReflectionXmlReader : ReflectionReader
{
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected override void ReflectionReadMembers(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString[] memberNames, XmlDictionaryString[] memberNamespaces, ClassDataContract classContract, ref object obj)
	{
		int num = classContract.MemberNames.Length;
		context.IncrementItemCount(num);
		int num2 = -1;
		GetRequiredMembers(classContract, out var firstRequiredMember);
		bool flag = firstRequiredMember < num;
		int requiredIndex = (flag ? firstRequiredMember : (-1));
		DataMember[] array = new DataMember[num];
		int num3 = ReflectionGetMembers(classContract, array);
		ExtensionDataObject extensionData = null;
		if (classContract.HasExtensionData)
		{
			extensionData = new ExtensionDataObject();
			((IExtensibleDataObject)obj).ExtensionData = extensionData;
		}
		while (XmlObjectSerializerReadContext.MoveToNextElement(xmlReader))
		{
			num2 = ((!flag) ? context.GetMemberIndex(xmlReader, memberNames, memberNamespaces, num2, extensionData) : context.GetMemberIndexWithRequiredMembers(xmlReader, memberNames, memberNamespaces, num2, requiredIndex, extensionData));
			if (num2 < array.Length)
			{
				ReflectionReadMember(xmlReader, context, classContract, ref obj, num2, array);
				requiredIndex = num2 + 1;
			}
		}
	}

	protected override string GetClassContractNamespace(ClassDataContract classContract)
	{
		return classContract.StableName.Namespace;
	}

	protected override string GetCollectionContractItemName(CollectionDataContract collectionContract)
	{
		return collectionContract.ItemName;
	}

	protected override string GetCollectionContractNamespace(CollectionDataContract collectionContract)
	{
		return collectionContract.StableName.Namespace;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected override object ReflectionReadDictionaryItem(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, CollectionDataContract collectionContract)
	{
		context.ReadAttributes(xmlReader);
		return collectionContract.ItemContract.ReadXmlValue(xmlReader, context);
	}

	private bool[] GetRequiredMembers(ClassDataContract contract, out int firstRequiredMember)
	{
		int num = contract.MemberNames.Length;
		bool[] array = new bool[num];
		GetRequiredMembers(contract, array);
		firstRequiredMember = 0;
		while (firstRequiredMember < num && !array[firstRequiredMember])
		{
			firstRequiredMember++;
		}
		return array;
	}

	private int GetRequiredMembers(ClassDataContract contract, bool[] requiredMembers)
	{
		int num = ((contract.BaseContract != null) ? GetRequiredMembers(contract.BaseContract, requiredMembers) : 0);
		List<DataMember> members = contract.Members;
		int num2 = 0;
		while (num2 < members.Count)
		{
			requiredMembers[num] = members[num2].IsRequired;
			num2++;
			num++;
		}
		return num;
	}
}
