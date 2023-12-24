using System;
using Microsoft.Xna.Framework;
using SDL2;

namespace ReLogic.OS;

public interface IWindowService
{
	void SetUnicodeTitle(GameWindow window, string title);

	void StartFlashingIcon(GameWindow window);

	void StopFlashingIcon(GameWindow window);

	float GetScaling();

	void SetQuickEditEnabled(bool enabled);

	void HideConsole();

	void SetIcon(GameWindow window)
	{
		IntPtr surface = SDL.SDL_LoadBMP("Libraries/Native/tModLoader.bmp");
		SDL.SDL_SetWindowIcon(window.Handle, surface);
	}
}
