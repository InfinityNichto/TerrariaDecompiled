using System;

namespace Microsoft.Xna.Framework.Graphics;

[Flags]
internal enum EffectStateFlags : uint
{
	AllVertexSamplers = 0x780000u,
	AllSamplers = 0x7FFF8u,
	VertexSampler3 = 0x400000u,
	VertexSampler2 = 0x200000u,
	VertexSampler1 = 0x100000u,
	VertexSampler0 = 0x80000u,
	Sampler15 = 0x40000u,
	Sampler14 = 0x20000u,
	Sampler13 = 0x10000u,
	Sampler12 = 0x8000u,
	Sampler11 = 0x4000u,
	Sampler10 = 0x2000u,
	Sampler9 = 0x1000u,
	Sampler8 = 0x800u,
	Sampler7 = 0x400u,
	Sampler6 = 0x200u,
	Sampler5 = 0x100u,
	Sampler4 = 0x80u,
	Sampler3 = 0x40u,
	Sampler2 = 0x20u,
	Sampler1 = 0x10u,
	Sampler0 = 8u,
	Rasterizer = 4u,
	DepthStencil = 2u,
	Blend = 1u,
	None = 0u
}
