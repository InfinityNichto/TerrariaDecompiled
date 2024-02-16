namespace System.ComponentModel.Design;

public abstract class DesignerTransaction : IDisposable
{
	private bool _suppressedFinalization;

	public bool Canceled { get; private set; }

	public bool Committed { get; private set; }

	public string Description { get; }

	protected DesignerTransaction()
		: this("")
	{
	}

	protected DesignerTransaction(string description)
	{
		Description = description;
	}

	public void Cancel()
	{
		if (!Canceled && !Committed)
		{
			Canceled = true;
			GC.SuppressFinalize(this);
			_suppressedFinalization = true;
			OnCancel();
		}
	}

	public void Commit()
	{
		if (!Committed && !Canceled)
		{
			Committed = true;
			GC.SuppressFinalize(this);
			_suppressedFinalization = true;
			OnCommit();
		}
	}

	protected abstract void OnCancel();

	protected abstract void OnCommit();

	~DesignerTransaction()
	{
		Dispose(disposing: false);
	}

	void IDisposable.Dispose()
	{
		Dispose(disposing: true);
		if (!_suppressedFinalization)
		{
			GC.SuppressFinalize(this);
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		Cancel();
	}
}
