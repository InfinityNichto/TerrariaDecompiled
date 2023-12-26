using System;
using System.Diagnostics;
using ReLogic.OS.Base;

namespace ReLogic.OS.Linux;

internal class Clipboard : ReLogic.OS.Base.Clipboard
{
	protected override string GetClipboard()
	{
		try
		{
			string result;
			using (Process process = new Process())
			{
				process.StartInfo = new ProcessStartInfo("xsel", "-o")
				{
					UseShellExecute = false,
					RedirectStandardOutput = true
				};
				process.Start();
				result = process.StandardOutput.ReadToEnd();
				process.WaitForExit();
			}
			return result;
		}
		catch (Exception)
		{
			return "";
		}
	}

	private void ClearClipboard()
	{
		try
		{
			using Process process = new Process();
			process.StartInfo = new ProcessStartInfo("xsel", "-c")
			{
				UseShellExecute = false,
				RedirectStandardOutput = false,
				RedirectStandardInput = true
			};
			process.Start();
			process.WaitForExit();
		}
		catch (Exception)
		{
		}
	}

	protected override void SetClipboard(string text)
	{
		if (text == "")
		{
			ClearClipboard();
			return;
		}
		try
		{
			using Process process = new Process();
			process.StartInfo = new ProcessStartInfo("xsel", "-i")
			{
				UseShellExecute = false,
				RedirectStandardOutput = false,
				RedirectStandardInput = true
			};
			process.Start();
			process.StandardInput.Write(text);
			process.StandardInput.Close();
			process.WaitForExit();
		}
		catch (Exception)
		{
		}
	}
}
