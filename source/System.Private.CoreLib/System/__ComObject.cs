using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System;

[SupportedOSPlatform("windows")]
internal class __ComObject : MarshalByRefObject
{
	private Hashtable m_ObjectToDataMap;

	protected __ComObject()
	{
	}

	internal object GetData(object key)
	{
		object result = null;
		lock (this)
		{
			if (m_ObjectToDataMap != null)
			{
				result = m_ObjectToDataMap[key];
			}
		}
		return result;
	}

	internal bool SetData(object key, object data)
	{
		bool result = false;
		lock (this)
		{
			if (m_ObjectToDataMap == null)
			{
				m_ObjectToDataMap = new Hashtable();
			}
			if (m_ObjectToDataMap[key] == null)
			{
				m_ObjectToDataMap[key] = data;
				result = true;
			}
		}
		return result;
	}

	internal void ReleaseAllData()
	{
		lock (this)
		{
			if (m_ObjectToDataMap == null)
			{
				return;
			}
			foreach (object value in m_ObjectToDataMap.Values)
			{
				if (value is IDisposable disposable)
				{
					disposable.Dispose();
				}
				if (value is __ComObject o)
				{
					Marshal.ReleaseComObject(o);
				}
			}
			m_ObjectToDataMap = null;
		}
	}

	internal object GetEventProvider([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] RuntimeType t)
	{
		object data = GetData(t);
		if (data != null)
		{
			return data;
		}
		return CreateEventProvider(t);
	}

	internal int ReleaseSelf()
	{
		return Marshal.InternalReleaseComObject(this);
	}

	internal void FinalReleaseSelf()
	{
		Marshal.InternalFinalReleaseComObject(this);
	}

	private object CreateEventProvider([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] RuntimeType t)
	{
		object obj = Activator.CreateInstance(t, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance, null, new object[1] { this }, null);
		if (!SetData(t, obj))
		{
			if (obj is IDisposable disposable)
			{
				disposable.Dispose();
			}
			obj = GetData(t);
		}
		return obj;
	}
}
