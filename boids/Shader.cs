﻿using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;

namespace boids
{
	public class Shader : IDisposable
	{
		public int ID { get; set; }

		private bool disposed;

		public Shader(string vShader, string fShader)
		{
			Compile(vShader, fShader);
		}

		// define esse shader como o shader ativo no OpenGL
		public Shader Use()
		{
			GL.UseProgram(ID);
			return this;
		}

		// compilação do código do shader para um programa do openGL
		public void Compile(string vShader, string fShader)
		{
			int sVertex, sFragment;

			//Vertex
			sVertex = GL.CreateShader(ShaderType.VertexShader);
			GL.ShaderSource(sVertex, vShader);
			GL.CompileShader(sVertex);
			CheckCompileErrors(sVertex, "VERTEX");

			//Fragment
			sFragment = GL.CreateShader(ShaderType.FragmentShader);
			GL.ShaderSource(sFragment, fShader);
			GL.CompileShader(sFragment);
			CheckCompileErrors(sFragment, "FRAGMENT");

			//Program
			this.ID = GL.CreateProgram();
			GL.AttachShader(ID, sVertex);
			GL.AttachShader(ID, sFragment);
			GL.LinkProgram(ID);
			CheckCompileErrors(ID, "PROGRAM");

			// depois de compilar, podemos liberar os recursos
			GL.DetachShader(ID, sVertex);
			GL.DetachShader(ID, sFragment);
			GL.DeleteShader(sVertex);
			GL.DeleteShader(sFragment);
		}

		// diversos métodos para o envio de informações ao programa do OpenGL
		public void SetFloat(string name, float value, bool useShader = false)
		{
			if (useShader)
				this.Use();
			GL.Uniform1(GL.GetUniformLocation(ID, name), value);
		}

		public void SetInteger(string name, int value, bool useShader = false)
		{
			if (useShader)
				this.Use();
			GL.Uniform1(GL.GetUniformLocation(ID, name), value);
		}

		public void SetVector1iv(string name, int count, int[] value, bool useShader = false)
		{
			if (useShader)
				this.Use();
			GL.Uniform1(GL.GetUniformLocation(ID, name), count, value);
		}

		public void SetVector1fv(string name, int count, float[] value, bool useShader = false)
		{
			if (useShader)
				this.Use();
			GL.Uniform1(GL.GetUniformLocation(ID, name), count, value);
		}

		public void SetVector2f(string name, float x, float y, bool useShader = false)
		{
			if (useShader)
				this.Use();
			GL.Uniform2(GL.GetUniformLocation(ID, name), x, y);
		}

		public void SetVector3f(string name, Vector3 vector, bool useShader = false)
		{
			if (useShader)
				Use();
			GL.Uniform3(GL.GetUniformLocation(ID, name), vector);
		}

		public void SetVector4f(string name, Vector4 vector, bool useShader = false)
		{
			if (useShader)
				Use();
			GL.Uniform4(GL.GetUniformLocation(ID, name), vector);
		}

		public void SetMatrix4(string name, Matrix4 matrix, bool useShader = false)
		{
			if (useShader)
				Use();
			GL.UniformMatrix4(GL.GetUniformLocation(ID, name), false, ref matrix);
		}

		public void SetVector2fv(string name, int count, ref float value, bool useShader = false)
		{
			if (useShader)
				Use();
			GL.Uniform2(GL.GetUniformLocation(ID, name), count, ref value);
		}

		private void CheckCompileErrors(int obj, string type)
		{
			int success;
			if (type != "PROGRAM")
			{
				GL.GetShader(obj, ShaderParameter.CompileStatus, out success);
				if (success == 0)
				{
					string error = GL.GetShaderInfoLog(obj);
					Console.WriteLine($"ERROR::SHADER: Compile time error! Type: {type}");
					Console.WriteLine(error);
					Console.WriteLine("-- ---------------------------------------------- --\n");
				}
			}
			else
			{
				GL.GetProgram(obj, GetProgramParameterName.LinkStatus, out success);
				if (success == 0)
				{
					string error = GL.GetProgramInfoLog(obj);
					Console.WriteLine($"ERROR::SHADER: Link time error! Type: {type}");
					Console.WriteLine(error);
					Console.WriteLine("-- ---------------------------------------------- --\n");
				}
			}
		}

		~Shader() => Dispose(false);

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					GL.DeleteProgram(ID);
				}

				disposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
