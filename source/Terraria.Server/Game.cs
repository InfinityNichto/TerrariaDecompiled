using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.Server;

public class Game : IDisposable
{
	public GameComponentCollection Components => null;

	public ContentManager Content
	{
		get
		{
			return null;
		}
		set
		{
		}
	}

	public GraphicsDevice GraphicsDevice => null;

	public TimeSpan InactiveSleepTime
	{
		get
		{
			return TimeSpan.Zero;
		}
		set
		{
		}
	}

	public bool IsActive => true;

	public bool IsFixedTimeStep
	{
		get
		{
			return true;
		}
		set
		{
		}
	}

	public bool IsMouseVisible
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	public LaunchParameters LaunchParameters => null;

	public GameServiceContainer Services => null;

	public TimeSpan TargetElapsedTime
	{
		get
		{
			return TimeSpan.Zero;
		}
		set
		{
		}
	}

	public GameWindow Window => null;

	public event EventHandler<EventArgs> Activated;

	public event EventHandler<EventArgs> Deactivated;

	public event EventHandler<EventArgs> Disposed;

	public event EventHandler<EventArgs> Exiting;

	protected virtual bool BeginDraw()
	{
		return true;
	}

	protected virtual void BeginRun()
	{
	}

	public void Dispose()
	{
	}

	protected virtual void Dispose(bool disposing)
	{
	}

	protected virtual void Draw(GameTime gameTime)
	{
	}

	protected virtual void EndDraw()
	{
	}

	protected virtual void EndRun()
	{
	}

	public void Exit()
	{
	}

	protected virtual void Initialize()
	{
	}

	protected virtual void LoadContent()
	{
	}

	protected virtual void OnActivated(object sender, EventArgs args)
	{
	}

	protected virtual void OnDeactivated(object sender, EventArgs args)
	{
	}

	protected virtual void OnExiting(object sender, EventArgs args)
	{
	}

	public void ResetElapsedTime()
	{
	}

	public void Run()
	{
	}

	public void RunOneFrame()
	{
	}

	protected virtual bool ShowMissingRequirementMessage(Exception exception)
	{
		return true;
	}

	public void SuppressDraw()
	{
	}

	public void Tick()
	{
	}

	protected virtual void UnloadContent()
	{
	}

	protected virtual void Update(GameTime gameTime)
	{
	}
}
