using System;
using System.Collections.Generic;

namespace ReLogic.Localization.IME;

public abstract class PlatformIme : IImeService, IDisposable
{
	private readonly List<Action<char>> _keyPressCallbacks = new List<Action<char>>();

	private bool _disposedValue;

	public abstract string CompositionString { get; }

	public abstract bool IsCandidateListVisible { get; }

	public abstract uint SelectedCandidate { get; }

	public abstract uint CandidateCount { get; }

	public bool IsEnabled { get; private set; }

	protected PlatformIme()
	{
		IsEnabled = false;
	}

	public void AddKeyListener(Action<char> listener)
	{
		_keyPressCallbacks.Add(listener);
	}

	public void RemoveKeyListener(Action<char> listener)
	{
		_keyPressCallbacks.Remove(listener);
	}

	protected void OnKeyPress(char character)
	{
		foreach (Action<char> keyPressCallback in _keyPressCallbacks)
		{
			keyPressCallback(character);
		}
	}

	public abstract string GetCandidate(uint index);

	public void Enable()
	{
		if (!IsEnabled)
		{
			OnEnable();
			IsEnabled = true;
		}
	}

	public void Disable()
	{
		if (IsEnabled)
		{
			OnDisable();
			IsEnabled = false;
		}
	}

	protected virtual void OnEnable()
	{
	}

	protected virtual void OnDisable()
	{
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			_disposedValue = true;
		}
	}

	~PlatformIme()
	{
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
