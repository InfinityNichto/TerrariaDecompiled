using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Xml.Schema;

namespace System.Xml.Serialization;

internal sealed class ReflectionXmlSerializationWriter : XmlSerializationWriter
{
	[Flags]
	private enum WritePrimitiveMethodRequirement
	{
		None = 0,
		Raw = 1,
		WriteAttribute = 2,
		WriteElementString = 4,
		WriteNullableStringLiteral = 8,
		Encoded = 0x10
	}

	private readonly XmlMapping _mapping;

	public ReflectionXmlSerializationWriter(XmlMapping xmlMapping, XmlWriter xmlWriter, XmlSerializerNamespaces namespaces, string encodingStyle, string id)
	{
		Init(xmlWriter, namespaces, encodingStyle, id, null);
		if (!xmlMapping.IsWriteable || !xmlMapping.GenerateSerializer)
		{
			throw new ArgumentException(System.SR.Format(System.SR.XmlInternalError, "xmlMapping"));
		}
		if (xmlMapping is XmlTypeMapping || xmlMapping is XmlMembersMapping)
		{
			_mapping = xmlMapping;
			return;
		}
		throw new ArgumentException(System.SR.Format(System.SR.XmlInternalError, "xmlMapping"));
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	protected override void InitCallbacks()
	{
		TypeScope scope = _mapping.Scope;
		foreach (TypeMapping typeMapping in scope.TypeMappings)
		{
			if (typeMapping.IsSoap && (typeMapping is StructMapping || typeMapping is EnumMapping) && !typeMapping.TypeDesc.IsRoot)
			{
				AddWriteCallback(typeMapping.TypeDesc.Type, typeMapping.TypeName, typeMapping.Namespace, CreateXmlSerializationWriteCallback(typeMapping, typeMapping.TypeName, typeMapping.Namespace, typeMapping.TypeDesc.IsNullable));
			}
		}
	}

	[RequiresUnreferencedCode("calls WriteObjectOfTypeElement")]
	public void WriteObject(object o)
	{
		XmlMapping mapping = _mapping;
		if (mapping is XmlTypeMapping mapping2)
		{
			WriteObjectOfTypeElement(o, mapping2);
		}
		else if (mapping is XmlMembersMapping xmlMembersMapping)
		{
			GenerateMembersElement(o, xmlMembersMapping);
		}
	}

	[RequiresUnreferencedCode("calls GenerateTypeElement")]
	private void WriteObjectOfTypeElement(object o, XmlTypeMapping mapping)
	{
		GenerateTypeElement(o, mapping);
	}

	[RequiresUnreferencedCode("calls WriteReferencedElements")]
	private void GenerateTypeElement(object o, XmlTypeMapping xmlMapping)
	{
		ElementAccessor accessor = xmlMapping.Accessor;
		TypeMapping mapping = accessor.Mapping;
		WriteStartDocument();
		if (o == null)
		{
			string ns = ((accessor.Form == XmlSchemaForm.Qualified) ? accessor.Namespace : string.Empty);
			if (accessor.IsNullable)
			{
				if (mapping.IsSoap)
				{
					WriteNullTagEncoded(accessor.Name, ns);
				}
				else
				{
					WriteNullTagLiteral(accessor.Name, ns);
				}
			}
			else
			{
				WriteEmptyTag(accessor.Name, ns);
			}
			return;
		}
		if (!mapping.TypeDesc.IsValueType && !mapping.TypeDesc.Type.IsPrimitive)
		{
			TopLevelElement();
		}
		WriteMember(o, null, new ElementAccessor[1] { accessor }, null, null, mapping.TypeDesc, !accessor.IsSoap);
		if (mapping.IsSoap)
		{
			WriteReferencedElements();
		}
	}

	[RequiresUnreferencedCode("calls WriteElements")]
	private void WriteMember(object o, object choiceSource, ElementAccessor[] elements, TextAccessor text, ChoiceIdentifierAccessor choice, TypeDesc memberTypeDesc, bool writeAccessors)
	{
		if (memberTypeDesc.IsArrayLike && (elements.Length != 1 || !(elements[0].Mapping is ArrayMapping)))
		{
			WriteArray(o, choiceSource, elements, text, choice, memberTypeDesc);
		}
		else
		{
			WriteElements(o, choiceSource, elements, text, choice, writeAccessors, memberTypeDesc.IsNullable);
		}
	}

	[RequiresUnreferencedCode("calls WriteArrayItems")]
	private void WriteArray(object o, object choiceSource, ElementAccessor[] elements, TextAccessor text, ChoiceIdentifierAccessor choice, TypeDesc arrayTypeDesc)
	{
		if ((elements.Length != 0 || text != null) && (!arrayTypeDesc.IsNullable || o != null))
		{
			if (choice != null && (choiceSource == null || ((Array)choiceSource).Length < ((Array)o).Length))
			{
				throw CreateInvalidChoiceIdentifierValueException(choice.Mapping.TypeDesc.FullName, choice.MemberName);
			}
			WriteArrayItems(elements, text, choice, arrayTypeDesc, o);
		}
	}

	[RequiresUnreferencedCode("calls WriteElements")]
	private void WriteArrayItems(ElementAccessor[] elements, TextAccessor text, ChoiceIdentifierAccessor choice, TypeDesc arrayTypeDesc, object o)
	{
		if (o is IList list)
		{
			for (int i = 0; i < list.Count; i++)
			{
				object o2 = list[i];
				WriteElements(o2, null, elements, text, choice, writeAccessors: true, isNullable: true);
			}
			return;
		}
		IEnumerable enumerable = o as IEnumerable;
		IEnumerator enumerator = enumerable.GetEnumerator();
		if (enumerator != null)
		{
			while (enumerator.MoveNext())
			{
				object current = enumerator.Current;
				WriteElements(current, null, elements, text, choice, writeAccessors: true, isNullable: true);
			}
		}
	}

	[RequiresUnreferencedCode("calls CreateUnknownTypeException")]
	private void WriteElements(object o, object enumSource, ElementAccessor[] elements, TextAccessor text, ChoiceIdentifierAccessor choice, bool writeAccessors, bool isNullable)
	{
		if (elements.Length == 0 && text == null)
		{
			return;
		}
		if (elements.Length == 1 && text == null)
		{
			WriteElement(o, elements[0], writeAccessors);
		}
		else
		{
			if (isNullable && choice == null && o == null)
			{
				return;
			}
			int num = 0;
			List<ElementAccessor> list = new List<ElementAccessor>();
			ElementAccessor elementAccessor = null;
			foreach (ElementAccessor elementAccessor2 in elements)
			{
				if (elementAccessor2.Any)
				{
					num++;
					if (elementAccessor2.Name != null && elementAccessor2.Name.Length > 0)
					{
						list.Add(elementAccessor2);
					}
					else if (elementAccessor == null)
					{
						elementAccessor = elementAccessor2;
					}
				}
				else if (choice != null)
				{
					if (o != null && o.GetType() == elementAccessor2.Mapping.TypeDesc.Type)
					{
						WriteElement(o, elementAccessor2, writeAccessors);
						return;
					}
				}
				else
				{
					TypeDesc typeDesc = (elementAccessor2.IsUnbounded ? elementAccessor2.Mapping.TypeDesc.CreateArrayTypeDesc() : elementAccessor2.Mapping.TypeDesc);
					if (o.GetType() == typeDesc.Type)
					{
						WriteElement(o, elementAccessor2, writeAccessors);
						return;
					}
				}
			}
			if (num > 0 && o is XmlElement xmlElement)
			{
				foreach (ElementAccessor item in list)
				{
					if (item.Name == xmlElement.Name && item.Namespace == xmlElement.NamespaceURI)
					{
						WriteElement(xmlElement, item, writeAccessors);
						return;
					}
				}
				if (choice != null)
				{
					throw CreateChoiceIdentifierValueException(choice.Mapping.TypeDesc.FullName, choice.MemberName, xmlElement.Name, xmlElement.NamespaceURI);
				}
				if (elementAccessor == null)
				{
					throw CreateUnknownAnyElementException(xmlElement.Name, xmlElement.NamespaceURI);
				}
				WriteElement(xmlElement, elementAccessor, writeAccessors);
			}
			else if (text != null)
			{
				WriteText(o, text);
			}
			else if (elements.Length != 0 && o != null)
			{
				throw CreateUnknownTypeException(o);
			}
		}
	}

	private void WriteText(object o, TextAccessor text)
	{
		if (text.Mapping is PrimitiveMapping primitiveMapping)
		{
			string stringValue;
			if (text.Mapping is EnumMapping mapping)
			{
				stringValue = WriteEnumMethod(mapping, o);
			}
			else
			{
				WritePrimitiveValue(primitiveMapping.TypeDesc, o, isElement: false, out stringValue);
			}
			if (o is byte[] value)
			{
				WriteValue(value);
			}
			else
			{
				WriteValue(stringValue);
			}
		}
		else if (text.Mapping is SpecialMapping specialMapping)
		{
			TypeKind kind = specialMapping.TypeDesc.Kind;
			if (kind != TypeKind.Node)
			{
				throw new InvalidOperationException(System.SR.XmlInternalError);
			}
			((XmlNode)o).WriteTo(base.Writer);
		}
	}

	[RequiresUnreferencedCode("calls WritePotentiallyReferencingElement")]
	private void WriteElement(object o, ElementAccessor element, bool writeAccessor)
	{
		string text = (writeAccessor ? element.Name : element.Mapping.TypeName);
		string ns = ((element.Any && element.Name.Length == 0) ? null : ((element.Form != XmlSchemaForm.Qualified) ? string.Empty : (writeAccessor ? element.Namespace : element.Mapping.Namespace)));
		if (element.Mapping is NullableMapping nullableMapping)
		{
			if (o != null)
			{
				ElementAccessor elementAccessor = element.Clone();
				elementAccessor.Mapping = nullableMapping.BaseMapping;
				WriteElement(o, elementAccessor, writeAccessor);
			}
			else if (element.IsNullable)
			{
				WriteNullTagLiteral(element.Name, ns);
			}
			return;
		}
		if (element.Mapping is ArrayMapping)
		{
			ArrayMapping arrayMapping = element.Mapping as ArrayMapping;
			if (element.IsNullable && o == null)
			{
				WriteNullTagLiteral(element.Name, (element.Form == XmlSchemaForm.Qualified) ? element.Namespace : string.Empty);
				return;
			}
			if (arrayMapping.IsSoap)
			{
				if (arrayMapping.Elements == null || arrayMapping.Elements.Length != 1)
				{
					throw new InvalidOperationException(System.SR.XmlInternalError);
				}
				if (!writeAccessor)
				{
					WritePotentiallyReferencingElement(text, ns, o, arrayMapping.TypeDesc.Type, suppressReference: true, element.IsNullable);
				}
				else
				{
					WritePotentiallyReferencingElement(text, ns, o, null, suppressReference: false, element.IsNullable);
				}
				return;
			}
			if (element.IsUnbounded)
			{
				IEnumerable enumerable = (IEnumerable)o;
				{
					foreach (object item in enumerable)
					{
						element.IsUnbounded = false;
						WriteElement(item, element, writeAccessor);
						element.IsUnbounded = true;
					}
					return;
				}
			}
			if (o != null)
			{
				WriteStartElement(text, ns, writePrefixed: false);
				WriteArrayItems(arrayMapping.ElementsSortedByDerivation, null, null, arrayMapping.TypeDesc, o);
				WriteEndElement();
			}
			return;
		}
		if (element.Mapping is EnumMapping)
		{
			if (element.Mapping.IsSoap)
			{
				base.Writer.WriteStartElement(text, ns);
				WriteEnumMethod((EnumMapping)element.Mapping, o);
				WriteEndElement();
			}
			else
			{
				WritePrimitive(WritePrimitiveMethodRequirement.WriteElementString, text, ns, element.Default, o, element.Mapping, writeXsiType: false, isElement: true, element.IsNullable);
			}
			return;
		}
		if (element.Mapping is PrimitiveMapping)
		{
			PrimitiveMapping primitiveMapping = element.Mapping as PrimitiveMapping;
			if (primitiveMapping.TypeDesc == ReflectionXmlSerializationReader.QnameTypeDesc)
			{
				WriteQualifiedNameElement(text, ns, element.Default, (XmlQualifiedName)o, element.IsNullable, primitiveMapping.IsSoap, primitiveMapping);
			}
			else if (o == null && element.IsNullable)
			{
				if (primitiveMapping.IsSoap)
				{
					WriteNullTagEncoded(element.Name, ns);
				}
				else
				{
					WriteNullTagLiteral(element.Name, ns);
				}
			}
			else
			{
				WritePrimitiveMethodRequirement writePrimitiveMethodRequirement = (primitiveMapping.IsSoap ? WritePrimitiveMethodRequirement.Encoded : WritePrimitiveMethodRequirement.None);
				WritePrimitiveMethodRequirement writePrimitiveMethodRequirement2 = (primitiveMapping.TypeDesc.XmlEncodingNotRequired ? WritePrimitiveMethodRequirement.Raw : WritePrimitiveMethodRequirement.None);
				WritePrimitive(element.IsNullable ? (WritePrimitiveMethodRequirement.WriteNullableStringLiteral | writePrimitiveMethodRequirement | writePrimitiveMethodRequirement2) : (WritePrimitiveMethodRequirement.WriteElementString | writePrimitiveMethodRequirement2), text, ns, element.Default, o, primitiveMapping, primitiveMapping.IsSoap, isElement: true, element.IsNullable);
			}
			return;
		}
		if (element.Mapping is StructMapping)
		{
			StructMapping structMapping = element.Mapping as StructMapping;
			if (structMapping.IsSoap)
			{
				WritePotentiallyReferencingElement(text, ns, o, (!writeAccessor) ? structMapping.TypeDesc.Type : null, !writeAccessor, element.IsNullable);
			}
			else
			{
				WriteStructMethod(structMapping, text, ns, o, element.IsNullable, needType: false);
			}
			return;
		}
		if (element.Mapping is SpecialMapping)
		{
			if (element.Mapping is SerializableMapping)
			{
				WriteSerializable((IXmlSerializable)o, text, ns, element.IsNullable, !element.Any);
				return;
			}
			if (o is XmlNode node)
			{
				WriteElementLiteral(node, text, ns, element.IsNullable, element.Any);
				return;
			}
			throw CreateInvalidAnyTypeException(o);
		}
		throw new InvalidOperationException(System.SR.XmlInternalError);
	}

	[RequiresUnreferencedCode("calls WriteStructMethod")]
	private XmlSerializationWriteCallback CreateXmlSerializationWriteCallback(TypeMapping mapping, string name, string ns, bool isNullable)
	{
		StructMapping structMapping = mapping as StructMapping;
		if (structMapping != null)
		{
			return Wrapper;
		}
		EnumMapping enumMapping = mapping as EnumMapping;
		if (enumMapping != null)
		{
			return delegate(object o)
			{
				WriteEnumMethod(enumMapping, o);
			};
		}
		throw new InvalidOperationException(System.SR.XmlInternalError);
		[RequiresUnreferencedCode("calls WriteStructMethod")]
		void Wrapper(object o)
		{
			WriteStructMethod(structMapping, name, ns, o, isNullable, needType: false);
		}
	}

	private void WriteQualifiedNameElement(string name, string ns, object defaultValue, XmlQualifiedName o, bool nullable, bool isSoap, PrimitiveMapping mapping)
	{
		if (defaultValue != null && defaultValue != DBNull.Value && mapping.TypeDesc.HasDefaultSupport && IsDefaultValue(mapping, o, defaultValue, nullable))
		{
			return;
		}
		if (isSoap)
		{
			if (nullable)
			{
				WriteNullableQualifiedNameEncoded(name, ns, o, new XmlQualifiedName(mapping.TypeName, mapping.Namespace));
			}
			else
			{
				WriteElementQualifiedName(name, ns, o, new XmlQualifiedName(mapping.TypeName, mapping.Namespace));
			}
		}
		else if (nullable)
		{
			WriteNullableQualifiedNameLiteral(name, ns, o);
		}
		else
		{
			WriteElementQualifiedName(name, ns, o);
		}
	}

	[RequiresUnreferencedCode("calls WriteTypedPrimitive")]
	private void WriteStructMethod(StructMapping mapping, string n, string ns, object o, bool isNullable, bool needType)
	{
		if (mapping.IsSoap && mapping.TypeDesc.IsRoot)
		{
			return;
		}
		if (!mapping.IsSoap)
		{
			if (o == null)
			{
				if (isNullable)
				{
					WriteNullTagLiteral(n, ns);
				}
				return;
			}
			if (!needType && o.GetType() != mapping.TypeDesc.Type)
			{
				if (!WriteDerivedTypes(mapping, n, ns, o, isNullable))
				{
					if (!mapping.TypeDesc.IsRoot)
					{
						throw CreateUnknownTypeException(o);
					}
					if (!WriteEnumAndArrayTypes(mapping, o, n, ns))
					{
						WriteTypedPrimitive(n, ns, o, xsiType: true);
					}
				}
				return;
			}
		}
		if (mapping.TypeDesc.IsAbstract)
		{
			return;
		}
		if (mapping.TypeDesc.Type != null && typeof(XmlSchemaObject).IsAssignableFrom(mapping.TypeDesc.Type))
		{
			base.EscapeName = false;
		}
		XmlSerializerNamespaces xmlSerializerNamespaces = null;
		MemberMapping[] allMembers = TypeScope.GetAllMembers(mapping);
		int num = FindXmlnsIndex(allMembers);
		if (num >= 0)
		{
			MemberMapping memberMapping = allMembers[num];
			xmlSerializerNamespaces = (XmlSerializerNamespaces)GetMemberValue(o, memberMapping.Name);
		}
		if (!mapping.IsSoap)
		{
			WriteStartElement(n, ns, o, writePrefixed: false, xmlSerializerNamespaces);
			if (!mapping.TypeDesc.IsRoot && needType)
			{
				WriteXsiType(mapping.TypeName, mapping.Namespace);
			}
		}
		else if (xmlSerializerNamespaces != null)
		{
			WriteNamespaceDeclarations(xmlSerializerNamespaces);
		}
		foreach (MemberMapping memberMapping2 in allMembers)
		{
			bool flag = true;
			bool flag2 = true;
			if (memberMapping2.CheckSpecified != 0)
			{
				string memberName = memberMapping2.Name + "Specified";
				flag = (bool)GetMemberValue(o, memberName);
			}
			if (memberMapping2.CheckShouldPersist)
			{
				string name = "ShouldSerialize" + memberMapping2.Name;
				MethodInfo method = o.GetType().GetMethod(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				flag2 = (bool)method.Invoke(o, Array.Empty<object>());
			}
			if (memberMapping2.Attribute != null && flag && flag2)
			{
				object memberValue = GetMemberValue(o, memberMapping2.Name);
				WriteMember(memberValue, memberMapping2.Attribute, memberMapping2.TypeDesc, o);
			}
		}
		foreach (MemberMapping memberMapping3 in allMembers)
		{
			if (memberMapping3.Xmlns != null)
			{
				continue;
			}
			bool flag3 = true;
			bool flag4 = true;
			if (memberMapping3.CheckSpecified != 0)
			{
				string memberName2 = memberMapping3.Name + "Specified";
				flag3 = (bool)GetMemberValue(o, memberName2);
			}
			if (memberMapping3.CheckShouldPersist)
			{
				string name2 = "ShouldSerialize" + memberMapping3.Name;
				MethodInfo method2 = o.GetType().GetMethod(name2, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				flag4 = (bool)method2.Invoke(o, Array.Empty<object>());
			}
			if (!memberMapping3.CheckShouldPersist || (memberMapping3.Elements.Length == 0 && memberMapping3.Text == null))
			{
				flag4 = true;
			}
			if (flag3 && flag4)
			{
				object choiceSource = null;
				if (memberMapping3.ChoiceIdentifier != null)
				{
					choiceSource = GetMemberValue(o, memberMapping3.ChoiceIdentifier.MemberName);
				}
				object memberValue2 = GetMemberValue(o, memberMapping3.Name);
				WriteMember(memberValue2, choiceSource, memberMapping3.ElementsSortedByDerivation, memberMapping3.Text, memberMapping3.ChoiceIdentifier, memberMapping3.TypeDesc, writeAccessors: true);
			}
		}
		if (!mapping.IsSoap)
		{
			WriteEndElement(o);
		}
	}

	[RequiresUnreferencedCode("Calls GetType on object")]
	private object GetMemberValue(object o, string memberName)
	{
		MemberInfo effectiveGetInfo = ReflectionXmlSerializationHelper.GetEffectiveGetInfo(o.GetType(), memberName);
		return GetMemberValue(o, effectiveGetInfo);
	}

	[RequiresUnreferencedCode("calls WriteMember")]
	private bool WriteEnumAndArrayTypes(StructMapping structMapping, object o, string n, string ns)
	{
		Type type = o.GetType();
		foreach (object typeMapping in _mapping.Scope.TypeMappings)
		{
			if (typeMapping is EnumMapping enumMapping && enumMapping.TypeDesc.Type == type)
			{
				base.Writer.WriteStartElement(n, ns);
				WriteXsiType(enumMapping.TypeName, ns);
				base.Writer.WriteString(WriteEnumMethod(enumMapping, o));
				base.Writer.WriteEndElement();
				return true;
			}
			if (typeMapping is ArrayMapping arrayMapping && arrayMapping.TypeDesc.Type == type)
			{
				base.Writer.WriteStartElement(n, ns);
				WriteXsiType(arrayMapping.TypeName, ns);
				WriteMember(o, null, arrayMapping.ElementsSortedByDerivation, null, null, arrayMapping.TypeDesc, writeAccessors: true);
				base.Writer.WriteEndElement();
				return true;
			}
		}
		return false;
	}

	private string WriteEnumMethod(EnumMapping mapping, object v)
	{
		string text = null;
		if (mapping != null)
		{
			ConstantMapping[] constants = mapping.Constants;
			if (constants.Length != 0)
			{
				bool flag = false;
				long num = Convert.ToInt64(v);
				foreach (ConstantMapping constantMapping in constants)
				{
					if (num == constantMapping.Value)
					{
						text = constantMapping.XmlName;
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					if (!mapping.IsFlags)
					{
						throw CreateInvalidEnumValueException(v, mapping.TypeDesc.FullName);
					}
					string[] array = new string[constants.Length];
					long[] array2 = new long[constants.Length];
					for (int j = 0; j < constants.Length; j++)
					{
						array[j] = constants[j].XmlName;
						array2[j] = constants[j].Value;
					}
					text = XmlSerializationWriter.FromEnum(num, array, array2);
				}
			}
		}
		else
		{
			text = v.ToString();
		}
		if (mapping.IsSoap)
		{
			WriteXsiType(mapping.TypeName, mapping.Namespace);
			base.Writer.WriteString(text);
			return null;
		}
		return text;
	}

	private object GetMemberValue(object o, MemberInfo memberInfo)
	{
		if (memberInfo is PropertyInfo propertyInfo)
		{
			return propertyInfo.GetValue(o);
		}
		if (memberInfo is FieldInfo fieldInfo)
		{
			return fieldInfo.GetValue(o);
		}
		throw new InvalidOperationException(System.SR.XmlInternalError);
	}

	private void WriteMember(object memberValue, AttributeAccessor attribute, TypeDesc memberTypeDesc, object container)
	{
		if (memberTypeDesc.IsAbstract)
		{
			return;
		}
		if (memberTypeDesc.IsArrayLike)
		{
			StringBuilder stringBuilder = new StringBuilder();
			TypeDesc arrayElementTypeDesc = memberTypeDesc.ArrayElementTypeDesc;
			bool flag = CanOptimizeWriteListSequence(arrayElementTypeDesc);
			if (attribute.IsList && flag)
			{
				base.Writer.WriteStartAttribute(null, attribute.Name, (attribute.Form == XmlSchemaForm.Qualified) ? attribute.Namespace : string.Empty);
			}
			if (memberValue == null)
			{
				return;
			}
			IEnumerable enumerable = (IEnumerable)memberValue;
			IEnumerator enumerator = enumerable.GetEnumerator();
			bool flag2 = false;
			if (enumerator == null)
			{
				return;
			}
			while (enumerator.MoveNext())
			{
				object current = enumerator.Current;
				if (attribute.IsList)
				{
					string stringValue;
					if (attribute.Mapping is EnumMapping mapping)
					{
						stringValue = WriteEnumMethod(mapping, current);
					}
					else
					{
						WritePrimitiveValue(arrayElementTypeDesc, current, isElement: true, out stringValue);
					}
					if (flag)
					{
						if (flag2)
						{
							base.Writer.WriteString(" ");
						}
						if (current is byte[])
						{
							WriteValue((byte[])current);
						}
						else
						{
							WriteValue(stringValue);
						}
					}
					else
					{
						if (flag2)
						{
							stringBuilder.Append(' ');
						}
						stringBuilder.Append(stringValue);
					}
				}
				else
				{
					WriteAttribute(current, attribute, container);
				}
				flag2 = true;
			}
			if (attribute.IsList)
			{
				if (flag)
				{
					base.Writer.WriteEndAttribute();
				}
				else if (stringBuilder.Length != 0)
				{
					string ns = ((attribute.Form == XmlSchemaForm.Qualified) ? attribute.Namespace : string.Empty);
					WriteAttribute(attribute.Name, ns, stringBuilder.ToString());
				}
			}
		}
		else
		{
			WriteAttribute(memberValue, attribute, container);
		}
	}

	private bool CanOptimizeWriteListSequence(TypeDesc listElementTypeDesc)
	{
		if (listElementTypeDesc != null)
		{
			return listElementTypeDesc != ReflectionXmlSerializationReader.QnameTypeDesc;
		}
		return false;
	}

	private void WriteAttribute(object memberValue, AttributeAccessor attribute, object container)
	{
		if (attribute.Mapping is SpecialMapping specialMapping)
		{
			if (specialMapping.TypeDesc.Kind != TypeKind.Attribute && !specialMapping.TypeDesc.CanBeAttributeValue)
			{
				throw new InvalidOperationException(System.SR.XmlInternalError);
			}
			WriteXmlAttribute((XmlNode)memberValue, container);
		}
		else
		{
			string ns = ((attribute.Form == XmlSchemaForm.Qualified) ? attribute.Namespace : string.Empty);
			WritePrimitive(WritePrimitiveMethodRequirement.WriteAttribute, attribute.Name, ns, attribute.Default, memberValue, attribute.Mapping, writeXsiType: false, isElement: false, isNullable: false);
		}
	}

	private int FindXmlnsIndex(MemberMapping[] members)
	{
		for (int i = 0; i < members.Length; i++)
		{
			if (members[i].Xmlns != null)
			{
				return i;
			}
		}
		return -1;
	}

	[RequiresUnreferencedCode("calls WriteStructMethod")]
	private bool WriteDerivedTypes(StructMapping mapping, string n, string ns, object o, bool isNullable)
	{
		Type type = o.GetType();
		for (StructMapping structMapping = mapping.DerivedMappings; structMapping != null; structMapping = structMapping.NextDerivedMapping)
		{
			if (type == structMapping.TypeDesc.Type)
			{
				WriteStructMethod(structMapping, n, ns, o, isNullable, needType: true);
				return true;
			}
			if (WriteDerivedTypes(structMapping, n, ns, o, isNullable))
			{
				return true;
			}
		}
		return false;
	}

	private void WritePrimitive(WritePrimitiveMethodRequirement method, string name, string ns, object defaultValue, object o, TypeMapping mapping, bool writeXsiType, bool isElement, bool isNullable)
	{
		TypeDesc typeDesc = mapping.TypeDesc;
		if (defaultValue != null && defaultValue != DBNull.Value && mapping.TypeDesc.HasDefaultSupport)
		{
			if (mapping is EnumMapping)
			{
				if (((EnumMapping)mapping).IsFlags)
				{
					IEnumerable<string> values = defaultValue.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
					string text = string.Join(", ", values);
					if (o.ToString() == text)
					{
						return;
					}
				}
				else if (o.ToString() == defaultValue.ToString())
				{
					return;
				}
			}
			else if (IsDefaultValue(mapping, o, defaultValue, isNullable))
			{
				return;
			}
		}
		XmlQualifiedName xsiType = null;
		if (writeXsiType)
		{
			xsiType = new XmlQualifiedName(mapping.TypeName, mapping.Namespace);
		}
		string stringValue = null;
		bool flag = false;
		if (mapping is EnumMapping mapping2)
		{
			stringValue = WriteEnumMethod(mapping2, o);
			flag = true;
		}
		else
		{
			flag = WritePrimitiveValue(typeDesc, o, isElement, out stringValue);
		}
		if (flag)
		{
			if (hasRequirement(method, WritePrimitiveMethodRequirement.WriteElementString))
			{
				if (hasRequirement(method, WritePrimitiveMethodRequirement.Raw))
				{
					WriteElementStringRaw(name, ns, stringValue, xsiType);
				}
				else
				{
					WriteElementString(name, ns, stringValue, xsiType);
				}
			}
			else if (hasRequirement(method, WritePrimitiveMethodRequirement.WriteNullableStringLiteral))
			{
				if (hasRequirement(method, WritePrimitiveMethodRequirement.Encoded))
				{
					if (hasRequirement(method, WritePrimitiveMethodRequirement.Raw))
					{
						WriteNullableStringEncodedRaw(name, ns, stringValue, xsiType);
					}
					else
					{
						WriteNullableStringEncoded(name, ns, stringValue, xsiType);
					}
				}
				else if (hasRequirement(method, WritePrimitiveMethodRequirement.Raw))
				{
					WriteNullableStringLiteralRaw(name, ns, stringValue);
				}
				else
				{
					WriteNullableStringLiteral(name, ns, stringValue);
				}
			}
			else if (hasRequirement(method, WritePrimitiveMethodRequirement.WriteAttribute))
			{
				WriteAttribute(name, ns, stringValue);
			}
		}
		else if (o is byte[] value)
		{
			if (hasRequirement(method, WritePrimitiveMethodRequirement.Raw | WritePrimitiveMethodRequirement.WriteElementString))
			{
				WriteElementStringRaw(name, ns, XmlSerializationWriter.FromByteArrayBase64(value));
			}
			else if (hasRequirement(method, WritePrimitiveMethodRequirement.Raw | WritePrimitiveMethodRequirement.WriteNullableStringLiteral))
			{
				WriteNullableStringLiteralRaw(name, ns, XmlSerializationWriter.FromByteArrayBase64(value));
			}
			else if (hasRequirement(method, WritePrimitiveMethodRequirement.WriteAttribute))
			{
				WriteAttribute(name, ns, value);
			}
		}
	}

	private bool hasRequirement(WritePrimitiveMethodRequirement value, WritePrimitiveMethodRequirement requirement)
	{
		return (value & requirement) == requirement;
	}

	private bool IsDefaultValue(TypeMapping mapping, object o, object value, bool isNullable)
	{
		if (value is string && ((string)value).Length == 0)
		{
			string text = (string)o;
			if (text != null)
			{
				return text.Length == 0;
			}
			return true;
		}
		return value.Equals(o);
	}

	private bool WritePrimitiveValue(TypeDesc typeDesc, object o, bool isElement, out string stringValue)
	{
		if (typeDesc == ReflectionXmlSerializationReader.StringTypeDesc || typeDesc.FormatterName == "String")
		{
			stringValue = (string)o;
			return true;
		}
		if (!typeDesc.HasCustomFormatter)
		{
			stringValue = ConvertPrimitiveToString(o, typeDesc);
			return true;
		}
		if (o is byte[] && typeDesc.FormatterName == "ByteArrayHex")
		{
			stringValue = XmlSerializationWriter.FromByteArrayHex((byte[])o);
			return true;
		}
		if (o is DateTime)
		{
			if (typeDesc.FormatterName == "DateTime")
			{
				stringValue = XmlSerializationWriter.FromDateTime((DateTime)o);
				return true;
			}
			if (typeDesc.FormatterName == "Date")
			{
				stringValue = XmlSerializationWriter.FromDate((DateTime)o);
				return true;
			}
			if (typeDesc.FormatterName == "Time")
			{
				stringValue = XmlSerializationWriter.FromTime((DateTime)o);
				return true;
			}
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlInternalErrorDetails, "Invalid DateTime"));
		}
		if (typeDesc == ReflectionXmlSerializationReader.QnameTypeDesc)
		{
			stringValue = FromXmlQualifiedName((XmlQualifiedName)o);
			return true;
		}
		if (o is string)
		{
			switch (typeDesc.FormatterName)
			{
			case "XmlName":
				stringValue = XmlSerializationWriter.FromXmlName((string)o);
				break;
			case "XmlNCName":
				stringValue = XmlSerializationWriter.FromXmlNCName((string)o);
				break;
			case "XmlNmToken":
				stringValue = XmlSerializationWriter.FromXmlNmToken((string)o);
				break;
			case "XmlNmTokens":
				stringValue = XmlSerializationWriter.FromXmlNmTokens((string)o);
				break;
			default:
				stringValue = null;
				return false;
			}
			return true;
		}
		if (o is char && typeDesc.FormatterName == "Char")
		{
			stringValue = XmlSerializationWriter.FromChar((char)o);
			return true;
		}
		if (!(o is byte[]))
		{
			throw new InvalidOperationException(System.SR.XmlInternalError);
		}
		stringValue = null;
		return false;
	}

	private string ConvertPrimitiveToString(object o, TypeDesc typeDesc)
	{
		return typeDesc.FormatterName switch
		{
			"Boolean" => XmlConvert.ToString((bool)o), 
			"Int32" => XmlConvert.ToString((int)o), 
			"Int16" => XmlConvert.ToString((short)o), 
			"Int64" => XmlConvert.ToString((long)o), 
			"Single" => XmlConvert.ToString((float)o), 
			"Double" => XmlConvert.ToString((double)o), 
			"Decimal" => XmlConvert.ToString((decimal)o), 
			"Byte" => XmlConvert.ToString((byte)o), 
			"SByte" => XmlConvert.ToString((sbyte)o), 
			"UInt16" => XmlConvert.ToString((ushort)o), 
			"UInt32" => XmlConvert.ToString((uint)o), 
			"UInt64" => XmlConvert.ToString((ulong)o), 
			"Guid" => XmlConvert.ToString((Guid)o), 
			"Char" => XmlConvert.ToString((char)o), 
			"TimeSpan" => XmlConvert.ToString((TimeSpan)o), 
			"DateTimeOffset" => XmlConvert.ToString((DateTimeOffset)o), 
			_ => o.ToString(), 
		};
	}

	[RequiresUnreferencedCode("calls WritePotentiallyReferencingElement")]
	private void GenerateMembersElement(object o, XmlMembersMapping xmlMembersMapping)
	{
		ElementAccessor accessor = xmlMembersMapping.Accessor;
		MembersMapping membersMapping = (MembersMapping)accessor.Mapping;
		bool hasWrapperElement = membersMapping.HasWrapperElement;
		bool writeAccessors = membersMapping.WriteAccessors;
		bool flag = xmlMembersMapping.IsSoap && writeAccessors;
		WriteStartDocument();
		if (!membersMapping.IsSoap)
		{
			TopLevelElement();
		}
		object[] array = (object[])o;
		int num = array.Length;
		if (hasWrapperElement)
		{
			WriteStartElement(accessor.Name, (accessor.Form == XmlSchemaForm.Qualified) ? accessor.Namespace : string.Empty, membersMapping.IsSoap);
			int num2 = FindXmlnsIndex(membersMapping.Members);
			if (num2 >= 0)
			{
				XmlSerializerNamespaces xmlns = (XmlSerializerNamespaces)array[num2];
				if (num > num2)
				{
					WriteNamespaceDeclarations(xmlns);
				}
			}
			for (int i = 0; i < membersMapping.Members.Length; i++)
			{
				MemberMapping memberMapping = membersMapping.Members[i];
				if (memberMapping.Attribute == null || memberMapping.Ignore)
				{
					continue;
				}
				object memberValue = array[i];
				bool? flag2 = null;
				if (memberMapping.CheckSpecified != 0)
				{
					string text = memberMapping.Name + "Specified";
					for (int j = 0; j < Math.Min(num, membersMapping.Members.Length); j++)
					{
						if (membersMapping.Members[j].Name == text)
						{
							flag2 = (bool)array[j];
							break;
						}
					}
				}
				if (num > i && (!flag2.HasValue || flag2.Value))
				{
					WriteMember(memberValue, memberMapping.Attribute, memberMapping.TypeDesc, null);
				}
			}
		}
		for (int k = 0; k < membersMapping.Members.Length; k++)
		{
			MemberMapping memberMapping2 = membersMapping.Members[k];
			if (memberMapping2.Xmlns != null || memberMapping2.Ignore)
			{
				continue;
			}
			bool? flag3 = null;
			if (memberMapping2.CheckSpecified != 0)
			{
				string text2 = memberMapping2.Name + "Specified";
				for (int l = 0; l < Math.Min(num, membersMapping.Members.Length); l++)
				{
					if (membersMapping.Members[l].Name == text2)
					{
						flag3 = (bool)array[l];
						break;
					}
				}
			}
			if (num <= k || (flag3.HasValue && !flag3.Value))
			{
				continue;
			}
			object o2 = array[k];
			object choiceSource = null;
			if (memberMapping2.ChoiceIdentifier != null)
			{
				for (int m = 0; m < membersMapping.Members.Length; m++)
				{
					if (membersMapping.Members[m].Name == memberMapping2.ChoiceIdentifier.MemberName)
					{
						choiceSource = array[m];
						break;
					}
				}
			}
			if (flag && memberMapping2.IsReturnValue && memberMapping2.Elements.Length != 0)
			{
				WriteRpcResult(memberMapping2.Elements[0].Name, string.Empty);
			}
			WriteMember(o2, choiceSource, memberMapping2.ElementsSortedByDerivation, memberMapping2.Text, memberMapping2.ChoiceIdentifier, memberMapping2.TypeDesc, writeAccessors || hasWrapperElement);
		}
		if (hasWrapperElement)
		{
			WriteEndElement();
		}
		if (!accessor.IsSoap)
		{
			return;
		}
		if (!hasWrapperElement && !writeAccessors && num > membersMapping.Members.Length)
		{
			for (int n = membersMapping.Members.Length; n < num; n++)
			{
				if (array[n] != null)
				{
					WritePotentiallyReferencingElement(null, null, array[n], array[n].GetType(), suppressReference: true, isNullable: false);
				}
			}
		}
		WriteReferencedElements();
	}
}
