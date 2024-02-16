using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Data;

[DefaultProperty("ConstraintName")]
[TypeConverter(typeof(ConstraintConverter))]
public abstract class Constraint
{
	private string _schemaName = string.Empty;

	private bool _inCollection;

	private DataSet _dataSet;

	internal string _name = string.Empty;

	internal PropertyCollection _extendedProperties;

	[DefaultValue("")]
	public virtual string ConstraintName
	{
		get
		{
			return _name;
		}
		[param: AllowNull]
		set
		{
			if (value == null)
			{
				value = string.Empty;
			}
			if (string.IsNullOrEmpty(value) && Table != null && InCollection)
			{
				throw ExceptionBuilder.NoConstraintName();
			}
			CultureInfo culture = ((Table != null) ? Table.Locale : CultureInfo.CurrentCulture);
			if (string.Compare(_name, value, ignoreCase: true, culture) != 0)
			{
				if (Table != null && InCollection)
				{
					Table.Constraints.RegisterName(value);
					if (_name.Length != 0)
					{
						Table.Constraints.UnregisterName(_name);
					}
				}
				_name = value;
			}
			else if (string.Compare(_name, value, ignoreCase: false, culture) != 0)
			{
				_name = value;
			}
		}
	}

	internal string SchemaName
	{
		get
		{
			if (!string.IsNullOrEmpty(_schemaName))
			{
				return _schemaName;
			}
			return ConstraintName;
		}
		set
		{
			if (!string.IsNullOrEmpty(value))
			{
				_schemaName = value;
			}
		}
	}

	internal virtual bool InCollection
	{
		get
		{
			return _inCollection;
		}
		set
		{
			_inCollection = value;
			_dataSet = (value ? Table.DataSet : null);
		}
	}

	public abstract DataTable? Table { get; }

	[Browsable(false)]
	public PropertyCollection ExtendedProperties => _extendedProperties ?? (_extendedProperties = new PropertyCollection());

	[CLSCompliant(false)]
	protected virtual DataSet? _DataSet => _dataSet;

	internal Constraint()
	{
	}

	internal abstract bool ContainsColumn(DataColumn column);

	internal abstract bool CanEnableConstraint();

	internal abstract Constraint Clone(DataSet destination);

	internal abstract Constraint Clone(DataSet destination, bool ignoreNSforTableLookup);

	internal void CheckConstraint()
	{
		if (!CanEnableConstraint())
		{
			throw ExceptionBuilder.ConstraintViolation(ConstraintName);
		}
	}

	internal abstract void CheckCanAddToCollection(ConstraintCollection constraint);

	internal abstract bool CanBeRemovedFromCollection(ConstraintCollection constraint, bool fThrowException);

	internal abstract void CheckConstraint(DataRow row, DataRowAction action);

	internal abstract void CheckState();

	protected void CheckStateForProperty()
	{
		try
		{
			CheckState();
		}
		catch (Exception ex) when (ADP.IsCatchableExceptionType(ex))
		{
			throw ExceptionBuilder.BadObjectPropertyAccess(ex.Message);
		}
	}

	protected internal void SetDataSet(DataSet dataSet)
	{
		_dataSet = dataSet;
	}

	internal abstract bool IsConstraintViolated();

	public override string ToString()
	{
		return ConstraintName;
	}
}
