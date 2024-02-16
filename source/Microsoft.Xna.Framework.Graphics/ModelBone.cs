namespace Microsoft.Xna.Framework.Graphics;

public sealed class ModelBone
{
	private string name;

	internal Matrix transform;

	private int index;

	private ModelBone parent;

	private ModelBoneCollection children;

	public string Name => name;

	public int Index => index;

	public Matrix Transform
	{
		get
		{
			return transform;
		}
		set
		{
			transform = value;
		}
	}

	public ModelBone Parent => parent;

	public ModelBoneCollection Children => children;

	internal ModelBone(string name, Matrix transform, int index)
	{
		this.name = name;
		this.transform = transform;
		this.index = index;
	}

	internal void SetParentAndChildren(ModelBone newParent, ModelBone[] newChildren)
	{
		parent = newParent;
		children = new ModelBoneCollection(newChildren);
	}
}
