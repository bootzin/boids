using OpenTK.Mathematics;

namespace boids
{
	public class Tower : EngineObject
	{
		public float Radius { get; set; }
		public float Height { get; set; }

		public Tower(Model model, Vector3 position, float radius, float height, Vector3? color = null)
		{
			Model = model;
			Radius = radius;
			Height = height;
			Size = new Vector3(2 * radius, height, 2 * radius);
			Position = position;
			Color = color ?? Vector3.One;
		}
	}
}
