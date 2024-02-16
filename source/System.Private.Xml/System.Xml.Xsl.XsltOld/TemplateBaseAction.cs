namespace System.Xml.Xsl.XsltOld;

internal abstract class TemplateBaseAction : ContainerAction
{
	protected int variableCount;

	private int _variableFreeSlot;

	public int AllocateVariableSlot()
	{
		int variableFreeSlot = _variableFreeSlot;
		_variableFreeSlot++;
		if (variableCount < _variableFreeSlot)
		{
			variableCount = _variableFreeSlot;
		}
		return variableFreeSlot;
	}

	public void ReleaseVariableSlots(int n)
	{
	}
}
