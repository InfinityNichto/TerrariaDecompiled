using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace System.Data;

[ToolboxItem(false)]
[DesignTimeVisible(false)]
[DefaultProperty("ColumnName")]
[Editor("Microsoft.VSDesigner.Data.Design.DataColumnEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
public class DataColumn : MarshalByValueComponent
{
	private bool _allowNull = true;

	private string _caption;

	private string _columnName;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
	private Type _dataType;

	private StorageType _storageType;

	internal object _defaultValue = DBNull.Value;

	private DataSetDateTime _dateTimeMode = DataSetDateTime.UnspecifiedLocal;

	private DataExpression _expression;

	private int _maxLength = -1;

	private int _ordinal = -1;

	private bool _readOnly;

	internal Index _sortIndex;

	internal DataTable _table;

	private bool _unique;

	internal MappingType _columnMapping = MappingType.Element;

	internal int _hashCode;

	internal int _errors;

	private bool _isSqlType;

	private bool _implementsINullable;

	private bool _implementsIChangeTracking;

	private bool _implementsIRevertibleChangeTracking;

	private bool _implementsIXMLSerializable;

	private bool _defaultValueIsNull = true;

	internal List<DataColumn> _dependentColumns;

	internal PropertyCollection _extendedProperties;

	private DataStorage _storage;

	private AutoIncrementValue _autoInc;

	internal string _columnUri;

	private string _columnPrefix = string.Empty;

	internal string _encodedColumnName;

	internal SimpleType _simpleType;

	private static int s_objectTypeCount;

	private readonly int _objectID = Interlocked.Increment(ref s_objectTypeCount);

	[DefaultValue(true)]
	public bool AllowDBNull
	{
		get
		{
			return _allowNull;
		}
		set
		{
			long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataColumn.set_AllowDBNull|API> {0}, {1}", ObjectID, value);
			try
			{
				if (_allowNull != value)
				{
					if (_table != null && !value && _table.EnforceConstraints)
					{
						CheckNotAllowNull();
					}
					_allowNull = value;
				}
			}
			finally
			{
				DataCommonEventSource.Log.ExitScope(scopeId);
			}
		}
	}

	[DefaultValue(false)]
	[RefreshProperties(RefreshProperties.All)]
	public bool AutoIncrement
	{
		get
		{
			if (_autoInc != null)
			{
				return _autoInc.Auto;
			}
			return false;
		}
		set
		{
			DataCommonEventSource.Log.Trace("<ds.DataColumn.set_AutoIncrement|API> {0}, {1}", ObjectID, value);
			if (AutoIncrement == value)
			{
				return;
			}
			if (value)
			{
				if (_expression != null)
				{
					throw ExceptionBuilder.AutoIncrementAndExpression();
				}
				if (!DefaultValueIsNull)
				{
					throw ExceptionBuilder.AutoIncrementAndDefaultValue();
				}
				if (!IsAutoIncrementType(DataType))
				{
					if (HasData)
					{
						throw ExceptionBuilder.AutoIncrementCannotSetIfHasData(DataType.Name);
					}
					DataType = typeof(int);
				}
			}
			AutoInc.Auto = value;
		}
	}

	internal object AutoIncrementCurrent
	{
		get
		{
			if (_autoInc == null)
			{
				return AutoIncrementSeed;
			}
			return _autoInc.Current;
		}
		set
		{
			if ((BigInteger)AutoIncrementSeed != BigIntegerStorage.ConvertToBigInteger(value, FormatProvider))
			{
				AutoInc.SetCurrent(value, FormatProvider);
			}
		}
	}

	internal AutoIncrementValue AutoInc => _autoInc ?? (_autoInc = ((DataType == typeof(BigInteger)) ? ((AutoIncrementValue)new AutoIncrementBigInteger()) : ((AutoIncrementValue)new AutoIncrementInt64())));

	[DefaultValue(0L)]
	public long AutoIncrementSeed
	{
		get
		{
			if (_autoInc == null)
			{
				return 0L;
			}
			return _autoInc.Seed;
		}
		set
		{
			DataCommonEventSource.Log.Trace("<ds.DataColumn.set_AutoIncrementSeed|API> {0}, {1}", ObjectID, value);
			if (AutoIncrementSeed != value)
			{
				AutoInc.Seed = value;
			}
		}
	}

	[DefaultValue(1L)]
	public long AutoIncrementStep
	{
		get
		{
			if (_autoInc == null)
			{
				return 1L;
			}
			return _autoInc.Step;
		}
		set
		{
			DataCommonEventSource.Log.Trace("<ds.DataColumn.set_AutoIncrementStep|API> {0}, {1}", ObjectID, value);
			if (AutoIncrementStep != value)
			{
				AutoInc.Step = value;
			}
		}
	}

	public string Caption
	{
		get
		{
			if (_caption == null)
			{
				return _columnName;
			}
			return _caption;
		}
		[param: AllowNull]
		set
		{
			if (value == null)
			{
				value = string.Empty;
			}
			if (_caption == null || string.Compare(_caption, value, ignoreCase: true, Locale) != 0)
			{
				_caption = value;
			}
		}
	}

	[RefreshProperties(RefreshProperties.All)]
	[DefaultValue("")]
	public string ColumnName
	{
		get
		{
			return _columnName;
		}
		[param: AllowNull]
		set
		{
			long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataColumn.set_ColumnName|API> {0}, '{1}'", ObjectID, value);
			try
			{
				if (value == null)
				{
					value = string.Empty;
				}
				if (string.Compare(_columnName, value, ignoreCase: true, Locale) != 0)
				{
					if (_table != null)
					{
						if (value.Length == 0)
						{
							throw ExceptionBuilder.ColumnNameRequired();
						}
						_table.Columns.RegisterColumnName(value, this);
						if (_columnName.Length != 0)
						{
							_table.Columns.UnregisterName(_columnName);
						}
					}
					RaisePropertyChanging("ColumnName");
					_columnName = value;
					_encodedColumnName = null;
					if (_table != null)
					{
						_table.Columns.OnColumnPropertyChanged(new CollectionChangeEventArgs(CollectionChangeAction.Refresh, this));
					}
				}
				else if (_columnName != value)
				{
					RaisePropertyChanging("ColumnName");
					_columnName = value;
					_encodedColumnName = null;
					if (_table != null)
					{
						_table.Columns.OnColumnPropertyChanged(new CollectionChangeEventArgs(CollectionChangeAction.Refresh, this));
					}
				}
			}
			finally
			{
				DataCommonEventSource.Log.ExitScope(scopeId);
			}
		}
	}

	internal string EncodedColumnName
	{
		get
		{
			if (_encodedColumnName == null)
			{
				_encodedColumnName = XmlConvert.EncodeLocalName(ColumnName);
			}
			return _encodedColumnName;
		}
	}

	internal IFormatProvider FormatProvider
	{
		get
		{
			if (_table == null)
			{
				return CultureInfo.CurrentCulture;
			}
			return _table.FormatProvider;
		}
	}

	internal CultureInfo Locale
	{
		get
		{
			if (_table == null)
			{
				return CultureInfo.CurrentCulture;
			}
			return _table.Locale;
		}
	}

	internal int ObjectID => _objectID;

	[DefaultValue("")]
	public string Prefix
	{
		get
		{
			return _columnPrefix;
		}
		[param: AllowNull]
		set
		{
			if (value == null)
			{
				value = string.Empty;
			}
			DataCommonEventSource.Log.Trace("<ds.DataColumn.set_Prefix|API> {0}, '{1}'", ObjectID, value);
			if (XmlConvert.DecodeName(value) == value && XmlConvert.EncodeName(value) != value)
			{
				throw ExceptionBuilder.InvalidPrefix(value);
			}
			_columnPrefix = value;
		}
	}

	internal bool Computed => _expression != null;

	internal DataExpression? DataExpression => _expression;

	[DefaultValue(typeof(string))]
	[RefreshProperties(RefreshProperties.All)]
	[TypeConverter(typeof(ColumnTypeConverter))]
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
	public Type DataType
	{
		get
		{
			return _dataType;
		}
		[param: AllowNull]
		set
		{
			if (!(_dataType != value))
			{
				return;
			}
			if (HasData)
			{
				throw ExceptionBuilder.CantChangeDataType();
			}
			if (value == null)
			{
				throw ExceptionBuilder.NullDataType();
			}
			StorageType storageType = DataStorage.GetStorageType(value);
			if (DataStorage.ImplementsINullableValue(storageType, value))
			{
				throw ExceptionBuilder.ColumnTypeNotSupported();
			}
			if (_table != null && IsInRelation())
			{
				throw ExceptionBuilder.ColumnsTypeMismatch();
			}
			if (storageType == StorageType.BigInteger && _expression != null)
			{
				throw ExprException.UnsupportedDataType(value);
			}
			if (!DefaultValueIsNull)
			{
				try
				{
					if (_defaultValue is BigInteger)
					{
						_defaultValue = BigIntegerStorage.ConvertFromBigInteger((BigInteger)_defaultValue, value, FormatProvider);
					}
					else if (typeof(BigInteger) == value)
					{
						_defaultValue = BigIntegerStorage.ConvertToBigInteger(_defaultValue, FormatProvider);
					}
					else if (typeof(string) == value)
					{
						_defaultValue = DefaultValue.ToString();
					}
					else if (typeof(SqlString) == value)
					{
						_defaultValue = SqlConvert.ConvertToSqlString(DefaultValue);
					}
					else if (typeof(object) != value)
					{
						DefaultValue = SqlConvert.ChangeTypeForDefaultValue(DefaultValue, value, FormatProvider);
					}
				}
				catch (InvalidCastException inner)
				{
					throw ExceptionBuilder.DefaultValueDataType(ColumnName, DefaultValue.GetType(), value, inner);
				}
				catch (FormatException inner2)
				{
					throw ExceptionBuilder.DefaultValueDataType(ColumnName, DefaultValue.GetType(), value, inner2);
				}
			}
			if (ColumnMapping == MappingType.SimpleContent && value == typeof(char))
			{
				throw ExceptionBuilder.CannotSetSimpleContentType(ColumnName, value);
			}
			SimpleType = System.Data.SimpleType.CreateSimpleType(storageType, value);
			if (StorageType.String == storageType)
			{
				_maxLength = -1;
			}
			UpdateColumnType(value, storageType);
			XmlDataType = null;
			if (!AutoIncrement)
			{
				return;
			}
			if (!IsAutoIncrementType(value))
			{
				AutoIncrement = false;
			}
			if (_autoInc != null)
			{
				AutoIncrementValue autoInc = _autoInc;
				_autoInc = null;
				AutoInc.Auto = autoInc.Auto;
				AutoInc.Seed = autoInc.Seed;
				AutoInc.Step = autoInc.Step;
				if (_autoInc.DataType == autoInc.DataType)
				{
					_autoInc.Current = autoInc.Current;
				}
				else if (autoInc.DataType == typeof(long))
				{
					AutoInc.Current = (BigInteger)(long)autoInc.Current;
				}
				else
				{
					AutoInc.Current = (long)(BigInteger)autoInc.Current;
				}
			}
		}
	}

	[DefaultValue(DataSetDateTime.UnspecifiedLocal)]
	[RefreshProperties(RefreshProperties.All)]
	public DataSetDateTime DateTimeMode
	{
		get
		{
			return _dateTimeMode;
		}
		set
		{
			if (_dateTimeMode == value)
			{
				return;
			}
			if (DataType != typeof(DateTime) && value != DataSetDateTime.UnspecifiedLocal)
			{
				throw ExceptionBuilder.CannotSetDateTimeModeForNonDateTimeColumns();
			}
			switch (value)
			{
			case DataSetDateTime.Local:
			case DataSetDateTime.Utc:
				if (HasData)
				{
					throw ExceptionBuilder.CantChangeDateTimeMode(_dateTimeMode, value);
				}
				break;
			case DataSetDateTime.Unspecified:
			case DataSetDateTime.UnspecifiedLocal:
				if (_dateTimeMode != DataSetDateTime.Unspecified && _dateTimeMode != DataSetDateTime.UnspecifiedLocal && HasData)
				{
					throw ExceptionBuilder.CantChangeDateTimeMode(_dateTimeMode, value);
				}
				break;
			default:
				throw ExceptionBuilder.InvalidDateTimeMode(value);
			}
			_dateTimeMode = value;
		}
	}

	[TypeConverter(typeof(DefaultValueTypeConverter))]
	public object DefaultValue
	{
		get
		{
			if (_defaultValue == DBNull.Value && _implementsINullable)
			{
				if (_storage != null)
				{
					_defaultValue = _storage._nullValue;
				}
				else if (_isSqlType)
				{
					_defaultValue = SqlConvert.ChangeTypeForDefaultValue(_defaultValue, _dataType, FormatProvider);
				}
				else if (_implementsINullable)
				{
					PropertyInfo property = _dataType.GetProperty("Null", BindingFlags.Static | BindingFlags.Public);
					if (property != null)
					{
						_defaultValue = property.GetValue(null, null);
					}
				}
			}
			return _defaultValue;
		}
		[param: AllowNull]
		set
		{
			DataCommonEventSource.Log.Trace("<ds.DataColumn.set_DefaultValue|API> {0}", ObjectID);
			if (_defaultValue != null && DefaultValue.Equals(value))
			{
				return;
			}
			if (AutoIncrement)
			{
				throw ExceptionBuilder.DefaultValueAndAutoIncrement();
			}
			object obj = ((value == null) ? DBNull.Value : value);
			if (obj != DBNull.Value && DataType != typeof(object))
			{
				try
				{
					obj = SqlConvert.ChangeTypeForDefaultValue(obj, DataType, FormatProvider);
				}
				catch (InvalidCastException inner)
				{
					throw ExceptionBuilder.DefaultValueColumnDataType(ColumnName, obj.GetType(), DataType, inner);
				}
			}
			_defaultValue = obj;
			_defaultValueIsNull = ((obj == DBNull.Value || (ImplementsINullable && DataStorage.IsObjectSqlNull(obj))) ? true : false);
		}
	}

	internal bool DefaultValueIsNull => _defaultValueIsNull;

	[RefreshProperties(RefreshProperties.All)]
	[DefaultValue("")]
	public string Expression
	{
		get
		{
			if (_expression != null)
			{
				return _expression.Expression;
			}
			return "";
		}
		[RequiresUnreferencedCode("Members from types used in the expressions may be trimmed if not referenced directly.")]
		[param: AllowNull]
		set
		{
			long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataColumn.set_Expression|API> {0}, '{1}'", ObjectID, value);
			if (value == null)
			{
				value = string.Empty;
			}
			try
			{
				DataExpression dataExpression = null;
				if (value.Length > 0)
				{
					DataExpression dataExpression2 = new DataExpression(_table, value, _dataType);
					if (dataExpression2.HasValue)
					{
						dataExpression = dataExpression2;
					}
				}
				if (_expression == null && dataExpression != null)
				{
					if (AutoIncrement || Unique)
					{
						throw ExceptionBuilder.ExpressionAndUnique();
					}
					if (_table != null)
					{
						for (int i = 0; i < _table.Constraints.Count; i++)
						{
							if (_table.Constraints[i].ContainsColumn(this))
							{
								throw ExceptionBuilder.ExpressionAndConstraint(this, _table.Constraints[i]);
							}
						}
					}
					bool readOnly = ReadOnly;
					try
					{
						ReadOnly = true;
					}
					catch (ReadOnlyException e)
					{
						ExceptionBuilder.TraceExceptionForCapture(e);
						ReadOnly = readOnly;
						throw ExceptionBuilder.ExpressionAndReadOnly();
					}
				}
				if (_table != null)
				{
					if (dataExpression != null && dataExpression.DependsOn(this))
					{
						throw ExceptionBuilder.ExpressionCircular();
					}
					HandleDependentColumnList(_expression, dataExpression);
					DataExpression expression = _expression;
					_expression = dataExpression;
					try
					{
						if (dataExpression == null)
						{
							for (int j = 0; j < _table.RecordCapacity; j++)
							{
								InitializeRecord(j);
							}
						}
						else
						{
							_table.EvaluateExpressions(this);
						}
						_table.ResetInternalIndexes(this);
						_table.EvaluateDependentExpressions(this);
						return;
					}
					catch (Exception e2) when (ADP.IsCatchableExceptionType(e2))
					{
						ExceptionBuilder.TraceExceptionForCapture(e2);
						try
						{
							_expression = expression;
							HandleDependentColumnList(dataExpression, _expression);
							if (expression == null)
							{
								for (int k = 0; k < _table.RecordCapacity; k++)
								{
									InitializeRecord(k);
								}
							}
							else
							{
								_table.EvaluateExpressions(this);
							}
							_table.ResetInternalIndexes(this);
							_table.EvaluateDependentExpressions(this);
						}
						catch (Exception e3) when (ADP.IsCatchableExceptionType(e3))
						{
							ExceptionBuilder.TraceExceptionWithoutRethrow(e3);
						}
						throw;
					}
				}
				_expression = dataExpression;
			}
			finally
			{
				DataCommonEventSource.Log.ExitScope(scopeId);
			}
		}
	}

	[Browsable(false)]
	public PropertyCollection ExtendedProperties => _extendedProperties ?? (_extendedProperties = new PropertyCollection());

	internal bool HasData => _storage != null;

	internal bool ImplementsINullable => _implementsINullable;

	internal bool ImplementsIChangeTracking => _implementsIChangeTracking;

	internal bool ImplementsIRevertibleChangeTracking => _implementsIRevertibleChangeTracking;

	internal bool IsValueType => _storage._isValueType;

	internal bool IsSqlType => _isSqlType;

	[DefaultValue(-1)]
	public int MaxLength
	{
		get
		{
			return _maxLength;
		}
		set
		{
			long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataColumn.set_MaxLength|API> {0}, {1}", ObjectID, value);
			try
			{
				if (_maxLength != value)
				{
					if (ColumnMapping == MappingType.SimpleContent)
					{
						throw ExceptionBuilder.CannotSetMaxLength2(this);
					}
					if (DataType != typeof(string) && DataType != typeof(SqlString))
					{
						throw ExceptionBuilder.HasToBeStringType(this);
					}
					int maxLength = _maxLength;
					_maxLength = Math.Max(value, -1);
					if ((maxLength < 0 || value < maxLength) && _table != null && _table.EnforceConstraints && !CheckMaxLength())
					{
						_maxLength = maxLength;
						throw ExceptionBuilder.CannotSetMaxLength(this, value);
					}
					SetMaxLengthSimpleType();
				}
			}
			finally
			{
				DataCommonEventSource.Log.ExitScope(scopeId);
			}
		}
	}

	public string Namespace
	{
		get
		{
			if (_columnUri == null)
			{
				if (Table != null && _columnMapping != MappingType.Attribute)
				{
					return Table.Namespace;
				}
				return string.Empty;
			}
			return _columnUri;
		}
		[param: AllowNull]
		set
		{
			DataCommonEventSource.Log.Trace("<ds.DataColumn.set_Namespace|API> {0}, '{1}'", ObjectID, value);
			if (_columnUri != value)
			{
				if (_columnMapping != MappingType.SimpleContent)
				{
					RaisePropertyChanging("Namespace");
					_columnUri = value;
				}
				else if (value != Namespace)
				{
					throw ExceptionBuilder.CannotChangeNamespace(ColumnName);
				}
			}
		}
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public int Ordinal => _ordinal;

	[DefaultValue(false)]
	public bool ReadOnly
	{
		get
		{
			return _readOnly;
		}
		set
		{
			DataCommonEventSource.Log.Trace("<ds.DataColumn.set_ReadOnly|API> {0}, {1}", ObjectID, value);
			if (_readOnly != value)
			{
				if (!value && _expression != null)
				{
					throw ExceptionBuilder.ReadOnlyAndExpression();
				}
				_readOnly = value;
			}
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private Index SortIndex
	{
		get
		{
			if (_sortIndex == null)
			{
				IndexField[] indexDesc = new IndexField[1]
				{
					new IndexField(this, isDescending: false)
				};
				_sortIndex = _table.GetIndex(indexDesc, DataViewRowState.CurrentRows, null);
				_sortIndex.AddRef();
			}
			return _sortIndex;
		}
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public DataTable? Table => _table;

	internal object this[int record]
	{
		get
		{
			return _storage.Get(record);
		}
		set
		{
			try
			{
				_storage.Set(record, value);
			}
			catch (Exception ex)
			{
				ExceptionBuilder.TraceExceptionForCapture(ex);
				throw ExceptionBuilder.SetFailed(value, this, DataType, ex);
			}
			if (AutoIncrement && !_storage.IsNull(record))
			{
				AutoInc.SetCurrentAndIncrement(_storage.Get(record));
			}
			if (Computed)
			{
				DataRow dataRow = GetDataRow(record);
				if (dataRow != null)
				{
					dataRow.LastChangedColumn = this;
				}
			}
		}
	}

	[DefaultValue(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool Unique
	{
		get
		{
			return _unique;
		}
		set
		{
			long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataColumn.set_Unique|API> {0}, {1}", ObjectID, value);
			try
			{
				if (_unique == value)
				{
					return;
				}
				if (value && _expression != null)
				{
					throw ExceptionBuilder.UniqueAndExpression();
				}
				UniqueConstraint constraint = null;
				if (_table != null)
				{
					if (value)
					{
						CheckUnique();
					}
					else
					{
						IEnumerator enumerator = _table.Constraints.GetEnumerator();
						while (enumerator.MoveNext())
						{
							if (enumerator.Current is UniqueConstraint uniqueConstraint && uniqueConstraint.ColumnsReference.Length == 1 && uniqueConstraint.ColumnsReference[0] == this)
							{
								constraint = uniqueConstraint;
							}
						}
						_table.Constraints.CanRemove(constraint, fThrowException: true);
					}
				}
				_unique = value;
				if (_table != null)
				{
					if (value)
					{
						UniqueConstraint constraint2 = new UniqueConstraint(this);
						_table.Constraints.Add(constraint2);
					}
					else
					{
						_table.Constraints.Remove(constraint);
					}
				}
			}
			finally
			{
				DataCommonEventSource.Log.ExitScope(scopeId);
			}
		}
	}

	internal string? XmlDataType { get; set; } = string.Empty;


	internal SimpleType? SimpleType
	{
		get
		{
			return _simpleType;
		}
		set
		{
			_simpleType = value;
			if (value != null && value.CanHaveMaxLength())
			{
				_maxLength = value.MaxLength;
			}
		}
	}

	[DefaultValue(MappingType.Element)]
	public virtual MappingType ColumnMapping
	{
		get
		{
			return _columnMapping;
		}
		set
		{
			DataCommonEventSource.Log.Trace("<ds.DataColumn.set_ColumnMapping|API> {0}, {1}", ObjectID, value);
			if (value == _columnMapping)
			{
				return;
			}
			if (value == MappingType.SimpleContent && _table != null)
			{
				int num = 0;
				if (_columnMapping == MappingType.Element)
				{
					num = 1;
				}
				if (_dataType == typeof(char))
				{
					throw ExceptionBuilder.CannotSetSimpleContent(ColumnName, _dataType);
				}
				if (_table.XmlText != null && _table.XmlText != this)
				{
					throw ExceptionBuilder.CannotAddColumn3();
				}
				if (_table.ElementColumnCount > num)
				{
					throw ExceptionBuilder.CannotAddColumn4(ColumnName);
				}
			}
			RaisePropertyChanging("ColumnMapping");
			if (_table != null)
			{
				if (_columnMapping == MappingType.SimpleContent)
				{
					_table._xmlText = null;
				}
				if (value == MappingType.Element)
				{
					_table.ElementColumnCount++;
				}
				else if (_columnMapping == MappingType.Element)
				{
					_table.ElementColumnCount--;
				}
			}
			_columnMapping = value;
			if (value == MappingType.SimpleContent)
			{
				_columnUri = null;
				if (_table != null)
				{
					_table.XmlText = this;
				}
				SimpleType = null;
			}
		}
	}

	internal bool IsCustomType
	{
		get
		{
			if (_storage == null)
			{
				return DataStorage.IsTypeCustomType(DataType);
			}
			return _storage._isCustomDefinedType;
		}
	}

	internal bool ImplementsIXMLSerializable => _implementsIXMLSerializable;

	internal event PropertyChangedEventHandler? PropertyChanging;

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This is safe because type is string and expression is null.")]
	public DataColumn()
		: this(null, typeof(string), null, MappingType.Element)
	{
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This is safe because type is string and expression is null.")]
	public DataColumn(string? columnName)
		: this(columnName, typeof(string), null, MappingType.Element)
	{
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Expression is null and `dataType` is marked appropriately.")]
	public DataColumn(string? columnName, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type dataType)
		: this(columnName, dataType, null, MappingType.Element)
	{
	}

	[RequiresUnreferencedCode("Members from serialized types or types used in expressions may be trimmed if not referenced directly.")]
	public DataColumn(string? columnName, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type dataType, string? expr)
		: this(columnName, dataType, expr, MappingType.Element)
	{
	}

	[RequiresUnreferencedCode("Members from serialized types or types used in expressions may be trimmed if not referenced directly.")]
	public DataColumn(string? columnName, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type dataType, string? expr, MappingType type)
	{
		GC.SuppressFinalize(this);
		DataCommonEventSource.Log.Trace("<ds.DataColumn.DataColumn|API> {0}, columnName='{1}', expr='{2}', type={3}", ObjectID, columnName, expr, type);
		if (dataType == null)
		{
			throw ExceptionBuilder.ArgumentNull("dataType");
		}
		StorageType storageType = DataStorage.GetStorageType(dataType);
		if (DataStorage.ImplementsINullableValue(storageType, dataType))
		{
			throw ExceptionBuilder.ColumnTypeNotSupported();
		}
		_columnName = columnName ?? string.Empty;
		SimpleType simpleType = System.Data.SimpleType.CreateSimpleType(storageType, dataType);
		if (simpleType != null)
		{
			SimpleType = simpleType;
		}
		UpdateColumnType(dataType, storageType);
		if (!string.IsNullOrEmpty(expr))
		{
			Expression = expr;
		}
		_columnMapping = type;
	}

	private void UpdateColumnType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, StorageType typeCode)
	{
		TypeLimiter.EnsureTypeIsAllowed(type);
		_dataType = type;
		_storageType = typeCode;
		if (StorageType.DateTime != typeCode)
		{
			_dateTimeMode = DataSetDateTime.UnspecifiedLocal;
		}
		DataStorage.ImplementsInterfaces(typeCode, type, out _isSqlType, out _implementsINullable, out _implementsIXMLSerializable, out _implementsIChangeTracking, out _implementsIRevertibleChangeTracking);
		if (!_isSqlType && _implementsINullable)
		{
			SqlUdtStorage.GetStaticNullForUdtType(type);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal string GetColumnValueAsString(DataRow row, DataRowVersion version)
	{
		object value = this[row.GetRecordFromVersion(version)];
		if (DataStorage.IsObjectNull(value))
		{
			return null;
		}
		return ConvertObjectToXml(value);
	}

	internal void BindExpression()
	{
		DataExpression.Bind(_table);
	}

	private void SetMaxLengthSimpleType()
	{
		if (_simpleType != null)
		{
			_simpleType.MaxLength = _maxLength;
			if (_simpleType.IsPlainString())
			{
				_simpleType = null;
			}
			else if (_simpleType.Name != null && XmlDataType != null)
			{
				_simpleType.ConvertToAnnonymousSimpleType();
				XmlDataType = null;
			}
		}
		else if (-1 < _maxLength)
		{
			SimpleType = System.Data.SimpleType.CreateLimitedStringType(_maxLength);
		}
	}

	public void SetOrdinal(int ordinal)
	{
		if (_ordinal == -1)
		{
			throw ExceptionBuilder.ColumnNotInAnyTable();
		}
		if (_ordinal != ordinal)
		{
			_table.Columns.MoveTo(this, ordinal);
		}
	}

	internal void SetOrdinalInternal(int ordinal)
	{
		if (_ordinal == ordinal)
		{
			return;
		}
		if (Unique && _ordinal != -1 && ordinal == -1)
		{
			UniqueConstraint uniqueConstraint = _table.Constraints.FindKeyConstraint(this);
			if (uniqueConstraint != null)
			{
				_table.Constraints.Remove(uniqueConstraint);
			}
		}
		if (_sortIndex != null && -1 == ordinal)
		{
			_sortIndex.RemoveRef();
			_sortIndex.RemoveRef();
			_sortIndex = null;
		}
		int ordinal2 = _ordinal;
		_ordinal = ordinal;
		if (ordinal2 == -1 && _ordinal != -1 && Unique)
		{
			UniqueConstraint constraint = new UniqueConstraint(this);
			_table.Constraints.Add(constraint);
		}
	}

	internal void SetTable(DataTable table)
	{
		if (_table == table)
		{
			return;
		}
		if (Computed && (table == null || (!table.fInitInProgress && (table.DataSet == null || (!table.DataSet._fIsSchemaLoading && !table.DataSet._fInitInProgress)))))
		{
			DataExpression.Bind(table);
		}
		if (Unique && _table != null)
		{
			UniqueConstraint uniqueConstraint = table.Constraints.FindKeyConstraint(this);
			if (uniqueConstraint != null)
			{
				table.Constraints.CanRemove(uniqueConstraint, fThrowException: true);
			}
		}
		_table = table;
		_storage = null;
	}

	private DataRow GetDataRow(int index)
	{
		return _table._recordManager[index];
	}

	internal void InitializeRecord(int record)
	{
		_storage.Set(record, DefaultValue);
	}

	internal void SetValue(int record, object value)
	{
		try
		{
			_storage.Set(record, value);
		}
		catch (Exception ex)
		{
			ExceptionBuilder.TraceExceptionForCapture(ex);
			throw ExceptionBuilder.SetFailed(value, this, DataType, ex);
		}
		DataRow dataRow = GetDataRow(record);
		if (dataRow != null)
		{
			dataRow.LastChangedColumn = this;
		}
	}

	internal void FreeRecord(int record)
	{
		_storage.Set(record, _storage._nullValue);
	}

	internal void InternalUnique(bool value)
	{
		_unique = value;
	}

	internal void CheckColumnConstraint(DataRow row, DataRowAction action)
	{
		if (_table.UpdatingCurrent(row, action))
		{
			CheckNullable(row);
			CheckMaxLength(row);
		}
	}

	internal bool CheckMaxLength()
	{
		if (0 <= _maxLength && Table != null && 0 < Table.Rows.Count)
		{
			foreach (DataRow row in Table.Rows)
			{
				if (row.HasVersion(DataRowVersion.Current) && _maxLength < GetStringLength(row.GetCurrentRecordNo()))
				{
					return false;
				}
			}
		}
		return true;
	}

	internal void CheckMaxLength(DataRow dr)
	{
		if (0 <= _maxLength && _maxLength < GetStringLength(dr.GetDefaultRecord()))
		{
			throw ExceptionBuilder.LongerThanMaxLength(this);
		}
	}

	protected internal void CheckNotAllowNull()
	{
		if (_storage == null)
		{
			return;
		}
		if (_sortIndex != null)
		{
			if (!_sortIndex.IsKeyInIndex(_storage._nullValue))
			{
				return;
			}
			throw ExceptionBuilder.NullKeyValues(ColumnName);
		}
		foreach (DataRow row in _table.Rows)
		{
			if (row.RowState == DataRowState.Deleted)
			{
				continue;
			}
			if (!_implementsINullable)
			{
				if (row[this] == DBNull.Value)
				{
					throw ExceptionBuilder.NullKeyValues(ColumnName);
				}
			}
			else if (DataStorage.IsObjectNull(row[this]))
			{
				throw ExceptionBuilder.NullKeyValues(ColumnName);
			}
		}
	}

	internal void CheckNullable(DataRow row)
	{
		if (!AllowDBNull && _storage.IsNull(row.GetDefaultRecord()))
		{
			throw ExceptionBuilder.NullValues(ColumnName);
		}
	}

	protected void CheckUnique()
	{
		if (!SortIndex.CheckUnique())
		{
			throw ExceptionBuilder.NonUniqueValues(ColumnName);
		}
	}

	internal int Compare(int record1, int record2)
	{
		return _storage.Compare(record1, record2);
	}

	internal bool CompareValueTo(int record1, object value, bool checkType)
	{
		if (CompareValueTo(record1, value) == 0)
		{
			Type type = value.GetType();
			Type type2 = _storage.Get(record1).GetType();
			if (type == typeof(string) && type2 == typeof(string))
			{
				if (string.CompareOrdinal((string)_storage.Get(record1), (string)value) != 0)
				{
					return false;
				}
				return true;
			}
			if (type == type2)
			{
				return true;
			}
		}
		return false;
	}

	internal int CompareValueTo(int record1, object value)
	{
		return _storage.CompareValueTo(record1, value);
	}

	internal object ConvertValue(object value)
	{
		return _storage.ConvertValue(value);
	}

	internal void Copy(int srcRecordNo, int dstRecordNo)
	{
		_storage.Copy(srcRecordNo, dstRecordNo);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal DataColumn Clone()
	{
		DataColumn dataColumn = (DataColumn)Activator.CreateInstance(GetType());
		dataColumn.SimpleType = SimpleType;
		dataColumn._allowNull = _allowNull;
		if (_autoInc != null)
		{
			dataColumn._autoInc = _autoInc.Clone();
		}
		dataColumn._caption = _caption;
		dataColumn.ColumnName = ColumnName;
		dataColumn._columnUri = _columnUri;
		dataColumn._columnPrefix = _columnPrefix;
		dataColumn.DataType = DataType;
		dataColumn._defaultValue = _defaultValue;
		dataColumn._defaultValueIsNull = ((_defaultValue == DBNull.Value || (dataColumn.ImplementsINullable && DataStorage.IsObjectSqlNull(_defaultValue))) ? true : false);
		dataColumn._columnMapping = _columnMapping;
		dataColumn._readOnly = _readOnly;
		dataColumn.MaxLength = MaxLength;
		dataColumn.XmlDataType = XmlDataType;
		dataColumn._dateTimeMode = _dateTimeMode;
		if (_extendedProperties != null)
		{
			foreach (object key in _extendedProperties.Keys)
			{
				dataColumn.ExtendedProperties[key] = _extendedProperties[key];
			}
		}
		return dataColumn;
	}

	internal object GetAggregateValue(int[] records, AggregateType kind)
	{
		if (_storage == null)
		{
			if (kind != AggregateType.Count)
			{
				return DBNull.Value;
			}
			return 0;
		}
		return _storage.Aggregate(records, kind);
	}

	private int GetStringLength(int record)
	{
		return _storage.GetStringLength(record);
	}

	internal void Init(int record)
	{
		if (AutoIncrement)
		{
			object current = _autoInc.Current;
			_autoInc.MoveAfter();
			_storage.Set(record, current);
		}
		else
		{
			this[record] = _defaultValue;
		}
	}

	internal static bool IsAutoIncrementType(Type dataType)
	{
		if (!(dataType == typeof(int)) && !(dataType == typeof(long)) && !(dataType == typeof(short)) && !(dataType == typeof(decimal)) && !(dataType == typeof(BigInteger)) && !(dataType == typeof(SqlInt32)) && !(dataType == typeof(SqlInt64)) && !(dataType == typeof(SqlInt16)))
		{
			return dataType == typeof(SqlDecimal);
		}
		return true;
	}

	internal bool IsValueCustomTypeInstance(object value)
	{
		if (DataStorage.IsTypeCustomType(value.GetType()))
		{
			return !(value is Type);
		}
		return false;
	}

	internal bool IsNull(int record)
	{
		return _storage.IsNull(record);
	}

	internal bool IsInRelation()
	{
		DataRelationCollection parentRelations = _table.ParentRelations;
		for (int i = 0; i < parentRelations.Count; i++)
		{
			if (parentRelations[i].ChildKey.ContainsColumn(this))
			{
				return true;
			}
		}
		parentRelations = _table.ChildRelations;
		for (int j = 0; j < parentRelations.Count; j++)
		{
			if (parentRelations[j].ParentKey.ContainsColumn(this))
			{
				return true;
			}
		}
		return false;
	}

	internal bool IsMaxLengthViolated()
	{
		if (MaxLength < 0)
		{
			return true;
		}
		bool result = false;
		string text = null;
		foreach (DataRow row in Table.Rows)
		{
			if (!row.HasVersion(DataRowVersion.Current))
			{
				continue;
			}
			object obj = row[this];
			if (!_isSqlType)
			{
				if (obj != null && obj != DBNull.Value && ((string)obj).Length > MaxLength)
				{
					if (text == null)
					{
						text = ExceptionBuilder.MaxLengthViolationText(ColumnName);
					}
					row.RowError = text;
					row.SetColumnError(this, text);
					result = true;
				}
			}
			else if (!DataStorage.IsObjectNull(obj) && ((SqlString)obj).Value.Length > MaxLength)
			{
				if (text == null)
				{
					text = ExceptionBuilder.MaxLengthViolationText(ColumnName);
				}
				row.RowError = text;
				row.SetColumnError(this, text);
				result = true;
			}
		}
		return result;
	}

	internal bool IsNotAllowDBNullViolated()
	{
		Index sortIndex = SortIndex;
		DataRow[] rows = sortIndex.GetRows(sortIndex.FindRecords(DBNull.Value));
		for (int i = 0; i < rows.Length; i++)
		{
			string text = ExceptionBuilder.NotAllowDBNullViolationText(ColumnName);
			rows[i].RowError = text;
			rows[i].SetColumnError(this, text);
		}
		return rows.Length != 0;
	}

	internal void FinishInitInProgress()
	{
		if (Computed)
		{
			BindExpression();
		}
	}

	protected virtual void OnPropertyChanging(PropertyChangedEventArgs pcevent)
	{
		this.PropertyChanging?.Invoke(this, pcevent);
	}

	protected internal void RaisePropertyChanging(string name)
	{
		OnPropertyChanging(new PropertyChangedEventArgs(name));
	}

	private DataStorage InsureStorage()
	{
		if (_storage == null)
		{
			_storage = DataStorage.CreateStorage(this, _dataType, _storageType);
		}
		return _storage;
	}

	internal void SetCapacity(int capacity)
	{
		InsureStorage().SetCapacity(capacity);
	}

	internal void OnSetDataSet()
	{
	}

	public override string ToString()
	{
		if (_expression != null)
		{
			return ColumnName + " + " + Expression;
		}
		return ColumnName;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal object ConvertXmlToObject(string s)
	{
		return InsureStorage().ConvertXmlToObject(s);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal object ConvertXmlToObject(XmlReader xmlReader, XmlRootAttribute xmlAttrib)
	{
		return InsureStorage().ConvertXmlToObject(xmlReader, xmlAttrib);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal string ConvertObjectToXml(object value)
	{
		return InsureStorage().ConvertObjectToXml(value);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void ConvertObjectToXml(object value, XmlWriter xmlWriter, XmlRootAttribute xmlAttrib)
	{
		InsureStorage().ConvertObjectToXml(value, xmlWriter, xmlAttrib);
	}

	internal object GetEmptyColumnStore(int recordCount)
	{
		return InsureStorage().GetEmptyStorageInternal(recordCount);
	}

	internal void CopyValueIntoStore(int record, object store, BitArray nullbits, int storeIndex)
	{
		_storage.CopyValueInternal(record, store, nullbits, storeIndex);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void SetStorage(object store, BitArray nullbits)
	{
		InsureStorage().SetStorageInternal(store, nullbits);
	}

	internal void AddDependentColumn(DataColumn expressionColumn)
	{
		if (_dependentColumns == null)
		{
			_dependentColumns = new List<DataColumn>();
		}
		_dependentColumns.Add(expressionColumn);
		_table.AddDependentColumn(expressionColumn);
	}

	internal void RemoveDependentColumn(DataColumn expressionColumn)
	{
		if (_dependentColumns != null && _dependentColumns.Contains(expressionColumn))
		{
			_dependentColumns.Remove(expressionColumn);
		}
		_table.RemoveDependentColumn(expressionColumn);
	}

	internal void HandleDependentColumnList(DataExpression oldExpression, DataExpression newExpression)
	{
		DataColumn[] dependency;
		if (oldExpression != null)
		{
			dependency = oldExpression.GetDependency();
			DataColumn[] array = dependency;
			foreach (DataColumn dataColumn in array)
			{
				dataColumn.RemoveDependentColumn(this);
				if (dataColumn._table != _table)
				{
					_table.RemoveDependentColumn(this);
				}
			}
			_table.RemoveDependentColumn(this);
		}
		if (newExpression == null)
		{
			return;
		}
		dependency = newExpression.GetDependency();
		DataColumn[] array2 = dependency;
		foreach (DataColumn dataColumn2 in array2)
		{
			dataColumn2.AddDependentColumn(this);
			if (dataColumn2._table != _table)
			{
				_table.AddDependentColumn(this);
			}
		}
		_table.AddDependentColumn(this);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "User has already got warning when creating original column.")]
	internal void CopyExpressionFrom(DataColumn source)
	{
		Expression = source.Expression;
	}
}
