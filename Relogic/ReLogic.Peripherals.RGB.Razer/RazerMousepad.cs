using Microsoft.Xna.Framework;

namespace ReLogic.Peripherals.RGB.Razer;

internal class RazerMousepad : RgbDevice
{
	private NativeMethods.CustomMousepadEffect _effect = NativeMethods.CustomMousepadEffect.Create();

	private readonly EffectHandle _handle = new EffectHandle();

	public RazerMousepad(DeviceColorProfile colorProfile)
		: base(RgbDeviceVendor.Razer, RgbDeviceType.Mousepad, Fragment.FromCustom(CreatePositionList()), colorProfile)
	{
	}

	private static Point[] CreatePositionList()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		Point[] array = (Point[])(object)new Point[15];
		Point point = default(Point);
		((Point)(ref point))._002Ector(26, 0);
		for (int i = 0; i < 5; i++)
		{
			array[i] = new Point(point.X, point.Y + i);
			array[14 - i] = new Point(point.X + 6, point.Y + i);
		}
		for (int j = 5; j < 10; j++)
		{
			array[j] = new Point(j - 5 + point.X + 1, point.Y + 5);
		}
		return array;
	}

	public override void Present()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < base.LedCount; i++)
		{
			_effect.Color[i] = RazerHelper.Vector4ToDeviceColor(GetProcessedLedColor(i));
		}
		_handle.SetAsMousepadEffect(ref _effect);
		_handle.Apply();
	}
}
