namespace ReLogic.Localization.IME;

public class UnsupportedPlatformIme : PlatformIme
{
	public override uint CandidateCount => 0u;

	public override string CompositionString => string.Empty;

	public override bool IsCandidateListVisible => false;

	public override uint SelectedCandidate => 0u;

	public override string GetCandidate(uint index)
	{
		return string.Empty;
	}
}
