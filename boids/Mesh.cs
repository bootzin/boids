using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;

namespace boids
{
	public class Mesh
	{
		private int VAO;

		public Vertex[] Vertices { get; set; } = { };
		public int[] Indices { get; set; } = { };
		public List<Texture> Textures { get; set; } = new List<Texture>();

		public Mesh() { }

		public Mesh(Vertex[] vertices, int[] indices, List<Texture> textures)
		{
			Vertices = vertices;
			Indices = indices;
			Textures = textures;

			SetupMesh();
		}

		public void Draw(Shader shader)
		{
			int diffuseCount = 1;
			int specularCount = 1;
			int normalCount = 1;
			int heightCount = 1;
			for (int i = 0; i < Textures.Count; i++)
			{
				GL.ActiveTexture(TextureUnit.Texture0 + i);
				string number = null;
				string name = Textures[i].Type;
				if (name == "texture_diffuse")
					number = diffuseCount++.ToString();
				else if (name == "texture_specular")
					number = specularCount++.ToString();
				else if (name == "texture_normal")
					number = normalCount++.ToString();
				else if (name == "texture_height")
					number = heightCount++.ToString();

				// assign samplers
				shader.SetInteger(name + number, i);
				GL.BindTexture(TextureTarget.Texture2D, Textures[i].ID);
			}

			GL.BindVertexArray(VAO);
			GL.DrawElements(PrimitiveType.Triangles, Indices.Length, DrawElementsType.UnsignedInt, 0);
			GL.ActiveTexture(TextureUnit.Texture0);
		}

		public void SetupMesh()
		{
			VAO = GL.GenVertexArray();
			int VBO = GL.GenBuffer();
			int EBO = GL.GenBuffer();

			GL.BindVertexArray(VAO);

			GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
			GL.BufferData(BufferTarget.ArrayBuffer, Vertex.SizeInBytes * Vertices.Length, Vertices, BufferUsageHint.StaticDraw);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
			GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Length * sizeof(int), Indices, BufferUsageHint.StaticDraw);

			// vertex position
			GL.EnableVertexAttribArray(0);
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vertex.SizeInBytes, 0);

			// vertex texCoord
			GL.EnableVertexAttribArray(1);
			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Vertex.SizeInBytes, 3 * sizeof(float));

			// vertex normals
			GL.EnableVertexAttribArray(2);
			GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, Vertex.SizeInBytes, 5 * sizeof(float));

			//vertex tangent
			GL.EnableVertexAttribArray(3);
			GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, Vertex.SizeInBytes, 8 * sizeof(float));

			//vertex bitangent
			GL.EnableVertexAttribArray(4);
			GL.VertexAttribPointer(4, 3, VertexAttribPointerType.Float, false, Vertex.SizeInBytes, 11 * sizeof(float));

			GL.BindVertexArray(0);
		}
	}
}
