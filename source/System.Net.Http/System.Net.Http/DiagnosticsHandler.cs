using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

internal sealed class DiagnosticsHandler : HttpMessageHandlerStage
{
	private sealed class ActivityStartData
	{
		public HttpRequestMessage Request { get; }

		[DynamicDependency("RequestUri", typeof(HttpRequestMessage))]
		[DynamicDependency("Method", typeof(HttpRequestMessage))]
		[DynamicDependency("RequestUri", typeof(HttpRequestMessage))]
		[DynamicDependency("Host", typeof(Uri))]
		[DynamicDependency("Port", typeof(Uri))]
		internal ActivityStartData(HttpRequestMessage request)
		{
			Request = request;
		}

		public override string ToString()
		{
			return $"{{ {"Request"} = {Request} }}";
		}
	}

	private sealed class ActivityStopData
	{
		public HttpResponseMessage Response { get; }

		public HttpRequestMessage Request { get; }

		public TaskStatus RequestTaskStatus { get; }

		internal ActivityStopData(HttpResponseMessage response, HttpRequestMessage request, TaskStatus requestTaskStatus)
		{
			Response = response;
			Request = request;
			RequestTaskStatus = requestTaskStatus;
		}

		public override string ToString()
		{
			return $"{{ {"Response"} = {Response}, {"Request"} = {Request}, {"RequestTaskStatus"} = {RequestTaskStatus} }}";
		}
	}

	private sealed class ExceptionData
	{
		public Exception Exception { get; }

		public HttpRequestMessage Request { get; }

		[DynamicDependency("RequestUri", typeof(HttpRequestMessage))]
		[DynamicDependency("Method", typeof(HttpRequestMessage))]
		[DynamicDependency("RequestUri", typeof(HttpRequestMessage))]
		[DynamicDependency("Host", typeof(Uri))]
		[DynamicDependency("Port", typeof(Uri))]
		[DynamicDependency("Message", typeof(Exception))]
		[DynamicDependency("StackTrace", typeof(Exception))]
		internal ExceptionData(Exception exception, HttpRequestMessage request)
		{
			Exception = exception;
			Request = request;
		}

		public override string ToString()
		{
			return $"{{ {"Exception"} = {Exception}, {"Request"} = {Request} }}";
		}
	}

	private sealed class RequestData
	{
		public HttpRequestMessage Request { get; }

		public Guid LoggingRequestId { get; }

		public long Timestamp { get; }

		[DynamicDependency("RequestUri", typeof(HttpRequestMessage))]
		[DynamicDependency("Method", typeof(HttpRequestMessage))]
		[DynamicDependency("RequestUri", typeof(HttpRequestMessage))]
		[DynamicDependency("Host", typeof(Uri))]
		[DynamicDependency("Port", typeof(Uri))]
		internal RequestData(HttpRequestMessage request, Guid loggingRequestId, long timestamp)
		{
			Request = request;
			LoggingRequestId = loggingRequestId;
			Timestamp = timestamp;
		}

		public override string ToString()
		{
			return $"{{ {"Request"} = {Request}, {"LoggingRequestId"} = {LoggingRequestId}, {"Timestamp"} = {Timestamp} }}";
		}
	}

	private sealed class ResponseData
	{
		public HttpResponseMessage Response { get; }

		public Guid LoggingRequestId { get; }

		public long Timestamp { get; }

		public TaskStatus RequestTaskStatus { get; }

		[DynamicDependency("StatusCode", typeof(HttpResponseMessage))]
		internal ResponseData(HttpResponseMessage response, Guid loggingRequestId, long timestamp, TaskStatus requestTaskStatus)
		{
			Response = response;
			LoggingRequestId = loggingRequestId;
			Timestamp = timestamp;
			RequestTaskStatus = requestTaskStatus;
		}

		public override string ToString()
		{
			return $"{{ {"Response"} = {Response}, {"LoggingRequestId"} = {LoggingRequestId}, {"Timestamp"} = {Timestamp}, {"RequestTaskStatus"} = {RequestTaskStatus} }}";
		}
	}

	private static readonly DiagnosticListener s_diagnosticListener = new DiagnosticListener("HttpHandlerDiagnosticListener");

	private readonly HttpMessageHandler _innerHandler;

	private readonly DistributedContextPropagator _propagator;

	private readonly HeaderDescriptor[] _propagatorFields;

	public DiagnosticsHandler(HttpMessageHandler innerHandler, DistributedContextPropagator propagator, bool autoRedirect = false)
	{
		_innerHandler = innerHandler;
		_propagator = propagator;
		if (!autoRedirect)
		{
			return;
		}
		IReadOnlyCollection<string> fields = _propagator.Fields;
		if (fields == null || fields.Count <= 0)
		{
			return;
		}
		List<HeaderDescriptor> list = new List<HeaderDescriptor>(fields.Count);
		foreach (string item in fields)
		{
			if (item != null && HeaderDescriptor.TryGet(item, out var descriptor))
			{
				list.Add(descriptor);
			}
		}
		_propagatorFields = list.ToArray();
	}

	private static bool IsEnabled()
	{
		if (Activity.Current == null)
		{
			return s_diagnosticListener.IsEnabled();
		}
		return true;
	}

	internal static bool IsGloballyEnabled()
	{
		return GlobalHttpSettings.DiagnosticsHandler.EnableActivityPropagation;
	}

	internal override ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
	{
		if (IsEnabled())
		{
			return SendAsyncCore(request, async, cancellationToken);
		}
		if (!async)
		{
			return new ValueTask<HttpResponseMessage>(_innerHandler.Send(request, cancellationToken));
		}
		return new ValueTask<HttpResponseMessage>(_innerHandler.SendAsync(request, cancellationToken));
	}

	private async ValueTask<HttpResponseMessage> SendAsyncCore(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
	{
		if (request == null)
		{
			throw new ArgumentNullException("request", System.SR.net_http_handler_norequest);
		}
		if (request.WasRedirected())
		{
			HeaderDescriptor[] propagatorFields = _propagatorFields;
			if (propagatorFields != null)
			{
				HeaderDescriptor[] array = propagatorFields;
				foreach (HeaderDescriptor descriptor in array)
				{
					request.Headers.Remove(descriptor);
				}
			}
		}
		Activity activity2 = null;
		DiagnosticListener diagnosticListener = s_diagnosticListener;
		if (!diagnosticListener.IsEnabled())
		{
			activity2 = new Activity("System.Net.Http.HttpRequestOut");
			activity2.Start();
			InjectHeaders(activity2, request);
			try
			{
				return (!async) ? _innerHandler.Send(request, cancellationToken) : (await _innerHandler.SendAsync(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
			}
			finally
			{
				activity2.Stop();
			}
		}
		Guid loggingRequestId = Guid.Empty;
		if (diagnosticListener.IsEnabled("System.Net.Http.HttpRequestOut", request))
		{
			activity2 = new Activity("System.Net.Http.HttpRequestOut");
			if (diagnosticListener.IsEnabled("System.Net.Http.HttpRequestOut.Start"))
			{
				StartActivity(diagnosticListener, activity2, new ActivityStartData(request));
			}
			else
			{
				activity2.Start();
			}
		}
		if (diagnosticListener.IsEnabled("System.Net.Http.Request"))
		{
			long timestamp = Stopwatch.GetTimestamp();
			loggingRequestId = Guid.NewGuid();
			Write(diagnosticListener, "System.Net.Http.Request", new RequestData(request, loggingRequestId, timestamp));
		}
		Activity current = Activity.Current;
		if (current != null)
		{
			InjectHeaders(current, request);
		}
		HttpResponseMessage response = null;
		TaskStatus taskStatus = TaskStatus.RanToCompletion;
		try
		{
			HttpResponseMessage httpResponseMessage = ((!async) ? _innerHandler.Send(request, cancellationToken) : (await _innerHandler.SendAsync(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)));
			response = httpResponseMessage;
			return response;
		}
		catch (OperationCanceledException)
		{
			taskStatus = TaskStatus.Canceled;
			throw;
		}
		catch (Exception exception)
		{
			taskStatus = TaskStatus.Faulted;
			if (diagnosticListener.IsEnabled("System.Net.Http.Exception"))
			{
				Write(diagnosticListener, "System.Net.Http.Exception", new ExceptionData(exception, request));
			}
			throw;
		}
		finally
		{
			if (activity2 != null)
			{
				StopActivity(diagnosticListener, activity2, new ActivityStopData(response, request, taskStatus));
			}
			if (diagnosticListener.IsEnabled("System.Net.Http.Response"))
			{
				long timestamp2 = Stopwatch.GetTimestamp();
				Write(diagnosticListener, "System.Net.Http.Response", new ResponseData(response, loggingRequestId, timestamp2, taskStatus));
			}
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_innerHandler.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InjectHeaders(Activity currentActivity, HttpRequestMessage request)
	{
		_propagator.Inject(currentActivity, request, delegate(object carrier, string key, string value)
		{
			if (carrier is HttpRequestMessage httpRequestMessage && key != null && HeaderDescriptor.TryGet(key, out var descriptor) && !httpRequestMessage.Headers.TryGetHeaderValue(descriptor, out var _))
			{
				httpRequestMessage.Headers.TryAddWithoutValidation(descriptor, value);
			}
		});
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "The values being passed into Write have the commonly used properties being preserved with DynamicDependency.")]
	private static void Write<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(DiagnosticSource diagnosticSource, string name, T value)
	{
		diagnosticSource.Write(name, value);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "The args being passed into StartActivity have the commonly used properties being preserved with DynamicDependency.")]
	private static Activity StartActivity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(DiagnosticSource diagnosticSource, Activity activity, T args)
	{
		return diagnosticSource.StartActivity(activity, args);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "The args being passed into StopActivity have the commonly used properties being preserved with DynamicDependency.")]
	private static void StopActivity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(DiagnosticSource diagnosticSource, Activity activity, T args)
	{
		diagnosticSource.StopActivity(activity, args);
	}
}
