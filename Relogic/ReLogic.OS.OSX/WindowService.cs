using Microsoft.Xna.Framework;

namespace ReLogic.OS.OSX;

internal class WindowService : IWindowService
{
	public float GetScaling()
	{
		return 1f;
	}

	public void SetQuickEditEnabled(bool enabled)
	{
	}

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

	public void HideConsole()
	{
	}
}
