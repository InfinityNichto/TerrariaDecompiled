using System.Runtime.Versioning;

namespace System.Runtime.InteropServices;

[SupportedOSPlatform("windows")]
public static class ComEventsHelper
{
	public static void Combine(object rcw, Guid iid, int dispid, Delegate d)
	{
		lock (rcw)
		{
			ComEventsInfo comEventsInfo = ComEventsInfo.FromObject(rcw);
			ComEventsSink comEventsSink = comEventsInfo.FindSink(ref iid) ?? comEventsInfo.AddSink(ref iid);
			ComEventsMethod comEventsMethod = comEventsSink.FindMethod(dispid) ?? comEventsSink.AddMethod(dispid);
			comEventsMethod.AddDelegate(d);
		}
	}

	public static Delegate? Remove(object rcw, Guid iid, int dispid, Delegate d)
	{
		lock (rcw)
		{
			ComEventsInfo comEventsInfo = ComEventsInfo.Find(rcw);
			if (comEventsInfo == null)
			{
				return null;
			}
			ComEventsSink comEventsSink = comEventsInfo.FindSink(ref iid);
			if (comEventsSink == null)
			{
				return null;
			}
			ComEventsMethod comEventsMethod = comEventsSink.FindMethod(dispid);
			if (comEventsMethod == null)
			{
				return null;
			}
			comEventsMethod.RemoveDelegate(d);
			if (comEventsMethod.Empty)
			{
				comEventsMethod = comEventsSink.RemoveMethod(comEventsMethod);
			}
			if (comEventsMethod == null)
			{
				comEventsSink = comEventsInfo.RemoveSink(comEventsSink);
			}
			if (comEventsSink == null)
			{
				Marshal.SetComObjectData(rcw, typeof(ComEventsInfo), null);
				GC.SuppressFinalize(comEventsInfo);
			}
			return d;
		}
	}
}
