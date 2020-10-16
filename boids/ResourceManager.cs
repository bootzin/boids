using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace boids
{
	public static class ResourceManager
	{
		public static Dictionary<string, Shader> Shaders { get; set; } = new Dictionary<string, Shader>();
		public static Dictionary<string, Model> Models { get; set; } = new Dictionary<string, Model>();
		public static Dictionary<string, Sound> Sounds { get; set; } = new Dictionary<string, Sound>();

		public static Shader GetShader(string name) => Shaders[name];
		public static Model GetModel(string name) => Models[name];
		public static Sound GetSound(string name) => Sounds[name];

		public static Shader LoadShader(string vShaderPath, string fShaderPath, string name)
		{
			if (!Shaders.ContainsKey(name))
				Shaders[name] = LoadShaderFromFile(vShaderPath, fShaderPath);
			return Shaders[name];
		}

		public static Model LoadModel(string modelPath, string name)
		{
			if (!Models.ContainsKey(name))
			{
				Console.WriteLine("Loading model: " + name);
				Models[name] = new Model(modelPath);
				Console.WriteLine("Model Loaded!");
			}
			return Models[name];
		}

		public static Sound LoadSound(string filePath, string name, bool loop = false)
		{
			if (!Sounds.ContainsKey(name))
				Sounds[name] = LoadSoundFromFile(filePath, loop);
			return Sounds[name];
		}

		private static Sound LoadSoundFromFile(string filePath, bool loop)
		{
			using var audioFileReader = new AudioFileReader(filePath);
			var resampler = new WdlResamplingSampleProvider(audioFileReader, 44100);
			Sound snd = new Sound
			{
				WaveFormat = resampler.WaveFormat
			};
			List<float> wholeFile = new List<float>((int)(audioFileReader.Length / 4));
			float[] readBuffer = new float[resampler.WaveFormat.SampleRate * resampler.WaveFormat.Channels];
			int samplesRead;
			while ((samplesRead = resampler.Read(readBuffer, 0, readBuffer.Length)) > 0)
			{
				wholeFile.AddRange(readBuffer.Take(samplesRead));
			}
			snd.AudioData = wholeFile.ToArray();
			snd.Loop = loop;
			return snd;
		}

		private static Shader LoadShaderFromFile(string vertexShaderPath, string fragmentShaderPath)
		{
			using StreamReader vsr = new StreamReader(Path.Combine(AppContext.BaseDirectory, vertexShaderPath));
			using StreamReader fsr = new StreamReader(Path.Combine(AppContext.BaseDirectory, fragmentShaderPath));
			string vertexCode = vsr.ReadToEnd();
			string fragCode = fsr.ReadToEnd();
			return new Shader(vertexCode, fragCode);
		}
	}
}
