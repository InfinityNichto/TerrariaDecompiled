using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace Microsoft.Xna.Framework;

public static class FrameworkDispatcher
{
	private struct ManagedCallAndArg
	{
		public ManagedCallType ManagedCallType;

		public uint ManagedCallArg;

		public ManagedCallAndArg(ManagedCallType callType, uint arg)
		{
			ManagedCallType = callType;
			ManagedCallArg = arg;
		}

		public bool IsEqual(ManagedCallType type, uint arg)
		{
			if (type == ManagedCallType)
			{
				return arg == ManagedCallArg;
			}
			return false;
		}
	}

	internal static bool UpdateCalledAtLeastOnce;

	private static List<ManagedCallAndArg> pendingCalls = new List<ManagedCallAndArg>();

	private static List<ManagedCallAndArg> pendingCallsCopy = new List<ManagedCallAndArg>();

	public static void Update()
	{
		UpdateCalledAtLeastOnce = true;
		PollForEvents();
		lock (pendingCalls)
		{
			foreach (ManagedCallAndArg pendingCall in pendingCalls)
			{
				pendingCallsCopy.Add(pendingCall);
			}
			pendingCalls.Clear();
		}
		foreach (ManagedCallAndArg item in pendingCallsCopy)
		{
			switch (item.ManagedCallType)
			{
			case ManagedCallType.Media_ActiveSongChanged:
				MediaPlayer.OnActiveSongChanged(EventArgs.Empty);
				break;
			case ManagedCallType.Media_PlayStateChanged:
				MediaPlayer.OnMediaStateChanged(EventArgs.Empty);
				break;
			case ManagedCallType.CaptureBufferReady:
				Microphone.AllMicrophones.OnBufferReady(item.ManagedCallArg);
				break;
			case ManagedCallType.PlaybackBufferNeeded:
				DynamicSoundEffectInstance.RaiseBufferNeededOnInstance(item.ManagedCallArg);
				break;
			case ManagedCallType.System_DeviceChanged:
				FrameworkCallbackLinker.OnStorageDeviceChanged(EventArgs.Empty);
				break;
			}
		}
		pendingCallsCopy.Clear();
		SoundEffect.RecycleStoppedFireAndForgetInstances();
	}

	internal static void AddNewPendingCall(ManagedCallType callType, uint arg)
	{
		if (!UpdateCalledAtLeastOnce)
		{
			throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, FrameworkResources.CallFrameworkDispatcherUpdate));
		}
		lock (pendingCalls)
		{
			if (IsOncePerUpdateEvent(callType))
			{
				foreach (ManagedCallAndArg pendingCall in pendingCalls)
				{
					if (pendingCall.IsEqual(callType, arg))
					{
						return;
					}
				}
			}
			pendingCalls.Add(new ManagedCallAndArg(callType, arg));
		}
	}

	internal static bool IsOncePerUpdateEvent(ManagedCallType type)
	{
		if (type != ManagedCallType.CaptureBufferReady && type != ManagedCallType.Media_ActiveSongChanged)
		{
			return type == ManagedCallType.Media_PlayStateChanged;
		}
		return true;
	}

	private static void PollForEvents()
	{
	}
}
