using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System;

[DebuggerTypeProxy(typeof(LazyDebugView<>))]
[DebuggerDisplay("ThreadSafetyMode={Mode}, IsValueCreated={IsValueCreated}, IsValueFaulted={IsValueFaulted}, Value={ValueForDebugDisplay}")]
public class Lazy<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>
{
	private volatile LazyHelper _state;

	private Func<T> _factory;

	private T _value;

	internal T? ValueForDebugDisplay
	{
		get
		{
			if (!IsValueCreated)
			{
				return default(T);
			}
			return _value;
		}
	}

	internal LazyThreadSafetyMode? Mode => LazyHelper.GetMode(_state);

	internal bool IsValueFaulted => LazyHelper.GetIsValueFaulted(_state);

	public bool IsValueCreated => _state == null;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public T Value
	{
		get
		{
			if (_state != null)
			{
				return CreateValue();
			}
			return _value;
		}
	}

	private static T CreateViaDefaultConstructor()
	{
		return LazyHelper.CreateViaDefaultConstructor<T>();
	}

	public Lazy()
		: this((Func<T>)null, LazyThreadSafetyMode.ExecutionAndPublication, useDefaultConstructor: true)
	{
	}

	public Lazy(T value)
	{
		_value = value;
	}

	public Lazy(Func<T> valueFactory)
		: this(valueFactory, LazyThreadSafetyMode.ExecutionAndPublication, useDefaultConstructor: false)
	{
	}

	public Lazy(bool isThreadSafe)
		: this((Func<T>)null, LazyHelper.GetModeFromIsThreadSafe(isThreadSafe), useDefaultConstructor: true)
	{
	}

	public Lazy(LazyThreadSafetyMode mode)
		: this((Func<T>)null, mode, useDefaultConstructor: true)
	{
	}

	public Lazy(Func<T> valueFactory, bool isThreadSafe)
		: this(valueFactory, LazyHelper.GetModeFromIsThreadSafe(isThreadSafe), useDefaultConstructor: false)
	{
	}

	public Lazy(Func<T> valueFactory, LazyThreadSafetyMode mode)
		: this(valueFactory, mode, useDefaultConstructor: false)
	{
	}

	private Lazy(Func<T> valueFactory, LazyThreadSafetyMode mode, bool useDefaultConstructor)
	{
		if (valueFactory == null && !useDefaultConstructor)
		{
			throw new ArgumentNullException("valueFactory");
		}
		_factory = valueFactory;
		_state = LazyHelper.Create(mode, useDefaultConstructor);
	}

	private void ViaConstructor()
	{
		_value = CreateViaDefaultConstructor();
		_state = null;
	}

	private void ViaFactory(LazyThreadSafetyMode mode)
	{
		try
		{
			Func<T> factory = _factory;
			if (factory == null)
			{
				throw new InvalidOperationException(SR.Lazy_Value_RecursiveCallsToValue);
			}
			_factory = null;
			_value = factory();
			_state = null;
		}
		catch (Exception exception)
		{
			_state = new LazyHelper(mode, exception);
			throw;
		}
	}

	private void ExecutionAndPublication(LazyHelper executionAndPublication, bool useDefaultConstructor)
	{
		lock (executionAndPublication)
		{
			if (_state == executionAndPublication)
			{
				if (useDefaultConstructor)
				{
					ViaConstructor();
				}
				else
				{
					ViaFactory(LazyThreadSafetyMode.ExecutionAndPublication);
				}
			}
		}
	}

	private void PublicationOnly(LazyHelper publicationOnly, T possibleValue)
	{
		LazyHelper lazyHelper = Interlocked.CompareExchange(ref _state, LazyHelper.PublicationOnlyWaitForOtherThreadToPublish, publicationOnly);
		if (lazyHelper == publicationOnly)
		{
			_factory = null;
			_value = possibleValue;
			_state = null;
		}
	}

	private void PublicationOnlyViaConstructor(LazyHelper initializer)
	{
		PublicationOnly(initializer, CreateViaDefaultConstructor());
	}

	private void PublicationOnlyViaFactory(LazyHelper initializer)
	{
		Func<T> factory = _factory;
		if (factory == null)
		{
			PublicationOnlyWaitForOtherThreadToPublish();
		}
		else
		{
			PublicationOnly(initializer, factory());
		}
	}

	private void PublicationOnlyWaitForOtherThreadToPublish()
	{
		SpinWait spinWait = default(SpinWait);
		while (_state != null)
		{
			spinWait.SpinOnce();
		}
	}

	private T CreateValue()
	{
		LazyHelper state = _state;
		if (state != null)
		{
			switch (state.State)
			{
			case LazyState.NoneViaConstructor:
				ViaConstructor();
				break;
			case LazyState.NoneViaFactory:
				ViaFactory(LazyThreadSafetyMode.None);
				break;
			case LazyState.PublicationOnlyViaConstructor:
				PublicationOnlyViaConstructor(state);
				break;
			case LazyState.PublicationOnlyViaFactory:
				PublicationOnlyViaFactory(state);
				break;
			case LazyState.PublicationOnlyWait:
				PublicationOnlyWaitForOtherThreadToPublish();
				break;
			case LazyState.ExecutionAndPublicationViaConstructor:
				ExecutionAndPublication(state, useDefaultConstructor: true);
				break;
			case LazyState.ExecutionAndPublicationViaFactory:
				ExecutionAndPublication(state, useDefaultConstructor: false);
				break;
			default:
				state.ThrowException();
				break;
			}
		}
		return Value;
	}

	public override string? ToString()
	{
		if (!IsValueCreated)
		{
			return SR.Lazy_ToString_ValueNotCreated;
		}
		return Value.ToString();
	}
}
public class Lazy<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T, TMetadata> : Lazy<T>
{
	private readonly TMetadata _metadata;

	public TMetadata Metadata => _metadata;

	public Lazy(Func<T> valueFactory, TMetadata metadata)
		: base(valueFactory)
	{
		_metadata = metadata;
	}

	public Lazy(TMetadata metadata)
	{
		_metadata = metadata;
	}

	public Lazy(TMetadata metadata, bool isThreadSafe)
		: base(isThreadSafe)
	{
		_metadata = metadata;
	}

	public Lazy(Func<T> valueFactory, TMetadata metadata, bool isThreadSafe)
		: base(valueFactory, isThreadSafe)
	{
		_metadata = metadata;
	}

	public Lazy(TMetadata metadata, LazyThreadSafetyMode mode)
		: base(mode)
	{
		_metadata = metadata;
	}

	public Lazy(Func<T> valueFactory, TMetadata metadata, LazyThreadSafetyMode mode)
		: base(valueFactory, mode)
	{
		_metadata = metadata;
	}
}
