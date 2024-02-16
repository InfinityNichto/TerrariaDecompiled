using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace System.Data;

internal sealed class TypeLimiter
{
	private sealed class Scope : IDisposable
	{
		private static readonly HashSet<Type> s_allowedTypes = new HashSet<Type>
		{
			typeof(bool),
			typeof(char),
			typeof(sbyte),
			typeof(byte),
			typeof(short),
			typeof(ushort),
			typeof(int),
			typeof(uint),
			typeof(long),
			typeof(ulong),
			typeof(float),
			typeof(double),
			typeof(decimal),
			typeof(DateTime),
			typeof(DateTimeOffset),
			typeof(TimeSpan),
			typeof(string),
			typeof(Guid),
			typeof(SqlBinary),
			typeof(SqlBoolean),
			typeof(SqlByte),
			typeof(SqlBytes),
			typeof(SqlChars),
			typeof(SqlDateTime),
			typeof(SqlDecimal),
			typeof(SqlDouble),
			typeof(SqlGuid),
			typeof(SqlInt16),
			typeof(SqlInt32),
			typeof(SqlInt64),
			typeof(SqlMoney),
			typeof(SqlSingle),
			typeof(SqlString),
			typeof(object),
			typeof(Type),
			typeof(BigInteger),
			typeof(Uri),
			typeof(Color),
			typeof(Point),
			typeof(PointF),
			typeof(Rectangle),
			typeof(RectangleF),
			typeof(Size),
			typeof(SizeF)
		};

		private HashSet<Type> m_allowedTypes;

		private readonly Scope m_previousScope;

		private readonly DeserializationToken m_deserializationToken;

		internal Scope(Scope previousScope, IEnumerable<Type> allowedTypes)
		{
			m_previousScope = previousScope;
			m_allowedTypes = new HashSet<Type>(allowedTypes.Where((Type type) => type != null));
			m_deserializationToken = SerializationInfo.StartDeserialization();
		}

		public void Dispose()
		{
			if (this != s_activeScope)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}
			m_deserializationToken.Dispose();
			s_activeScope = m_previousScope;
		}

		public bool IsAllowedType(Type type)
		{
			if (IsTypeUnconditionallyAllowed(type))
			{
				return true;
			}
			for (Scope scope = this; scope != null; scope = scope.m_previousScope)
			{
				if (scope.m_allowedTypes.Contains(type))
				{
					return true;
				}
			}
			Type[] array = (Type[])AppDomain.CurrentDomain.GetData("System.Data.DataSetDefaultAllowedTypes");
			if (array != null)
			{
				for (int i = 0; i < array.Length; i++)
				{
					if (type == array[i])
					{
						return true;
					}
				}
			}
			return false;
		}

		private static bool IsTypeUnconditionallyAllowed(Type type)
		{
			while (true)
			{
				if (s_allowedTypes.Contains(type))
				{
					return true;
				}
				if (type.IsEnum)
				{
					return true;
				}
				if (type.IsSZArray)
				{
					type = type.GetElementType();
					continue;
				}
				if (!type.IsGenericType || type.IsGenericTypeDefinition || !(type.GetGenericTypeDefinition() == typeof(List<>)))
				{
					break;
				}
				type = type.GetGenericArguments()[0];
			}
			return false;
		}
	}

	[ThreadStatic]
	private static Scope s_activeScope;

	private Scope m_instanceScope;

	private static bool IsTypeLimitingDisabled => System.LocalAppContextSwitches.AllowArbitraryTypeInstantiation;

	private TypeLimiter(Scope scope)
	{
		m_instanceScope = scope;
	}

	public static TypeLimiter Capture()
	{
		Scope scope = s_activeScope;
		if (scope == null)
		{
			return null;
		}
		return new TypeLimiter(scope);
	}

	public static void EnsureTypeIsAllowed(Type type, TypeLimiter capturedLimiter = null)
	{
		if ((object)type != null)
		{
			Scope scope = capturedLimiter?.m_instanceScope ?? s_activeScope;
			if (scope != null && !scope.IsAllowedType(type))
			{
				throw ExceptionBuilder.TypeNotAllowed(type);
			}
		}
	}

	public static IDisposable EnterRestrictedScope(DataSet dataSet)
	{
		if (IsTypeLimitingDisabled)
		{
			return null;
		}
		return s_activeScope = new Scope(s_activeScope, GetPreviouslyDeclaredDataTypes(dataSet));
	}

	public static IDisposable EnterRestrictedScope(DataTable dataTable)
	{
		if (IsTypeLimitingDisabled)
		{
			return null;
		}
		return s_activeScope = new Scope(s_activeScope, GetPreviouslyDeclaredDataTypes(dataTable));
	}

	private static IEnumerable<Type> GetPreviouslyDeclaredDataTypes(DataTable dataTable)
	{
		if (dataTable == null)
		{
			return Enumerable.Empty<Type>();
		}
		return from DataColumn column in dataTable.Columns
			select column.DataType;
	}

	private static IEnumerable<Type> GetPreviouslyDeclaredDataTypes(DataSet dataSet)
	{
		if (dataSet == null)
		{
			return Enumerable.Empty<Type>();
		}
		return dataSet.Tables.Cast<DataTable>().SelectMany((DataTable table) => GetPreviouslyDeclaredDataTypes(table));
	}
}
