using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.ComponentModel;

internal sealed class ReflectTypeDescriptionProvider : TypeDescriptionProvider
{
	private sealed class IntrinsicTypeConverterData
	{
		private readonly Func<Type, TypeConverter> _constructionFunc;

		private readonly bool _cacheConverterInstance;

		private TypeConverter _converterInstance;

		public IntrinsicTypeConverterData(Func<Type, TypeConverter> constructionFunc, bool cacheConverterInstance = true)
		{
			_constructionFunc = constructionFunc;
			_cacheConverterInstance = cacheConverterInstance;
		}

		public TypeConverter GetOrCreateConverterInstance(Type innerType)
		{
			if (!_cacheConverterInstance)
			{
				return _constructionFunc(innerType);
			}
			if (_converterInstance == null)
			{
				_converterInstance = _constructionFunc(innerType);
			}
			return _converterInstance;
		}
	}

	private sealed class ReflectedTypeData
	{
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
		private readonly Type _type;

		private AttributeCollection _attributes;

		private EventDescriptorCollection _events;

		private PropertyDescriptorCollection _properties;

		private TypeConverter _converter;

		private object[] _editors;

		private Type[] _editorTypes;

		private int _editorCount;

		internal bool IsPopulated => (_attributes != null) | (_events != null) | (_properties != null);

		internal ReflectedTypeData([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
		{
			_type = type;
		}

		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2062:UnrecognizedReflectionPattern", Justification = "_type is annotated as preserve All members, so any Types returned from GetInterfaces should be preserved as well once https://github.com/mono/linker/issues/1731 is fixed.")]
		internal AttributeCollection GetAttributes()
		{
			if (_attributes == null)
			{
				List<Attribute> list = new List<Attribute>(ReflectGetAttributes(_type));
				Type baseType = _type.BaseType;
				while (baseType != null && baseType != typeof(object))
				{
					list.AddRange(ReflectGetAttributes(baseType));
					baseType = baseType.BaseType;
				}
				int count = list.Count;
				Type[] interfaces = _type.GetInterfaces();
				foreach (Type type in interfaces)
				{
					if ((type.Attributes & TypeAttributes.NestedPrivate) != 0)
					{
						list.AddRange(TypeDescriptor.GetAttributes(type).Attributes);
					}
				}
				if (list.Count != 0)
				{
					HashSet<object> hashSet = new HashSet<object>(list.Count);
					int num = 0;
					for (int j = 0; j < list.Count; j++)
					{
						Attribute attribute = list[j];
						bool flag = true;
						if (j >= count)
						{
							for (int k = 0; k < s_skipInterfaceAttributeList.Length; k++)
							{
								if (s_skipInterfaceAttributeList[k].IsInstanceOfType(attribute))
								{
									flag = false;
									break;
								}
							}
						}
						if (flag && hashSet.Add(attribute.TypeId))
						{
							list[num++] = list[j];
						}
					}
					list.RemoveRange(num, list.Count - num);
				}
				_attributes = new AttributeCollection(list.ToArray());
			}
			return _attributes;
		}

		internal string GetClassName(object instance)
		{
			return _type.FullName;
		}

		internal string GetComponentName(object instance)
		{
			ISite site = ((instance is IComponent component) ? component.Site : null);
			if (site != null)
			{
				return ((site is INestedSite nestedSite) ? nestedSite.FullName : null) ?? site.Name;
			}
			return null;
		}

		[RequiresUnreferencedCode("NullableConverter's UnderlyingType cannot be statically discovered. The Type of instance cannot be statically discovered.")]
		internal TypeConverter GetConverter(object instance)
		{
			TypeConverterAttribute typeConverterAttribute = null;
			if (instance != null)
			{
				typeConverterAttribute = (TypeConverterAttribute)TypeDescriptor.GetAttributes(_type)[typeof(TypeConverterAttribute)];
				TypeConverterAttribute typeConverterAttribute2 = (TypeConverterAttribute)TypeDescriptor.GetAttributes(instance)[typeof(TypeConverterAttribute)];
				if (typeConverterAttribute != typeConverterAttribute2)
				{
					Type typeFromName = GetTypeFromName(typeConverterAttribute2.ConverterTypeName);
					if (typeFromName != null && typeof(TypeConverter).IsAssignableFrom(typeFromName))
					{
						return (TypeConverter)CreateInstance(typeFromName, _type);
					}
				}
			}
			if (_converter == null)
			{
				if (typeConverterAttribute == null)
				{
					typeConverterAttribute = (TypeConverterAttribute)TypeDescriptor.GetAttributes(_type)[typeof(TypeConverterAttribute)];
				}
				if (typeConverterAttribute != null)
				{
					Type typeFromName2 = GetTypeFromName(typeConverterAttribute.ConverterTypeName);
					if (typeFromName2 != null && typeof(TypeConverter).IsAssignableFrom(typeFromName2))
					{
						_converter = (TypeConverter)CreateInstance(typeFromName2, _type);
					}
				}
				if (_converter == null)
				{
					_converter = GetIntrinsicTypeConverter(_type);
				}
			}
			return _converter;
		}

		[RequiresUnreferencedCode("The Type of instance cannot be statically discovered.")]
		internal EventDescriptor GetDefaultEvent(object instance)
		{
			AttributeCollection attributeCollection = ((instance == null) ? TypeDescriptor.GetAttributes(_type) : TypeDescriptor.GetAttributes(instance));
			DefaultEventAttribute defaultEventAttribute = (DefaultEventAttribute)attributeCollection[typeof(DefaultEventAttribute)];
			if (defaultEventAttribute != null && defaultEventAttribute.Name != null)
			{
				if (instance != null)
				{
					return TypeDescriptor.GetEvents(instance)[defaultEventAttribute.Name];
				}
				return TypeDescriptor.GetEvents(_type)[defaultEventAttribute.Name];
			}
			return null;
		}

		[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The Type of instance cannot be statically discovered.")]
		internal PropertyDescriptor GetDefaultProperty(object instance)
		{
			AttributeCollection attributeCollection = ((instance == null) ? TypeDescriptor.GetAttributes(_type) : TypeDescriptor.GetAttributes(instance));
			DefaultPropertyAttribute defaultPropertyAttribute = (DefaultPropertyAttribute)attributeCollection[typeof(DefaultPropertyAttribute)];
			if (defaultPropertyAttribute != null && defaultPropertyAttribute.Name != null)
			{
				if (instance != null)
				{
					return TypeDescriptor.GetProperties(instance)[defaultPropertyAttribute.Name];
				}
				return TypeDescriptor.GetProperties(_type)[defaultPropertyAttribute.Name];
			}
			return null;
		}

		[RequiresUnreferencedCode("Editors registered in TypeDescriptor.AddEditorTable may be trimmed. The Type of instance cannot be statically discovered.")]
		internal object GetEditor(object instance, Type editorBaseType)
		{
			EditorAttribute editorAttribute;
			if (instance != null)
			{
				editorAttribute = GetEditorAttribute(TypeDescriptor.GetAttributes(_type), editorBaseType);
				EditorAttribute editorAttribute2 = GetEditorAttribute(TypeDescriptor.GetAttributes(instance), editorBaseType);
				if (editorAttribute != editorAttribute2)
				{
					Type typeFromName = GetTypeFromName(editorAttribute2.EditorTypeName);
					if (typeFromName != null && editorBaseType.IsAssignableFrom(typeFromName))
					{
						return CreateInstance(typeFromName, _type);
					}
				}
			}
			lock (this)
			{
				for (int i = 0; i < _editorCount; i++)
				{
					if (_editorTypes[i] == editorBaseType)
					{
						return _editors[i];
					}
				}
			}
			object obj = null;
			editorAttribute = GetEditorAttribute(TypeDescriptor.GetAttributes(_type), editorBaseType);
			if (editorAttribute != null)
			{
				Type typeFromName2 = GetTypeFromName(editorAttribute.EditorTypeName);
				if (typeFromName2 != null && editorBaseType.IsAssignableFrom(typeFromName2))
				{
					obj = CreateInstance(typeFromName2, _type);
				}
			}
			if (obj == null)
			{
				Hashtable editorTable = GetEditorTable(editorBaseType);
				if (editorTable != null)
				{
					obj = GetIntrinsicTypeEditor(editorTable, _type);
				}
				if (obj != null && !editorBaseType.IsInstanceOfType(obj))
				{
					obj = null;
				}
			}
			if (obj != null)
			{
				lock (this)
				{
					if (_editorTypes == null || _editorTypes.Length == _editorCount)
					{
						int num = ((_editorTypes == null) ? 4 : (_editorTypes.Length * 2));
						Type[] array = new Type[num];
						object[] array2 = new object[num];
						if (_editorTypes != null)
						{
							_editorTypes.CopyTo(array, 0);
							_editors.CopyTo(array2, 0);
						}
						_editorTypes = array;
						_editors = array2;
						_editorTypes[_editorCount] = editorBaseType;
						_editors[_editorCount++] = obj;
					}
				}
			}
			return obj;
		}

		private static EditorAttribute GetEditorAttribute(AttributeCollection attributes, Type editorBaseType)
		{
			foreach (Attribute attribute in attributes)
			{
				if (attribute is EditorAttribute editorAttribute)
				{
					Type type = Type.GetType(editorAttribute.EditorBaseTypeName);
					if (type != null && type == editorBaseType)
					{
						return editorAttribute;
					}
				}
			}
			return null;
		}

		internal EventDescriptorCollection GetEvents()
		{
			if (_events == null)
			{
				Dictionary<string, EventDescriptor> dictionary = new Dictionary<string, EventDescriptor>(16);
				Type type = _type;
				Type typeFromHandle = typeof(object);
				EventDescriptor[] array;
				do
				{
					array = ReflectGetEvents(type);
					EventDescriptor[] array2 = array;
					foreach (EventDescriptor eventDescriptor in array2)
					{
						dictionary.TryAdd(eventDescriptor.Name, eventDescriptor);
					}
					type = type.BaseType;
				}
				while (type != null && type != typeFromHandle);
				array = new EventDescriptor[dictionary.Count];
				dictionary.Values.CopyTo(array, 0);
				_events = new EventDescriptorCollection(array, readOnly: true);
			}
			return _events;
		}

		[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
		internal PropertyDescriptorCollection GetProperties()
		{
			if (_properties == null)
			{
				Dictionary<string, PropertyDescriptor> dictionary = new Dictionary<string, PropertyDescriptor>(10);
				Type type = _type;
				Type typeFromHandle = typeof(object);
				PropertyDescriptor[] array;
				do
				{
					array = ReflectGetProperties(type);
					PropertyDescriptor[] array2 = array;
					foreach (PropertyDescriptor propertyDescriptor in array2)
					{
						dictionary.TryAdd(propertyDescriptor.Name, propertyDescriptor);
					}
					type = type.BaseType;
				}
				while (type != null && type != typeFromHandle);
				array = new PropertyDescriptor[dictionary.Count];
				dictionary.Values.CopyTo(array, 0);
				_properties = new PropertyDescriptorCollection(array, readOnly: true);
			}
			return _properties;
		}

		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Calling _type.Assembly.GetType on a non-assembly qualified type will still work. See https://github.com/mono/linker/issues/1895")]
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2057:TypeGetType", Justification = "Using the non-assembly qualified type name will still work.")]
		private Type GetTypeFromName([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] string typeName)
		{
			if (string.IsNullOrEmpty(typeName))
			{
				return null;
			}
			int num = typeName.IndexOf(',');
			Type type = null;
			if (num == -1)
			{
				type = _type.Assembly.GetType(typeName);
			}
			if (type == null)
			{
				type = Type.GetType(typeName);
			}
			if (type == null && num != -1)
			{
				type = Type.GetType(typeName.Substring(0, num));
			}
			return type;
		}

		internal void Refresh()
		{
			_attributes = null;
			_events = null;
			_properties = null;
			_converter = null;
			_editors = null;
			_editorTypes = null;
			_editorCount = 0;
		}
	}

	private Hashtable _typeData;

	private static readonly Type[] s_typeConstructor = new Type[1] { typeof(Type) };

	private static Hashtable s_editorTables;

	private static Dictionary<object, IntrinsicTypeConverterData> s_intrinsicTypeConverters;

	private static readonly object s_intrinsicReferenceKey = new object();

	private static readonly object s_intrinsicNullableKey = new object();

	private static readonly object s_dictionaryKey = new object();

	private static Hashtable s_propertyCache;

	private static Hashtable s_eventCache;

	private static Hashtable s_attributeCache;

	private static Hashtable s_extendedPropertyCache;

	private static readonly Guid s_extenderPropertiesKey = Guid.NewGuid();

	private static readonly Guid s_extenderProviderPropertiesKey = Guid.NewGuid();

	private static readonly Type[] s_skipInterfaceAttributeList = new Type[1] { typeof(ComVisibleAttribute) };

	private static readonly object s_internalSyncObject = new object();

	internal static Guid ExtenderProviderKey { get; } = Guid.NewGuid();


	private static Hashtable EditorTables => LazyInitializer.EnsureInitialized(ref s_editorTables, () => new Hashtable(4));

	private static Dictionary<object, IntrinsicTypeConverterData> IntrinsicTypeConverters
	{
		[RequiresUnreferencedCode("NullableConverter's UnderlyingType cannot be statically discovered.")]
		get
		{
			return LazyInitializer.EnsureInitialized(ref s_intrinsicTypeConverters, () => new Dictionary<object, IntrinsicTypeConverterData>(27)
			{
				[typeof(bool)] = new IntrinsicTypeConverterData((Type type) => new BooleanConverter()),
				[typeof(byte)] = new IntrinsicTypeConverterData((Type type) => new ByteConverter()),
				[typeof(sbyte)] = new IntrinsicTypeConverterData((Type type) => new SByteConverter()),
				[typeof(char)] = new IntrinsicTypeConverterData((Type type) => new CharConverter()),
				[typeof(double)] = new IntrinsicTypeConverterData((Type type) => new DoubleConverter()),
				[typeof(string)] = new IntrinsicTypeConverterData((Type type) => new StringConverter()),
				[typeof(int)] = new IntrinsicTypeConverterData((Type type) => new Int32Converter()),
				[typeof(short)] = new IntrinsicTypeConverterData((Type type) => new Int16Converter()),
				[typeof(long)] = new IntrinsicTypeConverterData((Type type) => new Int64Converter()),
				[typeof(float)] = new IntrinsicTypeConverterData((Type type) => new SingleConverter()),
				[typeof(ushort)] = new IntrinsicTypeConverterData((Type type) => new UInt16Converter()),
				[typeof(uint)] = new IntrinsicTypeConverterData((Type type) => new UInt32Converter()),
				[typeof(ulong)] = new IntrinsicTypeConverterData((Type type) => new UInt64Converter()),
				[typeof(object)] = new IntrinsicTypeConverterData((Type type) => new TypeConverter()),
				[typeof(CultureInfo)] = new IntrinsicTypeConverterData((Type type) => new CultureInfoConverter()),
				[typeof(DateTime)] = new IntrinsicTypeConverterData((Type type) => new DateTimeConverter()),
				[typeof(DateTimeOffset)] = new IntrinsicTypeConverterData((Type type) => new DateTimeOffsetConverter()),
				[typeof(decimal)] = new IntrinsicTypeConverterData((Type type) => new DecimalConverter()),
				[typeof(TimeSpan)] = new IntrinsicTypeConverterData((Type type) => new TimeSpanConverter()),
				[typeof(Guid)] = new IntrinsicTypeConverterData((Type type) => new GuidConverter()),
				[typeof(Uri)] = new IntrinsicTypeConverterData((Type type) => new UriTypeConverter()),
				[typeof(Version)] = new IntrinsicTypeConverterData((Type type) => new VersionConverter()),
				[typeof(Array)] = new IntrinsicTypeConverterData((Type type) => new ArrayConverter()),
				[typeof(ICollection)] = new IntrinsicTypeConverterData((Type type) => new CollectionConverter()),
				[typeof(Enum)] = new IntrinsicTypeConverterData((Type type) => CreateEnumConverter(type), cacheConverterInstance: false),
				[s_intrinsicNullableKey] = new IntrinsicTypeConverterData((Type type) => CreateNullableConverter(type), cacheConverterInstance: false),
				[s_intrinsicReferenceKey] = new IntrinsicTypeConverterData((Type type) => new ReferenceConverter(type), cacheConverterInstance: false)
			});
		}
	}

	private static Hashtable PropertyCache => LazyInitializer.EnsureInitialized(ref s_propertyCache, () => new Hashtable());

	private static Hashtable EventCache => LazyInitializer.EnsureInitialized(ref s_eventCache, () => new Hashtable());

	private static Hashtable AttributeCache => LazyInitializer.EnsureInitialized(ref s_attributeCache, () => new Hashtable());

	private static Hashtable ExtendedPropertyCache => LazyInitializer.EnsureInitialized(ref s_extendedPropertyCache, () => new Hashtable());

	internal ReflectTypeDescriptionProvider()
	{
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "IntrinsicTypeConverters is marked with RequiresUnreferencedCode. It is the only place that should call this.")]
	private static NullableConverter CreateNullableConverter(Type type)
	{
		return new NullableConverter(type);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern", Justification = "Trimmer does not trim enums")]
	private static EnumConverter CreateEnumConverter(Type type)
	{
		return new EnumConverter(type);
	}

	internal static void ClearReflectionCaches()
	{
		s_propertyCache = null;
		s_eventCache = null;
		s_attributeCache = null;
		s_extendedPropertyCache = null;
	}

	[RequiresUnreferencedCode("The Types specified in table may be trimmed, or have their static construtors trimmed.")]
	internal static void AddEditorTable(Type editorBaseType, Hashtable table)
	{
		if (editorBaseType == null)
		{
			throw new ArgumentNullException("editorBaseType");
		}
		lock (s_internalSyncObject)
		{
			Hashtable editorTables = EditorTables;
			if (!editorTables.ContainsKey(editorBaseType))
			{
				editorTables[editorBaseType] = table;
			}
		}
	}

	public override object CreateInstance(IServiceProvider provider, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type objectType, Type[] argTypes, object[] args)
	{
		object obj = null;
		if (argTypes != null)
		{
			obj = objectType.GetConstructor(argTypes)?.Invoke(args);
		}
		else
		{
			if (args != null)
			{
				argTypes = new Type[args.Length];
				for (int i = 0; i < args.Length; i++)
				{
					if (args[i] != null)
					{
						argTypes[i] = args[i].GetType();
					}
					else
					{
						argTypes[i] = typeof(object);
					}
				}
			}
			else
			{
				argTypes = Type.EmptyTypes;
			}
			obj = objectType.GetConstructor(argTypes)?.Invoke(args);
		}
		return obj ?? Activator.CreateInstance(objectType, args);
	}

	private static object CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type objectType, Type callingType)
	{
		return objectType.GetConstructor(s_typeConstructor)?.Invoke(new object[1] { callingType }) ?? Activator.CreateInstance(objectType);
	}

	internal AttributeCollection GetAttributes([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
	{
		ReflectedTypeData typeData = GetTypeData(type, createIfNeeded: true);
		return typeData.GetAttributes();
	}

	public override IDictionary GetCache(object instance)
	{
		if (instance is IComponent { Site: not null } component && component.Site.GetService(typeof(IDictionaryService)) is IDictionaryService dictionaryService)
		{
			IDictionary dictionary = dictionaryService.GetValue(s_dictionaryKey) as IDictionary;
			if (dictionary == null)
			{
				dictionary = new Hashtable();
				dictionaryService.SetValue(s_dictionaryKey, dictionary);
			}
			return dictionary;
		}
		return null;
	}

	internal string GetClassName([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
	{
		ReflectedTypeData typeData = GetTypeData(type, createIfNeeded: true);
		return typeData.GetClassName(null);
	}

	internal string GetComponentName([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, object instance)
	{
		ReflectedTypeData typeData = GetTypeData(type, createIfNeeded: true);
		return typeData.GetComponentName(instance);
	}

	[RequiresUnreferencedCode("NullableConverter's UnderlyingType cannot be statically discovered. The Type of instance cannot be statically discovered.")]
	internal TypeConverter GetConverter([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, object instance)
	{
		ReflectedTypeData typeData = GetTypeData(type, createIfNeeded: true);
		return typeData.GetConverter(instance);
	}

	[RequiresUnreferencedCode("The Type of instance cannot be statically discovered.")]
	internal EventDescriptor GetDefaultEvent([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, object instance)
	{
		ReflectedTypeData typeData = GetTypeData(type, createIfNeeded: true);
		return typeData.GetDefaultEvent(instance);
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The Type of instance cannot be statically discovered.")]
	internal PropertyDescriptor GetDefaultProperty([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, object instance)
	{
		ReflectedTypeData typeData = GetTypeData(type, createIfNeeded: true);
		return typeData.GetDefaultProperty(instance);
	}

	[RequiresUnreferencedCode("Editors registered in TypeDescriptor.AddEditorTable may be trimmed. The Type of instance cannot be statically discovered.")]
	internal object GetEditor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, object instance, Type editorBaseType)
	{
		ReflectedTypeData typeData = GetTypeData(type, createIfNeeded: true);
		return typeData.GetEditor(instance, editorBaseType);
	}

	[RequiresUnreferencedCode("The Types specified in EditorTables may be trimmed, or have their static construtors trimmed.")]
	private static Hashtable GetEditorTable(Type editorBaseType)
	{
		Hashtable editorTables = EditorTables;
		object obj = editorTables[editorBaseType];
		if (obj == null)
		{
			RuntimeHelpers.RunClassConstructor(editorBaseType.TypeHandle);
			obj = editorTables[editorBaseType];
			if (obj == null)
			{
				lock (s_internalSyncObject)
				{
					obj = editorTables[editorBaseType];
					if (obj == null)
					{
						editorTables[editorBaseType] = editorTables;
					}
				}
			}
		}
		if (obj == editorTables)
		{
			obj = null;
		}
		return (Hashtable)obj;
	}

	internal EventDescriptorCollection GetEvents([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
	{
		ReflectedTypeData typeData = GetTypeData(type, createIfNeeded: true);
		return typeData.GetEvents();
	}

	internal AttributeCollection GetExtendedAttributes(object instance)
	{
		return AttributeCollection.Empty;
	}

	[RequiresUnreferencedCode("The Type of instance cannot be statically discovered.")]
	internal string GetExtendedClassName(object instance)
	{
		return GetClassName(instance.GetType());
	}

	[RequiresUnreferencedCode("The Type of instance cannot be statically discovered.")]
	internal string GetExtendedComponentName(object instance)
	{
		return GetComponentName(instance.GetType(), instance);
	}

	[RequiresUnreferencedCode("The Type of instance cannot be statically discovered. NullableConverter's UnderlyingType cannot be statically discovered.")]
	internal TypeConverter GetExtendedConverter(object instance)
	{
		return GetConverter(instance.GetType(), instance);
	}

	internal EventDescriptor GetExtendedDefaultEvent(object instance)
	{
		return null;
	}

	internal PropertyDescriptor GetExtendedDefaultProperty(object instance)
	{
		return null;
	}

	[RequiresUnreferencedCode("Editors registered in TypeDescriptor.AddEditorTable may be trimmed. The Type of instance cannot be statically discovered.")]
	internal object GetExtendedEditor(object instance, Type editorBaseType)
	{
		return GetEditor(instance.GetType(), instance, editorBaseType);
	}

	internal EventDescriptorCollection GetExtendedEvents(object instance)
	{
		return EventDescriptorCollection.Empty;
	}

	[RequiresUnreferencedCode("The Type of instance and its IExtenderProviders cannot be statically discovered.")]
	internal PropertyDescriptorCollection GetExtendedProperties(object instance)
	{
		Type type = instance.GetType();
		IExtenderProvider[] extenderProviders = GetExtenderProviders(instance);
		IDictionary cache = TypeDescriptor.GetCache(instance);
		if (extenderProviders.Length == 0)
		{
			return PropertyDescriptorCollection.Empty;
		}
		PropertyDescriptorCollection propertyDescriptorCollection = null;
		if (cache != null)
		{
			propertyDescriptorCollection = cache[s_extenderPropertiesKey] as PropertyDescriptorCollection;
		}
		if (propertyDescriptorCollection != null)
		{
			return propertyDescriptorCollection;
		}
		List<PropertyDescriptor> list = null;
		for (int i = 0; i < extenderProviders.Length; i++)
		{
			PropertyDescriptor[] array = ReflectGetExtendedProperties(extenderProviders[i]);
			if (list == null)
			{
				list = new List<PropertyDescriptor>(array.Length * extenderProviders.Length);
			}
			foreach (PropertyDescriptor propertyDescriptor in array)
			{
				if (propertyDescriptor.Attributes[typeof(ExtenderProvidedPropertyAttribute)] is ExtenderProvidedPropertyAttribute extenderProvidedPropertyAttribute)
				{
					Type receiverType = extenderProvidedPropertyAttribute.ReceiverType;
					if (receiverType != null && receiverType.IsAssignableFrom(type))
					{
						list.Add(propertyDescriptor);
					}
				}
			}
		}
		if (list != null)
		{
			PropertyDescriptor[] array2 = new PropertyDescriptor[list.Count];
			list.CopyTo(array2, 0);
			propertyDescriptorCollection = new PropertyDescriptorCollection(array2, readOnly: true);
		}
		else
		{
			propertyDescriptorCollection = PropertyDescriptorCollection.Empty;
		}
		if (cache != null)
		{
			cache[s_extenderPropertiesKey] = propertyDescriptorCollection;
		}
		return propertyDescriptorCollection;
	}

	protected internal override IExtenderProvider[] GetExtenderProviders(object instance)
	{
		if (instance == null)
		{
			throw new ArgumentNullException("instance");
		}
		if (instance is IComponent { Site: not null } component)
		{
			IExtenderListService extenderListService = component.Site.GetService(typeof(IExtenderListService)) as IExtenderListService;
			IDictionary cache = TypeDescriptor.GetCache(instance);
			if (extenderListService != null)
			{
				return GetExtenders(extenderListService.GetExtenderProviders(), instance, cache);
			}
		}
		return Array.Empty<IExtenderProvider>();
	}

	private static IExtenderProvider[] GetExtenders(ICollection components, object instance, IDictionary cache)
	{
		bool flag = false;
		int num = 0;
		IExtenderProvider[] array = null;
		ulong num2 = 0uL;
		int num3 = 64;
		IExtenderProvider[] array2 = components as IExtenderProvider[];
		if (cache != null)
		{
			array = cache[ExtenderProviderKey] as IExtenderProvider[];
		}
		if (array == null)
		{
			flag = true;
		}
		int num4 = 0;
		int num5 = 0;
		if (array2 != null)
		{
			for (num4 = 0; num4 < array2.Length; num4++)
			{
				if (array2[num4].CanExtend(instance))
				{
					num++;
					if (num4 < num3)
					{
						num2 |= (ulong)(1L << num4);
					}
					if (!flag && (num5 >= array.Length || array2[num4] != array[num5++]))
					{
						flag = true;
					}
				}
			}
		}
		else if (components != null)
		{
			foreach (object component in components)
			{
				if (component is IExtenderProvider extenderProvider && extenderProvider.CanExtend(instance))
				{
					num++;
					if (num4 < num3)
					{
						num2 |= (ulong)(1L << num4);
					}
					if (!flag && (num5 >= array.Length || extenderProvider != array[num5++]))
					{
						flag = true;
					}
				}
				num4++;
			}
		}
		if (array != null && num != array.Length)
		{
			flag = true;
		}
		if (flag)
		{
			if (array2 == null || num != array2.Length)
			{
				IExtenderProvider[] array3 = new IExtenderProvider[num];
				num4 = 0;
				num5 = 0;
				if (array2 != null && num > 0)
				{
					for (; num4 < array2.Length; num4++)
					{
						if ((num4 < num3 && (num2 & (ulong)(1L << num4)) != 0L) || (num4 >= num3 && array2[num4].CanExtend(instance)))
						{
							array3[num5++] = array2[num4];
						}
					}
				}
				else if (num > 0)
				{
					foreach (object component2 in components)
					{
						if (component2 is IExtenderProvider extenderProvider2 && ((num4 < num3 && (num2 & (ulong)(1L << num4)) != 0L) || (num4 >= num3 && extenderProvider2.CanExtend(instance))))
						{
							array3[num5++] = extenderProvider2;
						}
						num4++;
					}
				}
				array2 = array3;
			}
			if (cache != null)
			{
				cache[ExtenderProviderKey] = array2;
				cache.Remove(s_extenderPropertiesKey);
			}
		}
		else
		{
			array2 = array;
		}
		return array2;
	}

	internal object GetExtendedPropertyOwner(object instance, PropertyDescriptor pd)
	{
		return GetPropertyOwner(instance.GetType(), instance, pd);
	}

	[RequiresUnreferencedCode("The Type of instance cannot be statically discovered.")]
	public override ICustomTypeDescriptor GetExtendedTypeDescriptor(object instance)
	{
		return null;
	}

	[RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
	public override string GetFullComponentName(object component)
	{
		if (((component is IComponent component2) ? component2.Site : null) is INestedSite nestedSite)
		{
			return nestedSite.FullName;
		}
		return TypeDescriptor.GetComponentName(component);
	}

	internal Type[] GetPopulatedTypes(Module module)
	{
		List<Type> list = new List<Type>();
		lock (s_internalSyncObject)
		{
			Hashtable typeData = _typeData;
			if (typeData != null)
			{
				IDictionaryEnumerator enumerator = typeData.GetEnumerator();
				while (enumerator.MoveNext())
				{
					DictionaryEntry entry = enumerator.Entry;
					Type type = (Type)entry.Key;
					if (type.Module == module && ((ReflectedTypeData)entry.Value).IsPopulated)
					{
						list.Add(type);
					}
				}
			}
		}
		return list.ToArray();
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
	internal PropertyDescriptorCollection GetProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
	{
		ReflectedTypeData typeData = GetTypeData(type, createIfNeeded: true);
		return typeData.GetProperties();
	}

	internal object GetPropertyOwner(Type type, object instance, PropertyDescriptor pd)
	{
		return TypeDescriptor.GetAssociation(type, instance);
	}

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicFields)]
	public override Type GetReflectionType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicFields)] Type objectType, object instance)
	{
		return objectType;
	}

	private ReflectedTypeData GetTypeData([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, bool createIfNeeded)
	{
		ReflectedTypeData reflectedTypeData = null;
		if (_typeData != null)
		{
			reflectedTypeData = (ReflectedTypeData)_typeData[type];
			if (reflectedTypeData != null)
			{
				return reflectedTypeData;
			}
		}
		lock (s_internalSyncObject)
		{
			if (_typeData != null)
			{
				reflectedTypeData = (ReflectedTypeData)_typeData[type];
			}
			if (reflectedTypeData == null && createIfNeeded)
			{
				reflectedTypeData = new ReflectedTypeData(type);
				if (_typeData == null)
				{
					_typeData = new Hashtable();
				}
				_typeData[type] = reflectedTypeData;
			}
		}
		return reflectedTypeData;
	}

	public override ICustomTypeDescriptor GetTypeDescriptor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType, object instance)
	{
		return null;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2057:TypeGetType", Justification = "typeName is annotated with DynamicallyAccessedMembers, which will preserve the type. Using the non-assembly qualified type name will still work.")]
	private static Type GetTypeFromName([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string typeName)
	{
		Type type = Type.GetType(typeName);
		if (type == null)
		{
			int num = typeName.IndexOf(',');
			if (num != -1)
			{
				type = Type.GetType(typeName.Substring(0, num));
			}
		}
		return type;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern", Justification = "ReflectedTypeData is not being created here, just checking if was already created.")]
	internal bool IsPopulated(Type type)
	{
		return GetTypeData(type, createIfNeeded: false)?.IsPopulated ?? false;
	}

	internal static Attribute[] ReflectGetAttributes(Type type)
	{
		Hashtable attributeCache = AttributeCache;
		Attribute[] array = (Attribute[])attributeCache[type];
		if (array != null)
		{
			return array;
		}
		lock (s_internalSyncObject)
		{
			array = (Attribute[])attributeCache[type];
			if (array == null)
			{
				array = (Attribute[])(attributeCache[type] = type.GetCustomAttributes(typeof(Attribute), inherit: false).OfType<Attribute>().ToArray());
			}
		}
		return array;
	}

	internal static Attribute[] ReflectGetAttributes(MemberInfo member)
	{
		Hashtable attributeCache = AttributeCache;
		Attribute[] array = (Attribute[])attributeCache[member];
		if (array != null)
		{
			return array;
		}
		lock (s_internalSyncObject)
		{
			array = (Attribute[])attributeCache[member];
			if (array == null)
			{
				array = (Attribute[])(attributeCache[member] = member.GetCustomAttributes(typeof(Attribute), inherit: false).OfType<Attribute>().ToArray());
			}
		}
		return array;
	}

	private static EventDescriptor[] ReflectGetEvents([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
	{
		Hashtable eventCache = EventCache;
		EventDescriptor[] array = (EventDescriptor[])eventCache[type];
		if (array != null)
		{
			return array;
		}
		lock (s_internalSyncObject)
		{
			array = (EventDescriptor[])eventCache[type];
			if (array == null)
			{
				BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public;
				EventInfo[] events = type.GetEvents(bindingAttr);
				array = new EventDescriptor[events.Length];
				int num = 0;
				foreach (EventInfo eventInfo in events)
				{
					if ((eventInfo.DeclaringType.IsPublic || eventInfo.DeclaringType.IsNestedPublic || !(eventInfo.DeclaringType.Assembly == typeof(ReflectTypeDescriptionProvider).Assembly)) && eventInfo.AddMethod != null && eventInfo.RemoveMethod != null)
					{
						array[num++] = new ReflectEventDescriptor(type, eventInfo);
					}
				}
				if (num != array.Length)
				{
					EventDescriptor[] array2 = new EventDescriptor[num];
					Array.Copy(array, array2, num);
					array = array2;
				}
				eventCache[type] = array;
			}
		}
		return array;
	}

	[RequiresUnreferencedCode("The type of provider cannot be statically discovered.")]
	private static PropertyDescriptor[] ReflectGetExtendedProperties(IExtenderProvider provider)
	{
		IDictionary cache = TypeDescriptor.GetCache(provider);
		if (cache != null && cache[s_extenderProviderPropertiesKey] is PropertyDescriptor[] result)
		{
			return result;
		}
		Type type = provider.GetType();
		Hashtable extendedPropertyCache = ExtendedPropertyCache;
		ReflectPropertyDescriptor[] array = (ReflectPropertyDescriptor[])extendedPropertyCache[type];
		if (array == null)
		{
			lock (s_internalSyncObject)
			{
				array = (ReflectPropertyDescriptor[])extendedPropertyCache[type];
				if (array == null)
				{
					AttributeCollection attributes = TypeDescriptor.GetAttributes(type);
					List<ReflectPropertyDescriptor> list = new List<ReflectPropertyDescriptor>(attributes.Count);
					foreach (Attribute item in attributes)
					{
						if (!(item is ProvidePropertyAttribute providePropertyAttribute))
						{
							continue;
						}
						Type typeFromName = GetTypeFromName(providePropertyAttribute.ReceiverTypeName);
						if (!(typeFromName != null))
						{
							continue;
						}
						MethodInfo method = type.GetMethod("Get" + providePropertyAttribute.PropertyName, new Type[1] { typeFromName });
						if (method != null && !method.IsStatic && method.IsPublic)
						{
							MethodInfo methodInfo = type.GetMethod("Set" + providePropertyAttribute.PropertyName, new Type[2] { typeFromName, method.ReturnType });
							if (methodInfo != null && (methodInfo.IsStatic || !methodInfo.IsPublic))
							{
								methodInfo = null;
							}
							list.Add(new ReflectPropertyDescriptor(type, providePropertyAttribute.PropertyName, method.ReturnType, typeFromName, method, methodInfo, null));
						}
					}
					array = new ReflectPropertyDescriptor[list.Count];
					list.CopyTo(array, 0);
					extendedPropertyCache[type] = array;
				}
			}
		}
		PropertyDescriptor[] array2 = new PropertyDescriptor[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			ReflectPropertyDescriptor reflectPropertyDescriptor = array[i];
			array2[i] = new ExtendedPropertyDescriptor(reflectPropertyDescriptor, reflectPropertyDescriptor.ExtenderGetReceiverType(), provider, null);
		}
		if (cache != null)
		{
			cache[s_extenderProviderPropertiesKey] = array2;
		}
		return array2;
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
	private static PropertyDescriptor[] ReflectGetProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
	{
		Hashtable propertyCache = PropertyCache;
		PropertyDescriptor[] array = (PropertyDescriptor[])propertyCache[type];
		if (array != null)
		{
			return array;
		}
		lock (s_internalSyncObject)
		{
			array = (PropertyDescriptor[])propertyCache[type];
			if (array == null)
			{
				BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public;
				PropertyInfo[] properties = type.GetProperties(bindingAttr);
				array = new PropertyDescriptor[properties.Length];
				int num = 0;
				foreach (PropertyInfo propertyInfo in properties)
				{
					if (propertyInfo.GetIndexParameters().Length == 0)
					{
						MethodInfo getMethod = propertyInfo.GetGetMethod(nonPublic: false);
						MethodInfo setMethod = propertyInfo.GetSetMethod(nonPublic: false);
						string name = propertyInfo.Name;
						if (getMethod != null)
						{
							array[num++] = new ReflectPropertyDescriptor(type, name, propertyInfo.PropertyType, propertyInfo, getMethod, setMethod, null);
						}
					}
				}
				if (num != array.Length)
				{
					PropertyDescriptor[] array2 = new PropertyDescriptor[num];
					Array.Copy(array, array2, num);
					array = array2;
				}
				propertyCache[type] = array;
			}
		}
		return array;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern", Justification = "ReflectedTypeData is not being created here, just checking if was already created.")]
	internal void Refresh(Type type)
	{
		GetTypeData(type, createIfNeeded: false)?.Refresh();
	}

	[RequiresUnreferencedCode("Editors registered in TypeDescriptor.AddEditorTable may be trimmed.")]
	private static object GetIntrinsicTypeEditor(Hashtable table, Type callingType)
	{
		object obj = null;
		lock (table)
		{
			Type type = callingType;
			while (type != null && type != typeof(object))
			{
				obj = table[type];
				if (obj is string typeName)
				{
					obj = Type.GetType(typeName);
					if (obj != null)
					{
						table[type] = obj;
					}
				}
				if (obj != null)
				{
					break;
				}
				type = type.BaseType;
			}
			if (obj == null)
			{
				IDictionaryEnumerator enumerator = table.GetEnumerator();
				while (enumerator.MoveNext())
				{
					DictionaryEntry entry = enumerator.Entry;
					Type type2 = entry.Key as Type;
					if (!(type2 != null) || !type2.IsInterface || !type2.IsAssignableFrom(callingType))
					{
						continue;
					}
					obj = entry.Value;
					if (obj is string typeName2)
					{
						obj = Type.GetType(typeName2);
						if (obj != null)
						{
							table[callingType] = obj;
						}
					}
					if (obj != null)
					{
						break;
					}
				}
			}
			if (obj == null)
			{
				if (callingType.IsGenericType && callingType.GetGenericTypeDefinition() == typeof(Nullable<>))
				{
					obj = table[s_intrinsicNullableKey];
				}
				else if (callingType.IsInterface)
				{
					obj = table[s_intrinsicReferenceKey];
				}
			}
			if (obj == null)
			{
				obj = table[typeof(object)];
			}
			Type type3 = obj as Type;
			if (type3 != null)
			{
				obj = CreateInstance(type3, callingType);
				if (type3.GetConstructor(s_typeConstructor) == null)
				{
					table[callingType] = obj;
				}
			}
		}
		return obj;
	}

	[RequiresUnreferencedCode("NullableConverter's UnderlyingType cannot be statically discovered.")]
	private static TypeConverter GetIntrinsicTypeConverter(Type callingType)
	{
		lock (IntrinsicTypeConverters)
		{
			if (!IntrinsicTypeConverters.TryGetValue(callingType, out var value))
			{
				if (callingType.IsEnum)
				{
					value = IntrinsicTypeConverters[typeof(Enum)];
				}
				else if (callingType.IsArray)
				{
					value = IntrinsicTypeConverters[typeof(Array)];
				}
				else if (callingType.IsGenericType && callingType.GetGenericTypeDefinition() == typeof(Nullable<>))
				{
					value = IntrinsicTypeConverters[s_intrinsicNullableKey];
				}
				else if (typeof(ICollection).IsAssignableFrom(callingType))
				{
					value = IntrinsicTypeConverters[typeof(ICollection)];
				}
				else if (callingType.IsInterface)
				{
					value = IntrinsicTypeConverters[s_intrinsicReferenceKey];
				}
				else
				{
					Type type = null;
					Type baseType = callingType.BaseType;
					while (baseType != null && baseType != typeof(object))
					{
						if (baseType == typeof(Uri) || baseType == typeof(CultureInfo))
						{
							type = baseType;
							break;
						}
						baseType = baseType.BaseType;
					}
					if ((object)type == null)
					{
						type = typeof(object);
					}
					value = IntrinsicTypeConverters[type];
				}
			}
			return value.GetOrCreateConverterInstance(callingType);
		}
	}
}
