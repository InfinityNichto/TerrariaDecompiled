namespace Microsoft.Xna.Framework.Graphics;

internal static class VertexDeclarationFactory<T> where T : struct, IVertexType
{
	private static VertexDeclaration cachedDeclaration;

	public static VertexDeclaration VertexDeclaration
	{
		get
		{
			if (cachedDeclaration == null)
			{
				cachedDeclaration = VertexDeclaration.FromType(typeof(T));
			}
			return cachedDeclaration;
		}
	}
}
