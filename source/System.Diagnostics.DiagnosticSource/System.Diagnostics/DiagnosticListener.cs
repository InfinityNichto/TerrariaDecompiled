using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Diagnostics;

public class DiagnosticListener : DiagnosticSource, IObservable<KeyValuePair<string, object?>>, IDisposable
{
	private sealed class DiagnosticSubscription : IDisposable
	{
		internal IObserver<KeyValuePair<string, object>> Observer;

		internal Predicate<string> IsEnabled1Arg;

		internal Func<string, object, object, bool> IsEnabled3Arg;

		internal Action<Activity, object> OnActivityImport;

		internal Action<Activity, object> OnActivityExport;

		internal DiagnosticListener Owner;

		internal DiagnosticSubscription Next;

		public void Dispose()
		{
			DiagnosticSubscription subscriptions;
			DiagnosticSubscription value;
			do
			{
				subscriptions = Owner._subscriptions;
				value = Remove(subscriptions, this);
			}
			while (Interlocked.CompareExchange(ref Owner._subscriptions, value, subscriptions) != subscriptions);
		}

		private static DiagnosticSubscription Remove(DiagnosticSubscription subscriptions, DiagnosticSubscription subscription)
		{
			if (subscriptions == null)
			{
				return null;
			}
			if (subscriptions.Observer == subscription.Observer && subscriptions.IsEnabled1Arg == subscription.IsEnabled1Arg && subscriptions.IsEnabled3Arg == subscription.IsEnabled3Arg)
			{
				return subscriptions.Next;
			}
			return new DiagnosticSubscription
			{
				Observer = subscriptions.Observer,
				Owner = subscriptions.Owner,
				IsEnabled1Arg = subscriptions.IsEnabled1Arg,
				IsEnabled3Arg = subscriptions.IsEnabled3Arg,
				Next = Remove(subscriptions.Next, subscription)
			};
		}
	}

	private sealed class AllListenerObservable : IObservable<DiagnosticListener>
	{
		internal sealed class AllListenerSubscription : IDisposable
		{
			private readonly AllListenerObservable _owner;

			internal readonly IObserver<DiagnosticListener> Subscriber;

			internal AllListenerSubscription Next;

			internal AllListenerSubscription(AllListenerObservable owner, IObserver<DiagnosticListener> subscriber, AllListenerSubscription next)
			{
				_owner = owner;
				Subscriber = subscriber;
				Next = next;
			}

			public void Dispose()
			{
				if (_owner.Remove(this))
				{
					Subscriber.OnCompleted();
				}
			}
		}

		private AllListenerSubscription _subscriptions;

		public IDisposable Subscribe(IObserver<DiagnosticListener> observer)
		{
			lock (s_allListenersLock)
			{
				for (DiagnosticListener diagnosticListener = s_allListeners; diagnosticListener != null; diagnosticListener = diagnosticListener._next)
				{
					observer.OnNext(diagnosticListener);
				}
				_subscriptions = new AllListenerSubscription(this, observer, _subscriptions);
				return _subscriptions;
			}
		}

		internal void OnNewDiagnosticListener(DiagnosticListener diagnosticListener)
		{
			for (AllListenerSubscription allListenerSubscription = _subscriptions; allListenerSubscription != null; allListenerSubscription = allListenerSubscription.Next)
			{
				allListenerSubscription.Subscriber.OnNext(diagnosticListener);
			}
		}

		private bool Remove(AllListenerSubscription subscription)
		{
			lock (s_allListenersLock)
			{
				if (_subscriptions == subscription)
				{
					_subscriptions = subscription.Next;
					return true;
				}
				if (_subscriptions != null)
				{
					AllListenerSubscription allListenerSubscription = _subscriptions;
					while (allListenerSubscription.Next != null)
					{
						if (allListenerSubscription.Next == subscription)
						{
							allListenerSubscription.Next = allListenerSubscription.Next.Next;
							return true;
						}
						allListenerSubscription = allListenerSubscription.Next;
					}
				}
				return false;
			}
		}
	}

	private volatile DiagnosticSubscription _subscriptions;

	private DiagnosticListener _next;

	private bool _disposed;

	private static DiagnosticListener s_allListeners;

	private static volatile AllListenerObservable s_allListenerObservable;

	private static readonly object s_allListenersLock = new object();

	public static IObservable<DiagnosticListener> AllListeners => s_allListenerObservable ?? Interlocked.CompareExchange(ref s_allListenerObservable, new AllListenerObservable(), null) ?? s_allListenerObservable;

	public string Name { get; private set; }

	public virtual IDisposable Subscribe(IObserver<KeyValuePair<string, object?>> observer, Predicate<string>? isEnabled)
	{
		if (isEnabled == null)
		{
			return SubscribeInternal(observer, null, null, null, null);
		}
		Predicate<string> localIsEnabled = isEnabled;
		return SubscribeInternal(observer, isEnabled, (string name, object arg1, object arg2) => localIsEnabled(name), null, null);
	}

	public virtual IDisposable Subscribe(IObserver<KeyValuePair<string, object?>> observer, Func<string, object?, object?, bool>? isEnabled)
	{
		if (isEnabled != null)
		{
			return SubscribeInternal(observer, (string name) => IsEnabled(name, null), isEnabled, null, null);
		}
		return SubscribeInternal(observer, null, null, null, null);
	}

	public virtual IDisposable Subscribe(IObserver<KeyValuePair<string, object?>> observer)
	{
		return SubscribeInternal(observer, null, null, null, null);
	}

	public DiagnosticListener(string name)
	{
		Name = name;
		lock (s_allListenersLock)
		{
			s_allListenerObservable?.OnNewDiagnosticListener(this);
			_next = s_allListeners;
			s_allListeners = this;
		}
		GC.KeepAlive(DiagnosticSourceEventSource.Log);
	}

	public virtual void Dispose()
	{
		lock (s_allListenersLock)
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
			if (s_allListeners == this)
			{
				s_allListeners = s_allListeners._next;
			}
			else
			{
				for (DiagnosticListener next = s_allListeners; next != null; next = next._next)
				{
					if (next._next == this)
					{
						next._next = _next;
						break;
					}
				}
			}
			_next = null;
		}
		DiagnosticSubscription location = null;
		Interlocked.Exchange(ref location, _subscriptions);
		while (location != null)
		{
			location.Observer.OnCompleted();
			location = location.Next;
		}
	}

	public override string ToString()
	{
		return Name ?? string.Empty;
	}

	public bool IsEnabled()
	{
		return _subscriptions != null;
	}

	public override bool IsEnabled(string name)
	{
		for (DiagnosticSubscription diagnosticSubscription = _subscriptions; diagnosticSubscription != null; diagnosticSubscription = diagnosticSubscription.Next)
		{
			if (diagnosticSubscription.IsEnabled1Arg == null || diagnosticSubscription.IsEnabled1Arg(name))
			{
				return true;
			}
		}
		return false;
	}

	public override bool IsEnabled(string name, object? arg1, object? arg2 = null)
	{
		for (DiagnosticSubscription diagnosticSubscription = _subscriptions; diagnosticSubscription != null; diagnosticSubscription = diagnosticSubscription.Next)
		{
			if (diagnosticSubscription.IsEnabled3Arg == null || diagnosticSubscription.IsEnabled3Arg(name, arg1, arg2))
			{
				return true;
			}
		}
		return false;
	}

	[RequiresUnreferencedCode("The type of object being written to DiagnosticSource cannot be discovered statically.")]
	public override void Write(string name, object? value)
	{
		for (DiagnosticSubscription diagnosticSubscription = _subscriptions; diagnosticSubscription != null; diagnosticSubscription = diagnosticSubscription.Next)
		{
			diagnosticSubscription.Observer.OnNext(new KeyValuePair<string, object>(name, value));
		}
	}

	private IDisposable SubscribeInternal(IObserver<KeyValuePair<string, object>> observer, Predicate<string> isEnabled1Arg, Func<string, object, object, bool> isEnabled3Arg, Action<Activity, object> onActivityImport, Action<Activity, object> onActivityExport)
	{
		if (_disposed)
		{
			return new DiagnosticSubscription
			{
				Owner = this
			};
		}
		DiagnosticSubscription diagnosticSubscription = new DiagnosticSubscription
		{
			Observer = observer,
			IsEnabled1Arg = isEnabled1Arg,
			IsEnabled3Arg = isEnabled3Arg,
			OnActivityImport = onActivityImport,
			OnActivityExport = onActivityExport,
			Owner = this,
			Next = _subscriptions
		};
		while (Interlocked.CompareExchange(ref _subscriptions, diagnosticSubscription, diagnosticSubscription.Next) != diagnosticSubscription.Next)
		{
			diagnosticSubscription.Next = _subscriptions;
		}
		return diagnosticSubscription;
	}

	public override void OnActivityImport(Activity activity, object? payload)
	{
		for (DiagnosticSubscription diagnosticSubscription = _subscriptions; diagnosticSubscription != null; diagnosticSubscription = diagnosticSubscription.Next)
		{
			diagnosticSubscription.OnActivityImport?.Invoke(activity, payload);
		}
	}

	public override void OnActivityExport(Activity activity, object? payload)
	{
		for (DiagnosticSubscription diagnosticSubscription = _subscriptions; diagnosticSubscription != null; diagnosticSubscription = diagnosticSubscription.Next)
		{
			diagnosticSubscription.OnActivityExport?.Invoke(activity, payload);
		}
	}

	public virtual IDisposable Subscribe(IObserver<KeyValuePair<string, object?>> observer, Func<string, object?, object?, bool>? isEnabled, Action<Activity, object?>? onActivityImport = null, Action<Activity, object?>? onActivityExport = null)
	{
		if (isEnabled != null)
		{
			return SubscribeInternal(observer, (string name) => IsEnabled(name, null), isEnabled, onActivityImport, onActivityExport);
		}
		return SubscribeInternal(observer, null, null, onActivityImport, onActivityExport);
	}
}
