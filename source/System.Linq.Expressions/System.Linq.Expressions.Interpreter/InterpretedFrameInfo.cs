namespace System.Linq.Expressions.Interpreter;

internal readonly struct InterpretedFrameInfo
{
	private readonly string _methodName;

	private readonly DebugInfo _debugInfo;

	public InterpretedFrameInfo(string methodName, DebugInfo info)
	{
		_methodName = methodName;
		_debugInfo = info;
	}

	public override string ToString()
	{
		if (_debugInfo == null)
		{
			return _methodName;
		}
		return _methodName + ": " + _debugInfo;
	}
}
