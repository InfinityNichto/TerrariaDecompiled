using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Threading;

public readonly struct CancellationTokenRegistration : IEquatable<CancellationTokenRegistration>, IDisposable, IAsyncDisposable
{
	private readonly long _id;

	private readonly CancellationTokenSource.CallbackNode _node;

	public CancellationToken Token
	{
		get
		{
			CancellationTokenSource.CallbackNode node = _node;
			if (node == null)
			{
				return default(CancellationToken);
			}
			return new CancellationToken(node.Registrations.Source);
		}
	}

	internal CancellationTokenRegistration(long id, CancellationTokenSource.CallbackNode node)
	{
		_id = id;
		_node = node;
	}

	public void Dispose()
	{
		CancellationTokenSource.CallbackNode node2 = _node;
		if (node2 != null && !node2.Registrations.Unregister(_id, node2))
		{
			WaitForCallbackIfNecessary(_id, node2);
		}
		static void WaitForCallbackIfNecessary(long id, CancellationTokenSource.CallbackNode node)
		{
			CancellationTokenSource source = node.Registrations.Source;
			if (source.IsCancellationRequested && !source.IsCancellationCompleted && node.Registrations.ThreadIDExecutingCallbacks != Environment.CurrentManagedThreadId)
			{
				node.Registrations.WaitForCallbackToComplete(id);
			}
		}
	}

	public ValueTask DisposeAsync()
	{
		CancellationTokenSource.CallbackNode node2 = _node;
		if (node2 == null || node2.Registrations.Unregister(_id, node2))
		{
			return default(ValueTask);
		}
		return WaitForCallbackIfNecessaryAsync(_id, node2);
		static ValueTask WaitForCallbackIfNecessaryAsync(long id, CancellationTokenSource.CallbackNode node)
		{
			CancellationTokenSource source = node.Registrations.Source;
			if (source.IsCancellationRequested && !source.IsCancellationCompleted && node.Registrations.ThreadIDExecutingCallbacks != Environment.CurrentManagedThreadId)
			{
				return node.Registrations.WaitForCallbackToCompleteAsync(id);
			}
			return default(ValueTask);
		}
	}

	public bool Unregister()
	{
		CancellationTokenSource.CallbackNode node = _node;
		return node?.Registrations.Unregister(_id, node) ?? false;
	}

	public static bool operator ==(CancellationTokenRegistration left, CancellationTokenRegistration right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(CancellationTokenRegistration left, CancellationTokenRegistration right)
	{
		return !left.Equals(right);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is CancellationTokenRegistration other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(CancellationTokenRegistration other)
	{
		if (_node == other._node)
		{
			return _id == other._id;
		}
		return false;
	}

	public override int GetHashCode()
	{
		if (_node == null)
		{
			return _id.GetHashCode();
		}
		return _node.GetHashCode() ^ _id.GetHashCode();
	}
}
