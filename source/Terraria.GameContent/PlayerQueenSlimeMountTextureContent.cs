using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;

namespace Terraria.GameContent;

public class PlayerQueenSlimeMountTextureContent : ARenderTargetContentByRequest
{
	protected override void HandleUseReqest(GraphicsDevice device, SpriteBatch spriteBatch)
	{
		Asset<Texture2D> val = TextureAssets.Extra[204];
		PrepareARenderTarget_AndListenToEvents(ref _target, device, val.Width(), val.Height(), RenderTargetUsage.PreserveContents);
		device.SetRenderTarget(_target);
		device.Clear(Color.Transparent);
		DrawData value = new DrawData(val.Value, Vector2.Zero, Color.White);
		spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
		GameShaders.Misc["QueenSlime"].Apply(value);
		value.Draw(spriteBatch);
		spriteBatch.End();
		device.SetRenderTarget(null);
		_wasPrepared = true;
	}
}
