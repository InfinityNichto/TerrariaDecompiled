namespace System.ComponentModel.Design;

public class ActiveDesignerEventArgs : EventArgs
{
	public IDesignerHost? OldDesigner { get; }

	public IDesignerHost? NewDesigner { get; }

	public ActiveDesignerEventArgs(IDesignerHost? oldDesigner, IDesignerHost? newDesigner)
	{
		OldDesigner = oldDesigner;
		NewDesigner = newDesigner;
	}
}
