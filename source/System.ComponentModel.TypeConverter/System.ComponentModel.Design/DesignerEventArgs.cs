namespace System.ComponentModel.Design;

public class DesignerEventArgs : EventArgs
{
	public IDesignerHost? Designer { get; }

	public DesignerEventArgs(IDesignerHost? host)
	{
		Designer = host;
	}
}
