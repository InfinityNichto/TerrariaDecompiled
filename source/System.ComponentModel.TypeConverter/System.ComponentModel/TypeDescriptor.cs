using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Threading;

namespace System.ComponentModel;

public sealed class TypeDescriptor
{
	private sealed class ComNativeDescriptionProvider : TypeDescriptionProvider
	{
		private sealed class ComNativeTypeDescriptor : ICustomTypeDescriptor
		{
			private readonly IComNativeDescriptorHandler _handler;

			private readonly object _instance;

			internal ComNativeTypeDescriptor(IComNativeDescriptorHandler handler, object instance)
			{
				_handler = handler;
				_instance = instance;
			}

			AttributeCollection ICustomTypeDescriptor.GetAttributes()
			{
				return _handler.GetAttributes(_instance);
			}

			string ICustomTypeDescriptor.GetClassName()
			{
				return _handler.GetClassName(_instance);
			}

			string ICustomTypeDescriptor.GetComponentName()
			{
				return null;
			}

			[RequiresUnreferencedCode("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.")]
			TypeConverter ICustomTypeDescriptor.GetConverter()
			{
				return _handler.GetConverter(_instance);
			}

			[RequiresUnreferencedCode("The built-in EventDescriptor implementation uses Reflection which requires unreferenced code.")]
			EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
			{
				return _handler.GetDefaultEvent(_instance);
			}

			[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
			PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
			{
				return _handler.GetDefaultProperty(_instance);
			}

			[RequiresUnreferencedCode("Editors registered in TypeDescriptor.AddEditorTable may be trimmed.")]
			object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
			{
				return _handler.GetEditor(_instance, editorBaseType);
			}

			EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
			{
				return _handler.GetEvents(_instance);
			}

			[RequiresUnreferencedCode("The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
			EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
			{
				return _handler.GetEvents(_instance, attributes);
			}

			[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
			PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
			{
				return _handler.GetProperties(_instance, null);
			}

			[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
			PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
			{
				return _handler.GetProperties(_instance, attributes);
			}

			object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
			{
				return _instance;
			}
		}

		internal IComNativeDescriptorHandler Handler { get; set; }

		internal ComNativeDescriptionProvider(IComNativeDescriptorHandler handler)
		{
			Handler = handler;
		}

		[return: NotNullIfNotNull("instance")]
		public override ICustomTypeDescriptor GetTypeDescriptor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType, object instance)
		{
			if (objectType == null)
			{
				throw new ArgumentNullException("objectType");
			}
			if (instance == null)
			{
				return null;
			}
			if (!objectType.IsInstanceOfType(instance))
			{
				throw new ArgumentException(System.SR.Format(System.SR.ConvertToException, "objectType", instance.GetType()), "instance");
			}
			return new ComNativeTypeDescriptor(Handler, instance);
		}
	}

	private sealed class AttributeProvider : TypeDescriptionProvider
	{
		private sealed class AttributeTypeDescriptor : CustomTypeDescriptor
		{
			private readonly Attribute[] _attributeArray;

			internal AttributeTypeDescriptor(Attribute[] attrs, ICustomTypeDescriptor parent)
				: base(parent)
			{
				_attributeArray = attrs;
			}

			public override AttributeCollection GetAttributes()
			{
				Attribute[] array = null;
				AttributeCollection attributes = base.GetAttributes();
				Attribute[] attributeArray = _attributeArray;
				Attribute[] array2 = new Attribute[attributes.Count + attributeArray.Length];
				int count = attributes.Count;
				attributes.CopyTo(array2, 0);
				for (int i = 0; i < attributeArray.Length; i++)
				{
					bool flag = false;
					for (int j = 0; j < attributes.Count; j++)
					{
						if (array2[j].TypeId.Equals(attributeArray[i].TypeId))
						{
							flag = true;
							array2[j] = attributeArray[i];
							break;
						}
					}
					if (!flag)
					{
						array2[count++] = attributeArray[i];
					}
				}
				if (count < array2.Length)
				{
					array = new Attribute[count];
					Array.Copy(array2, array, count);
				}
				else
				{
					array = array2;
				}
				return new AttributeCollection(array);
			}
		}

		private readonly Attribute[] _attrs;

		internal AttributeProvider(TypeDescriptionProvider existingProvider, params Attribute[] attrs)
			: base(existingProvider)
		{
			_attrs = attrs;
		}

		public override ICustomTypeDescriptor GetTypeDescriptor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType, object instance)
		{
			return new AttributeTypeDescriptor(_attrs, base.GetTypeDescriptor(objectType, instance));
		}
	}

	private sealed class AttributeFilterCacheItem
	{
		private readonly Attribute[] _filter;

		internal readonly ICollection FilteredMembers;

		internal AttributeFilterCacheItem(Attribute[] filter, ICollection filteredMembers)
		{
			_filter = filter;
			FilteredMembers = filteredMembers;
		}

		internal bool IsValid(Attribute[] filter)
		{
			if (_filter.Length != filter.Length)
			{
				return false;
			}
			for (int i = 0; i < filter.Length; i++)
			{
				if (_filter[i] != filter[i])
				{
					return false;
				}
			}
			return true;
		}
	}

	private sealed class FilterCacheItem
	{
		private readonly ITypeDescriptorFilterService _filterService;

		internal readonly ICollection FilteredMembers;

		internal FilterCacheItem(ITypeDescriptorFilterService filterService, ICollection filteredMembers)
		{
			_filterService = filterService;
			FilteredMembers = filteredMembers;
		}

		internal bool IsValid(ITypeDescriptorFilterService filterService)
		{
			if (_filterService != filterService)
			{
				return false;
			}
			return true;
		}
	}

	private sealed class MemberDescriptorComparer : IComparer
	{
		public static readonly MemberDescriptorComparer Instance = new MemberDescriptorComparer();

		public int Compare(object left, object right)
		{
			return CultureInfo.InvariantCulture.CompareInfo.Compare((left as MemberDescriptor)?.Name, (right as MemberDescriptor)?.Name);
		}
	}

	[TypeDescriptionProvider(typeof(ComNativeDescriptorProxy))]
	private sealed class TypeDescriptorComObject
	{
	}

	private sealed class ComNativeDescriptorProxy : TypeDescriptionProvider
	{
		private readonly TypeDescriptionProvider _comNativeDescriptor;

		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072:UnrecognizedReflectionPattern", Justification = "The trimmer can't find the ComNativeDescriptor type when System.Windows.Forms isn't available. When System.Windows.Forms is available, the type will be seen by the trimmer and the ctor will be preserved.")]
		public ComNativeDescriptorProxy()
		{
			Type type = Type.GetType("System.Windows.Forms.ComponentModel.Com2Interop.ComNativeDescriptor, System.Windows.Forms", throwOnError: true);
			_comNativeDescriptor = (TypeDescriptionProvider)Activator.CreateInstance(type);
		}

		[return: NotNullIfNotNull("instance")]
		public override ICustomTypeDescriptor GetTypeDescriptor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType, object instance)
		{
			return _comNativeDescriptor.GetTypeDescriptor(objectType, instance);
		}
	}

	private sealed class MergedTypeDescriptor : ICustomTypeDescriptor
	{
		private readonly ICustomTypeDescriptor _primary;

		private readonly ICustomTypeDescriptor _secondary;

		internal MergedTypeDescriptor(ICustomTypeDescriptor primary, ICustomTypeDescriptor secondary)
		{
			_primary = primary;
			_secondary = secondary;
		}

		AttributeCollection ICustomTypeDescriptor.GetAttributes()
		{
			return _primary.GetAttributes() ?? _secondary.GetAttributes();
		}

		string ICustomTypeDescriptor.GetClassName()
		{
			return _primary.GetClassName() ?? _secondary.GetClassName();
		}

		string ICustomTypeDescriptor.GetComponentName()
		{
			return _primary.GetComponentName() ?? _secondary.GetComponentName();
		}

		[RequiresUnreferencedCode("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.")]
		TypeConverter ICustomTypeDescriptor.GetConverter()
		{
			return _primary.GetConverter() ?? _secondary.GetConverter();
		}

		[RequiresUnreferencedCode("The built-in EventDescriptor implementation uses Reflection which requires unreferenced code.")]
		EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
		{
			return _primary.GetDefaultEvent() ?? _secondary.GetDefaultEvent();
		}

		[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
		PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
		{
			return _primary.GetDefaultProperty() ?? _secondary.GetDefaultProperty();
		}

		[RequiresUnreferencedCode("Editors registered in TypeDescriptor.AddEditorTable may be trimmed.")]
		object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
		{
			if (editorBaseType == null)
			{
				throw new ArgumentNullException("editorBaseType");
			}
			return _primary.GetEditor(editorBaseType) ?? _secondary.GetEditor(editorBaseType);
		}

		EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
		{
			return _primary.GetEvents() ?? _secondary.GetEvents();
		}

		[RequiresUnreferencedCode("The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
		EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
		{
			return _primary.GetEvents(attributes) ?? _secondary.GetEvents(attributes);
		}

		[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
		PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
		{
			return _primary.GetProperties() ?? _secondary.GetProperties();
		}

		[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
		PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
		{
			PropertyDescriptorCollection properties = _primary.GetProperties(attributes);
			if (properties == null)
			{
				properties = _secondary.GetProperties(attributes);
			}
			return properties;
		}

		object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
		{
			return _primary.GetPropertyOwner(pd) ?? _secondary.GetPropertyOwner(pd);
		}
	}

	private sealed class TypeDescriptionNode : TypeDescriptionProvider
	{
		private readonly struct DefaultExtendedTypeDescriptor : ICustomTypeDescriptor
		{
			private readonly TypeDescriptionNode _node;

			private readonly object _instance;

			[RequiresUnreferencedCode("The Type of instance cannot be statically discovered.")]
			internal DefaultExtendedTypeDescriptor(TypeDescriptionNode node, object instance)
			{
				_node = node;
				_instance = instance;
			}

			[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor of this Type has RequiresUnreferencedCode.")]
			AttributeCollection ICustomTypeDescriptor.GetAttributes()
			{
				TypeDescriptionProvider provider = _node.Provider;
				if (provider is ReflectTypeDescriptionProvider reflectTypeDescriptionProvider)
				{
					return reflectTypeDescriptionProvider.GetExtendedAttributes(_instance);
				}
				ICustomTypeDescriptor extendedTypeDescriptor = provider.GetExtendedTypeDescriptor(_instance);
				if (extendedTypeDescriptor == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetExtendedTypeDescriptor"));
				}
				AttributeCollection attributes = extendedTypeDescriptor.GetAttributes();
				if (attributes == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetAttributes"));
				}
				return attributes;
			}

			[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor of this Type has RequiresUnreferencedCode.")]
			string ICustomTypeDescriptor.GetClassName()
			{
				TypeDescriptionProvider provider = _node.Provider;
				if (provider is ReflectTypeDescriptionProvider reflectTypeDescriptionProvider)
				{
					return reflectTypeDescriptionProvider.GetExtendedClassName(_instance);
				}
				ICustomTypeDescriptor extendedTypeDescriptor = provider.GetExtendedTypeDescriptor(_instance);
				if (extendedTypeDescriptor == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetExtendedTypeDescriptor"));
				}
				return extendedTypeDescriptor.GetClassName() ?? _instance.GetType().FullName;
			}

			[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor of this Type has RequiresUnreferencedCode.")]
			string ICustomTypeDescriptor.GetComponentName()
			{
				TypeDescriptionProvider provider = _node.Provider;
				if (provider is ReflectTypeDescriptionProvider reflectTypeDescriptionProvider)
				{
					return reflectTypeDescriptionProvider.GetExtendedComponentName(_instance);
				}
				ICustomTypeDescriptor extendedTypeDescriptor = provider.GetExtendedTypeDescriptor(_instance);
				if (extendedTypeDescriptor == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetExtendedTypeDescriptor"));
				}
				return extendedTypeDescriptor.GetComponentName();
			}

			[RequiresUnreferencedCode("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.")]
			TypeConverter ICustomTypeDescriptor.GetConverter()
			{
				TypeDescriptionProvider provider = _node.Provider;
				if (provider is ReflectTypeDescriptionProvider reflectTypeDescriptionProvider)
				{
					return reflectTypeDescriptionProvider.GetExtendedConverter(_instance);
				}
				ICustomTypeDescriptor extendedTypeDescriptor = provider.GetExtendedTypeDescriptor(_instance);
				if (extendedTypeDescriptor == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetExtendedTypeDescriptor"));
				}
				TypeConverter converter = extendedTypeDescriptor.GetConverter();
				if (converter == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetConverter"));
				}
				return converter;
			}

			[RequiresUnreferencedCode("The built-in EventDescriptor implementation uses Reflection which requires unreferenced code.")]
			EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
			{
				TypeDescriptionProvider provider = _node.Provider;
				if (provider is ReflectTypeDescriptionProvider reflectTypeDescriptionProvider)
				{
					return reflectTypeDescriptionProvider.GetExtendedDefaultEvent(_instance);
				}
				ICustomTypeDescriptor extendedTypeDescriptor = provider.GetExtendedTypeDescriptor(_instance);
				if (extendedTypeDescriptor == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetExtendedTypeDescriptor"));
				}
				return extendedTypeDescriptor.GetDefaultEvent();
			}

			[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
			PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
			{
				TypeDescriptionProvider provider = _node.Provider;
				if (provider is ReflectTypeDescriptionProvider reflectTypeDescriptionProvider)
				{
					return reflectTypeDescriptionProvider.GetExtendedDefaultProperty(_instance);
				}
				ICustomTypeDescriptor extendedTypeDescriptor = provider.GetExtendedTypeDescriptor(_instance);
				if (extendedTypeDescriptor == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetExtendedTypeDescriptor"));
				}
				return extendedTypeDescriptor.GetDefaultProperty();
			}

			[RequiresUnreferencedCode("Editors registered in TypeDescriptor.AddEditorTable may be trimmed.")]
			object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
			{
				if (editorBaseType == null)
				{
					throw new ArgumentNullException("editorBaseType");
				}
				TypeDescriptionProvider provider = _node.Provider;
				if (provider is ReflectTypeDescriptionProvider reflectTypeDescriptionProvider)
				{
					return reflectTypeDescriptionProvider.GetExtendedEditor(_instance, editorBaseType);
				}
				ICustomTypeDescriptor extendedTypeDescriptor = provider.GetExtendedTypeDescriptor(_instance);
				if (extendedTypeDescriptor == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetExtendedTypeDescriptor"));
				}
				return extendedTypeDescriptor.GetEditor(editorBaseType);
			}

			[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor of this Type has RequiresUnreferencedCode.")]
			EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
			{
				TypeDescriptionProvider provider = _node.Provider;
				if (provider is ReflectTypeDescriptionProvider reflectTypeDescriptionProvider)
				{
					return reflectTypeDescriptionProvider.GetExtendedEvents(_instance);
				}
				ICustomTypeDescriptor extendedTypeDescriptor = provider.GetExtendedTypeDescriptor(_instance);
				if (extendedTypeDescriptor == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetExtendedTypeDescriptor"));
				}
				EventDescriptorCollection events = extendedTypeDescriptor.GetEvents();
				if (events == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetEvents"));
				}
				return events;
			}

			[RequiresUnreferencedCode("The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
			EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
			{
				TypeDescriptionProvider provider = _node.Provider;
				if (provider is ReflectTypeDescriptionProvider reflectTypeDescriptionProvider)
				{
					return reflectTypeDescriptionProvider.GetExtendedEvents(_instance);
				}
				ICustomTypeDescriptor extendedTypeDescriptor = provider.GetExtendedTypeDescriptor(_instance);
				if (extendedTypeDescriptor == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetExtendedTypeDescriptor"));
				}
				EventDescriptorCollection events = extendedTypeDescriptor.GetEvents(attributes);
				if (events == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetEvents"));
				}
				return events;
			}

			[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
			PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
			{
				TypeDescriptionProvider provider = _node.Provider;
				if (provider is ReflectTypeDescriptionProvider reflectTypeDescriptionProvider)
				{
					return reflectTypeDescriptionProvider.GetExtendedProperties(_instance);
				}
				ICustomTypeDescriptor extendedTypeDescriptor = provider.GetExtendedTypeDescriptor(_instance);
				if (extendedTypeDescriptor == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetExtendedTypeDescriptor"));
				}
				PropertyDescriptorCollection properties = extendedTypeDescriptor.GetProperties();
				if (properties == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetProperties"));
				}
				return properties;
			}

			[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
			PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
			{
				TypeDescriptionProvider provider = _node.Provider;
				if (provider is ReflectTypeDescriptionProvider reflectTypeDescriptionProvider)
				{
					return reflectTypeDescriptionProvider.GetExtendedProperties(_instance);
				}
				ICustomTypeDescriptor extendedTypeDescriptor = provider.GetExtendedTypeDescriptor(_instance);
				if (extendedTypeDescriptor == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetExtendedTypeDescriptor"));
				}
				PropertyDescriptorCollection properties = extendedTypeDescriptor.GetProperties(attributes);
				if (properties == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetProperties"));
				}
				return properties;
			}

			[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor of this Type has RequiresUnreferencedCode.")]
			object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
			{
				TypeDescriptionProvider provider = _node.Provider;
				if (provider is ReflectTypeDescriptionProvider reflectTypeDescriptionProvider)
				{
					return reflectTypeDescriptionProvider.GetExtendedPropertyOwner(_instance, pd);
				}
				ICustomTypeDescriptor extendedTypeDescriptor = provider.GetExtendedTypeDescriptor(_instance);
				if (extendedTypeDescriptor == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetExtendedTypeDescriptor"));
				}
				return extendedTypeDescriptor.GetPropertyOwner(pd) ?? _instance;
			}
		}

		private readonly struct DefaultTypeDescriptor : ICustomTypeDescriptor
		{
			private readonly TypeDescriptionNode _node;

			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
			private readonly Type _objectType;

			private readonly object _instance;

			internal DefaultTypeDescriptor(TypeDescriptionNode node, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType, object instance)
			{
				_node = node;
				_objectType = objectType;
				_instance = instance;
			}

			AttributeCollection ICustomTypeDescriptor.GetAttributes()
			{
				TypeDescriptionProvider provider = _node.Provider;
				AttributeCollection attributes;
				if (provider is ReflectTypeDescriptionProvider reflectTypeDescriptionProvider)
				{
					attributes = reflectTypeDescriptionProvider.GetAttributes(_objectType);
				}
				else
				{
					ICustomTypeDescriptor typeDescriptor = provider.GetTypeDescriptor(_objectType, _instance);
					if (typeDescriptor == null)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetTypeDescriptor"));
					}
					attributes = typeDescriptor.GetAttributes();
					if (attributes == null)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetAttributes"));
					}
				}
				return attributes;
			}

			string ICustomTypeDescriptor.GetClassName()
			{
				TypeDescriptionProvider provider = _node.Provider;
				if (provider is ReflectTypeDescriptionProvider reflectTypeDescriptionProvider)
				{
					return reflectTypeDescriptionProvider.GetClassName(_objectType);
				}
				ICustomTypeDescriptor typeDescriptor = provider.GetTypeDescriptor(_objectType, _instance);
				if (typeDescriptor == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetTypeDescriptor"));
				}
				return typeDescriptor.GetClassName() ?? _objectType.FullName;
			}

			string ICustomTypeDescriptor.GetComponentName()
			{
				TypeDescriptionProvider provider = _node.Provider;
				if (provider is ReflectTypeDescriptionProvider reflectTypeDescriptionProvider)
				{
					return reflectTypeDescriptionProvider.GetComponentName(_objectType, _instance);
				}
				ICustomTypeDescriptor typeDescriptor = provider.GetTypeDescriptor(_objectType, _instance);
				if (typeDescriptor == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetTypeDescriptor"));
				}
				return typeDescriptor.GetComponentName();
			}

			[RequiresUnreferencedCode("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.")]
			TypeConverter ICustomTypeDescriptor.GetConverter()
			{
				TypeDescriptionProvider provider = _node.Provider;
				TypeConverter converter;
				if (provider is ReflectTypeDescriptionProvider reflectTypeDescriptionProvider)
				{
					converter = reflectTypeDescriptionProvider.GetConverter(_objectType, _instance);
				}
				else
				{
					ICustomTypeDescriptor typeDescriptor = provider.GetTypeDescriptor(_objectType, _instance);
					if (typeDescriptor == null)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetTypeDescriptor"));
					}
					converter = typeDescriptor.GetConverter();
					if (converter == null)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetConverter"));
					}
				}
				return converter;
			}

			[RequiresUnreferencedCode("The built-in EventDescriptor implementation uses Reflection which requires unreferenced code.")]
			EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
			{
				TypeDescriptionProvider provider = _node.Provider;
				if (provider is ReflectTypeDescriptionProvider reflectTypeDescriptionProvider)
				{
					return reflectTypeDescriptionProvider.GetDefaultEvent(_objectType, _instance);
				}
				ICustomTypeDescriptor typeDescriptor = provider.GetTypeDescriptor(_objectType, _instance);
				if (typeDescriptor == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetTypeDescriptor"));
				}
				return typeDescriptor.GetDefaultEvent();
			}

			[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
			PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
			{
				TypeDescriptionProvider provider = _node.Provider;
				if (provider is ReflectTypeDescriptionProvider reflectTypeDescriptionProvider)
				{
					return reflectTypeDescriptionProvider.GetDefaultProperty(_objectType, _instance);
				}
				ICustomTypeDescriptor typeDescriptor = provider.GetTypeDescriptor(_objectType, _instance);
				if (typeDescriptor == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetTypeDescriptor"));
				}
				return typeDescriptor.GetDefaultProperty();
			}

			[RequiresUnreferencedCode("Editors registered in TypeDescriptor.AddEditorTable may be trimmed.")]
			object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
			{
				if (editorBaseType == null)
				{
					throw new ArgumentNullException("editorBaseType");
				}
				TypeDescriptionProvider provider = _node.Provider;
				if (provider is ReflectTypeDescriptionProvider reflectTypeDescriptionProvider)
				{
					return reflectTypeDescriptionProvider.GetEditor(_objectType, _instance, editorBaseType);
				}
				ICustomTypeDescriptor typeDescriptor = provider.GetTypeDescriptor(_objectType, _instance);
				if (typeDescriptor == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetTypeDescriptor"));
				}
				return typeDescriptor.GetEditor(editorBaseType);
			}

			EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
			{
				TypeDescriptionProvider provider = _node.Provider;
				EventDescriptorCollection events;
				if (provider is ReflectTypeDescriptionProvider reflectTypeDescriptionProvider)
				{
					events = reflectTypeDescriptionProvider.GetEvents(_objectType);
				}
				else
				{
					ICustomTypeDescriptor typeDescriptor = provider.GetTypeDescriptor(_objectType, _instance);
					if (typeDescriptor == null)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetTypeDescriptor"));
					}
					events = typeDescriptor.GetEvents();
					if (events == null)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetEvents"));
					}
				}
				return events;
			}

			[RequiresUnreferencedCode("The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
			EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
			{
				TypeDescriptionProvider provider = _node.Provider;
				EventDescriptorCollection events;
				if (provider is ReflectTypeDescriptionProvider reflectTypeDescriptionProvider)
				{
					events = reflectTypeDescriptionProvider.GetEvents(_objectType);
				}
				else
				{
					ICustomTypeDescriptor typeDescriptor = provider.GetTypeDescriptor(_objectType, _instance);
					if (typeDescriptor == null)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetTypeDescriptor"));
					}
					events = typeDescriptor.GetEvents(attributes);
					if (events == null)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetEvents"));
					}
				}
				return events;
			}

			[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
			PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
			{
				TypeDescriptionProvider provider = _node.Provider;
				PropertyDescriptorCollection properties;
				if (provider is ReflectTypeDescriptionProvider reflectTypeDescriptionProvider)
				{
					properties = reflectTypeDescriptionProvider.GetProperties(_objectType);
				}
				else
				{
					ICustomTypeDescriptor typeDescriptor = provider.GetTypeDescriptor(_objectType, _instance);
					if (typeDescriptor == null)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetTypeDescriptor"));
					}
					properties = typeDescriptor.GetProperties();
					if (properties == null)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetProperties"));
					}
				}
				return properties;
			}

			[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
			PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
			{
				TypeDescriptionProvider provider = _node.Provider;
				PropertyDescriptorCollection properties;
				if (provider is ReflectTypeDescriptionProvider reflectTypeDescriptionProvider)
				{
					properties = reflectTypeDescriptionProvider.GetProperties(_objectType);
				}
				else
				{
					ICustomTypeDescriptor typeDescriptor = provider.GetTypeDescriptor(_objectType, _instance);
					if (typeDescriptor == null)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetTypeDescriptor"));
					}
					properties = typeDescriptor.GetProperties(attributes);
					if (properties == null)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetProperties"));
					}
				}
				return properties;
			}

			object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
			{
				TypeDescriptionProvider provider = _node.Provider;
				if (provider is ReflectTypeDescriptionProvider reflectTypeDescriptionProvider)
				{
					return reflectTypeDescriptionProvider.GetPropertyOwner(_objectType, _instance, pd);
				}
				ICustomTypeDescriptor typeDescriptor = provider.GetTypeDescriptor(_objectType, _instance);
				if (typeDescriptor == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.TypeDescriptorProviderError, _node.Provider.GetType().FullName, "GetTypeDescriptor"));
				}
				return typeDescriptor.GetPropertyOwner(pd) ?? _instance;
			}
		}

		internal TypeDescriptionNode Next;

		internal TypeDescriptionProvider Provider;

		internal TypeDescriptionNode(TypeDescriptionProvider provider)
		{
			Provider = provider;
		}

		public override object CreateInstance(IServiceProvider provider, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type objectType, Type[] argTypes, object[] args)
		{
			if (objectType == null)
			{
				throw new ArgumentNullException("objectType");
			}
			if (argTypes != null)
			{
				if (args == null)
				{
					throw new ArgumentNullException("args");
				}
				if (argTypes.Length != args.Length)
				{
					throw new ArgumentException(System.SR.TypeDescriptorArgsCountMismatch);
				}
			}
			return Provider.CreateInstance(provider, objectType, argTypes, args);
		}

		public override IDictionary GetCache(object instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException("instance");
			}
			return Provider.GetCache(instance);
		}

		[RequiresUnreferencedCode("The Type of instance cannot be statically discovered.")]
		public override ICustomTypeDescriptor GetExtendedTypeDescriptor(object instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException("instance");
			}
			return new DefaultExtendedTypeDescriptor(this, instance);
		}

		protected internal override IExtenderProvider[] GetExtenderProviders(object instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException("instance");
			}
			return Provider.GetExtenderProviders(instance);
		}

		[RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
		public override string GetFullComponentName(object component)
		{
			if (component == null)
			{
				throw new ArgumentNullException("component");
			}
			return Provider.GetFullComponentName(component);
		}

		[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicFields)]
		public override Type GetReflectionType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicFields)] Type objectType, object instance)
		{
			if (objectType == null)
			{
				throw new ArgumentNullException("objectType");
			}
			return Provider.GetReflectionType(objectType, instance);
		}

		public override Type GetRuntimeType(Type objectType)
		{
			if (objectType == null)
			{
				throw new ArgumentNullException("objectType");
			}
			return Provider.GetRuntimeType(objectType);
		}

		public override ICustomTypeDescriptor GetTypeDescriptor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType, object instance)
		{
			if (objectType == null)
			{
				throw new ArgumentNullException("objectType");
			}
			if (instance != null && !objectType.IsInstanceOfType(instance))
			{
				throw new ArgumentException("instance");
			}
			return new DefaultTypeDescriptor(this, objectType, instance);
		}

		public override bool IsSupportedType(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			return Provider.IsSupportedType(type);
		}
	}

	private sealed class TypeDescriptorInterface
	{
	}

	private static readonly WeakHashtable s_providerTable = new WeakHashtable();

	private static readonly Hashtable s_providerTypeTable = new Hashtable();

	private static readonly Hashtable s_defaultProviders = new Hashtable();

	private static WeakHashtable s_associationTable;

	private static int s_metadataVersion;

	private static int s_collisionIndex;

	private static readonly Guid[] s_pipelineInitializeKeys = new Guid[3]
	{
		Guid.NewGuid(),
		Guid.NewGuid(),
		Guid.NewGuid()
	};

	private static readonly Guid[] s_pipelineMergeKeys = new Guid[3]
	{
		Guid.NewGuid(),
		Guid.NewGuid(),
		Guid.NewGuid()
	};

	private static readonly Guid[] s_pipelineFilterKeys = new Guid[3]
	{
		Guid.NewGuid(),
		Guid.NewGuid(),
		Guid.NewGuid()
	};

	private static readonly Guid[] s_pipelineAttributeFilterKeys = new Guid[3]
	{
		Guid.NewGuid(),
		Guid.NewGuid(),
		Guid.NewGuid()
	};

	private static readonly object s_internalSyncObject = new object();

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static Type InterfaceType
	{
		[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		get
		{
			return typeof(TypeDescriptorInterface);
		}
	}

	internal static int MetadataVersion => s_metadataVersion;

	private static WeakHashtable AssociationTable => LazyInitializer.EnsureInitialized(ref s_associationTable, () => new WeakHashtable());

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static Type ComObjectType
	{
		[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		get
		{
			return typeof(TypeDescriptorComObject);
		}
	}

	[Obsolete("TypeDescriptor.ComNativeDescriptorHandler has been deprecated. Use a type description provider to supply type information for COM types instead.")]
	public static IComNativeDescriptorHandler? ComNativeDescriptorHandler
	{
		get
		{
			TypeDescriptionNode typeDescriptionNode = NodeFor(ComObjectType);
			ComNativeDescriptionProvider comNativeDescriptionProvider;
			do
			{
				comNativeDescriptionProvider = typeDescriptionNode.Provider as ComNativeDescriptionProvider;
				typeDescriptionNode = typeDescriptionNode.Next;
			}
			while (typeDescriptionNode != null && comNativeDescriptionProvider == null);
			return comNativeDescriptionProvider?.Handler;
		}
		[param: DisallowNull]
		set
		{
			TypeDescriptionNode typeDescriptionNode = NodeFor(ComObjectType);
			while (typeDescriptionNode != null && !(typeDescriptionNode.Provider is ComNativeDescriptionProvider))
			{
				typeDescriptionNode = typeDescriptionNode.Next;
			}
			if (typeDescriptionNode == null)
			{
				AddProvider(new ComNativeDescriptionProvider(value), ComObjectType);
			}
			else
			{
				((ComNativeDescriptionProvider)typeDescriptionNode.Provider).Handler = value;
			}
		}
	}

	public static event RefreshEventHandler? Refreshed;

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static TypeDescriptionProvider AddAttributes(Type type, params Attribute[] attributes)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (attributes == null)
		{
			throw new ArgumentNullException("attributes");
		}
		TypeDescriptionProvider provider = GetProvider(type);
		TypeDescriptionProvider typeDescriptionProvider = new AttributeProvider(provider, attributes);
		AddProvider(typeDescriptionProvider, type);
		return typeDescriptionProvider;
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static TypeDescriptionProvider AddAttributes(object instance, params Attribute[] attributes)
	{
		if (instance == null)
		{
			throw new ArgumentNullException("instance");
		}
		if (attributes == null)
		{
			throw new ArgumentNullException("attributes");
		}
		TypeDescriptionProvider provider = GetProvider(instance);
		TypeDescriptionProvider typeDescriptionProvider = new AttributeProvider(provider, attributes);
		AddProvider(typeDescriptionProvider, instance);
		return typeDescriptionProvider;
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[RequiresUnreferencedCode("The Types specified in table may be trimmed, or have their static construtors trimmed.")]
	public static void AddEditorTable(Type editorBaseType, Hashtable table)
	{
		ReflectTypeDescriptionProvider.AddEditorTable(editorBaseType, table);
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static void AddProvider(TypeDescriptionProvider provider, Type type)
	{
		if (provider == null)
		{
			throw new ArgumentNullException("provider");
		}
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		lock (s_providerTable)
		{
			TypeDescriptionNode next = NodeFor(type, createDelegator: true);
			TypeDescriptionNode value = new TypeDescriptionNode(provider)
			{
				Next = next
			};
			s_providerTable[type] = value;
			s_providerTypeTable.Clear();
		}
		Refresh(type);
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static void AddProvider(TypeDescriptionProvider provider, object instance)
	{
		if (provider == null)
		{
			throw new ArgumentNullException("provider");
		}
		if (instance == null)
		{
			throw new ArgumentNullException("instance");
		}
		bool flag;
		lock (s_providerTable)
		{
			flag = s_providerTable.ContainsKey(instance);
			TypeDescriptionNode next = NodeFor(instance, createDelegator: true);
			TypeDescriptionNode value = new TypeDescriptionNode(provider)
			{
				Next = next
			};
			s_providerTable.SetWeak(instance, value);
			s_providerTypeTable.Clear();
		}
		if (flag)
		{
			Refresh(instance, refreshReflectionProvider: false);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static void AddProviderTransparent(TypeDescriptionProvider provider, Type type)
	{
		if (provider == null)
		{
			throw new ArgumentNullException("provider");
		}
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		AddProvider(provider, type);
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static void AddProviderTransparent(TypeDescriptionProvider provider, object instance)
	{
		if (provider == null)
		{
			throw new ArgumentNullException("provider");
		}
		if (instance == null)
		{
			throw new ArgumentNullException("instance");
		}
		AddProvider(provider, instance);
	}

	private static void CheckDefaultProvider(Type type)
	{
		if (s_defaultProviders.ContainsKey(type))
		{
			return;
		}
		lock (s_internalSyncObject)
		{
			if (s_defaultProviders.ContainsKey(type))
			{
				return;
			}
			s_defaultProviders[type] = null;
		}
		object[] customAttributes = type.GetCustomAttributes(typeof(TypeDescriptionProviderAttribute), inherit: false);
		bool flag = false;
		for (int num = customAttributes.Length - 1; num >= 0; num--)
		{
			TypeDescriptionProviderAttribute typeDescriptionProviderAttribute = (TypeDescriptionProviderAttribute)customAttributes[num];
			Type type2 = Type.GetType(typeDescriptionProviderAttribute.TypeName);
			if (type2 != null && typeof(TypeDescriptionProvider).IsAssignableFrom(type2))
			{
				TypeDescriptionProvider provider = (TypeDescriptionProvider)Activator.CreateInstance(type2);
				AddProvider(provider, type);
				flag = true;
			}
		}
		if (!flag)
		{
			Type baseType = type.BaseType;
			if (baseType != null && baseType != type)
			{
				CheckDefaultProvider(baseType);
			}
		}
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static void CreateAssociation(object primary, object secondary)
	{
		if (primary == null)
		{
			throw new ArgumentNullException("primary");
		}
		if (secondary == null)
		{
			throw new ArgumentNullException("secondary");
		}
		if (primary == secondary)
		{
			throw new ArgumentException(System.SR.TypeDescriptorSameAssociation);
		}
		WeakHashtable associationTable = AssociationTable;
		IList list = (IList)associationTable[primary];
		if (list == null)
		{
			lock (associationTable)
			{
				list = (IList)associationTable[primary];
				if (list == null)
				{
					list = new ArrayList(4);
					associationTable.SetWeak(primary, list);
				}
			}
		}
		else
		{
			for (int num = list.Count - 1; num >= 0; num--)
			{
				WeakReference weakReference = (WeakReference)list[num];
				if (weakReference.IsAlive && weakReference.Target == secondary)
				{
					throw new ArgumentException(System.SR.TypeDescriptorAlreadyAssociated);
				}
			}
		}
		lock (list)
		{
			list.Add(new WeakReference(secondary));
		}
	}

	public static EventDescriptor CreateEvent([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType, string name, Type type, params Attribute[] attributes)
	{
		return new ReflectEventDescriptor(componentType, name, type, attributes);
	}

	public static EventDescriptor CreateEvent([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType, EventDescriptor oldEventDescriptor, params Attribute[] attributes)
	{
		return new ReflectEventDescriptor(componentType, oldEventDescriptor, attributes);
	}

	public static object? CreateInstance(IServiceProvider? provider, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type objectType, Type[]? argTypes, object[]? args)
	{
		if (objectType == null)
		{
			throw new ArgumentNullException("objectType");
		}
		if (argTypes != null)
		{
			if (args == null)
			{
				throw new ArgumentNullException("args");
			}
			if (argTypes.Length != args.Length)
			{
				throw new ArgumentException(System.SR.TypeDescriptorArgsCountMismatch);
			}
		}
		object obj = null;
		if (provider?.GetService(typeof(TypeDescriptionProvider)) is TypeDescriptionProvider typeDescriptionProvider)
		{
			obj = typeDescriptionProvider.CreateInstance(provider, objectType, argTypes, args);
		}
		return obj ?? NodeFor(objectType).CreateInstance(provider, objectType, argTypes, args);
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
	public static PropertyDescriptor CreateProperty([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType, string name, Type type, params Attribute[] attributes)
	{
		return new ReflectPropertyDescriptor(componentType, name, type, attributes);
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
	public static PropertyDescriptor CreateProperty([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType, PropertyDescriptor oldPropertyDescriptor, params Attribute[] attributes)
	{
		if (componentType == oldPropertyDescriptor.ComponentType)
		{
			ExtenderProvidedPropertyAttribute extenderProvidedPropertyAttribute = (ExtenderProvidedPropertyAttribute)oldPropertyDescriptor.Attributes[typeof(ExtenderProvidedPropertyAttribute)];
			if (extenderProvidedPropertyAttribute.ExtenderProperty is ReflectPropertyDescriptor)
			{
				return new ExtendedPropertyDescriptor(oldPropertyDescriptor, attributes);
			}
		}
		return new ReflectPropertyDescriptor(componentType, oldPropertyDescriptor, attributes);
	}

	[RequiresUnreferencedCode("The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	private static ArrayList FilterMembers(IList members, Attribute[] attributes)
	{
		ArrayList arrayList = null;
		int count = members.Count;
		for (int i = 0; i < count; i++)
		{
			bool flag = false;
			for (int j = 0; j < attributes.Length; j++)
			{
				if (ShouldHideMember((MemberDescriptor)members[i], attributes[j]))
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				if (arrayList == null)
				{
					arrayList = new ArrayList(count);
					for (int k = 0; k < i; k++)
					{
						arrayList.Add(members[k]);
					}
				}
			}
			else
			{
				arrayList?.Add(members[i]);
			}
		}
		return arrayList;
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static object GetAssociation(Type type, object primary)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (primary == null)
		{
			throw new ArgumentNullException("primary");
		}
		object obj = primary;
		if (!type.IsInstanceOfType(primary))
		{
			IList list = (IList)(AssociationTable?[primary]);
			if (list != null)
			{
				lock (list)
				{
					for (int num = list.Count - 1; num >= 0; num--)
					{
						WeakReference weakReference = (WeakReference)list[num];
						object target = weakReference.Target;
						if (target == null)
						{
							list.RemoveAt(num);
						}
						else if (type.IsInstanceOfType(target))
						{
							obj = target;
						}
					}
				}
			}
			if (obj == primary && primary is IComponent component)
			{
				ISite site = component.Site;
				if (site != null && site.DesignMode && site.GetService(typeof(IDesignerHost)) is IDesignerHost designerHost)
				{
					object designer = designerHost.GetDesigner(component);
					if (designer != null && type.IsInstanceOfType(designer))
					{
						obj = designer;
					}
				}
			}
		}
		return obj;
	}

	public static AttributeCollection GetAttributes([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType)
	{
		if (componentType == null)
		{
			return new AttributeCollection((Attribute[]?)null);
		}
		return GetDescriptor(componentType, "componentType").GetAttributes();
	}

	[RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
	public static AttributeCollection GetAttributes(object component)
	{
		return GetAttributes(component, noCustomTypeDesc: false);
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
	public static AttributeCollection GetAttributes(object component, bool noCustomTypeDesc)
	{
		if (component == null)
		{
			return new AttributeCollection((Attribute[]?)null);
		}
		ICustomTypeDescriptor descriptor = GetDescriptor(component, noCustomTypeDesc);
		ICollection collection = descriptor.GetAttributes();
		if (component is ICustomTypeDescriptor)
		{
			if (noCustomTypeDesc)
			{
				ICustomTypeDescriptor extendedDescriptor = GetExtendedDescriptor(component);
				if (extendedDescriptor != null)
				{
					ICollection attributes = extendedDescriptor.GetAttributes();
					collection = PipelineMerge(0, collection, attributes, component, null);
				}
			}
			else
			{
				collection = PipelineFilter(0, collection, component, null);
			}
		}
		else
		{
			IDictionary cache = GetCache(component);
			collection = PipelineInitialize(0, collection, cache);
			ICustomTypeDescriptor extendedDescriptor2 = GetExtendedDescriptor(component);
			if (extendedDescriptor2 != null)
			{
				ICollection attributes2 = extendedDescriptor2.GetAttributes();
				collection = PipelineMerge(0, collection, attributes2, component, cache);
			}
			collection = PipelineFilter(0, collection, component, cache);
		}
		AttributeCollection attributeCollection = collection as AttributeCollection;
		if (attributeCollection == null)
		{
			Attribute[] array = new Attribute[collection.Count];
			collection.CopyTo(array, 0);
			attributeCollection = new AttributeCollection(array);
		}
		return attributeCollection;
	}

	internal static IDictionary GetCache(object instance)
	{
		return NodeFor(instance).GetCache(instance);
	}

	[RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
	public static string? GetClassName(object component)
	{
		return GetClassName(component, noCustomTypeDesc: false);
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
	public static string? GetClassName(object component, bool noCustomTypeDesc)
	{
		return GetDescriptor(component, noCustomTypeDesc).GetClassName();
	}

	public static string? GetClassName([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType)
	{
		return GetDescriptor(componentType, "componentType").GetClassName();
	}

	[RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
	public static string? GetComponentName(object component)
	{
		return GetComponentName(component, noCustomTypeDesc: false);
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
	public static string? GetComponentName(object component, bool noCustomTypeDesc)
	{
		return GetDescriptor(component, noCustomTypeDesc).GetComponentName();
	}

	[RequiresUnreferencedCode("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All. The Type of component cannot be statically discovered.")]
	public static TypeConverter GetConverter(object component)
	{
		return GetConverter(component, noCustomTypeDesc: false);
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[RequiresUnreferencedCode("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All. The Type of component cannot be statically discovered.")]
	public static TypeConverter GetConverter(object component, bool noCustomTypeDesc)
	{
		return GetDescriptor(component, noCustomTypeDesc).GetConverter();
	}

	[RequiresUnreferencedCode("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.")]
	public static TypeConverter GetConverter([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
	{
		return GetDescriptor(type, "type").GetConverter();
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The callers of this method ensure getting the converter is trim compatible - i.e. the type is not Nullable<T>.")]
	internal static TypeConverter GetConverterTrimUnsafe([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
	{
		return GetConverter(type);
	}

	[RequiresUnreferencedCode("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.")]
	private static object ConvertFromInvariantString([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, string stringValue)
	{
		return GetConverter(type).ConvertFromInvariantString(stringValue);
	}

	[RequiresUnreferencedCode("The built-in EventDescriptor implementation uses Reflection which requires unreferenced code.")]
	public static EventDescriptor? GetDefaultEvent([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType)
	{
		if (componentType == null)
		{
			return null;
		}
		return GetDescriptor(componentType, "componentType").GetDefaultEvent();
	}

	[RequiresUnreferencedCode("The built-in EventDescriptor implementation uses Reflection which requires unreferenced code. The Type of component cannot be statically discovered.")]
	public static EventDescriptor? GetDefaultEvent(object component)
	{
		return GetDefaultEvent(component, noCustomTypeDesc: false);
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[RequiresUnreferencedCode("The built-in EventDescriptor implementation uses Reflection which requires unreferenced code. The Type of component cannot be statically discovered.")]
	public static EventDescriptor? GetDefaultEvent(object component, bool noCustomTypeDesc)
	{
		if (component == null)
		{
			return null;
		}
		return GetDescriptor(component, noCustomTypeDesc).GetDefaultEvent();
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
	public static PropertyDescriptor? GetDefaultProperty([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType)
	{
		if (componentType == null)
		{
			return null;
		}
		return GetDescriptor(componentType, "componentType").GetDefaultProperty();
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The Type of component cannot be statically discovered.")]
	public static PropertyDescriptor? GetDefaultProperty(object component)
	{
		return GetDefaultProperty(component, noCustomTypeDesc: false);
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The Type of component cannot be statically discovered.")]
	public static PropertyDescriptor? GetDefaultProperty(object component, bool noCustomTypeDesc)
	{
		if (component == null)
		{
			return null;
		}
		return GetDescriptor(component, noCustomTypeDesc).GetDefaultProperty();
	}

	internal static ICustomTypeDescriptor GetDescriptor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, string typeName)
	{
		if (type == null)
		{
			throw new ArgumentNullException(typeName);
		}
		return NodeFor(type).GetTypeDescriptor(type);
	}

	[RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
	internal static ICustomTypeDescriptor GetDescriptor(object component, bool noCustomTypeDesc)
	{
		if (component == null)
		{
			throw new ArgumentException("component");
		}
		ICustomTypeDescriptor customTypeDescriptor = NodeFor(component).GetTypeDescriptor(component);
		ICustomTypeDescriptor customTypeDescriptor2 = component as ICustomTypeDescriptor;
		if (!noCustomTypeDesc && customTypeDescriptor2 != null)
		{
			customTypeDescriptor = new MergedTypeDescriptor(customTypeDescriptor2, customTypeDescriptor);
		}
		return customTypeDescriptor;
	}

	[RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
	internal static ICustomTypeDescriptor GetExtendedDescriptor(object component)
	{
		if (component == null)
		{
			throw new ArgumentException("component");
		}
		return NodeFor(component).GetExtendedTypeDescriptor(component);
	}

	[RequiresUnreferencedCode("Editors registered in TypeDescriptor.AddEditorTable may be trimmed. The Type of component cannot be statically discovered.")]
	public static object? GetEditor(object component, Type editorBaseType)
	{
		return GetEditor(component, editorBaseType, noCustomTypeDesc: false);
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[RequiresUnreferencedCode("Editors registered in TypeDescriptor.AddEditorTable may be trimmed. The Type of component cannot be statically discovered.")]
	public static object? GetEditor(object component, Type editorBaseType, bool noCustomTypeDesc)
	{
		if (editorBaseType == null)
		{
			throw new ArgumentNullException("editorBaseType");
		}
		return GetDescriptor(component, noCustomTypeDesc).GetEditor(editorBaseType);
	}

	[RequiresUnreferencedCode("Editors registered in TypeDescriptor.AddEditorTable may be trimmed.")]
	public static object? GetEditor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, Type editorBaseType)
	{
		if (editorBaseType == null)
		{
			throw new ArgumentNullException("editorBaseType");
		}
		return GetDescriptor(type, "type").GetEditor(editorBaseType);
	}

	public static EventDescriptorCollection GetEvents([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType)
	{
		if (componentType == null)
		{
			return new EventDescriptorCollection(null, readOnly: true);
		}
		return GetDescriptor(componentType, "componentType").GetEvents();
	}

	[RequiresUnreferencedCode("The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	public static EventDescriptorCollection GetEvents([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType, Attribute[] attributes)
	{
		if (componentType == null)
		{
			return new EventDescriptorCollection(null, readOnly: true);
		}
		EventDescriptorCollection eventDescriptorCollection = GetDescriptor(componentType, "componentType").GetEvents(attributes);
		if (attributes != null && attributes.Length != 0)
		{
			ArrayList arrayList = FilterMembers(eventDescriptorCollection, attributes);
			if (arrayList != null)
			{
				EventDescriptor[] array = new EventDescriptor[arrayList.Count];
				arrayList.CopyTo(array);
				eventDescriptorCollection = new EventDescriptorCollection(array, readOnly: true);
			}
		}
		return eventDescriptorCollection;
	}

	[RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
	public static EventDescriptorCollection GetEvents(object component)
	{
		return GetEvents(component, null, noCustomTypeDesc: false);
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
	public static EventDescriptorCollection GetEvents(object component, bool noCustomTypeDesc)
	{
		return GetEvents(component, null, noCustomTypeDesc);
	}

	[RequiresUnreferencedCode("The Type of component cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	public static EventDescriptorCollection GetEvents(object component, Attribute[] attributes)
	{
		return GetEvents(component, attributes, noCustomTypeDesc: false);
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[RequiresUnreferencedCode("The Type of component cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	public static EventDescriptorCollection GetEvents(object component, Attribute[]? attributes, bool noCustomTypeDesc)
	{
		if (component == null)
		{
			return new EventDescriptorCollection(null, readOnly: true);
		}
		ICustomTypeDescriptor descriptor = GetDescriptor(component, noCustomTypeDesc);
		ICollection collection;
		if (component is ICustomTypeDescriptor)
		{
			collection = descriptor.GetEvents(attributes);
			if (noCustomTypeDesc)
			{
				ICustomTypeDescriptor extendedDescriptor = GetExtendedDescriptor(component);
				if (extendedDescriptor != null)
				{
					ICollection events = extendedDescriptor.GetEvents(attributes);
					collection = PipelineMerge(2, collection, events, component, null);
				}
			}
			else
			{
				collection = PipelineFilter(2, collection, component, null);
				collection = PipelineAttributeFilter(2, collection, attributes, component, null);
			}
		}
		else
		{
			IDictionary cache = GetCache(component);
			collection = descriptor.GetEvents(attributes);
			collection = PipelineInitialize(2, collection, cache);
			ICustomTypeDescriptor extendedDescriptor2 = GetExtendedDescriptor(component);
			if (extendedDescriptor2 != null)
			{
				ICollection events2 = extendedDescriptor2.GetEvents(attributes);
				collection = PipelineMerge(2, collection, events2, component, cache);
			}
			collection = PipelineFilter(2, collection, component, cache);
			collection = PipelineAttributeFilter(2, collection, attributes, component, cache);
		}
		EventDescriptorCollection eventDescriptorCollection = collection as EventDescriptorCollection;
		if (eventDescriptorCollection == null)
		{
			EventDescriptor[] array = new EventDescriptor[collection.Count];
			collection.CopyTo(array, 0);
			eventDescriptorCollection = new EventDescriptorCollection(array, readOnly: true);
		}
		return eventDescriptorCollection;
	}

	private static string GetExtenderCollisionSuffix(MemberDescriptor member)
	{
		string result = null;
		IExtenderProvider extenderProvider = ((member.Attributes[typeof(ExtenderProvidedPropertyAttribute)] is ExtenderProvidedPropertyAttribute extenderProvidedPropertyAttribute) ? extenderProvidedPropertyAttribute.Provider : null);
		if (extenderProvider != null)
		{
			string text = null;
			if (extenderProvider is IComponent { Site: not null } component)
			{
				text = component.Site.Name;
			}
			if (text == null || text.Length == 0)
			{
				text = (Interlocked.Increment(ref s_collisionIndex) - 1).ToString(CultureInfo.InvariantCulture);
			}
			result = "_" + text;
		}
		return result;
	}

	[RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
	public static string? GetFullComponentName(object component)
	{
		if (component == null)
		{
			throw new ArgumentNullException("component");
		}
		return GetProvider(component).GetFullComponentName(component);
	}

	private static Type GetNodeForBaseType(Type searchType)
	{
		if (searchType.IsInterface)
		{
			return InterfaceType;
		}
		if (searchType == InterfaceType)
		{
			return null;
		}
		return searchType.BaseType;
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
	public static PropertyDescriptorCollection GetProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType)
	{
		if (componentType == null)
		{
			return new PropertyDescriptorCollection(null, readOnly: true);
		}
		return GetDescriptor(componentType, "componentType").GetProperties();
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	public static PropertyDescriptorCollection GetProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType, Attribute[]? attributes)
	{
		if (componentType == null)
		{
			return new PropertyDescriptorCollection(null, readOnly: true);
		}
		PropertyDescriptorCollection propertyDescriptorCollection = GetDescriptor(componentType, "componentType").GetProperties(attributes);
		if (attributes != null && attributes.Length != 0)
		{
			ArrayList arrayList = FilterMembers(propertyDescriptorCollection, attributes);
			if (arrayList != null)
			{
				PropertyDescriptor[] array = new PropertyDescriptor[arrayList.Count];
				arrayList.CopyTo(array);
				propertyDescriptorCollection = new PropertyDescriptorCollection(array, readOnly: true);
			}
		}
		return propertyDescriptorCollection;
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The Type of component cannot be statically discovered.")]
	public static PropertyDescriptorCollection GetProperties(object component)
	{
		return GetProperties(component, noCustomTypeDesc: false);
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The Type of component cannot be statically discovered.")]
	public static PropertyDescriptorCollection GetProperties(object component, bool noCustomTypeDesc)
	{
		return GetPropertiesImpl(component, null, noCustomTypeDesc, noAttributes: true);
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The Type of component cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	public static PropertyDescriptorCollection GetProperties(object component, Attribute[]? attributes)
	{
		return GetProperties(component, attributes, noCustomTypeDesc: false);
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The Type of component cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	public static PropertyDescriptorCollection GetProperties(object component, Attribute[]? attributes, bool noCustomTypeDesc)
	{
		return GetPropertiesImpl(component, attributes, noCustomTypeDesc, noAttributes: false);
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The Type of component cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	private static PropertyDescriptorCollection GetPropertiesImpl(object component, Attribute[] attributes, bool noCustomTypeDesc, bool noAttributes)
	{
		if (component == null)
		{
			return new PropertyDescriptorCollection(null, readOnly: true);
		}
		ICustomTypeDescriptor descriptor = GetDescriptor(component, noCustomTypeDesc);
		ICollection collection;
		if (component is ICustomTypeDescriptor)
		{
			collection = (noAttributes ? descriptor.GetProperties() : descriptor.GetProperties(attributes));
			if (noCustomTypeDesc)
			{
				ICustomTypeDescriptor extendedDescriptor = GetExtendedDescriptor(component);
				if (extendedDescriptor != null)
				{
					ICollection secondary = (noAttributes ? extendedDescriptor.GetProperties() : extendedDescriptor.GetProperties(attributes));
					collection = PipelineMerge(1, collection, secondary, component, null);
				}
			}
			else
			{
				collection = PipelineFilter(1, collection, component, null);
				collection = PipelineAttributeFilter(1, collection, attributes, component, null);
			}
		}
		else
		{
			IDictionary cache = GetCache(component);
			collection = (noAttributes ? descriptor.GetProperties() : descriptor.GetProperties(attributes));
			collection = PipelineInitialize(1, collection, cache);
			ICustomTypeDescriptor extendedDescriptor2 = GetExtendedDescriptor(component);
			if (extendedDescriptor2 != null)
			{
				ICollection secondary2 = (noAttributes ? extendedDescriptor2.GetProperties() : extendedDescriptor2.GetProperties(attributes));
				collection = PipelineMerge(1, collection, secondary2, component, cache);
			}
			collection = PipelineFilter(1, collection, component, cache);
			collection = PipelineAttributeFilter(1, collection, attributes, component, cache);
		}
		PropertyDescriptorCollection propertyDescriptorCollection = collection as PropertyDescriptorCollection;
		if (propertyDescriptorCollection == null)
		{
			PropertyDescriptor[] array = new PropertyDescriptor[collection.Count];
			collection.CopyTo(array, 0);
			propertyDescriptorCollection = new PropertyDescriptorCollection(array, readOnly: true);
		}
		return propertyDescriptorCollection;
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static TypeDescriptionProvider GetProvider(Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		return NodeFor(type, createDelegator: true);
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static TypeDescriptionProvider GetProvider(object instance)
	{
		if (instance == null)
		{
			throw new ArgumentNullException("instance");
		}
		return NodeFor(instance, createDelegator: true);
	}

	internal static TypeDescriptionProvider GetProviderRecursive(Type type)
	{
		return NodeFor(type, createDelegator: false);
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicFields)]
	public static Type GetReflectionType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicFields)] Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		return NodeFor(type).GetReflectionType(type);
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[RequiresUnreferencedCode("GetReflectionType is not trim compatible because the Type of object cannot be statically discovered.")]
	public static Type GetReflectionType(object instance)
	{
		if (instance == null)
		{
			throw new ArgumentNullException("instance");
		}
		return NodeFor(instance).GetReflectionType(instance);
	}

	private static TypeDescriptionNode NodeFor(Type type)
	{
		return NodeFor(type, createDelegator: false);
	}

	private static TypeDescriptionNode NodeFor(Type type, bool createDelegator)
	{
		CheckDefaultProvider(type);
		TypeDescriptionNode typeDescriptionNode = null;
		Type type2 = type;
		while (typeDescriptionNode == null)
		{
			typeDescriptionNode = ((TypeDescriptionNode)s_providerTypeTable[type2]) ?? ((TypeDescriptionNode)s_providerTable[type2]);
			if (typeDescriptionNode != null)
			{
				continue;
			}
			Type nodeForBaseType = GetNodeForBaseType(type2);
			if (type2 == typeof(object) || nodeForBaseType == null)
			{
				lock (s_providerTable)
				{
					typeDescriptionNode = (TypeDescriptionNode)s_providerTable[type2];
					if (typeDescriptionNode == null)
					{
						typeDescriptionNode = new TypeDescriptionNode(new ReflectTypeDescriptionProvider());
						s_providerTable[type2] = typeDescriptionNode;
					}
				}
			}
			else if (createDelegator)
			{
				typeDescriptionNode = new TypeDescriptionNode(new DelegatingTypeDescriptionProvider(nodeForBaseType));
				lock (s_providerTable)
				{
					s_providerTypeTable[type2] = typeDescriptionNode;
				}
			}
			else
			{
				type2 = nodeForBaseType;
			}
		}
		return typeDescriptionNode;
	}

	private static TypeDescriptionNode NodeFor(object instance)
	{
		return NodeFor(instance, createDelegator: false);
	}

	private static TypeDescriptionNode NodeFor(object instance, bool createDelegator)
	{
		TypeDescriptionNode typeDescriptionNode = (TypeDescriptionNode)s_providerTable[instance];
		if (typeDescriptionNode == null)
		{
			Type type = instance.GetType();
			if (type.IsCOMObject)
			{
				type = ComObjectType;
			}
			typeDescriptionNode = ((!createDelegator) ? NodeFor(type) : new TypeDescriptionNode(new DelegatingTypeDescriptionProvider(type)));
		}
		return typeDescriptionNode;
	}

	private static void NodeRemove(object key, TypeDescriptionProvider provider)
	{
		lock (s_providerTable)
		{
			TypeDescriptionNode typeDescriptionNode = (TypeDescriptionNode)s_providerTable[key];
			TypeDescriptionNode typeDescriptionNode2 = typeDescriptionNode;
			TypeDescriptionNode typeDescriptionNode3 = null;
			while (typeDescriptionNode2 != null && typeDescriptionNode2.Provider != provider)
			{
				typeDescriptionNode3 = typeDescriptionNode2;
				typeDescriptionNode2 = typeDescriptionNode2.Next;
			}
			if (typeDescriptionNode2 == null)
			{
				return;
			}
			if (typeDescriptionNode2.Next != null)
			{
				typeDescriptionNode2.Provider = typeDescriptionNode2.Next.Provider;
				typeDescriptionNode2.Next = typeDescriptionNode2.Next.Next;
				if (typeDescriptionNode2 == typeDescriptionNode && typeDescriptionNode2.Provider is DelegatingTypeDescriptionProvider)
				{
					s_providerTable.Remove(key);
				}
			}
			else if (typeDescriptionNode2 != typeDescriptionNode)
			{
				Type type = (key as Type) ?? key.GetType();
				typeDescriptionNode2.Provider = new DelegatingTypeDescriptionProvider(type.BaseType);
			}
			else
			{
				s_providerTable.Remove(key);
			}
			s_providerTypeTable.Clear();
		}
	}

	[RequiresUnreferencedCode("The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	private static ICollection PipelineAttributeFilter(int pipelineType, ICollection members, Attribute[] filter, object instance, IDictionary cache)
	{
		IList list = members as ArrayList;
		if (filter == null || filter.Length == 0)
		{
			return members;
		}
		if (cache != null && (list == null || list.IsReadOnly) && cache[s_pipelineAttributeFilterKeys[pipelineType]] is AttributeFilterCacheItem attributeFilterCacheItem && attributeFilterCacheItem.IsValid(filter))
		{
			return attributeFilterCacheItem.FilteredMembers;
		}
		if (list == null || list.IsReadOnly)
		{
			list = new ArrayList(members);
		}
		ArrayList arrayList = FilterMembers(list, filter);
		if (arrayList != null)
		{
			list = arrayList;
		}
		if (cache != null)
		{
			ICollection filteredMembers;
			switch (pipelineType)
			{
			case 1:
			{
				PropertyDescriptor[] array2 = new PropertyDescriptor[list.Count];
				list.CopyTo(array2, 0);
				filteredMembers = new PropertyDescriptorCollection(array2, readOnly: true);
				break;
			}
			case 2:
			{
				EventDescriptor[] array = new EventDescriptor[list.Count];
				list.CopyTo(array, 0);
				filteredMembers = new EventDescriptorCollection(array, readOnly: true);
				break;
			}
			default:
				filteredMembers = null;
				break;
			}
			AttributeFilterCacheItem value = new AttributeFilterCacheItem(filter, filteredMembers);
			cache[s_pipelineAttributeFilterKeys[pipelineType]] = value;
		}
		return list;
	}

	private static ICollection PipelineFilter(int pipelineType, ICollection members, object instance, IDictionary cache)
	{
		IComponent component = instance as IComponent;
		ITypeDescriptorFilterService typeDescriptorFilterService = null;
		ISite site = component?.Site;
		if (site != null)
		{
			typeDescriptorFilterService = site.GetService(typeof(ITypeDescriptorFilterService)) as ITypeDescriptorFilterService;
		}
		IList list = members as ArrayList;
		if (typeDescriptorFilterService == null)
		{
			return members;
		}
		if (cache != null && (list == null || list.IsReadOnly) && cache[s_pipelineFilterKeys[pipelineType]] is FilterCacheItem filterCacheItem && filterCacheItem.IsValid(typeDescriptorFilterService))
		{
			return filterCacheItem.FilteredMembers;
		}
		OrderedDictionary orderedDictionary = new OrderedDictionary(members.Count);
		bool flag;
		switch (pipelineType)
		{
		case 0:
			foreach (Attribute member in members)
			{
				orderedDictionary[member.TypeId] = member;
			}
			flag = typeDescriptorFilterService.FilterAttributes(component, orderedDictionary);
			break;
		case 1:
		case 2:
			foreach (MemberDescriptor member2 in members)
			{
				string name = member2.Name;
				if (orderedDictionary.Contains(name))
				{
					string extenderCollisionSuffix = GetExtenderCollisionSuffix(member2);
					if (extenderCollisionSuffix != null)
					{
						orderedDictionary[name + extenderCollisionSuffix] = member2;
					}
					MemberDescriptor memberDescriptor2 = (MemberDescriptor)orderedDictionary[name];
					extenderCollisionSuffix = GetExtenderCollisionSuffix(memberDescriptor2);
					if (extenderCollisionSuffix != null)
					{
						orderedDictionary.Remove(name);
						orderedDictionary[memberDescriptor2.Name + extenderCollisionSuffix] = memberDescriptor2;
					}
				}
				else
				{
					orderedDictionary[name] = member2;
				}
			}
			flag = ((pipelineType != 1) ? typeDescriptorFilterService.FilterEvents(component, orderedDictionary) : typeDescriptorFilterService.FilterProperties(component, orderedDictionary));
			break;
		default:
			flag = false;
			break;
		}
		if (list == null || list.IsReadOnly)
		{
			list = new ArrayList(orderedDictionary.Values);
		}
		else
		{
			list.Clear();
			foreach (object value2 in orderedDictionary.Values)
			{
				list.Add(value2);
			}
		}
		if (flag && cache != null)
		{
			ICollection filteredMembers;
			switch (pipelineType)
			{
			case 0:
			{
				Attribute[] array2 = new Attribute[list.Count];
				try
				{
					list.CopyTo(array2, 0);
				}
				catch (InvalidCastException)
				{
					throw new ArgumentException(System.SR.Format(System.SR.TypeDescriptorExpectedElementType, typeof(Attribute).FullName));
				}
				filteredMembers = new AttributeCollection(array2);
				break;
			}
			case 1:
			{
				PropertyDescriptor[] array3 = new PropertyDescriptor[list.Count];
				try
				{
					list.CopyTo(array3, 0);
				}
				catch (InvalidCastException)
				{
					throw new ArgumentException(System.SR.Format(System.SR.TypeDescriptorExpectedElementType, typeof(PropertyDescriptor).FullName));
				}
				filteredMembers = new PropertyDescriptorCollection(array3, readOnly: true);
				break;
			}
			case 2:
			{
				EventDescriptor[] array = new EventDescriptor[list.Count];
				try
				{
					list.CopyTo(array, 0);
				}
				catch (InvalidCastException)
				{
					throw new ArgumentException(System.SR.Format(System.SR.TypeDescriptorExpectedElementType, typeof(EventDescriptor).FullName));
				}
				filteredMembers = new EventDescriptorCollection(array, readOnly: true);
				break;
			}
			default:
				filteredMembers = null;
				break;
			}
			FilterCacheItem value = new FilterCacheItem(typeDescriptorFilterService, filteredMembers);
			cache[s_pipelineFilterKeys[pipelineType]] = value;
			cache.Remove(s_pipelineAttributeFilterKeys[pipelineType]);
		}
		return list;
	}

	private static ICollection PipelineInitialize(int pipelineType, ICollection members, IDictionary cache)
	{
		if (cache != null)
		{
			bool flag = true;
			if (cache[s_pipelineInitializeKeys[pipelineType]] is ICollection collection && collection.Count == members.Count)
			{
				IEnumerator enumerator = collection.GetEnumerator();
				IEnumerator enumerator2 = members.GetEnumerator();
				while (enumerator.MoveNext() && enumerator2.MoveNext())
				{
					if (enumerator.Current != enumerator2.Current)
					{
						flag = false;
						break;
					}
				}
			}
			if (!flag)
			{
				cache.Remove(s_pipelineMergeKeys[pipelineType]);
				cache.Remove(s_pipelineFilterKeys[pipelineType]);
				cache.Remove(s_pipelineAttributeFilterKeys[pipelineType]);
				cache[s_pipelineInitializeKeys[pipelineType]] = members;
			}
		}
		return members;
	}

	private static ICollection PipelineMerge(int pipelineType, ICollection primary, ICollection secondary, object instance, IDictionary cache)
	{
		if (secondary == null || secondary.Count == 0)
		{
			return primary;
		}
		if (cache?[s_pipelineMergeKeys[pipelineType]] is ICollection collection && collection.Count == primary.Count + secondary.Count)
		{
			IEnumerator enumerator = collection.GetEnumerator();
			IEnumerator enumerator2 = primary.GetEnumerator();
			bool flag = true;
			while (enumerator2.MoveNext() && enumerator.MoveNext())
			{
				if (enumerator2.Current != enumerator.Current)
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				IEnumerator enumerator3 = secondary.GetEnumerator();
				while (enumerator3.MoveNext() && enumerator.MoveNext())
				{
					if (enumerator3.Current != enumerator.Current)
					{
						flag = false;
						break;
					}
				}
			}
			if (flag)
			{
				return collection;
			}
		}
		ArrayList arrayList = new ArrayList(primary.Count + secondary.Count);
		foreach (object item in primary)
		{
			arrayList.Add(item);
		}
		foreach (object item2 in secondary)
		{
			arrayList.Add(item2);
		}
		if (cache != null)
		{
			ICollection value;
			switch (pipelineType)
			{
			case 0:
			{
				Attribute[] array3 = new Attribute[arrayList.Count];
				arrayList.CopyTo(array3, 0);
				value = new AttributeCollection(array3);
				break;
			}
			case 1:
			{
				PropertyDescriptor[] array2 = new PropertyDescriptor[arrayList.Count];
				arrayList.CopyTo(array2, 0);
				value = new PropertyDescriptorCollection(array2, readOnly: true);
				break;
			}
			case 2:
			{
				EventDescriptor[] array = new EventDescriptor[arrayList.Count];
				arrayList.CopyTo(array, 0);
				value = new EventDescriptorCollection(array, readOnly: true);
				break;
			}
			default:
				value = null;
				break;
			}
			cache[s_pipelineMergeKeys[pipelineType]] = value;
			cache.Remove(s_pipelineFilterKeys[pipelineType]);
			cache.Remove(s_pipelineAttributeFilterKeys[pipelineType]);
		}
		return arrayList;
	}

	private static void RaiseRefresh(object component)
	{
		Volatile.Read(ref TypeDescriptor.Refreshed)?.Invoke(new RefreshEventArgs(component));
	}

	private static void RaiseRefresh(Type type)
	{
		Volatile.Read(ref TypeDescriptor.Refreshed)?.Invoke(new RefreshEventArgs(type));
	}

	public static void Refresh(object component)
	{
		Refresh(component, refreshReflectionProvider: true);
	}

	private static void Refresh(object component, bool refreshReflectionProvider)
	{
		if (component == null)
		{
			return;
		}
		bool flag = false;
		if (refreshReflectionProvider)
		{
			Type type = component.GetType();
			lock (s_providerTable)
			{
				IDictionaryEnumerator enumerator = s_providerTable.GetEnumerator();
				while (enumerator.MoveNext())
				{
					DictionaryEntry entry = enumerator.Entry;
					Type type2 = entry.Key as Type;
					if ((!(type2 != null) || !type.IsAssignableFrom(type2)) && !(type2 == typeof(object)))
					{
						continue;
					}
					TypeDescriptionNode typeDescriptionNode = (TypeDescriptionNode)entry.Value;
					while (typeDescriptionNode != null && !(typeDescriptionNode.Provider is ReflectTypeDescriptionProvider))
					{
						flag = true;
						typeDescriptionNode = typeDescriptionNode.Next;
					}
					if (typeDescriptionNode != null)
					{
						ReflectTypeDescriptionProvider reflectTypeDescriptionProvider = (ReflectTypeDescriptionProvider)typeDescriptionNode.Provider;
						if (reflectTypeDescriptionProvider.IsPopulated(type))
						{
							flag = true;
							reflectTypeDescriptionProvider.Refresh(type);
						}
					}
				}
			}
		}
		IDictionary cache = GetCache(component);
		if (!flag && cache == null)
		{
			return;
		}
		if (cache != null)
		{
			for (int i = 0; i < s_pipelineFilterKeys.Length; i++)
			{
				cache.Remove(s_pipelineFilterKeys[i]);
				cache.Remove(s_pipelineMergeKeys[i]);
				cache.Remove(s_pipelineAttributeFilterKeys[i]);
			}
		}
		Interlocked.Increment(ref s_metadataVersion);
		RaiseRefresh(component);
	}

	public static void Refresh(Type type)
	{
		if (type == null)
		{
			return;
		}
		bool flag = false;
		lock (s_providerTable)
		{
			IDictionaryEnumerator enumerator = s_providerTable.GetEnumerator();
			while (enumerator.MoveNext())
			{
				DictionaryEntry entry = enumerator.Entry;
				Type type2 = entry.Key as Type;
				if ((!(type2 != null) || !type.IsAssignableFrom(type2)) && !(type2 == typeof(object)))
				{
					continue;
				}
				TypeDescriptionNode typeDescriptionNode = (TypeDescriptionNode)entry.Value;
				while (typeDescriptionNode != null && !(typeDescriptionNode.Provider is ReflectTypeDescriptionProvider))
				{
					flag = true;
					typeDescriptionNode = typeDescriptionNode.Next;
				}
				if (typeDescriptionNode != null)
				{
					ReflectTypeDescriptionProvider reflectTypeDescriptionProvider = (ReflectTypeDescriptionProvider)typeDescriptionNode.Provider;
					if (reflectTypeDescriptionProvider.IsPopulated(type))
					{
						flag = true;
						reflectTypeDescriptionProvider.Refresh(type);
					}
				}
			}
		}
		if (flag)
		{
			Interlocked.Increment(ref s_metadataVersion);
			RaiseRefresh(type);
		}
	}

	public static void Refresh(Module module)
	{
		if (module == null)
		{
			return;
		}
		Hashtable hashtable = null;
		lock (s_providerTable)
		{
			IDictionaryEnumerator enumerator = s_providerTable.GetEnumerator();
			while (enumerator.MoveNext())
			{
				DictionaryEntry entry = enumerator.Entry;
				Type type = entry.Key as Type;
				if ((!(type != null) || !type.Module.Equals(module)) && !(type == typeof(object)))
				{
					continue;
				}
				TypeDescriptionNode typeDescriptionNode = (TypeDescriptionNode)entry.Value;
				while (typeDescriptionNode != null && !(typeDescriptionNode.Provider is ReflectTypeDescriptionProvider))
				{
					if (hashtable == null)
					{
						hashtable = new Hashtable();
					}
					hashtable[type] = type;
					typeDescriptionNode = typeDescriptionNode.Next;
				}
				if (typeDescriptionNode == null)
				{
					continue;
				}
				ReflectTypeDescriptionProvider reflectTypeDescriptionProvider = (ReflectTypeDescriptionProvider)typeDescriptionNode.Provider;
				Type[] populatedTypes = reflectTypeDescriptionProvider.GetPopulatedTypes(module);
				Type[] array = populatedTypes;
				foreach (Type type2 in array)
				{
					reflectTypeDescriptionProvider.Refresh(type2);
					if (hashtable == null)
					{
						hashtable = new Hashtable();
					}
					hashtable[type2] = type2;
				}
			}
		}
		if (hashtable == null || TypeDescriptor.Refreshed == null)
		{
			return;
		}
		foreach (Type key in hashtable.Keys)
		{
			RaiseRefresh(key);
		}
	}

	public static void Refresh(Assembly assembly)
	{
		if (!(assembly == null))
		{
			Module[] modules = assembly.GetModules();
			foreach (Module module in modules)
			{
				Refresh(module);
			}
		}
	}

	[RequiresUnreferencedCode("The Type of component cannot be statically discovered.")]
	public static IDesigner? CreateDesigner(IComponent component, Type designerBaseType)
	{
		Type type = null;
		IDesigner result = null;
		AttributeCollection attributes = GetAttributes(component);
		for (int i = 0; i < attributes.Count; i++)
		{
			if (!(attributes[i] is DesignerAttribute designerAttribute))
			{
				continue;
			}
			Type type2 = Type.GetType(designerAttribute.DesignerBaseTypeName);
			if (type2 != null && type2 == designerBaseType)
			{
				ISite site = component.Site;
				bool flag = false;
				ITypeResolutionService typeResolutionService = (ITypeResolutionService)(site?.GetService(typeof(ITypeResolutionService)));
				if (typeResolutionService != null)
				{
					flag = true;
					type = typeResolutionService.GetType(designerAttribute.DesignerTypeName);
				}
				if (!flag)
				{
					type = Type.GetType(designerAttribute.DesignerTypeName);
				}
				if (type != null)
				{
					break;
				}
			}
		}
		if (type != null)
		{
			result = (IDesigner)Activator.CreateInstance(type);
		}
		return result;
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static void RemoveAssociation(object primary, object secondary)
	{
		if (primary == null)
		{
			throw new ArgumentNullException("primary");
		}
		if (secondary == null)
		{
			throw new ArgumentNullException("secondary");
		}
		IList list = (IList)(AssociationTable?[primary]);
		if (list == null)
		{
			return;
		}
		lock (list)
		{
			for (int num = list.Count - 1; num >= 0; num--)
			{
				WeakReference weakReference = (WeakReference)list[num];
				object target = weakReference.Target;
				if (target == null || target == secondary)
				{
					list.RemoveAt(num);
				}
			}
		}
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static void RemoveAssociations(object primary)
	{
		if (primary == null)
		{
			throw new ArgumentNullException("primary");
		}
		AssociationTable?.Remove(primary);
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static void RemoveProvider(TypeDescriptionProvider provider, Type type)
	{
		if (provider == null)
		{
			throw new ArgumentNullException("provider");
		}
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		NodeRemove(type, provider);
		RaiseRefresh(type);
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static void RemoveProvider(TypeDescriptionProvider provider, object instance)
	{
		if (provider == null)
		{
			throw new ArgumentNullException("provider");
		}
		if (instance == null)
		{
			throw new ArgumentNullException("instance");
		}
		NodeRemove(instance, provider);
		RaiseRefresh(instance);
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static void RemoveProviderTransparent(TypeDescriptionProvider provider, Type type)
	{
		if (provider == null)
		{
			throw new ArgumentNullException("provider");
		}
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		RemoveProvider(provider, type);
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static void RemoveProviderTransparent(TypeDescriptionProvider provider, object instance)
	{
		if (provider == null)
		{
			throw new ArgumentNullException("provider");
		}
		if (instance == null)
		{
			throw new ArgumentNullException("instance");
		}
		RemoveProvider(provider, instance);
	}

	[RequiresUnreferencedCode("The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	private static bool ShouldHideMember(MemberDescriptor member, Attribute attribute)
	{
		if (member == null || attribute == null)
		{
			return true;
		}
		Attribute attribute2 = member.Attributes[attribute.GetType()];
		if (attribute2 == null)
		{
			return !attribute.IsDefaultAttribute();
		}
		return !attribute.Match(attribute2);
	}

	public static void SortDescriptorArray(IList infos)
	{
		if (infos == null)
		{
			throw new ArgumentNullException("infos");
		}
		ArrayList.Adapter(infos).Sort(MemberDescriptorComparer.Instance);
	}
}
