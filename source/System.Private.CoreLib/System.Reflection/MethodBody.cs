using System.Collections.Generic;

namespace System.Reflection;

public class MethodBody
{
	public virtual int LocalSignatureMetadataToken => 0;

	public virtual IList<LocalVariableInfo> LocalVariables
	{
		get
		{
			throw new ArgumentNullException("array");
		}
	}

	public virtual int MaxStackSize => 0;

	public virtual bool InitLocals => false;

	public virtual IList<ExceptionHandlingClause> ExceptionHandlingClauses
	{
		get
		{
			throw new ArgumentNullException("array");
		}
	}

	protected MethodBody()
	{
	}

	public virtual byte[]? GetILAsByteArray()
	{
		return null;
	}
}
