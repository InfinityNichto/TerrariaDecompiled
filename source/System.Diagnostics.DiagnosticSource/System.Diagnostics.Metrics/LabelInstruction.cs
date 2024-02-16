namespace System.Diagnostics.Metrics;

internal struct LabelInstruction
{
	public int SourceIndex { get; }

	public string LabelName { get; }

	public LabelInstruction(int sourceIndex, string labelName)
	{
		SourceIndex = sourceIndex;
		LabelName = labelName;
	}
}
