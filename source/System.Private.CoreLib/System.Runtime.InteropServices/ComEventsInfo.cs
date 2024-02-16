using System.Runtime.Versioning;

namespace System.Runtime.InteropServices;

[SupportedOSPlatform("windows")]
internal sealed class ComEventsInfo
{
	private ComEventsSink _sinks;

	private readonly object _rcw;

	private ComEventsInfo(object rcw)
	{
		_rcw = rcw;
	}

	~ComEventsInfo()
	{
		_sinks = ComEventsSink.RemoveAll(_sinks);
	}

	public static ComEventsInfo Find(object rcw)
	{
		return (ComEventsInfo)Marshal.GetComObjectData(rcw, typeof(ComEventsInfo));
	}

	public static ComEventsInfo FromObject(object rcw)
	{
		ComEventsInfo comEventsInfo = Find(rcw);
		if (comEventsInfo == null)
		{
			comEventsInfo = new ComEventsInfo(rcw);
			Marshal.SetComObjectData(rcw, typeof(ComEventsInfo), comEventsInfo);
		}
		return comEventsInfo;
	}

	public ComEventsSink FindSink(ref Guid iid)
	{
		return ComEventsSink.Find(_sinks, ref iid);
	}

	public ComEventsSink AddSink(ref Guid iid)
	{
		ComEventsSink sink = new ComEventsSink(_rcw, iid);
		_sinks = ComEventsSink.Add(_sinks, sink);
		return _sinks;
	}

	internal ComEventsSink RemoveSink(ComEventsSink sink)
	{
		_sinks = ComEventsSink.Remove(_sinks, sink);
		return _sinks;
	}
}
