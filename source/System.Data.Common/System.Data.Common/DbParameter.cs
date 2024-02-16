using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.Common;

public abstract class DbParameter : MarshalByRefObject, IDbDataParameter, IDataParameter
{
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[RefreshProperties(RefreshProperties.All)]
	public abstract DbType DbType { get; set; }

	[DefaultValue(ParameterDirection.Input)]
	[RefreshProperties(RefreshProperties.All)]
	public abstract ParameterDirection Direction { get; set; }

	[Browsable(false)]
	[DesignOnly(true)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public abstract bool IsNullable { get; set; }

	[DefaultValue("")]
	public abstract string ParameterName
	{
		get; [param: AllowNull]
		set;
	}

	byte IDbDataParameter.Precision
	{
		get
		{
			return 0;
		}
		set
		{
		}
	}

	byte IDbDataParameter.Scale
	{
		get
		{
			return 0;
		}
		set
		{
		}
	}

	public virtual byte Precision
	{
		get
		{
			return ((IDbDataParameter)this).Precision;
		}
		set
		{
			((IDbDataParameter)this).Precision = value;
		}
	}

	public virtual byte Scale
	{
		get
		{
			return ((IDbDataParameter)this).Scale;
		}
		set
		{
			((IDbDataParameter)this).Scale = value;
		}
	}

	public abstract int Size { get; set; }

	[DefaultValue("")]
	public abstract string SourceColumn
	{
		get; [param: AllowNull]
		set;
	}

	[DefaultValue(false)]
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[RefreshProperties(RefreshProperties.All)]
	public abstract bool SourceColumnNullMapping { get; set; }

	[DefaultValue(DataRowVersion.Current)]
	public virtual DataRowVersion SourceVersion
	{
		get
		{
			return DataRowVersion.Default;
		}
		set
		{
		}
	}

	[DefaultValue(null)]
	[RefreshProperties(RefreshProperties.All)]
	public abstract object? Value { get; set; }

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public abstract void ResetDbType();
}
