using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Diagnostics;

public class StackFrame
{
	private MethodBase _method;

	private int _nativeOffset;

	private int _ilOffset;

	private string _fileName;

	private int _lineNumber;

	private int _columnNumber;

	private bool _isLastFrameFromForeignExceptionStackTrace;

	public const int OFFSET_UNKNOWN = -1;

	internal bool IsLastFrameFromForeignExceptionStackTrace => _isLastFrameFromForeignExceptionStackTrace;

	internal StackFrame(StackFrameHelper stackFrameHelper, int skipFrames, bool needFileInfo)
	{
		_method = stackFrameHelper.GetMethodBase(skipFrames);
		_nativeOffset = stackFrameHelper.GetOffset(skipFrames);
		_ilOffset = stackFrameHelper.GetILOffset(skipFrames);
		_isLastFrameFromForeignExceptionStackTrace = stackFrameHelper.IsLastFrameFromForeignExceptionStackTrace(skipFrames);
		if (needFileInfo)
		{
			_fileName = stackFrameHelper.GetFilename(skipFrames);
			_lineNumber = stackFrameHelper.GetLineNumber(skipFrames);
			_columnNumber = stackFrameHelper.GetColumnNumber(skipFrames);
		}
	}

	private void BuildStackFrame(int skipFrames, bool needFileInfo)
	{
		StackFrameHelper stackFrameHelper = new StackFrameHelper(null);
		stackFrameHelper.InitializeSourceInfo(0, needFileInfo, null);
		int numberOfFrames = stackFrameHelper.GetNumberOfFrames();
		skipFrames += StackTrace.CalculateFramesToSkip(stackFrameHelper, numberOfFrames);
		if (numberOfFrames - skipFrames > 0)
		{
			_method = stackFrameHelper.GetMethodBase(skipFrames);
			_nativeOffset = stackFrameHelper.GetOffset(skipFrames);
			_ilOffset = stackFrameHelper.GetILOffset(skipFrames);
			if (needFileInfo)
			{
				_fileName = stackFrameHelper.GetFilename(skipFrames);
				_lineNumber = stackFrameHelper.GetLineNumber(skipFrames);
				_columnNumber = stackFrameHelper.GetColumnNumber(skipFrames);
			}
		}
	}

	private static bool AppendStackFrameWithoutMethodBase(StringBuilder sb)
	{
		return false;
	}

	private void InitMembers()
	{
		_nativeOffset = -1;
		_ilOffset = -1;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public StackFrame()
	{
		InitMembers();
		BuildStackFrame(0, needFileInfo: false);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public StackFrame(bool needFileInfo)
	{
		InitMembers();
		BuildStackFrame(0, needFileInfo);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public StackFrame(int skipFrames)
	{
		InitMembers();
		BuildStackFrame(skipFrames, needFileInfo: false);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public StackFrame(int skipFrames, bool needFileInfo)
	{
		InitMembers();
		BuildStackFrame(skipFrames, needFileInfo);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public StackFrame(string? fileName, int lineNumber)
	{
		InitMembers();
		BuildStackFrame(0, needFileInfo: false);
		_fileName = fileName;
		_lineNumber = lineNumber;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public StackFrame(string? fileName, int lineNumber, int colNumber)
	{
		InitMembers();
		BuildStackFrame(0, needFileInfo: false);
		_fileName = fileName;
		_lineNumber = lineNumber;
		_columnNumber = colNumber;
	}

	public virtual MethodBase? GetMethod()
	{
		return _method;
	}

	public virtual int GetNativeOffset()
	{
		return _nativeOffset;
	}

	public virtual int GetILOffset()
	{
		return _ilOffset;
	}

	public virtual string? GetFileName()
	{
		return _fileName;
	}

	public virtual int GetFileLineNumber()
	{
		return _lineNumber;
	}

	public virtual int GetFileColumnNumber()
	{
		return _columnNumber;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder(255);
		bool flag2;
		if (_method != null)
		{
			stringBuilder.Append(_method.Name);
			if (_method is MethodInfo { IsGenericMethod: not false } methodInfo)
			{
				Type[] genericArguments = methodInfo.GetGenericArguments();
				stringBuilder.Append('<');
				int i = 0;
				bool flag = true;
				for (; i < genericArguments.Length; i++)
				{
					if (!flag)
					{
						stringBuilder.Append(',');
					}
					else
					{
						flag = false;
					}
					stringBuilder.Append(genericArguments[i].Name);
				}
				stringBuilder.Append('>');
			}
			flag2 = true;
		}
		else
		{
			flag2 = AppendStackFrameWithoutMethodBase(stringBuilder);
		}
		if (flag2)
		{
			stringBuilder.Append(" at offset ");
			if (_nativeOffset == -1)
			{
				stringBuilder.Append("<offset unknown>");
			}
			else
			{
				stringBuilder.Append(_nativeOffset);
			}
			stringBuilder.Append(" in file:line:column ");
			stringBuilder.Append(_fileName ?? "<filename unknown>");
			stringBuilder.Append(':');
			stringBuilder.Append(_lineNumber);
			stringBuilder.Append(':');
			stringBuilder.Append(_columnNumber);
		}
		else
		{
			stringBuilder.Append("<null>");
		}
		stringBuilder.AppendLine();
		return stringBuilder.ToString();
	}
}
