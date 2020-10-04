using System;
using System.Collections.Generic;
using System.IO;

namespace boids
{
	public static class ResourceManager
	{
		public static Dictionary<string, Shader> Shaders { get; set; } = new Dictionary<string, Shader>();
		public static Dictionary<string, Model> Models { get; set; } = new Dictionary<string, Model>();

		public static Shader GetShader(string name) => Shaders[name];
		public static Model GetModel(string name) => Models[name];

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
