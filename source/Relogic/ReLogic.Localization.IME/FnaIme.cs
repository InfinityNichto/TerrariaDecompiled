using Microsoft.Xna.Framework.Input;

namespace ReLogic.Localization.IME;

internal class FnaIme : PlatformIme
{
	private bool _disposedValue;

	public override uint CandidateCount => 0u;

	public override string CompositionString => string.Empty;

	public override bool IsCandidateListVisible => false;

	public override uint SelectedCandidate => 0u;

	public FnaIme()
	{
		TextInputEXT.TextInput += OnCharCallback;
	}

	private void OnCharCallback(char key)
	{
		if (base.IsEnabled)
		{
			OnKeyPress(key);
		}
	}

	public override string GetCandidate(uint index)
	{
		return string.Empty;
	}

	protected override void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				TextInputEXT.TextInput -= OnCharCallback;
			}
			if (base.IsEnabled)
			{
				Disable();
			}
			_disposedValue = true;
		}
	}

	~FnaIme()
	{
		Dispose(disposing: false);
	}
}
