namespace Microsoft.Xna.Framework.Graphics;

public sealed class DirectionalLight
{
	private EffectParameter directionParam;

	private EffectParameter diffuseColorParam;

	private EffectParameter specularColorParam;

	private bool enabled;

	private Vector3 cachedDirection;

	private Vector3 cachedDiffuseColor;

	private Vector3 cachedSpecularColor;

	public bool Enabled
	{
		get
		{
			return enabled;
		}
		set
		{
			if (enabled == value)
			{
				return;
			}
			enabled = value;
			if (enabled)
			{
				if (diffuseColorParam != null)
				{
					diffuseColorParam.SetValue(cachedDiffuseColor);
				}
				if (specularColorParam != null)
				{
					specularColorParam.SetValue(cachedSpecularColor);
				}
			}
			else
			{
				if (diffuseColorParam != null)
				{
					diffuseColorParam.SetValue(Vector3.Zero);
				}
				if (specularColorParam != null)
				{
					specularColorParam.SetValue(Vector3.Zero);
				}
			}
		}
	}

	public Vector3 Direction
	{
		get
		{
			return cachedDirection;
		}
		set
		{
			if (directionParam != null)
			{
				directionParam.SetValue(value);
			}
			cachedDirection = value;
		}
	}

	public Vector3 DiffuseColor
	{
		get
		{
			return cachedDiffuseColor;
		}
		set
		{
			if (enabled && diffuseColorParam != null)
			{
				diffuseColorParam.SetValue(value);
			}
			cachedDiffuseColor = value;
		}
	}

	public Vector3 SpecularColor
	{
		get
		{
			return cachedSpecularColor;
		}
		set
		{
			if (enabled && specularColorParam != null)
			{
				specularColorParam.SetValue(value);
			}
			cachedSpecularColor = value;
		}
	}

	public DirectionalLight(EffectParameter directionParameter, EffectParameter diffuseColorParameter, EffectParameter specularColorParameter, DirectionalLight cloneSource)
	{
		directionParam = directionParameter;
		diffuseColorParam = diffuseColorParameter;
		specularColorParam = specularColorParameter;
		if (cloneSource != null)
		{
			enabled = cloneSource.enabled;
			cachedDirection = cloneSource.cachedDirection;
			cachedDiffuseColor = cloneSource.cachedDiffuseColor;
			cachedSpecularColor = cloneSource.cachedSpecularColor;
		}
		else
		{
			Direction = Vector3.Down;
			DiffuseColor = Vector3.One;
			SpecularColor = Vector3.Zero;
		}
	}
}
