using Microsoft.Xna.Framework;

namespace ReLogic.OS.Linux;

internal class WindowService : IWindowService
{
	public void SetUnicodeTitle(GameWindow window, string title)
	{
		window.Title = title;
	}

	public void StartFlashingIcon(GameWindow window)
	{
	}

	public void StopFlashingIcon(GameWindow window)
	{
	}

	public float GetScaling()
	{
		return 1f;
	}

	public void SetQuickEditEnabled(bool enabled)
	{
	}

	public void HideConsole()
	{
	}
}
