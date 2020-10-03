using OpenTK;

namespace boids
{
	public abstract class EngineObject
	{
		public Vector3 Position { get; set; }
		public Vector3 Size { get; set; }
		public Vector3 Color { get; set; }
		public Vector3 Velocity { get; set; }
		public Model Model { get; set; }
	}
}
