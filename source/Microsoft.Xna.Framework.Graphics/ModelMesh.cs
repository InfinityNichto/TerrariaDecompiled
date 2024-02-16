using System;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class ModelMesh
{
	private string name;

	private ModelBone parentBone;

	private BoundingSphere boundingSphere = default(BoundingSphere);

	private ModelMeshPartCollection meshParts;

	private ModelEffectCollection effects = new ModelEffectCollection();

	private object tag;

	public string Name => name;

	public ModelBone ParentBone => parentBone;

	public BoundingSphere BoundingSphere => boundingSphere;

	public object Tag
	{
		get
		{
			return tag;
		}
		set
		{
			tag = value;
		}
	}

	public ModelMeshPartCollection MeshParts => meshParts;

	public ModelEffectCollection Effects => effects;

	internal ModelMesh(string name, ModelBone parentBone, BoundingSphere boundingSphere, ModelMeshPart[] meshParts, object tag)
	{
		this.name = name;
		this.parentBone = parentBone;
		this.boundingSphere = boundingSphere;
		this.meshParts = new ModelMeshPartCollection(meshParts);
		this.tag = tag;
		int num = meshParts.Length;
		for (int i = 0; i < num; i++)
		{
			ModelMeshPart modelMeshPart = meshParts[i];
			modelMeshPart.parent = this;
		}
	}

	public void Draw()
	{
		int count = MeshParts.Count;
		for (int i = 0; i < count; i++)
		{
			ModelMeshPart modelMeshPart = MeshParts[i];
			Effect effect = modelMeshPart.Effect;
			if (effect == null)
			{
				throw new InvalidOperationException(FrameworkResources.ModelHasNoEffect);
			}
			int count2 = effect.CurrentTechnique.Passes.Count;
			for (int j = 0; j < count2; j++)
			{
				effect.CurrentTechnique.Passes[j].Apply();
				modelMeshPart.Draw();
			}
		}
	}
}
