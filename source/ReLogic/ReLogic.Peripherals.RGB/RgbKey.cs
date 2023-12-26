using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ReLogic.Peripherals.RGB;

public class RgbKey
{
	public readonly Keys Key;

	public readonly string KeyTriggerName;

	private float _timeRemaining;

	private float _totalTime = 1f;

	private float _effectRate = 1f;

	public int CurrentIntegerRepresentation { get; private set; }

	public RgbKeyEffect Effect { get; private set; }

	public Color BaseColor { get; private set; }

	public Color TargetColor { get; private set; }

	public Color CurrentColor { get; private set; }

	public bool IsVisible => Effect != RgbKeyEffect.Clear;

	public RgbKey(Keys key, string keyTriggerName)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		Key = key;
		KeyTriggerName = keyTriggerName;
		BaseColor = Color.White;
		TargetColor = Color.White;
		CurrentColor = Color.White;
		Effect = RgbKeyEffect.Clear;
	}

	public void SetIntegerRepresentation(int integerValue)
	{
		CurrentIntegerRepresentation = integerValue;
	}

	public void FadeTo(Color targetColor, float time)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		TargetColor = targetColor;
		_timeRemaining = time;
		_totalTime = time;
		Effect = RgbKeyEffect.Fade;
	}

	public void SetFlashing(Color flashColor, float time, float flashesPerSecond = 1f)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		TargetColor = flashColor;
		_timeRemaining = time;
		_totalTime = time;
		_effectRate = flashesPerSecond;
		Effect = RgbKeyEffect.Flashing;
	}

	public void SetFlashing(Color baseColor, Color flashColor, float time, float flashesPerSecond = 1f)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		SetBaseColor(baseColor);
		SetFlashing(flashColor, time, flashesPerSecond);
	}

	public void SetBaseColor(Color color)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		BaseColor = color;
	}

	public void SetTargetColor(Color color)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		TargetColor = color;
	}

	public void SetSolid()
	{
		Effect = RgbKeyEffect.Solid;
	}

	public void SetSolid(Color color)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		BaseColor = color;
		Effect = RgbKeyEffect.Solid;
	}

	public void Clear()
	{
		Effect = RgbKeyEffect.Clear;
	}

	internal void Update(float timeElapsed)
	{
		switch (Effect)
		{
		case RgbKeyEffect.Solid:
			UpdateSolidEffect();
			break;
		case RgbKeyEffect.Fade:
			UpdateFadeEffect();
			break;
		case RgbKeyEffect.Flashing:
			UpdateFlashingEffect();
			break;
		}
		_timeRemaining = Math.Max(_timeRemaining - timeElapsed, 0f);
	}

	private void UpdateSolidEffect()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		CurrentColor = BaseColor;
	}

	private void UpdateFadeEffect()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		float amount = 0f;
		if (_totalTime > 0f)
		{
			amount = 1f - _timeRemaining / _totalTime;
		}
		CurrentColor = Color.Lerp(BaseColor, TargetColor, amount);
	}

	private void UpdateFlashingEffect()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		float amount = (float)Math.Sin(_timeRemaining * _effectRate * ((float)Math.PI * 2f)) * 0.5f + 0.5f;
		CurrentColor = Color.Lerp(BaseColor, TargetColor, amount);
	}
}
