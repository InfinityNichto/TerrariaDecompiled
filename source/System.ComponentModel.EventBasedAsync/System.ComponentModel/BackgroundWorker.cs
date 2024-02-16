using System.Threading;
using System.Threading.Tasks;

namespace System.ComponentModel;

public class BackgroundWorker : Component
{
	private bool _canCancelWorker;

	private bool _workerReportsProgress;

	private bool _cancellationPending;

	private bool _isRunning;

	private AsyncOperation _asyncOperation;

	private readonly SendOrPostCallback _operationCompleted;

	private readonly SendOrPostCallback _progressReporter;

	public bool CancellationPending => _cancellationPending;

	public bool IsBusy => _isRunning;

	public bool WorkerReportsProgress
	{
		get
		{
			return _workerReportsProgress;
		}
		set
		{
			_workerReportsProgress = value;
		}
	}

	public bool WorkerSupportsCancellation
	{
		get
		{
			return _canCancelWorker;
		}
		set
		{
			_canCancelWorker = value;
		}
	}

	public event DoWorkEventHandler? DoWork;

	public event ProgressChangedEventHandler? ProgressChanged;

	public event RunWorkerCompletedEventHandler? RunWorkerCompleted;

	public BackgroundWorker()
	{
		_operationCompleted = AsyncOperationCompleted;
		_progressReporter = ProgressReporter;
	}

	private void AsyncOperationCompleted(object arg)
	{
		_isRunning = false;
		_cancellationPending = false;
		OnRunWorkerCompleted((RunWorkerCompletedEventArgs)arg);
	}

	public void CancelAsync()
	{
		if (!WorkerSupportsCancellation)
		{
			throw new InvalidOperationException(System.SR.BackgroundWorker_WorkerDoesntSupportCancellation);
		}
		_cancellationPending = true;
	}

	protected virtual void OnDoWork(DoWorkEventArgs e)
	{
		this.DoWork?.Invoke(this, e);
	}

	protected virtual void OnRunWorkerCompleted(RunWorkerCompletedEventArgs e)
	{
		this.RunWorkerCompleted?.Invoke(this, e);
	}

	protected virtual void OnProgressChanged(ProgressChangedEventArgs e)
	{
		this.ProgressChanged?.Invoke(this, e);
	}

	private void ProgressReporter(object arg)
	{
		OnProgressChanged((ProgressChangedEventArgs)arg);
	}

	public void ReportProgress(int percentProgress)
	{
		ReportProgress(percentProgress, null);
	}

	public void ReportProgress(int percentProgress, object? userState)
	{
		if (!WorkerReportsProgress)
		{
			throw new InvalidOperationException(System.SR.BackgroundWorker_WorkerDoesntReportProgress);
		}
		ProgressChangedEventArgs progressChangedEventArgs = new ProgressChangedEventArgs(percentProgress, userState);
		if (_asyncOperation != null)
		{
			_asyncOperation.Post(_progressReporter, progressChangedEventArgs);
		}
		else
		{
			_progressReporter(progressChangedEventArgs);
		}
	}

	public void RunWorkerAsync()
	{
		RunWorkerAsync(null);
	}

	public void RunWorkerAsync(object? argument)
	{
		if (_isRunning)
		{
			throw new InvalidOperationException(System.SR.BackgroundWorker_WorkerAlreadyRunning);
		}
		_isRunning = true;
		_cancellationPending = false;
		_asyncOperation = AsyncOperationManager.CreateOperation(null);
		Task.Factory.StartNew(delegate(object arg)
		{
			WorkerThreadStart(arg);
		}, argument, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
	}

	private void WorkerThreadStart(object argument)
	{
		object result = null;
		Exception error = null;
		bool cancelled = false;
		try
		{
			DoWorkEventArgs doWorkEventArgs = new DoWorkEventArgs(argument);
			OnDoWork(doWorkEventArgs);
			if (doWorkEventArgs.Cancel)
			{
				cancelled = true;
			}
			else
			{
				result = doWorkEventArgs.Result;
			}
		}
		catch (Exception ex)
		{
			error = ex;
		}
		RunWorkerCompletedEventArgs arg = new RunWorkerCompletedEventArgs(result, error, cancelled);
		_asyncOperation.PostOperationCompleted(_operationCompleted, arg);
	}

	protected override void Dispose(bool disposing)
	{
	}
}
