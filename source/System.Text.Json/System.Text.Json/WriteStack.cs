using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json;

[DebuggerDisplay("Path:{PropertyPath()} Current: ConverterStrategy.{ConverterStrategy.JsonTypeInfo.PropertyInfoForTypeInfo.ConverterStrategy}, {Current.JsonTypeInfo.Type.Name}")]
internal struct WriteStack
{
	public WriteStackFrame Current;

	private WriteStackFrame[] _stack;

	private int _count;

	private int _continuationCount;

	public CancellationToken CancellationToken;

	public bool SuppressFlush;

	public Task PendingTask;

	public List<IAsyncDisposable> CompletedAsyncDisposables;

	public int FlushThreshold;

	public ReferenceResolver ReferenceResolver;

	public bool SupportContinuation;

	public string BoxedStructReferenceId;

	public bool IsContinuation => _continuationCount != 0;

	private void EnsurePushCapacity()
	{
		if (_stack == null)
		{
			_stack = new WriteStackFrame[4];
		}
		else if (_count - 1 == _stack.Length)
		{
			Array.Resize(ref _stack, 2 * _stack.Length);
		}
	}

	public JsonConverter Initialize(Type type, JsonSerializerOptions options, bool supportContinuation)
	{
		JsonTypeInfo orAddClassForRootType = options.GetOrAddClassForRootType(type);
		return Initialize(orAddClassForRootType, supportContinuation);
	}

	internal JsonConverter Initialize(JsonTypeInfo jsonTypeInfo, bool supportContinuation)
	{
		Current.JsonTypeInfo = jsonTypeInfo;
		Current.DeclaredJsonPropertyInfo = jsonTypeInfo.PropertyInfoForTypeInfo;
		Current.NumberHandling = Current.DeclaredJsonPropertyInfo.NumberHandling;
		JsonSerializerOptions options = jsonTypeInfo.Options;
		if (options.ReferenceHandlingStrategy != 0)
		{
			ReferenceResolver = options.ReferenceHandler.CreateResolver(writing: true);
		}
		SupportContinuation = supportContinuation;
		return jsonTypeInfo.PropertyInfoForTypeInfo.ConverterBase;
	}

	public void Push()
	{
		if (_continuationCount == 0)
		{
			if (_count == 0)
			{
				_count = 1;
				return;
			}
			JsonTypeInfo runtimeTypeInfo = Current.GetPolymorphicJsonPropertyInfo().RuntimeTypeInfo;
			JsonNumberHandling? numberHandling = Current.NumberHandling;
			EnsurePushCapacity();
			_stack[_count - 1] = Current;
			Current = default(WriteStackFrame);
			_count++;
			Current.JsonTypeInfo = runtimeTypeInfo;
			Current.DeclaredJsonPropertyInfo = runtimeTypeInfo.PropertyInfoForTypeInfo;
			Current.NumberHandling = numberHandling ?? Current.DeclaredJsonPropertyInfo.NumberHandling;
		}
		else
		{
			if (_count++ > 0)
			{
				Current = _stack[_count - 1];
			}
			if (_continuationCount == _count)
			{
				_continuationCount = 0;
			}
		}
	}

	public void Pop(bool success)
	{
		if (!success)
		{
			if (_continuationCount == 0)
			{
				if (_count == 1)
				{
					_continuationCount = 1;
					_count = 0;
					return;
				}
				EnsurePushCapacity();
				_continuationCount = _count--;
			}
			else if (--_count == 0)
			{
				return;
			}
			_stack[_count] = Current;
			Current = _stack[_count - 1];
		}
		else if (--_count > 0)
		{
			Current = _stack[_count - 1];
		}
	}

	public void AddCompletedAsyncDisposable(IAsyncDisposable asyncDisposable)
	{
		(CompletedAsyncDisposables ?? (CompletedAsyncDisposables = new List<IAsyncDisposable>())).Add(asyncDisposable);
	}

	public async ValueTask DisposeCompletedAsyncDisposables()
	{
		Exception exception = null;
		foreach (IAsyncDisposable completedAsyncDisposable in CompletedAsyncDisposables)
		{
			try
			{
				await completedAsyncDisposable.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex)
			{
				exception = ex;
			}
		}
		if (exception != null)
		{
			ExceptionDispatchInfo.Capture(exception).Throw();
		}
		CompletedAsyncDisposables.Clear();
	}

	public void DisposePendingDisposablesOnException()
	{
		Exception exception2 = null;
		DisposeFrame(Current.CollectionEnumerator, ref exception2);
		int num = Math.Max(_count, _continuationCount);
		for (int i = 0; i < num - 1; i++)
		{
			DisposeFrame(_stack[i].CollectionEnumerator, ref exception2);
		}
		if (exception2 != null)
		{
			ExceptionDispatchInfo.Capture(exception2).Throw();
		}
		static void DisposeFrame(IEnumerator collectionEnumerator, ref Exception exception)
		{
			try
			{
				if (collectionEnumerator is IDisposable disposable)
				{
					disposable.Dispose();
				}
			}
			catch (Exception ex)
			{
				exception = ex;
			}
		}
	}

	public async ValueTask DisposePendingDisposablesOnExceptionAsync()
	{
		Exception exception2 = null;
		exception2 = await DisposeFrame(Current.CollectionEnumerator, Current.AsyncDisposable, exception2).ConfigureAwait(continueOnCapturedContext: false);
		int stackSize = Math.Max(_count, _continuationCount);
		for (int i = 0; i < stackSize - 1; i++)
		{
			exception2 = await DisposeFrame(_stack[i].CollectionEnumerator, _stack[i].AsyncDisposable, exception2).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (exception2 != null)
		{
			ExceptionDispatchInfo.Capture(exception2).Throw();
		}
		static async ValueTask<Exception> DisposeFrame(IEnumerator collectionEnumerator, IAsyncDisposable asyncDisposable, Exception exception)
		{
			try
			{
				if (collectionEnumerator is IDisposable disposable)
				{
					disposable.Dispose();
				}
				else if (asyncDisposable != null)
				{
					await asyncDisposable.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			catch (Exception ex)
			{
				exception = ex;
			}
			return exception;
		}
	}

	public string PropertyPath()
	{
		StringBuilder stringBuilder = new StringBuilder("$");
		int num = Math.Max(_count, _continuationCount);
		for (int i = 0; i < num - 1; i++)
		{
			AppendStackFrame(stringBuilder, ref _stack[i]);
		}
		if (_continuationCount == 0)
		{
			AppendStackFrame(stringBuilder, ref Current);
		}
		return stringBuilder.ToString();
		static void AppendPropertyName(StringBuilder sb, string propertyName)
		{
			if (propertyName != null)
			{
				if (propertyName.IndexOfAny(ReadStack.SpecialCharacters) != -1)
				{
					sb.Append("['");
					sb.Append(propertyName);
					sb.Append("']");
				}
				else
				{
					sb.Append('.');
					sb.Append(propertyName);
				}
			}
		}
		static void AppendStackFrame(StringBuilder sb, ref WriteStackFrame frame)
		{
			string text = frame.DeclaredJsonPropertyInfo?.ClrName;
			if (text == null)
			{
				text = frame.JsonPropertyNameAsString;
			}
			AppendPropertyName(sb, text);
		}
	}
}
