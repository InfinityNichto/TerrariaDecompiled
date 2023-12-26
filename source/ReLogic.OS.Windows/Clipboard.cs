using System;
using System.Threading;
using ReLogic.OS.Base;

namespace ReLogic.OS.Windows;

internal class Clipboard : ReLogic.OS.Base.Clipboard
{
	protected override string GetClipboard()
	{
		return TryToGetClipboardText();
	}

	protected override void SetClipboard(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		try
		{
			InvokeInStaThread(delegate
			{
				NativeClipboard.SetText(text);
			});
		}
		catch
		{
			Console.WriteLine("Failed to set clipboard contents!");
		}
	}

	private string TryToGetClipboardText()
	{
		try
		{
			string text;
			return InvokeInStaThread(() => (!NativeClipboard.TryGetText(out text)) ? "" : text);
		}
		catch
		{
			Console.WriteLine("Failed to get clipboard contents!");
			return "";
		}
	}

	private static T InvokeInStaThread<T>(Func<T> callback)
	{
		if (GetApartmentStateSafely() == ApartmentState.STA)
		{
			return callback();
		}
		T result = default(T);
		Thread thread = new Thread((ThreadStart)delegate
		{
			result = callback();
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
		return result;
	}

	private static void InvokeInStaThread(Action callback)
	{
		if (GetApartmentStateSafely() == ApartmentState.STA)
		{
			callback();
			return;
		}
		Thread thread = new Thread((ThreadStart)delegate
		{
			callback();
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
	}

	private static ApartmentState GetApartmentStateSafely()
	{
		try
		{
			return Thread.CurrentThread.GetApartmentState();
		}
		catch
		{
			return ApartmentState.MTA;
		}
	}
}
