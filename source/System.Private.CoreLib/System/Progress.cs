using System.Threading;

namespace System;

public class Progress<T> : IProgress<T>
{
	private readonly SynchronizationContext _synchronizationContext;

	private readonly Action<T> _handler;

	private readonly SendOrPostCallback _invokeHandlers;

	public event EventHandler<T>? ProgressChanged;

	public Progress()
	{
		_synchronizationContext = SynchronizationContext.Current ?? ProgressStatics.DefaultContext;
		_invokeHandlers = InvokeHandlers;
	}

	public Progress(Action<T> handler)
		: this()
	{
		_handler = handler ?? throw new ArgumentNullException("handler");
	}

	protected virtual void OnReport(T value)
	{
		Action<T> handler = _handler;
		EventHandler<T> progressChanged = this.ProgressChanged;
		if (handler != null || progressChanged != null)
		{
			_synchronizationContext.Post(_invokeHandlers, value);
		}
	}

	void IProgress<T>.Report(T value)
	{
		OnReport(value);
	}

	private void InvokeHandlers(object state)
	{
		T val = (T)state;
		Action<T> handler = _handler;
		EventHandler<T> progressChanged = this.ProgressChanged;
		handler?.Invoke(val);
		progressChanged?.Invoke(this, val);
	}
}
