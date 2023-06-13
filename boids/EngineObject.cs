using OpenTK.Mathematics;

namespace boids
{
	public class EngineObject
	{
		public Vector3 Position { get; set; }
		public Vector3 Size { get; set; }
		public Vector3 Color { get; set; }
		public Vector3 Velocity { get; set; } = Vector3.One * 0.001f;
		public Model Model { get; set; }
		public float Pitch { get; set; } = 0;
		public float Yaw { get; set; } = 0;
	}
}
