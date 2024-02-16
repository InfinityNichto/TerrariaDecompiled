using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;

namespace System.Xml.Serialization;

internal sealed class LocalScope
{
	public readonly LocalScope parent;

	private readonly Dictionary<string, LocalBuilder> _locals;

	public LocalBuilder this[string key]
	{
		get
		{
			TryGetValue(key, out var value);
			return value;
		}
		[param: DisallowNull]
		set
		{
			_locals[key] = value;
		}
	}

	public LocalScope()
	{
		_locals = new Dictionary<string, LocalBuilder>();
	}

	public LocalScope(LocalScope parent)
		: this()
	{
		this.parent = parent;
	}

	public bool TryGetValue(string key, [NotNullWhen(true)] out LocalBuilder value)
	{
		if (_locals.TryGetValue(key, out value))
		{
			return true;
		}
		if (parent != null)
		{
			return parent.TryGetValue(key, out value);
		}
		value = null;
		return false;
	}

	public void AddToFreeLocals(Dictionary<(Type, string), Queue<LocalBuilder>> freeLocals)
	{
		foreach (KeyValuePair<string, LocalBuilder> local in _locals)
		{
			(Type, string) key = (local.Value.LocalType, local.Key);
			if (freeLocals.TryGetValue(key, out var value))
			{
				value.Enqueue(local.Value);
				continue;
			}
			value = new Queue<LocalBuilder>();
			value.Enqueue(local.Value);
			freeLocals.Add(key, value);
		}
	}
}
