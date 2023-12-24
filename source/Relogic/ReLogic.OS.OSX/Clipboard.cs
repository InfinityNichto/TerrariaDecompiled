using System;
using System.Diagnostics;
using ReLogic.OS.Base;

namespace ReLogic.OS.OSX;

internal class Clipboard : ReLogic.OS.Base.Clipboard
{
	protected override string GetClipboard()
	{
		try
		{
			string result;
			using (Process process = new Process())
			{
				process.StartInfo = new ProcessStartInfo("pbpaste", "-pboard general")
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

	protected override void SetClipboard(string text)
	{
		try
		{
			using Process process = new Process();
			process.StartInfo = new ProcessStartInfo("pbcopy", "-pboard general -Prefer txt")
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
