using NAudio.Wave;

namespace boids
{
	public class Sound
	{
		public float[] AudioData { get; set; }
		public WaveFormat WaveFormat { get; set; }
		public bool Loop { get; set; }
	}
}
