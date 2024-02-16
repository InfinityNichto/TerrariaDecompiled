using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;

namespace System.Data;

internal sealed class LikeNode : BinaryNode
{
	private static readonly char[] s_trimChars = new char[2] { ' ', '\u3000' };

	private int _kind;

	private string _pattern;

	internal LikeNode(DataTable table, int op, ExpressionNode left, ExpressionNode right)
		: base(table, op, left, right)
	{
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal override object Eval(DataRow row, DataRowVersion version)
	{
		object obj = _left.Eval(row, version);
		if (obj == DBNull.Value || (_left.IsSqlColumn && DataStorage.IsObjectSqlNull(obj)))
		{
			return DBNull.Value;
		}
		string text;
		if (_pattern == null)
		{
			object obj2 = _right.Eval(row, version);
			if (!(obj2 is string) && !(obj2 is SqlString))
			{
				SetTypeMismatchError(_op, obj.GetType(), obj2.GetType());
			}
			if (obj2 == DBNull.Value || DataStorage.IsObjectSqlNull(obj2))
			{
				return DBNull.Value;
			}
			string pat = (string)SqlConvert.ChangeType2(obj2, StorageType.String, typeof(string), base.FormatProvider);
			text = AnalyzePattern(pat);
			if (_right.IsConstant())
			{
				_pattern = text;
			}
		}
		else
		{
			text = _pattern;
		}
		if (!(obj is string) && !(obj is SqlString))
		{
			SetTypeMismatchError(_op, obj.GetType(), typeof(string));
		}
		string text2 = ((obj is SqlString sqlString) ? sqlString.Value : ((string)obj));
		string s = text2.TrimEnd(s_trimChars);
		switch (_kind)
		{
		case 5:
			return true;
		case 4:
			return base.table.Compare(s, text) == 0;
		case 3:
			return 0 <= base.table.IndexOf(s, text);
		case 1:
			return base.table.IndexOf(s, text) == 0;
		case 2:
		{
			string s2 = text.TrimEnd(s_trimChars);
			return base.table.IsSuffix(s, s2);
		}
		default:
			return DBNull.Value;
		}
	}

	internal string AnalyzePattern(string pat)
	{
		int length = pat.Length;
		char[] array = new char[length + 1];
		pat.CopyTo(0, array, 0, length);
		array[length] = '\0';
		string text = null;
		char[] array2 = new char[length + 1];
		int num = 0;
		int num2 = 0;
		int i = 0;
		while (i < length)
		{
			if (array[i] == '*' || array[i] == '%')
			{
				for (; (array[i] == '*' || array[i] == '%') && i < length; i++)
				{
				}
				if ((i < length && num > 0) || num2 >= 2)
				{
					throw ExprException.InvalidPattern(pat);
				}
				num2++;
			}
			else if (array[i] == '[')
			{
				i++;
				if (i >= length)
				{
					throw ExprException.InvalidPattern(pat);
				}
				array2[num++] = array[i++];
				if (i >= length)
				{
					throw ExprException.InvalidPattern(pat);
				}
				if (array[i] != ']')
				{
					throw ExprException.InvalidPattern(pat);
				}
				i++;
			}
			else
			{
				array2[num++] = array[i];
				i++;
			}
		}
		text = new string(array2, 0, num);
		if (num2 == 0)
		{
			_kind = 4;
		}
		else if (num > 0)
		{
			if (array[0] == '*' || array[0] == '%')
			{
				if (array[length - 1] == '*' || array[length - 1] == '%')
				{
					_kind = 3;
				}
				else
				{
					_kind = 2;
				}
			}
			else
			{
				_kind = 1;
			}
		}
		else
		{
			_kind = 5;
		}
		return text;
	}
}
