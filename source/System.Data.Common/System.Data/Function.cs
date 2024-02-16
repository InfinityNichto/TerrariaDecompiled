namespace System.Data;

internal sealed class Function
{
	internal readonly string _name;

	internal readonly FunctionId _id;

	internal readonly Type _result;

	internal readonly bool _isValidateArguments;

	internal readonly bool _isVariantArgumentList;

	internal readonly int _argumentCount;

	internal readonly Type[] _parameters = new Type[3];

	internal static string[] s_functionName = new string[39]
	{
		"Unknown", "Ascii", "Char", "CharIndex", "Difference", "Len", "Lower", "LTrim", "Patindex", "Replicate",
		"Reverse", "Right", "RTrim", "Soundex", "Space", "Str", "Stuff", "Substring", "Upper", "IsNull",
		"Iif", "Convert", "cInt", "cBool", "cDate", "cDbl", "cStr", "Abs", "Acos", "In",
		"Trim", "Sum", "Avg", "Min", "Max", "Count", "StDev", "Var", "DateTimeOffset"
	};

	internal Function(string name, FunctionId id, Type result, bool IsValidateArguments, bool IsVariantArgumentList, int argumentCount, Type a1, Type a2, Type a3)
	{
		_name = name;
		_id = id;
		_result = result;
		_isValidateArguments = IsValidateArguments;
		_isVariantArgumentList = IsVariantArgumentList;
		_argumentCount = argumentCount;
		if (a1 != null)
		{
			_parameters[0] = a1;
		}
		if (a2 != null)
		{
			_parameters[1] = a2;
		}
		if (a3 != null)
		{
			_parameters[2] = a3;
		}
	}
}
