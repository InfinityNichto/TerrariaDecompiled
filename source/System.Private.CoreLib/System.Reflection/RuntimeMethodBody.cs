using System.Collections.Generic;

namespace System.Reflection;

internal sealed class RuntimeMethodBody : MethodBody
{
	private byte[] _IL;

	private ExceptionHandlingClause[] _exceptionHandlingClauses;

	private LocalVariableInfo[] _localVariables;

	internal MethodBase _methodBase;

	private int _localSignatureMetadataToken;

	private int _maxStackSize;

	private bool _initLocals;

	public override int LocalSignatureMetadataToken => _localSignatureMetadataToken;

	public override IList<LocalVariableInfo> LocalVariables => Array.AsReadOnly(_localVariables);

	public override int MaxStackSize => _maxStackSize;

	public override bool InitLocals => _initLocals;

	public override IList<ExceptionHandlingClause> ExceptionHandlingClauses => Array.AsReadOnly(_exceptionHandlingClauses);

	private RuntimeMethodBody()
	{
		_IL = null;
		_exceptionHandlingClauses = null;
		_localVariables = null;
		_methodBase = null;
	}

	public override byte[] GetILAsByteArray()
	{
		return _IL;
	}
}
