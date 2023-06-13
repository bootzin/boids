using OpenTK.Mathematics;
using System.Runtime.InteropServices;

namespace boids
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct Vertex
	{
		public Vector3 Position	{ get; set; }
		public Vector2 TexCoords	{ get; set; }
		public Vector3 Normals { get; set; }
		public Vector3 Tangent	{ get; set; }
		public Vector3 Bitangent { get; set; }

		public static int SizeInBytes => 14 * sizeof(float);
	}
}
