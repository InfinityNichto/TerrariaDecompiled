using System.Text.RegularExpressions;

namespace System.ComponentModel.Design;

public class DesignerVerb : MenuCommand
{
	public string Description
	{
		get
		{
			object obj = Properties["Description"];
			if (obj == null)
			{
				return string.Empty;
			}
			return (string)obj;
		}
		set
		{
			Properties["Description"] = value;
		}
	}

	public string Text
	{
		get
		{
			object obj = Properties["Text"];
			if (obj == null)
			{
				return string.Empty;
			}
			return (string)obj;
		}
	}

	public DesignerVerb(string text, EventHandler handler)
		: base(handler, StandardCommands.VerbFirst)
	{
		Properties["Text"] = ((text == null) ? null : Regex.Replace(text, "\\(\\&.\\)", ""));
	}

	public DesignerVerb(string text, EventHandler handler, CommandID startCommandID)
		: base(handler, startCommandID)
	{
		Properties["Text"] = ((text == null) ? null : Regex.Replace(text, "\\(\\&.\\)", ""));
	}

	public override string ToString()
	{
		return Text + " : " + base.ToString();
	}
}
