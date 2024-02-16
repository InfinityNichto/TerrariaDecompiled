using System.Collections;
using System.Collections.Generic;

namespace System.Linq.Expressions.Compiler;

internal sealed class ParameterList : IReadOnlyList<ParameterExpression>, IEnumerable<ParameterExpression>, IEnumerable, IReadOnlyCollection<ParameterExpression>
{
	private readonly IParameterProvider _provider;

	public ParameterExpression this[int index] => _provider.GetParameter(index);

	public int Count => _provider.ParameterCount;

	public ParameterList(IParameterProvider provider)
	{
		_provider = provider;
	}

	public IEnumerator<ParameterExpression> GetEnumerator()
	{
		int i = 0;
		for (int j = _provider.ParameterCount; i < j; i++)
		{
			yield return _provider.GetParameter(i);
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
