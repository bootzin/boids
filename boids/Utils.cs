using System;

namespace boids
{
	public static class Utils
	{
		public static Random Random { get; } = new Random();
		public static float Deg2Rad(float angle)
		{
			return (float)(Math.PI * angle / 180f);
		}
	}
}
