using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml.Schema;

namespace System.Xml.Serialization;

internal sealed class ReflectionXmlSerializationReader : XmlSerializationReader
{
	internal sealed class CollectionMember : List<object>
	{
	}

	internal sealed class Member
	{
		public MemberMapping Mapping;

		public CollectionMember Collection;

		public int FixupIndex = -1;

		public bool MultiRef;

		public Action<object> Source;

		public Func<object> GetSource;

		public Action<object> ArraySource;

		public Action<object> CheckSpecifiedSource;

		public Action<object> ChoiceSource;

		public Action<string, string> XmlnsSource;

		public Member(MemberMapping mapping)
		{
			Mapping = mapping;
		}
	}

	internal sealed class CheckTypeSource
	{
		public string Id { get; set; }

		public bool IsObject { get; set; }

		public Type Type { get; set; }

		public object RefObject { get; set; }
	}

	internal sealed class ObjectHolder
	{
		public object Object;
	}

	private readonly XmlMapping _mapping;

	private static readonly ConcurrentDictionary<(Type, string), ReflectionXmlSerializationReaderHelper.SetMemberValueDelegate> s_setMemberValueDelegateCache = new ConcurrentDictionary<(Type, string), ReflectionXmlSerializationReaderHelper.SetMemberValueDelegate>();

	internal static TypeDesc StringTypeDesc { get; } = new TypeScope().GetTypeDesc(typeof(string));


	internal static TypeDesc QnameTypeDesc { get; } = new TypeScope().GetTypeDesc(typeof(XmlQualifiedName));


	public ReflectionXmlSerializationReader(XmlMapping mapping, XmlReader xmlReader, XmlDeserializationEvents events, string encodingStyle)
	{
		Init(xmlReader, events, encodingStyle, null);
		_mapping = mapping;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	protected override void InitCallbacks()
	{
		TypeScope scope = _mapping.Scope;
		foreach (TypeMapping typeMapping in scope.TypeMappings)
		{
			if (typeMapping.IsSoap && (typeMapping is StructMapping || typeMapping is EnumMapping || typeMapping is ArrayMapping || typeMapping is NullableMapping) && !typeMapping.TypeDesc.IsRoot)
			{
				AddReadCallback(typeMapping.TypeName, typeMapping.Namespace, typeMapping.TypeDesc.Type, CreateXmlSerializationReadCallback(typeMapping));
			}
		}
	}

	protected override void InitIDs()
	{
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public object ReadObject()
	{
		XmlMapping mapping = _mapping;
		if (!mapping.IsReadable)
		{
			return null;
		}
		if (!mapping.GenerateSerializer)
		{
			throw new ArgumentException(System.SR.Format(System.SR.XmlInternalError, "xmlMapping"));
		}
		if (mapping is XmlTypeMapping xmlTypeMapping)
		{
			return GenerateTypeElement(xmlTypeMapping);
		}
		if (mapping is XmlMembersMapping xmlMembersMapping)
		{
			return GenerateMembersElement(xmlMembersMapping);
		}
		throw new ArgumentException(System.SR.Format(System.SR.XmlInternalError, "xmlMapping"));
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private object GenerateMembersElement(XmlMembersMapping xmlMembersMapping)
	{
		if (xmlMembersMapping.Accessor.IsSoap)
		{
			return GenerateEncodedMembersElement(xmlMembersMapping);
		}
		return GenerateLiteralMembersElement(xmlMembersMapping);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private object GenerateLiteralMembersElement(XmlMembersMapping xmlMembersMapping)
	{
		ElementAccessor accessor = xmlMembersMapping.Accessor;
		MemberMapping[] members = ((MembersMapping)accessor.Mapping).Members;
		bool hasWrapperElement = ((MembersMapping)accessor.Mapping).HasWrapperElement;
		base.Reader.MoveToContent();
		object[] array = new object[members.Length];
		InitializeValueTypes(array, members);
		if (hasWrapperElement)
		{
			string name = accessor.Name;
			string text = ((accessor.Form == XmlSchemaForm.Qualified) ? accessor.Namespace : string.Empty);
			base.Reader.MoveToContent();
			while (base.Reader.NodeType != XmlNodeType.EndElement && base.Reader.NodeType != 0)
			{
				if (base.Reader.IsStartElement(accessor.Name, text))
				{
					if (!GenerateLiteralMembersElementInternal(members, hasWrapperElement, array))
					{
						continue;
					}
					ReadEndElement();
				}
				else
				{
					UnknownNode(null, text + ":" + name);
				}
				base.Reader.MoveToContent();
			}
		}
		else
		{
			GenerateLiteralMembersElementInternal(members, hasWrapperElement, array);
		}
		return array;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private bool GenerateLiteralMembersElementInternal(MemberMapping[] mappings, bool hasWrapperElement, object[] p)
	{
		Member anyText = null;
		Member anyElement = null;
		Member member = null;
		List<Member> list = new List<Member>();
		List<Member> list2 = new List<Member>();
		List<Member> list3 = new List<Member>();
		for (int i = 0; i < mappings.Length; i++)
		{
			int index = i;
			MemberMapping memberMapping = mappings[index];
			Action<object> source = delegate(object o)
			{
				p[index] = o;
			};
			Member member2 = new Member(memberMapping);
			Member anyMember = new Member(memberMapping);
			if (memberMapping.Xmlns != null)
			{
				XmlSerializerNamespaces xmlns = new XmlSerializerNamespaces();
				p[index] = xmlns;
				member2.XmlnsSource = delegate(string ns, string name)
				{
					xmlns.Add(ns, name);
				};
			}
			member2.Source = source;
			anyMember.Source = source;
			if (memberMapping.CheckSpecified == SpecifiedAccessor.ReadWrite)
			{
				string text = memberMapping.Name + "Specified";
				for (int j = 0; j < mappings.Length; j++)
				{
					if (mappings[j].Name == text)
					{
						int indexJ = j;
						member2.CheckSpecifiedSource = delegate(object o)
						{
							p[indexJ] = o;
						};
					}
				}
			}
			bool flag = false;
			if (memberMapping.Text != null)
			{
				anyText = anyMember;
			}
			if (memberMapping.Attribute != null && memberMapping.Attribute.Any)
			{
				anyMember.Collection = new CollectionMember();
				anyMember.ArraySource = anyMember.Source;
				anyMember.Source = delegate(object item)
				{
					anyMember.Collection.Add(item);
				};
				member = anyMember;
			}
			if (memberMapping.Attribute != null || memberMapping.Xmlns != null)
			{
				list3.Add(member2);
			}
			else if (memberMapping.Text != null)
			{
				list2.Add(member2);
			}
			if (!memberMapping.IsSequence)
			{
				for (int k = 0; k < memberMapping.Elements.Length; k++)
				{
					if (!memberMapping.Elements[k].Any || memberMapping.Elements[k].Name.Length != 0)
					{
						continue;
					}
					anyElement = anyMember;
					if (memberMapping.Attribute == null && memberMapping.Text == null)
					{
						anyMember.Collection = new CollectionMember();
						anyMember.ArraySource = delegate(object item)
						{
							anyMember.Collection.Add(item);
						};
						list2.Add(anyMember);
					}
					flag = true;
					break;
				}
			}
			if (memberMapping.Attribute != null || memberMapping.Text != null || flag)
			{
				list.Add(anyMember);
			}
			else if (memberMapping.TypeDesc.IsArrayLike && (memberMapping.Elements.Length != 1 || !(memberMapping.Elements[0].Mapping is ArrayMapping)))
			{
				anyMember.Collection = new CollectionMember();
				anyMember.ArraySource = delegate(object item)
				{
					anyMember.Collection.Add(item);
				};
				list.Add(anyMember);
				list2.Add(anyMember);
			}
			else
			{
				list.Add(member2);
			}
		}
		Member[] array = list.ToArray();
		Member[] array2 = list2.ToArray();
		if (array.Length != 0 && array[0].Mapping.IsReturnValue)
		{
			base.IsReturnValue = true;
		}
		if (list3.Count > 0)
		{
			Member[] members = list3.ToArray();
			object o2 = null;
			WriteAttributes(members, member, base.UnknownNode, ref o2);
			base.Reader.MoveToElement();
		}
		if (hasWrapperElement)
		{
			if (base.Reader.IsEmptyElement)
			{
				base.Reader.Skip();
				base.Reader.MoveToContent();
				return false;
			}
			base.Reader.ReadStartElement();
		}
		base.Reader.MoveToContent();
		while (base.Reader.NodeType != XmlNodeType.EndElement && base.Reader.NodeType != 0)
		{
			WriteMemberElements(array, base.UnknownNode, base.UnknownNode, anyElement, anyText);
			base.Reader.MoveToContent();
		}
		Member[] array3 = array2;
		foreach (Member member3 in array3)
		{
			object collection = null;
			SetCollectionObjectWithCollectionMember(ref collection, member3.Collection, member3.Mapping.TypeDesc.Type);
			member3.Source(collection);
		}
		if (member != null)
		{
			object collection2 = null;
			SetCollectionObjectWithCollectionMember(ref collection2, member.Collection, member.Mapping.TypeDesc.Type);
			member.ArraySource(collection2);
		}
		return true;
	}

	private void InitializeValueTypes(object[] p, MemberMapping[] mappings)
	{
		for (int i = 0; i < mappings.Length; i++)
		{
			if (mappings[i].TypeDesc.IsValueType)
			{
				if (mappings[i].TypeDesc.IsOptionalValue && mappings[i].TypeDesc.BaseTypeDesc.UseReflection)
				{
					p[i] = null;
				}
				else
				{
					p[i] = ReflectionCreateObject(mappings[i].TypeDesc.Type);
				}
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private object GenerateEncodedMembersElement(XmlMembersMapping xmlMembersMapping)
	{
		ElementAccessor accessor = xmlMembersMapping.Accessor;
		MembersMapping membersMapping = (MembersMapping)accessor.Mapping;
		MemberMapping[] members = membersMapping.Members;
		bool hasWrapperElement = membersMapping.HasWrapperElement;
		bool writeAccessors = membersMapping.WriteAccessors;
		base.Reader.MoveToContent();
		object[] p = new object[members.Length];
		InitializeValueTypes(p, members);
		bool flag = true;
		if (hasWrapperElement)
		{
			base.Reader.MoveToContent();
			while (base.Reader.NodeType == XmlNodeType.Element)
			{
				string attribute = base.Reader.GetAttribute("root", "http://schemas.xmlsoap.org/soap/encoding/");
				if (attribute == null || XmlConvert.ToBoolean(attribute))
				{
					break;
				}
				ReadReferencedElement();
				base.Reader.MoveToContent();
			}
			if (membersMapping.ValidateRpcWrapperElement)
			{
				string name = accessor.Name;
				string ns = ((accessor.Form == XmlSchemaForm.Qualified) ? accessor.Namespace : string.Empty);
				if (!XmlNodeEqual(base.Reader, name, ns))
				{
					throw CreateUnknownNodeException();
				}
			}
			flag = base.Reader.IsEmptyElement;
			base.Reader.ReadStartElement();
		}
		Member[] array = new Member[members.Length];
		for (int i = 0; i < members.Length; i++)
		{
			int index = i;
			MemberMapping memberMapping = members[index];
			Member member = new Member(memberMapping);
			member.Source = delegate(object value)
			{
				p[index] = value;
			};
			array[index] = member;
			if (memberMapping.CheckSpecified != SpecifiedAccessor.ReadWrite)
			{
				continue;
			}
			string text = memberMapping.Name + "Specified";
			for (int j = 0; j < members.Length; j++)
			{
				if (members[j].Name == text)
				{
					int indexOfSpecifiedMember = j;
					member.CheckSpecifiedSource = delegate(object value)
					{
						p[indexOfSpecifiedMember] = value;
					};
					break;
				}
			}
		}
		Fixup fixup = WriteMemberFixupBegin(array, p);
		if (array.Length != 0 && array[0].Mapping.IsReturnValue)
		{
			base.IsReturnValue = true;
		}
		List<CheckTypeSource> list = null;
		if (!hasWrapperElement && !writeAccessors)
		{
			list = new List<CheckTypeSource>();
		}
		base.Reader.MoveToContent();
		while (base.Reader.NodeType != XmlNodeType.EndElement && base.Reader.NodeType != 0)
		{
			UnknownNodeAction elementElseAction = ((list != null) ? new UnknownNodeAction(Wrapper) : ((UnknownNodeAction)delegate
			{
				UnknownNode(p);
			}));
			WriteMemberElements(array, elementElseAction, delegate
			{
				UnknownNode(p);
			}, null, null, fixup, list);
			base.Reader.MoveToContent();
		}
		if (!flag)
		{
			ReadEndElement();
		}
		if (list != null)
		{
			foreach (CheckTypeSource item in list)
			{
				bool isReferenced = true;
				bool isObject = item.IsObject;
				object obj = (isObject ? item.RefObject : GetTarget((string)item.RefObject));
				if (obj != null)
				{
					CheckTypeSource checkTypeSource = new CheckTypeSource
					{
						RefObject = obj,
						Type = obj.GetType(),
						Id = null
					};
					WriteMemberElementsIf(array, null, delegate
					{
						isReferenced = false;
					}, fixup, checkTypeSource);
					if (isObject && isReferenced)
					{
						Referenced(obj);
					}
				}
			}
		}
		ReadReferencedElements();
		return p;
		[RequiresUnreferencedCode("calls ReadReferencedElement")]
		void Wrapper(object _)
		{
			if (base.Reader.GetAttribute("id", null) != null)
			{
				ReadReferencedElement();
			}
			else
			{
				UnknownNode(p);
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private object GenerateTypeElement(XmlTypeMapping xmlTypeMapping)
	{
		ElementAccessor accessor = xmlTypeMapping.Accessor;
		TypeMapping mapping = accessor.Mapping;
		base.Reader.MoveToContent();
		MemberMapping memberMapping = new MemberMapping();
		memberMapping.TypeDesc = mapping.TypeDesc;
		memberMapping.Elements = new ElementAccessor[1] { accessor };
		object obj = null;
		ObjectHolder holder = new ObjectHolder();
		Member member = new Member(memberMapping);
		member.Source = delegate(object value)
		{
			holder.Object = value;
		};
		member.GetSource = () => holder.Object;
		UnknownNodeAction elementElseAction = CreateUnknownNodeException;
		UnknownNodeAction elseAction = base.UnknownNode;
		WriteMemberElements(new Member[1] { member }, elementElseAction, elseAction, accessor.Any ? member : null, null);
		obj = holder.Object;
		if (accessor.IsSoap)
		{
			Referenced(obj);
			ReadReferencedElements();
		}
		return obj;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private void WriteMemberElements(Member[] expectedMembers, UnknownNodeAction elementElseAction, UnknownNodeAction elseAction, Member anyElement, Member anyText, Fixup fixup = null, List<CheckTypeSource> checkTypeHrefsSource = null)
	{
		bool flag = checkTypeHrefsSource != null;
		if (base.Reader.NodeType == XmlNodeType.Element)
		{
			if (flag)
			{
				if (base.Reader.GetAttribute("root", "http://schemas.xmlsoap.org/soap/encoding/") == "0")
				{
					elementElseAction(null);
				}
				else
				{
					WriteMemberElementsCheckType(checkTypeHrefsSource);
				}
			}
			else
			{
				WriteMemberElementsIf(expectedMembers, anyElement, elementElseAction, fixup);
			}
		}
		else if (anyText == null || anyText.Mapping == null || !WriteMemberText(anyText))
		{
			ProcessUnknownNode(elseAction);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private void WriteMemberElementsCheckType(List<CheckTypeSource> checkTypeHrefsSource)
	{
		string fixupReference;
		object obj = ReadReferencingElement(null, null, elementCanBeType: true, out fixupReference);
		CheckTypeSource checkTypeSource = new CheckTypeSource();
		if (fixupReference != null)
		{
			checkTypeSource.RefObject = fixupReference;
			checkTypeSource.IsObject = false;
			checkTypeHrefsSource.Add(checkTypeSource);
		}
		else if (obj != null)
		{
			checkTypeSource.RefObject = obj;
			checkTypeSource.IsObject = true;
			checkTypeHrefsSource.Add(checkTypeSource);
		}
	}

	private void ProcessUnknownNode(UnknownNodeAction action)
	{
		action?.Invoke(null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private void WriteMembers(ref object o, Member[] members, UnknownNodeAction elementElseAction, UnknownNodeAction elseAction, Member anyElement, Member anyText)
	{
		base.Reader.MoveToContent();
		while (base.Reader.NodeType != XmlNodeType.EndElement && base.Reader.NodeType != 0)
		{
			WriteMemberElements(members, elementElseAction, elseAction, anyElement, anyText);
			base.Reader.MoveToContent();
		}
	}

	private void SetCollectionObjectWithCollectionMember([NotNull] ref object collection, CollectionMember collectionMember, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type collectionType)
	{
		if (collectionType.IsArray)
		{
			Array array2;
			if (collection is Array array && array.Length == collectionMember.Count)
			{
				array2 = array;
			}
			else
			{
				Type elementType = collectionType.GetElementType();
				array2 = Array.CreateInstance(elementType, collectionMember.Count);
			}
			for (int i = 0; i < collectionMember.Count; i++)
			{
				array2.SetValue(collectionMember[i], i);
			}
			collection = array2;
		}
		else
		{
			if (collection == null)
			{
				collection = ReflectionCreateObject(collectionType);
			}
			AddObjectsIntoTargetCollection(collection, collectionMember, collectionType);
		}
	}

	private static void AddObjectsIntoTargetCollection(object targetCollection, List<object> sourceCollection, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type targetCollectionType)
	{
		if (targetCollection is IList list)
		{
			{
				foreach (object item in sourceCollection)
				{
					list.Add(item);
				}
				return;
			}
		}
		MethodInfo method = targetCollectionType.GetMethod("Add");
		if (method == null)
		{
			throw new InvalidOperationException(System.SR.XmlInternalError);
		}
		object[] array = new object[1];
		foreach (object item2 in sourceCollection)
		{
			array[0] = item2;
			method.Invoke(targetCollection, array);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private static ReflectionXmlSerializationReaderHelper.SetMemberValueDelegate GetSetMemberValueDelegate(object o, string memberName)
	{
		(Type, string) key = (o.GetType(), memberName);
		if (!s_setMemberValueDelegateCache.TryGetValue(key, out var value))
		{
			MemberInfo effectiveSetInfo = ReflectionXmlSerializationHelper.GetEffectiveSetInfo(o.GetType(), memberName);
			Type type;
			if (effectiveSetInfo is PropertyInfo propertyInfo)
			{
				type = propertyInfo.PropertyType;
			}
			else
			{
				if (!(effectiveSetInfo is FieldInfo fieldInfo))
				{
					throw new InvalidOperationException(System.SR.XmlInternalError);
				}
				type = fieldInfo.FieldType;
			}
			MethodInfo method = typeof(ReflectionXmlSerializationReaderHelper).GetMethod("GetSetMemberValueDelegateWithType", BindingFlags.Static | BindingFlags.Public);
			MethodInfo methodInfo = method.MakeGenericMethod(o.GetType(), type);
			Func<MemberInfo, ReflectionXmlSerializationReaderHelper.SetMemberValueDelegate> func = (Func<MemberInfo, ReflectionXmlSerializationReaderHelper.SetMemberValueDelegate>)methodInfo.CreateDelegate(typeof(Func<MemberInfo, ReflectionXmlSerializationReaderHelper.SetMemberValueDelegate>));
			value = func(effectiveSetInfo);
			s_setMemberValueDelegateCache.TryAdd(key, value);
		}
		return value;
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

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private bool WriteMemberText(Member anyText)
	{
		MemberMapping mapping = anyText.Mapping;
		if (base.Reader.NodeType == XmlNodeType.Text || base.Reader.NodeType == XmlNodeType.CDATA || base.Reader.NodeType == XmlNodeType.Whitespace || base.Reader.NodeType == XmlNodeType.SignificantWhitespace)
		{
			TextAccessor text = mapping.Text;
			object obj;
			if (text.Mapping is SpecialMapping specialMapping)
			{
				if (specialMapping.TypeDesc.Kind != TypeKind.Node)
				{
					throw new InvalidOperationException(System.SR.XmlInternalError);
				}
				obj = base.Document.CreateTextNode(base.Reader.ReadString());
			}
			else
			{
				obj = (mapping.TypeDesc.IsArrayLike ? ((!text.Mapping.TypeDesc.CollapseWhitespace) ? base.Reader.ReadString() : CollapseWhitespace(base.Reader.ReadString())) : ((text.Mapping.TypeDesc != StringTypeDesc && !(text.Mapping.TypeDesc.FormatterName == "String")) ? WritePrimitive(text.Mapping, (object state) => ((ReflectionXmlSerializationReader)state).Reader.ReadString(), this) : ReadString(null, text.Mapping.TypeDesc.CollapseWhitespace)));
			}
			anyText.Source(obj);
			return true;
		}
		return false;
	}

	private bool IsSequence(Member[] members)
	{
		return false;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private void WriteMemberElementsIf(Member[] expectedMembers, Member anyElementMember, UnknownNodeAction elementElseAction, Fixup fixup = null, CheckTypeSource checkTypeSource = null)
	{
		bool flag = checkTypeSource != null;
		bool flag2 = IsSequence(expectedMembers);
		ElementAccessor elementAccessor = null;
		Member member = null;
		bool flag3 = false;
		int elementIndex = -1;
		foreach (Member member2 in expectedMembers)
		{
			if (member2.Mapping.Xmlns != null || member2.Mapping.Ignore || (flag2 && (member2.Mapping.IsText || member2.Mapping.IsAttribute)))
			{
				continue;
			}
			for (int j = 0; j < member2.Mapping.Elements.Length; j++)
			{
				ElementAccessor elementAccessor2 = member2.Mapping.Elements[j];
				string text = ((elementAccessor2.Form == XmlSchemaForm.Qualified) ? elementAccessor2.Namespace : string.Empty);
				if (flag)
				{
					Type type;
					if (elementAccessor2.Mapping is NullableMapping nullableMapping)
					{
						TypeDesc typeDesc = nullableMapping.BaseMapping.TypeDesc;
						type = typeDesc.Type;
					}
					else
					{
						type = elementAccessor2.Mapping.TypeDesc.Type;
					}
					if (type.IsAssignableFrom(checkTypeSource.Type))
					{
						flag3 = true;
					}
				}
				else if (elementAccessor2.Name == base.Reader.LocalName && text == base.Reader.NamespaceURI)
				{
					flag3 = true;
				}
				if (flag3)
				{
					elementAccessor = elementAccessor2;
					member = member2;
					elementIndex = j;
					break;
				}
			}
			if (flag3)
			{
				break;
			}
		}
		if (flag3)
		{
			if (flag)
			{
				member.Source(checkTypeSource.RefObject);
				if (member.FixupIndex >= 0)
				{
					fixup.Ids[member.FixupIndex] = checkTypeSource.Id;
				}
			}
			else
			{
				string defaultNamespace = ((elementAccessor.Form == XmlSchemaForm.Qualified) ? elementAccessor.Namespace : string.Empty);
				bool flag4 = member.Mapping.TypeDesc.IsArrayLike && !member.Mapping.TypeDesc.IsArray;
				WriteElement(elementAccessor, member.Mapping.CheckSpecified == SpecifiedAccessor.ReadWrite, flag4 && member.Mapping.TypeDesc.IsNullable, member.Mapping.ReadOnly, defaultNamespace, member.FixupIndex, elementIndex, fixup, member);
			}
		}
		else if (anyElementMember != null && anyElementMember.Mapping != null)
		{
			MemberMapping mapping = anyElementMember.Mapping;
			member = anyElementMember;
			ElementAccessor[] elements = mapping.Elements;
			foreach (ElementAccessor elementAccessor3 in elements)
			{
				if (elementAccessor3.Any && elementAccessor3.Name.Length == 0)
				{
					string defaultNamespace2 = ((elementAccessor3.Form == XmlSchemaForm.Qualified) ? elementAccessor3.Namespace : string.Empty);
					WriteElement(elementAccessor3, mapping.CheckSpecified == SpecifiedAccessor.ReadWrite, checkForNull: false, readOnly: false, defaultNamespace2, -1, -1, fixup, member);
					break;
				}
			}
		}
		else
		{
			member = null;
			ProcessUnknownNode(elementElseAction);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private object WriteElement(ElementAccessor element, bool checkSpecified, bool checkForNull, bool readOnly, string defaultNamespace, int fixupIndex = -1, int elementIndex = -1, Fixup fixup = null, Member member = null)
	{
		object obj = null;
		if (element.Mapping is ArrayMapping arrayMapping)
		{
			obj = WriteArray(arrayMapping, readOnly, element.IsNullable, defaultNamespace, fixupIndex, fixup, member);
		}
		else if (element.Mapping is NullableMapping nullableMapping)
		{
			obj = WriteNullableMethod(nullableMapping, checkType: true, defaultNamespace);
		}
		else if (!element.Mapping.IsSoap && element.Mapping is PrimitiveMapping)
		{
			if (element.IsNullable && ReadNull())
			{
				obj = ((!element.Mapping.TypeDesc.IsValueType) ? null : ReflectionCreateObject(element.Mapping.TypeDesc.Type));
			}
			else if (element.Default != null && element.Default != DBNull.Value && element.Mapping.TypeDesc.IsValueType && base.Reader.IsEmptyElement)
			{
				base.Reader.Skip();
			}
			else if (element.Mapping.TypeDesc.Type == typeof(TimeSpan) && base.Reader.IsEmptyElement)
			{
				base.Reader.Skip();
				obj = default(TimeSpan);
			}
			else if (element.Mapping.TypeDesc.Type == typeof(DateTimeOffset) && base.Reader.IsEmptyElement)
			{
				base.Reader.Skip();
				obj = default(DateTimeOffset);
			}
			else if (element.Mapping.TypeDesc == QnameTypeDesc)
			{
				obj = ReadElementQualifiedName();
			}
			else if (element.Mapping.TypeDesc.FormatterName == "ByteArrayBase64")
			{
				obj = ToByteArrayBase64(isNull: false);
			}
			else if (element.Mapping.TypeDesc.FormatterName == "ByteArrayHex")
			{
				obj = ToByteArrayHex(isNull: false);
			}
			else
			{
				Func<object, string> readFunc = (object state) => ((XmlReader)state).ReadElementContentAsString();
				obj = WritePrimitive(element.Mapping, readFunc, base.Reader);
			}
		}
		else if (element.Mapping is StructMapping || (element.Mapping.IsSoap && element.Mapping is PrimitiveMapping))
		{
			TypeMapping mapping = element.Mapping;
			if (mapping.IsSoap)
			{
				object obj2 = ((fixupIndex >= 0) ? ReadReferencingElement(mapping.TypeName, mapping.Namespace, out fixup.Ids[fixupIndex]) : ReadReferencedElement(mapping.TypeName, mapping.Namespace));
				if (!mapping.TypeDesc.IsValueType || obj2 != null)
				{
					obj = obj2;
					Referenced(obj);
				}
				if (fixupIndex >= 0)
				{
					if (member == null)
					{
						throw new InvalidOperationException(System.SR.XmlInternalError);
					}
					member.Source(obj);
					return obj;
				}
			}
			else if (checkForNull && member.Source == null && member.ArraySource == null)
			{
				base.Reader.Skip();
			}
			else
			{
				obj = WriteStructMethod((StructMapping)mapping, mapping.TypeDesc.IsNullable && element.IsNullable, checkType: true, defaultNamespace);
			}
		}
		else
		{
			if (!(element.Mapping is SpecialMapping specialMapping))
			{
				throw new InvalidOperationException(System.SR.XmlInternalError);
			}
			switch (specialMapping.TypeDesc.Kind)
			{
			case TypeKind.Node:
				obj = ((!(specialMapping.TypeDesc.FullName == typeof(XmlDocument).FullName)) ? ReadXmlNode(!element.Any) : ReadXmlDocument(!element.Any));
				break;
			case TypeKind.Serializable:
			{
				SerializableMapping serializableMapping = (SerializableMapping)element.Mapping;
				bool flag = true;
				if (serializableMapping.DerivedMappings != null)
				{
					XmlQualifiedName xsiType = GetXsiType();
					if (!(xsiType == null) && !QNameEqual(xsiType, serializableMapping.XsiType.Name, serializableMapping.XsiType.Namespace, defaultNamespace))
					{
						flag = false;
					}
				}
				if (flag)
				{
					bool wrappedAny = !element.Any && IsWildcard(serializableMapping);
					obj = ReadSerializable((IXmlSerializable)ReflectionCreateObject(serializableMapping.TypeDesc.Type), wrappedAny);
				}
				if (serializableMapping.DerivedMappings != null)
				{
					throw new NotImplementedException("sm.DerivedMappings != null");
				}
				break;
			}
			default:
				throw new InvalidOperationException(System.SR.XmlInternalError);
			}
		}
		member?.ChoiceSource?.Invoke(element.Name);
		if (member != null && member.ArraySource != null)
		{
			member?.ArraySource(obj);
		}
		else
		{
			member?.Source?.Invoke(obj);
			member?.CheckSpecifiedSource?.Invoke(true);
		}
		return obj;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private XmlSerializationReadCallback CreateXmlSerializationReadCallback(TypeMapping mapping)
	{
		StructMapping structMapping = mapping as StructMapping;
		if (structMapping != null)
		{
			return WriteStruct;
		}
		EnumMapping enumMapping = mapping as EnumMapping;
		if (enumMapping != null)
		{
			return () => WriteEnumMethodSoap(enumMapping);
		}
		NullableMapping nullableMapping = mapping as NullableMapping;
		if (nullableMapping != null)
		{
			return Wrapper;
		}
		return DummyReadArrayMethod;
		[RequiresUnreferencedCode("calls WriteNullableMethod")]
		object Wrapper()
		{
			return WriteNullableMethod(nullableMapping, checkType: false, null);
		}
		[RequiresUnreferencedCode("calls WriteStructMethod")]
		object WriteStruct()
		{
			return WriteStructMethod(structMapping, mapping.TypeDesc.IsNullable, checkType: true, null);
		}
	}

	private static void NoopAction(object o)
	{
	}

	private object DummyReadArrayMethod()
	{
		UnknownNode(null);
		return null;
	}

	private static Type GetMemberType(MemberInfo memberInfo)
	{
		if (memberInfo is FieldInfo fieldInfo)
		{
			return fieldInfo.FieldType;
		}
		if (memberInfo is PropertyInfo propertyInfo)
		{
			return propertyInfo.PropertyType;
		}
		throw new InvalidOperationException(System.SR.XmlInternalError);
	}

	private static bool IsWildcard(SpecialMapping mapping)
	{
		if (mapping is SerializableMapping serializableMapping)
		{
			return serializableMapping.IsAny;
		}
		return mapping.TypeDesc.CanBeElementValue;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private object WriteArray(ArrayMapping arrayMapping, bool readOnly, bool isNullable, string defaultNamespace, int fixupIndex = -1, Fixup fixup = null, Member member = null)
	{
		object collection = null;
		if (arrayMapping.IsSoap)
		{
			object obj = ((fixupIndex < 0) ? ReadReferencedElement(arrayMapping.TypeName, arrayMapping.Namespace) : ReadReferencingElement(arrayMapping.TypeName, arrayMapping.Namespace, out fixup.Ids[fixupIndex]));
			TypeDesc typeDesc = arrayMapping.TypeDesc;
			if (obj != null)
			{
				if (typeDesc.IsEnumerable || typeDesc.IsCollection)
				{
					WriteAddCollectionFixup(member.GetSource, member.Source, obj, typeDesc, readOnly);
					member.Source = NoopAction;
				}
				else
				{
					if (member == null)
					{
						throw new InvalidOperationException(System.SR.XmlInternalError);
					}
					member.Source(obj);
				}
			}
			collection = obj;
		}
		else if (!ReadNull())
		{
			MemberMapping memberMapping = new MemberMapping
			{
				Elements = arrayMapping.Elements,
				TypeDesc = arrayMapping.TypeDesc,
				ReadOnly = readOnly
			};
			Type type = memberMapping.TypeDesc.Type;
			collection = ReflectionCreateObject(memberMapping.TypeDesc.Type);
			if (memberMapping.ChoiceIdentifier != null)
			{
				throw new NotImplementedException("memberMapping.ChoiceIdentifier != null");
			}
			Member arrayMember = new Member(memberMapping);
			arrayMember.Collection = new CollectionMember();
			arrayMember.ArraySource = delegate(object item)
			{
				arrayMember.Collection.Add(item);
			};
			if ((readOnly && collection == null) || base.Reader.IsEmptyElement)
			{
				base.Reader.Skip();
			}
			else
			{
				base.Reader.ReadStartElement();
				base.Reader.MoveToContent();
				while (base.Reader.NodeType != XmlNodeType.EndElement && base.Reader.NodeType != 0)
				{
					WriteMemberElements(new Member[1] { arrayMember }, base.UnknownNode, base.UnknownNode, null, null);
					base.Reader.MoveToContent();
				}
				ReadEndElement();
			}
			SetCollectionObjectWithCollectionMember(ref collection, arrayMember.Collection, type);
		}
		return collection;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private object WritePrimitive(TypeMapping mapping, Func<object, string> readFunc, object funcState)
	{
		if (mapping is EnumMapping mapping2)
		{
			return WriteEnumMethod(mapping2, readFunc, funcState);
		}
		if (mapping.TypeDesc == StringTypeDesc)
		{
			return readFunc(funcState);
		}
		if (mapping.TypeDesc.FormatterName == "String")
		{
			if (mapping.TypeDesc.CollapseWhitespace)
			{
				return CollapseWhitespace(readFunc(funcState));
			}
			return readFunc(funcState);
		}
		if (!mapping.TypeDesc.HasCustomFormatter)
		{
			string s = readFunc(funcState);
			return mapping.TypeDesc.FormatterName switch
			{
				"Boolean" => XmlConvert.ToBoolean(s), 
				"Int32" => XmlConvert.ToInt32(s), 
				"Int16" => XmlConvert.ToInt16(s), 
				"Int64" => XmlConvert.ToInt64(s), 
				"Single" => XmlConvert.ToSingle(s), 
				"Double" => XmlConvert.ToDouble(s), 
				"Decimal" => XmlConvert.ToDecimal(s), 
				"Byte" => XmlConvert.ToByte(s), 
				"SByte" => XmlConvert.ToSByte(s), 
				"UInt16" => XmlConvert.ToUInt16(s), 
				"UInt32" => XmlConvert.ToUInt32(s), 
				"UInt64" => XmlConvert.ToUInt64(s), 
				"Guid" => XmlConvert.ToGuid(s), 
				"Char" => XmlConvert.ToChar(s), 
				"TimeSpan" => XmlConvert.ToTimeSpan(s), 
				"DateTimeOffset" => XmlConvert.ToDateTimeOffset(s), 
				_ => throw new InvalidOperationException(System.SR.Format(System.SR.XmlInternalErrorDetails, "unknown FormatterName: " + mapping.TypeDesc.FormatterName)), 
			};
		}
		string name = "To" + mapping.TypeDesc.FormatterName;
		MethodInfo method = typeof(XmlSerializationReader).GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(string) });
		if (method == null)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlInternalErrorDetails, "unknown FormatterName: " + mapping.TypeDesc.FormatterName));
		}
		return method.Invoke(this, new object[1] { readFunc(funcState) });
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private object WriteStructMethod(StructMapping mapping, bool isNullable, bool checkType, string defaultNamespace)
	{
		if (mapping.IsSoap)
		{
			return WriteEncodedStructMethod(mapping);
		}
		return WriteLiteralStructMethod(mapping, isNullable, checkType, defaultNamespace);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private object WriteNullableMethod(NullableMapping nullableMapping, bool checkType, string defaultNamespace)
	{
		object result = Activator.CreateInstance(nullableMapping.TypeDesc.Type);
		if (!ReadNull())
		{
			ElementAccessor elementAccessor = new ElementAccessor();
			elementAccessor.Mapping = nullableMapping.BaseMapping;
			elementAccessor.Any = false;
			elementAccessor.IsNullable = nullableMapping.BaseMapping.TypeDesc.IsNullable;
			result = WriteElement(elementAccessor, checkSpecified: false, checkForNull: false, readOnly: false, defaultNamespace);
		}
		return result;
	}

	private object WriteEnumMethod(EnumMapping mapping, Func<object, string> readFunc, object funcState)
	{
		string source = readFunc(funcState);
		return WriteEnumMethod(mapping, source);
	}

	private object WriteEnumMethodSoap(EnumMapping mapping)
	{
		string source = base.Reader.ReadElementString();
		return WriteEnumMethod(mapping, source);
	}

	private object WriteEnumMethod(EnumMapping mapping, string source)
	{
		if (mapping.IsFlags)
		{
			Hashtable h = WriteHashtable(mapping, mapping.TypeDesc.Name);
			return Enum.ToObject(mapping.TypeDesc.Type, XmlSerializationReader.ToEnum(source, h, mapping.TypeDesc.Name));
		}
		ConstantMapping[] constants = mapping.Constants;
		foreach (ConstantMapping constantMapping in constants)
		{
			if (string.Equals(constantMapping.XmlName, source))
			{
				return Enum.Parse(mapping.TypeDesc.Type, constantMapping.Name);
			}
		}
		throw CreateUnknownConstantException(source, mapping.TypeDesc.Type);
	}

	private Hashtable WriteHashtable(EnumMapping mapping, string name)
	{
		Hashtable hashtable = new Hashtable();
		ConstantMapping[] constants = mapping.Constants;
		for (int i = 0; i < constants.Length; i++)
		{
			hashtable.Add(constants[i].XmlName, constants[i].Value);
		}
		return hashtable;
	}

	private object ReflectionCreateObject([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type)
	{
		if (type.IsArray)
		{
			return Activator.CreateInstance(type, 32);
		}
		ConstructorInfo defaultConstructor = GetDefaultConstructor(type);
		if (defaultConstructor != null)
		{
			return defaultConstructor.Invoke(Array.Empty<object>());
		}
		return Activator.CreateInstance(type);
	}

	private ConstructorInfo GetDefaultConstructor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type)
	{
		if (!type.IsValueType)
		{
			return type.GetConstructor(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
		}
		return null;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private object WriteEncodedStructMethod(StructMapping structMapping)
	{
		if (structMapping.TypeDesc.IsRoot)
		{
			return null;
		}
		Member[] array = null;
		if (structMapping.TypeDesc.IsAbstract)
		{
			throw CreateAbstractTypeException(structMapping.TypeName, structMapping.Namespace);
		}
		object o = ReflectionCreateObject(structMapping.TypeDesc.Type);
		MemberMapping[] settableMembers = TypeScope.GetSettableMembers(structMapping);
		array = new Member[settableMembers.Length];
		for (int i = 0; i < settableMembers.Length; i++)
		{
			MemberMapping mapping = settableMembers[i];
			Member member = new Member(mapping);
			TypeDesc typeDesc = member.Mapping.TypeDesc;
			if (typeDesc.IsCollection || typeDesc.IsEnumerable)
			{
				member.Source = Wrapper;
			}
			else if (!member.Mapping.ReadOnly)
			{
				ReflectionXmlSerializationReaderHelper.SetMemberValueDelegate setterDelegate = GetSetMemberValueDelegate(o, member.Mapping.MemberInfo.Name);
				member.Source = delegate(object value)
				{
					setterDelegate(o, value);
				};
			}
			else
			{
				member.Source = NoopAction;
			}
			array[i] = member;
			[RequiresUnreferencedCode("Calls WriteAddCollectionFixup")]
			void Wrapper(object value)
			{
				WriteAddCollectionFixup(o, member, value);
			}
		}
		Fixup fixup = WriteMemberFixupBegin(array, o);
		UnknownNodeAction elseCall = delegate
		{
			UnknownNode(o);
		};
		WriteAttributes(array, null, elseCall, ref o);
		base.Reader.MoveToElement();
		if (base.Reader.IsEmptyElement)
		{
			base.Reader.Skip();
			return o;
		}
		base.Reader.ReadStartElement();
		base.Reader.MoveToContent();
		while (base.Reader.NodeType != XmlNodeType.EndElement && base.Reader.NodeType != 0)
		{
			WriteMemberElements(array, base.UnknownNode, base.UnknownNode, null, null, fixup);
			base.Reader.MoveToContent();
		}
		ReadEndElement();
		return o;
	}

	private Fixup WriteMemberFixupBegin(Member[] members, object o)
	{
		int num = 0;
		foreach (Member member in members)
		{
			if (member.Mapping.Elements.Length != 0)
			{
				TypeMapping mapping = member.Mapping.Elements[0].Mapping;
				if (mapping is StructMapping || mapping is ArrayMapping || mapping is PrimitiveMapping || mapping is NullableMapping)
				{
					member.MultiRef = true;
					member.FixupIndex = num++;
				}
			}
		}
		Fixup fixup;
		if (num > 0)
		{
			fixup = new Fixup(o, CreateWriteFixupMethod(members), num);
			AddFixup(fixup);
		}
		else
		{
			fixup = null;
		}
		return fixup;
	}

	private XmlSerializationFixupCallback CreateWriteFixupMethod(Member[] members)
	{
		return delegate(object fixupObject)
		{
			Fixup fixup = (Fixup)fixupObject;
			string[] ids = fixup.Ids;
			Member[] array = members;
			foreach (Member member in array)
			{
				if (member.MultiRef)
				{
					int fixupIndex = member.FixupIndex;
					if (ids[fixupIndex] != null)
					{
						object target = GetTarget(ids[fixupIndex]);
						member.Source(target);
					}
				}
			}
		};
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private void WriteAddCollectionFixup(object o, Member member, object memberValue)
	{
		TypeDesc typeDesc = member.Mapping.TypeDesc;
		bool readOnly = member.Mapping.ReadOnly;
		Func<object> getSource = () => GetMemberValue(o, member.Mapping.MemberInfo);
		ReflectionXmlSerializationReaderHelper.SetMemberValueDelegate setterDelegate = GetSetMemberValueDelegate(o, member.Mapping.MemberInfo.Name);
		Action<object> setSource = delegate(object value)
		{
			setterDelegate(o, value);
		};
		WriteAddCollectionFixup(getSource, setSource, memberValue, typeDesc, readOnly);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private object WriteAddCollectionFixup(Func<object> getSource, Action<object> setSource, object memberValue, TypeDesc typeDesc, bool readOnly)
	{
		object obj = getSource();
		if (obj == null)
		{
			if (readOnly)
			{
				throw CreateReadOnlyCollectionException(typeDesc.CSharpName);
			}
			obj = ReflectionCreateObject(typeDesc.Type);
			setSource(obj);
		}
		CollectionFixup fixup = new CollectionFixup(obj, GetCreateCollectionOfObjectsCallback(typeDesc.Type).Invoke, memberValue);
		AddFixup(fixup);
		return obj;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private XmlSerializationCollectionFixupCallback GetCreateCollectionOfObjectsCallback(Type collectionType)
	{
		return Wrapper;
		[RequiresUnreferencedCode("Calls AddObjectsIntoTargetCollection")]
		void Wrapper(object collection, object collectionItems)
		{
			if (collectionItems != null && collection != null)
			{
				List<object> list = new List<object>();
				if (!(collectionItems is IEnumerable enumerable))
				{
					throw new InvalidOperationException(System.SR.XmlInternalError);
				}
				foreach (object item in enumerable)
				{
					list.Add(item);
				}
				AddObjectsIntoTargetCollection(collection, list, collectionType);
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private object WriteLiteralStructMethod(StructMapping structMapping, bool isNullable, bool checkType, string defaultNamespace)
	{
		XmlQualifiedName xmlQualifiedName = (checkType ? GetXsiType() : null);
		bool flag = false;
		if (isNullable)
		{
			flag = ReadNull();
		}
		if (checkType)
		{
			if (structMapping.TypeDesc.IsRoot && flag)
			{
				if (xmlQualifiedName != null)
				{
					return ReadTypedNull(xmlQualifiedName);
				}
				if (structMapping.TypeDesc.IsValueType)
				{
					return ReflectionCreateObject(structMapping.TypeDesc.Type);
				}
				return null;
			}
			object o2 = null;
			if (!(xmlQualifiedName == null) && (structMapping.TypeDesc.IsRoot || !QNameEqual(xmlQualifiedName, structMapping.TypeName, structMapping.Namespace, defaultNamespace)))
			{
				if (WriteDerivedTypes(out o2, structMapping, xmlQualifiedName, defaultNamespace, checkType, isNullable))
				{
					return o2;
				}
				if (structMapping.TypeDesc.IsRoot && WriteEnumAndArrayTypes(out o2, structMapping, xmlQualifiedName, defaultNamespace))
				{
					return o2;
				}
				if (structMapping.TypeDesc.IsRoot)
				{
					return ReadTypedPrimitive(xmlQualifiedName);
				}
				throw CreateUnknownTypeException(xmlQualifiedName);
			}
			if (structMapping.TypeDesc.IsRoot)
			{
				return ReadTypedPrimitive(new XmlQualifiedName("anyType", "http://www.w3.org/2001/XMLSchema"));
			}
		}
		if (structMapping.TypeDesc.IsNullable && flag)
		{
			return null;
		}
		if (structMapping.TypeDesc.IsAbstract)
		{
			throw CreateAbstractTypeException(structMapping.TypeName, structMapping.Namespace);
		}
		if (structMapping.TypeDesc.Type != null && typeof(XmlSchemaObject).IsAssignableFrom(structMapping.TypeDesc.Type))
		{
			throw new NotImplementedException("XmlSchemaObject");
		}
		object o = ReflectionCreateObject(structMapping.TypeDesc.Type);
		MemberMapping[] settableMembers = TypeScope.GetSettableMembers(structMapping);
		MemberMapping memberMapping = null;
		MemberMapping memberMapping2 = null;
		Member anyAttribute = null;
		Member anyElement = null;
		Member anyText = null;
		bool flag2 = structMapping.HasExplicitSequence();
		List<Member> list = new List<Member>(settableMembers.Length);
		List<MemberMapping> list2 = new List<MemberMapping>(settableMembers.Length);
		foreach (MemberMapping memberMapping3 in settableMembers)
		{
			Member member = new Member(memberMapping3);
			if (memberMapping3.Text != null)
			{
				memberMapping = memberMapping3;
			}
			if (memberMapping3.Attribute != null)
			{
				member.Source = Wrapper;
				if (memberMapping3.Attribute.Any)
				{
					anyAttribute = member;
				}
			}
			if (!flag2)
			{
				for (int j = 0; j < memberMapping3.Elements.Length; j++)
				{
					if (memberMapping3.Elements[j].Any && (memberMapping3.Elements[j].Name == null || memberMapping3.Elements[j].Name.Length == 0))
					{
						memberMapping2 = memberMapping3;
						break;
					}
				}
			}
			else if (memberMapping3.IsParticle && !memberMapping3.IsSequence)
			{
				structMapping.FindDeclaringMapping(memberMapping3, out var declaringMapping, structMapping.TypeName);
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlSequenceHierarchy, structMapping.TypeDesc.FullName, memberMapping3.Name, declaringMapping.TypeDesc.FullName, "Order"));
			}
			if (memberMapping3.TypeDesc.IsArrayLike)
			{
				if (member.Source == null && memberMapping3.TypeDesc.IsArrayLike && (memberMapping3.Elements.Length != 1 || !(memberMapping3.Elements[0].Mapping is ArrayMapping)))
				{
					member.Source = delegate(object item)
					{
						if (member.Collection == null)
						{
							member.Collection = new CollectionMember();
						}
						member.Collection.Add(item);
					};
					member.ArraySource = member.Source;
				}
				else
				{
					_ = memberMapping3.TypeDesc.IsArray;
				}
			}
			if (member.Source == null)
			{
				PropertyInfo pi = member.Mapping.MemberInfo as PropertyInfo;
				if (pi != null && typeof(IList).IsAssignableFrom(pi.PropertyType) && (pi.SetMethod == null || !pi.SetMethod.IsPublic))
				{
					member.Source = delegate(object value)
					{
						IList list3 = (IList)pi.GetValue(o);
						if (value is IList list4)
						{
							{
								foreach (object item in list4)
								{
									list3.Add(item);
								}
								return;
							}
						}
						list3.Add(value);
					};
				}
				else if (member.Mapping.Xmlns != null)
				{
					XmlSerializerNamespaces xmlSerializerNamespaces = new XmlSerializerNamespaces();
					ReflectionXmlSerializationReaderHelper.SetMemberValueDelegate setMemberValueDelegate = GetSetMemberValueDelegate(o, member.Mapping.Name);
					setMemberValueDelegate(o, xmlSerializerNamespaces);
					member.XmlnsSource = delegate(string ns, string name)
					{
						xmlSerializerNamespaces.Add(ns, name);
					};
				}
				else
				{
					ReflectionXmlSerializationReaderHelper.SetMemberValueDelegate setterDelegate = GetSetMemberValueDelegate(o, member.Mapping.Name);
					member.Source = delegate(object value)
					{
						setterDelegate(o, value);
					};
				}
			}
			if (member.Mapping.CheckSpecified == SpecifiedAccessor.ReadWrite)
			{
				member.CheckSpecifiedSource = Wrapper;
			}
			ChoiceIdentifierAccessor choice = memberMapping3.ChoiceIdentifier;
			if (choice != null && o != null)
			{
				member.ChoiceSource = Wrapper;
			}
			list2.Add(memberMapping3);
			list.Add(member);
			if (memberMapping3 == memberMapping2)
			{
				anyElement = member;
			}
			else if (memberMapping3 == memberMapping)
			{
				anyText = member;
			}
			[RequiresUnreferencedCode("calls SetOrAddValueToMember")]
			void Wrapper(object value)
			{
				SetOrAddValueToMember(o, value, member.Mapping.MemberInfo);
			}
			[RequiresUnreferencedCode("calls GetType on object")]
			void Wrapper(object _)
			{
				string text3 = member.Mapping.Name + "Specified";
				MethodInfo method = o.GetType().GetMethod("set_" + text3);
				if (method != null)
				{
					method.Invoke(o, new object[1] { true });
				}
			}
			[RequiresUnreferencedCode("Calls SetOrAddValueToMember")]
			void Wrapper(object elementNameObject)
			{
				string text = elementNameObject as string;
				string[] memberIds = choice.MemberIds;
				foreach (string text2 in memberIds)
				{
					if (text2 == text)
					{
						object value2 = Enum.Parse(choice.Mapping.TypeDesc.Type, text2);
						SetOrAddValueToMember(o, value2, choice.MemberInfo);
						break;
					}
				}
			}
		}
		Member[] array = list.ToArray();
		UnknownNodeAction unknownNodeAction = delegate
		{
			UnknownNode(o);
		};
		WriteAttributes(array, anyAttribute, unknownNodeAction, ref o);
		base.Reader.MoveToElement();
		if (base.Reader.IsEmptyElement)
		{
			base.Reader.Skip();
			return o;
		}
		base.Reader.ReadStartElement();
		bool flag3 = IsSequence(array);
		WriteMembers(ref o, array, unknownNodeAction, unknownNodeAction, anyElement, anyText);
		Member[] array2 = array;
		foreach (Member member2 in array2)
		{
			if (member2.Collection != null)
			{
				MemberInfo[] member3 = o.GetType().GetMember(member2.Mapping.Name);
				MemberInfo memberInfo = member3[0];
				object collection = null;
				SetCollectionObjectWithCollectionMember(ref collection, member2.Collection, member2.Mapping.TypeDesc.Type);
				ReflectionXmlSerializationReaderHelper.SetMemberValueDelegate setMemberValueDelegate2 = GetSetMemberValueDelegate(o, memberInfo.Name);
				setMemberValueDelegate2(o, collection);
			}
		}
		ReadEndElement();
		return o;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private bool WriteEnumAndArrayTypes(out object o, StructMapping mapping, XmlQualifiedName xsiType, string defaultNamespace)
	{
		foreach (object typeMapping in _mapping.Scope.TypeMappings)
		{
			if (typeMapping is EnumMapping enumMapping)
			{
				if (QNameEqual(xsiType, enumMapping.TypeName, enumMapping.Namespace, defaultNamespace))
				{
					base.Reader.ReadStartElement();
					Func<object, string> readFunc = delegate(object state)
					{
						ReflectionXmlSerializationReader reflectionXmlSerializationReader = (ReflectionXmlSerializationReader)state;
						return reflectionXmlSerializationReader.CollapseWhitespace(reflectionXmlSerializationReader.Reader.ReadString());
					};
					o = WriteEnumMethod(enumMapping, readFunc, this);
					ReadEndElement();
					return true;
				}
			}
			else if (typeMapping is ArrayMapping arrayMapping && QNameEqual(xsiType, arrayMapping.TypeName, arrayMapping.Namespace, defaultNamespace))
			{
				o = WriteArray(arrayMapping, readOnly: false, isNullable: false, defaultNamespace);
				return true;
			}
		}
		o = null;
		return false;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private bool WriteDerivedTypes(out object o, StructMapping mapping, XmlQualifiedName xsiType, string defaultNamespace, bool checkType, bool isNullable)
	{
		for (StructMapping structMapping = mapping.DerivedMappings; structMapping != null; structMapping = structMapping.NextDerivedMapping)
		{
			if (QNameEqual(xsiType, structMapping.TypeName, structMapping.Namespace, defaultNamespace))
			{
				o = WriteStructMethod(structMapping, isNullable, checkType, defaultNamespace);
				return true;
			}
			if (WriteDerivedTypes(out o, structMapping, xsiType, defaultNamespace, checkType, isNullable))
			{
				return true;
			}
		}
		o = null;
		return false;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private void WriteAttributes(Member[] members, Member anyAttribute, UnknownNodeAction elseCall, ref object o)
	{
		Member member = null;
		List<AttributeAccessor> list = new List<AttributeAccessor>();
		foreach (Member member2 in members)
		{
			if (member2.Mapping.Xmlns != null)
			{
				member = member2;
				break;
			}
		}
		while (base.Reader.MoveToNextAttribute())
		{
			bool flag = false;
			foreach (Member member3 in members)
			{
				if (member3.Mapping.Xmlns != null || member3.Mapping.Ignore)
				{
					continue;
				}
				AttributeAccessor attribute = member3.Mapping.Attribute;
				if (attribute != null && !attribute.Any)
				{
					list.Add(attribute);
					flag = ((!attribute.IsSpecialXmlNamespace) ? XmlNodeEqual(base.Reader, attribute.Name, (attribute.Form == XmlSchemaForm.Qualified) ? attribute.Namespace : string.Empty) : XmlNodeEqual(base.Reader, attribute.Name, "http://www.w3.org/XML/1998/namespace"));
					if (flag)
					{
						WriteAttribute(member3);
						flag = true;
						break;
					}
				}
			}
			if (flag)
			{
				continue;
			}
			bool flag2 = false;
			if (member != null)
			{
				if (IsXmlnsAttribute(base.Reader.Name))
				{
					member.XmlnsSource((base.Reader.Name.Length == 5) ? string.Empty : base.Reader.LocalName, base.Reader.Value);
				}
				else
				{
					flag2 = true;
				}
			}
			else if (!IsXmlnsAttribute(base.Reader.Name))
			{
				flag2 = true;
			}
			if (flag2)
			{
				if (anyAttribute != null)
				{
					XmlAttribute attr = base.Document.ReadNode(base.Reader) as XmlAttribute;
					ParseWsdlArrayType(attr);
					WriteAttribute(anyAttribute, attr);
				}
				else
				{
					elseCall(o);
				}
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private void WriteAttribute(Member member, object attr = null)
	{
		AttributeAccessor attribute = member.Mapping.Attribute;
		object obj = null;
		if (attribute.Mapping is SpecialMapping specialMapping)
		{
			if (specialMapping.TypeDesc.Kind != TypeKind.Attribute)
			{
				if (specialMapping.TypeDesc.CanBeAttributeValue)
				{
					throw new NotImplementedException("special.TypeDesc.CanBeAttributeValue");
				}
				throw new InvalidOperationException(System.SR.XmlInternalError);
			}
			obj = attr;
		}
		else if (attribute.IsList)
		{
			string value = base.Reader.Value;
			string[] array = value.Split((char[]?)null);
			Array array2 = Array.CreateInstance(member.Mapping.TypeDesc.Type.GetElementType(), array.Length);
			int i;
			for (i = 0; i < array.Length; i++)
			{
				array2.SetValue(WritePrimitive(attribute.Mapping, (object state) => ((string[])state)[i], array), i);
			}
			obj = array2;
		}
		else
		{
			obj = WritePrimitive(attribute.Mapping, (object state) => ((XmlReader)state).Value, base.Reader);
		}
		member.Source(obj);
		if (member.Mapping.CheckSpecified == SpecifiedAccessor.ReadWrite)
		{
			member.CheckSpecifiedSource?.Invoke(null);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private void SetOrAddValueToMember(object o, object value, MemberInfo memberInfo)
	{
		Type memberType = GetMemberType(memberInfo);
		if (memberType == value.GetType())
		{
			ReflectionXmlSerializationReaderHelper.SetMemberValueDelegate setMemberValueDelegate = GetSetMemberValueDelegate(o, memberInfo.Name);
			setMemberValueDelegate(o, value);
		}
		else if (memberType.IsArray)
		{
			AddItemInArrayMember(o, memberInfo, memberType, value);
		}
		else
		{
			ReflectionXmlSerializationReaderHelper.SetMemberValueDelegate setMemberValueDelegate2 = GetSetMemberValueDelegate(o, memberInfo.Name);
			setMemberValueDelegate2(o, value);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private void AddItemInArrayMember(object o, MemberInfo memberInfo, Type memberType, object item)
	{
		Array array = (Array)GetMemberValue(o, memberInfo);
		int num = array?.Length ?? 0;
		Array array2 = Array.CreateInstance(memberType.GetElementType(), num + 1);
		if (array != null)
		{
			Array.Copy(array, array2, num);
		}
		array2.SetValue(item, num);
		ReflectionXmlSerializationReaderHelper.SetMemberValueDelegate setMemberValueDelegate = GetSetMemberValueDelegate(o, memberInfo.Name);
		setMemberValueDelegate(o, array2);
	}

	private bool XmlNodeEqual(XmlReader source, string name, string ns)
	{
		if (source.LocalName == name)
		{
			return string.Equals(source.NamespaceURI, ns);
		}
		return false;
	}

	private bool QNameEqual(XmlQualifiedName xsiType, string name, string ns, string defaultNamespace)
	{
		if (xsiType.Name == name)
		{
			return string.Equals(xsiType.Namespace, defaultNamespace);
		}
		return false;
	}

	private void CreateUnknownNodeException(object o)
	{
		CreateUnknownNodeException();
	}
}
