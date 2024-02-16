using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Threading;

namespace System.Data.Common;

[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2113:ReflectionToRequiresUnreferencedCode", Justification = "The use of GetType preserves ICustomTypeDescriptor members with RequiresUnreferencedCode, but the GetType callsites either occur in RequiresUnreferencedCode scopes, or have individually justified suppressions.")]
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class DbConnectionStringBuilder : IDictionary, ICollection, IEnumerable, ICustomTypeDescriptor
{
	private Dictionary<string, object> _currentValues;

	private string _connectionString = string.Empty;

	private PropertyDescriptorCollection _propertyDescriptors;

	private bool _browsableConnectionString = true;

	private readonly bool _useOdbcRules;

	private static int s_objectTypeCount;

	internal readonly int _objectID = Interlocked.Increment(ref s_objectTypeCount);

	private ICollection Collection => CurrentValues;

	private IDictionary Dictionary => CurrentValues;

	private Dictionary<string, object> CurrentValues
	{
		get
		{
			Dictionary<string, object> dictionary = _currentValues;
			if (dictionary == null)
			{
				dictionary = (_currentValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));
			}
			return dictionary;
		}
	}

	object? IDictionary.this[object keyword]
	{
		get
		{
			return this[ObjectToString(keyword)];
		}
		set
		{
			this[ObjectToString(keyword)] = value;
		}
	}

	[Browsable(false)]
	public virtual object this[string keyword]
	{
		get
		{
			DataCommonEventSource.Log.Trace("<comm.DbConnectionStringBuilder.get_Item|API> {0}, keyword='{1}'", ObjectID, keyword);
			ADP.CheckArgumentNull(keyword, "keyword");
			if (CurrentValues.TryGetValue(keyword, out object value))
			{
				return value;
			}
			throw ADP.KeywordNotSupported(keyword);
		}
		[param: AllowNull]
		set
		{
			ADP.CheckArgumentNull(keyword, "keyword");
			bool flag = false;
			if (value != null)
			{
				string value2 = DbConnectionStringBuilderUtil.ConvertToString(value);
				DbConnectionOptions.ValidateKeyValuePair(keyword, value2);
				flag = CurrentValues.ContainsKey(keyword);
				CurrentValues[keyword] = value2;
			}
			else
			{
				flag = Remove(keyword);
			}
			_connectionString = null;
			if (flag)
			{
				_propertyDescriptors = null;
			}
		}
	}

	[Browsable(false)]
	[DesignOnly(true)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool BrowsableConnectionString
	{
		get
		{
			return _browsableConnectionString;
		}
		set
		{
			_browsableConnectionString = value;
			_propertyDescriptors = null;
		}
	}

	[RefreshProperties(RefreshProperties.All)]
	public string ConnectionString
	{
		get
		{
			DataCommonEventSource.Log.Trace("<comm.DbConnectionStringBuilder.get_ConnectionString|API> {0}", ObjectID);
			string text = _connectionString;
			if (text == null)
			{
				StringBuilder stringBuilder = new StringBuilder();
				foreach (string key in Keys)
				{
					if (ShouldSerialize(key) && TryGetValue(key, out object value))
					{
						string value2 = ConvertValueToString(value);
						AppendKeyValuePair(stringBuilder, key, value2, _useOdbcRules);
					}
				}
				text = (_connectionString = stringBuilder.ToString());
			}
			return text;
		}
		[param: AllowNull]
		set
		{
			DataCommonEventSource.Log.Trace("<comm.DbConnectionStringBuilder.set_ConnectionString|API> {0}", ObjectID);
			DbConnectionOptions dbConnectionOptions = new DbConnectionOptions(value, null, _useOdbcRules);
			string connectionString = ConnectionString;
			Clear();
			try
			{
				for (NameValuePair nameValuePair = dbConnectionOptions._keyChain; nameValuePair != null; nameValuePair = nameValuePair.Next)
				{
					if (nameValuePair.Value != null)
					{
						this[nameValuePair.Name] = nameValuePair.Value;
					}
					else
					{
						Remove(nameValuePair.Name);
					}
				}
				_connectionString = null;
			}
			catch (ArgumentException)
			{
				ConnectionString = connectionString;
				_connectionString = connectionString;
				throw;
			}
		}
	}

	[Browsable(false)]
	public virtual int Count => CurrentValues.Count;

	[Browsable(false)]
	public bool IsReadOnly => false;

	[Browsable(false)]
	public virtual bool IsFixedSize => false;

	bool ICollection.IsSynchronized => Collection.IsSynchronized;

	[Browsable(false)]
	public virtual ICollection Keys
	{
		get
		{
			DataCommonEventSource.Log.Trace("<comm.DbConnectionStringBuilder.Keys|API> {0}", ObjectID);
			return Dictionary.Keys;
		}
	}

	internal int ObjectID => _objectID;

	object ICollection.SyncRoot => Collection.SyncRoot;

	[Browsable(false)]
	public virtual ICollection Values
	{
		get
		{
			DataCommonEventSource.Log.Trace("<comm.DbConnectionStringBuilder.Values|API> {0}", ObjectID);
			ICollection<string> collection = (ICollection<string>)Keys;
			IEnumerator<string> enumerator = collection.GetEnumerator();
			object[] array = new object[collection.Count];
			for (int i = 0; i < array.Length; i++)
			{
				enumerator.MoveNext();
				array[i] = this[enumerator.Current];
			}
			return new ReadOnlyCollection<object>(array);
		}
	}

	public DbConnectionStringBuilder()
	{
	}

	public DbConnectionStringBuilder(bool useOdbcRules)
	{
		_useOdbcRules = useOdbcRules;
	}

	internal virtual string ConvertValueToString(object value)
	{
		if (value != null)
		{
			return Convert.ToString(value, CultureInfo.InvariantCulture);
		}
		return null;
	}

	void IDictionary.Add(object keyword, object value)
	{
		Add(ObjectToString(keyword), value);
	}

	public void Add(string keyword, object value)
	{
		this[keyword] = value;
	}

	public static void AppendKeyValuePair(StringBuilder builder, string keyword, string? value)
	{
		DbConnectionOptions.AppendKeyValuePairBuilder(builder, keyword, value, useOdbcRules: false);
	}

	public static void AppendKeyValuePair(StringBuilder builder, string keyword, string? value, bool useOdbcRules)
	{
		DbConnectionOptions.AppendKeyValuePairBuilder(builder, keyword, value, useOdbcRules);
	}

	public virtual void Clear()
	{
		DataCommonEventSource.Log.Trace("<comm.DbConnectionStringBuilder.Clear|API>");
		_connectionString = string.Empty;
		_propertyDescriptors = null;
		CurrentValues.Clear();
	}

	protected internal void ClearPropertyDescriptors()
	{
		_propertyDescriptors = null;
	}

	bool IDictionary.Contains(object keyword)
	{
		return ContainsKey(ObjectToString(keyword));
	}

	public virtual bool ContainsKey(string keyword)
	{
		ADP.CheckArgumentNull(keyword, "keyword");
		return CurrentValues.ContainsKey(keyword);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		DataCommonEventSource.Log.Trace("<comm.DbConnectionStringBuilder.ICollection.CopyTo|API> {0}", ObjectID);
		Collection.CopyTo(array, index);
	}

	public virtual bool EquivalentTo(DbConnectionStringBuilder connectionStringBuilder)
	{
		ADP.CheckArgumentNull(connectionStringBuilder, "connectionStringBuilder");
		DataCommonEventSource.Log.Trace("<comm.DbConnectionStringBuilder.EquivalentTo|API> {0}, connectionStringBuilder={1}", ObjectID, connectionStringBuilder.ObjectID);
		if (GetType() != connectionStringBuilder.GetType() || CurrentValues.Count != connectionStringBuilder.CurrentValues.Count)
		{
			return false;
		}
		foreach (KeyValuePair<string, object> currentValue in CurrentValues)
		{
			if (!connectionStringBuilder.CurrentValues.TryGetValue(currentValue.Key, out object value) || !currentValue.Value.Equals(value))
			{
				return false;
			}
		}
		return true;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		DataCommonEventSource.Log.Trace("<comm.DbConnectionStringBuilder.IEnumerable.GetEnumerator|API> {0}", ObjectID);
		return Collection.GetEnumerator();
	}

	IDictionaryEnumerator IDictionary.GetEnumerator()
	{
		DataCommonEventSource.Log.Trace("<comm.DbConnectionStringBuilder.IDictionary.GetEnumerator|API> {0}", ObjectID);
		return Dictionary.GetEnumerator();
	}

	private string ObjectToString(object keyword)
	{
		try
		{
			return (string)keyword;
		}
		catch (InvalidCastException)
		{
			throw new ArgumentException("not a string", "keyword");
		}
	}

	void IDictionary.Remove(object keyword)
	{
		Remove(ObjectToString(keyword));
	}

	public virtual bool Remove(string keyword)
	{
		DataCommonEventSource.Log.Trace("<comm.DbConnectionStringBuilder.Remove|API> {0}, keyword='{1}'", ObjectID, keyword);
		ADP.CheckArgumentNull(keyword, "keyword");
		if (CurrentValues.Remove(keyword))
		{
			_connectionString = null;
			_propertyDescriptors = null;
			return true;
		}
		return false;
	}

	public virtual bool ShouldSerialize(string keyword)
	{
		ADP.CheckArgumentNull(keyword, "keyword");
		return CurrentValues.ContainsKey(keyword);
	}

	public override string ToString()
	{
		return ConnectionString;
	}

	public virtual bool TryGetValue(string keyword, [NotNullWhen(true)] out object? value)
	{
		ADP.CheckArgumentNull(keyword, "keyword");
		return CurrentValues.TryGetValue(keyword, out value);
	}

	internal Attribute[] GetAttributesFromCollection(AttributeCollection collection)
	{
		Attribute[] array = new Attribute[collection.Count];
		collection.CopyTo(array, 0);
		return array;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode", Justification = "The use of GetType preserves this member with RequiresUnreferencedCode, but the GetType callsites either occur in RequiresUnreferencedCode scopes, or have individually justified suppressions.")]
	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
	private PropertyDescriptorCollection GetProperties()
	{
		PropertyDescriptorCollection propertyDescriptorCollection = _propertyDescriptors;
		if (propertyDescriptorCollection == null)
		{
			long scopeId = DataCommonEventSource.Log.EnterScope("<comm.DbConnectionStringBuilder.GetProperties|INFO> {0}", ObjectID);
			try
			{
				Hashtable hashtable = new Hashtable(StringComparer.OrdinalIgnoreCase);
				GetProperties(hashtable);
				PropertyDescriptor[] array = new PropertyDescriptor[hashtable.Count];
				hashtable.Values.CopyTo(array, 0);
				propertyDescriptorCollection = (_propertyDescriptors = new PropertyDescriptorCollection(array));
			}
			finally
			{
				DataCommonEventSource.Log.ExitScope(scopeId);
			}
		}
		return propertyDescriptorCollection;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode", Justification = "The use of GetType preserves this member with RequiresUnreferencedCode, but the GetType callsites either occur in RequiresUnreferencedCode scopes, or have individually justified suppressions.")]
	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
	protected virtual void GetProperties(Hashtable propertyDescriptors)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<comm.DbConnectionStringBuilder.GetProperties|API> {0}", ObjectID);
		try
		{
			Type type = GetType();
			Attribute[] attributesFromCollection;
			foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(this, noCustomTypeDesc: true))
			{
				if ("ConnectionString" != property.Name)
				{
					string displayName = property.DisplayName;
					if (!propertyDescriptors.ContainsKey(displayName))
					{
						attributesFromCollection = GetAttributesFromCollection(property.Attributes);
						PropertyDescriptor value = new DbConnectionStringBuilderDescriptor(property.Name, property.ComponentType, property.PropertyType, property.IsReadOnly, attributesFromCollection);
						propertyDescriptors[displayName] = value;
					}
				}
				else if (BrowsableConnectionString)
				{
					propertyDescriptors["ConnectionString"] = property;
				}
				else
				{
					propertyDescriptors.Remove("ConnectionString");
				}
			}
			if (IsFixedSize)
			{
				return;
			}
			attributesFromCollection = null;
			foreach (string key in Keys)
			{
				if (propertyDescriptors.ContainsKey(key))
				{
					continue;
				}
				object obj = this[key];
				Type type2;
				if (obj != null)
				{
					type2 = obj.GetType();
					if (typeof(string) == type2)
					{
						bool result2;
						if (int.TryParse((string)obj, out var _))
						{
							type2 = typeof(int);
						}
						else if (bool.TryParse((string)obj, out result2))
						{
							type2 = typeof(bool);
						}
					}
				}
				else
				{
					type2 = typeof(string);
				}
				Attribute[] attributes = null;
				if (StringComparer.OrdinalIgnoreCase.Equals("Password", key) || StringComparer.OrdinalIgnoreCase.Equals("pwd", key))
				{
					attributes = new Attribute[3]
					{
						BrowsableAttribute.Yes,
						PasswordPropertyTextAttribute.Yes,
						RefreshPropertiesAttribute.All
					};
				}
				else if (attributesFromCollection == null)
				{
					attributesFromCollection = new Attribute[2]
					{
						BrowsableAttribute.Yes,
						RefreshPropertiesAttribute.All
					};
					attributes = attributesFromCollection;
				}
				PropertyDescriptor value2 = new DbConnectionStringBuilderDescriptor(key, GetType(), type2, isReadOnly: false, attributes);
				propertyDescriptors[key] = value2;
			}
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode", Justification = "The use of GetType preserves this member with RequiresUnreferencedCode, but the GetType callsites either occur in RequiresUnreferencedCode scopes, or have individually justified suppressions.")]
	[RequiresUnreferencedCode("The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	private PropertyDescriptorCollection GetProperties(Attribute[] attributes)
	{
		PropertyDescriptorCollection properties = GetProperties();
		if (attributes == null || attributes.Length == 0)
		{
			return properties;
		}
		PropertyDescriptor[] array = new PropertyDescriptor[properties.Count];
		int num = 0;
		foreach (PropertyDescriptor item in properties)
		{
			bool flag = true;
			foreach (Attribute attribute in attributes)
			{
				Attribute attribute2 = item.Attributes[attribute.GetType()];
				if ((attribute2 == null && !attribute.IsDefaultAttribute()) || (attribute2 != null && !attribute2.Match(attribute)))
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				array[num] = item;
				num++;
			}
		}
		PropertyDescriptor[] array2 = new PropertyDescriptor[num];
		Array.Copy(array, array2, num);
		return new PropertyDescriptorCollection(array2);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The component type's class name is preserved because this class is marked with [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]")]
	string ICustomTypeDescriptor.GetClassName()
	{
		Type type = GetType();
		return TypeDescriptor.GetClassName(this, noCustomTypeDesc: true);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The component type's component name is preserved because this class is marked with [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]")]
	string ICustomTypeDescriptor.GetComponentName()
	{
		Type type = GetType();
		return TypeDescriptor.GetComponentName(this, noCustomTypeDesc: true);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The component type's attributes are preserved because this class is marked with [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]")]
	AttributeCollection ICustomTypeDescriptor.GetAttributes()
	{
		return TypeDescriptor.GetAttributes(this, noCustomTypeDesc: true);
	}

	[RequiresUnreferencedCode("Editors registered in TypeDescriptor.AddEditorTable may be trimmed.")]
	object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
	{
		return TypeDescriptor.GetEditor(this, editorBaseType, noCustomTypeDesc: true);
	}

	[RequiresUnreferencedCode("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.")]
	TypeConverter ICustomTypeDescriptor.GetConverter()
	{
		return TypeDescriptor.GetConverter(this, noCustomTypeDesc: true);
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
	PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
	{
		return TypeDescriptor.GetDefaultProperty(this, noCustomTypeDesc: true);
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
	{
		return GetProperties();
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
	{
		return GetProperties(attributes);
	}

	[RequiresUnreferencedCode("The built-in EventDescriptor implementation uses Reflection which requires unreferenced code.")]
	EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
	{
		return TypeDescriptor.GetDefaultEvent(this, noCustomTypeDesc: true);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The component type's events are preserved because this class is marked with [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]")]
	EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
	{
		Type type = GetType();
		return TypeDescriptor.GetEvents(this, noCustomTypeDesc: true);
	}

	[RequiresUnreferencedCode("The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
	{
		return TypeDescriptor.GetEvents(this, attributes, noCustomTypeDesc: true);
	}

	object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
	{
		return this;
	}
}
