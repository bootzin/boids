using System;

namespace boids
{
	public static class Program
	{
		[STAThread]
		public static void Main()
		{
			Engine prog = new Engine(800, 600, "Hello BOIdS!");
			prog.Run();
		}
	}
}
