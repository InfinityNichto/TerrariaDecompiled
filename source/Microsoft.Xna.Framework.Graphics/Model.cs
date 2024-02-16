using System;
using Microsoft.Xna.Framework.Content;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class Model
{
	private ModelBone root;

	private ModelBoneCollection bones;

	private ModelMeshCollection meshes;

	private object tag;

	private static Matrix[] sharedDrawBoneMatrices;

	public ModelBone Root => root;

	public ModelBoneCollection Bones => bones;

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

	public ModelMeshCollection Meshes => meshes;

	internal Model()
	{
	}

	public void CopyBoneTransformsTo(Matrix[] destinationBoneTransforms)
	{
		if (destinationBoneTransforms == null)
		{
			throw new ArgumentNullException("destinationBoneTransforms");
		}
		if (destinationBoneTransforms.Length < bones.Count)
		{
			throw new ArgumentOutOfRangeException("destinationBoneTransforms");
		}
		int count = bones.Count;
		for (int i = 0; i < count; i++)
		{
			ref Matrix reference = ref destinationBoneTransforms[i];
			reference = bones[i].transform;
		}
	}

	public void CopyAbsoluteBoneTransformsTo(Matrix[] destinationBoneTransforms)
	{
		if (destinationBoneTransforms == null)
		{
			throw new ArgumentNullException("destinationBoneTransforms");
		}
		if (destinationBoneTransforms.Length < bones.Count)
		{
			throw new ArgumentOutOfRangeException("destinationBoneTransforms");
		}
		int count = bones.Count;
		for (int i = 0; i < count; i++)
		{
			ModelBone modelBone = bones[i];
			if (modelBone.Parent == null)
			{
				ref Matrix reference = ref destinationBoneTransforms[i];
				reference = modelBone.transform;
			}
			else
			{
				int index = modelBone.Parent.Index;
				ref Matrix reference2 = ref destinationBoneTransforms[i];
				reference2 = modelBone.transform * destinationBoneTransforms[index];
			}
		}
	}

	public void CopyBoneTransformsFrom(Matrix[] sourceBoneTransforms)
	{
		if (sourceBoneTransforms == null)
		{
			throw new ArgumentNullException("sourceBoneTransforms");
		}
		if (sourceBoneTransforms.Length < bones.Count)
		{
			throw new ArgumentOutOfRangeException("sourceBoneTransforms");
		}
		int count = bones.Count;
		for (int i = 0; i < count; i++)
		{
			bones[i].transform = sourceBoneTransforms[i];
		}
	}

	public void Draw(Matrix world, Matrix view, Matrix projection)
	{
		int count = meshes.Count;
		int count2 = bones.Count;
		Matrix[] array = sharedDrawBoneMatrices;
		if (array == null || array.Length < count2)
		{
			array = (sharedDrawBoneMatrices = new Matrix[count2]);
		}
		CopyAbsoluteBoneTransformsTo(array);
		for (int i = 0; i < count; i++)
		{
			ModelMesh modelMesh = meshes[i];
			int index = modelMesh.ParentBone.Index;
			int count3 = modelMesh.Effects.Count;
			for (int j = 0; j < count3; j++)
			{
				Effect effect = modelMesh.Effects[j];
				if (effect == null)
				{
					throw new InvalidOperationException(FrameworkResources.ModelHasNoEffect);
				}
				if (!(effect is IEffectMatrices effectMatrices))
				{
					throw new InvalidOperationException(FrameworkResources.ModelHasNoIEffectMatrices);
				}
				effectMatrices.World = array[index] * world;
				effectMatrices.View = view;
				effectMatrices.Projection = projection;
			}
			modelMesh.Draw();
		}
	}

	internal static Model Read(ContentReader input)
	{
		Model model = new Model();
		model.ReadBones(input);
		model.ReadMeshes(input);
		model.root = model.ReadBoneReference(input);
		model.Tag = input.ReadObject<object>();
		return model;
	}

	private void ReadBones(ContentReader input)
	{
		int num = input.ReadInt32();
		ModelBone[] array = new ModelBone[num];
		for (int i = 0; i < array.Length; i++)
		{
			string name = input.ReadObject<string>();
			Matrix transform = input.ReadMatrix();
			array[i] = new ModelBone(name, transform, i);
		}
		bones = new ModelBoneCollection(array);
		ModelBone[] array2 = array;
		foreach (ModelBone modelBone in array2)
		{
			ModelBone newParent = ReadBoneReference(input);
			int num2 = input.ReadInt32();
			ModelBone[] array3 = new ModelBone[num2];
			for (int k = 0; k < num2; k++)
			{
				array3[k] = ReadBoneReference(input);
			}
			modelBone.SetParentAndChildren(newParent, array3);
		}
	}

	private ModelBone ReadBoneReference(ContentReader input)
	{
		int num = bones.Count + 1;
		int num2 = 0;
		num2 = ((num > 255) ? input.ReadInt32() : input.ReadByte());
		if (num2 != 0)
		{
			return bones[num2 - 1];
		}
		return null;
	}

	private void ReadMeshes(ContentReader input)
	{
		int num = input.ReadInt32();
		ModelMesh[] array = new ModelMesh[num];
		for (int i = 0; i < num; i++)
		{
			string name = input.ReadObject<string>();
			ModelBone parentBone = ReadBoneReference(input);
			BoundingSphere boundingSphere = default(BoundingSphere);
			boundingSphere.Center = input.ReadVector3();
			boundingSphere.Radius = input.ReadSingle();
			object obj = input.ReadObject<object>();
			ModelMeshPart[] meshParts = ReadMeshParts(input);
			array[i] = new ModelMesh(name, parentBone, boundingSphere, meshParts, obj);
		}
		meshes = new ModelMeshCollection(array);
	}

	private static ModelMeshPart[] ReadMeshParts(ContentReader input)
	{
		int num = input.ReadInt32();
		ModelMeshPart[] meshParts = new ModelMeshPart[num];
		for (int i = 0; i < num; i++)
		{
			int vertexOffset = input.ReadInt32();
			int numVertices = input.ReadInt32();
			int startIndex = input.ReadInt32();
			int primitiveCount = input.ReadInt32();
			object obj = input.ReadObject<object>();
			meshParts[i] = new ModelMeshPart(vertexOffset, numVertices, startIndex, primitiveCount, obj);
			int uniqueCopyOfI = i;
			input.ReadSharedResource(delegate(VertexBuffer vb)
			{
				meshParts[uniqueCopyOfI].vertexBuffer = vb;
			});
			input.ReadSharedResource(delegate(IndexBuffer ib)
			{
				meshParts[uniqueCopyOfI].indexBuffer = ib;
			});
			input.ReadSharedResource(delegate(Effect effect)
			{
				meshParts[uniqueCopyOfI].Effect = effect;
			});
		}
		return meshParts;
	}
}
