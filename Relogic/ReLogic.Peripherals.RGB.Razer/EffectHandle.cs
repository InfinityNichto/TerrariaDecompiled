using System;

namespace ReLogic.Peripherals.RGB.Razer;

internal class EffectHandle
{
	private Guid _handle = Guid.Empty;

	public void SetAsKeyboardEffect(ref NativeMethods.CustomKeyboardEffect effect)
	{
		Reset();
		ValidateNativeCall(NativeMethods.CreateKeyboardEffect(NativeMethods.KeyboardEffectType.CustomKey, ref effect, ref _handle));
	}

	public void SetAsMouseEffect(ref NativeMethods.CustomMouseEffect effect)
	{
		Reset();
		ValidateNativeCall(NativeMethods.CreateMouseEffect(NativeMethods.MouseEffectType.Custom2, ref effect, ref _handle));
	}

	public void SetAsHeadsetEffect(ref NativeMethods.CustomHeadsetEffect effect)
	{
		Reset();
		ValidateNativeCall(NativeMethods.CreateHeadsetEffect(NativeMethods.HeadsetEffectType.Custom, ref effect, ref _handle));
	}

	public void SetAsMousepadEffect(ref NativeMethods.CustomMousepadEffect effect)
	{
		Reset();
		ValidateNativeCall(NativeMethods.CreateMousepadEffect(NativeMethods.MousepadEffectType.Custom, ref effect, ref _handle));
	}

	public void SetAsKeypadEffect(ref NativeMethods.CustomKeypadEffect effect)
	{
		Reset();
		ValidateNativeCall(NativeMethods.CreateKeypadEffect(NativeMethods.KeypadEffectType.Custom, ref effect, ref _handle));
	}

	public void SetAsChromaLinkEffect(ref NativeMethods.CustomChromaLinkEffect effect)
	{
		Reset();
		ValidateNativeCall(NativeMethods.CreateChromaLinkEffect(NativeMethods.ChromaLinkEffectType.Custom, ref effect, ref _handle));
	}

	public void Reset()
	{
		if (!(_handle == Guid.Empty))
		{
			ValidateNativeCall(NativeMethods.DeleteEffect(_handle));
			_handle = Guid.Empty;
		}
	}

	public void Apply()
	{
		if (_handle != Guid.Empty)
		{
			ValidateNativeCall(NativeMethods.SetEffect(_handle));
		}
	}

	private static void ValidateNativeCall(RzResult result)
	{
	}
}
