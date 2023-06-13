using Assimp;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using StbImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace boids
{
	public class Model
	{
		private readonly List<Mesh> Meshes = new List<Mesh>();
		private static readonly List<Texture> TexturesLoaded = new List<Texture>();
		private string Directory;
		private bool GammaCorrection;

		public Model(string path, bool gammaCorrection = false)
		{
			GammaCorrection = gammaCorrection;
			LoadModel(path);
		}

		public void Draw(Shader shader)
		{
			for (int i = 0; i < Meshes.Count; i++)
				Meshes[i].Draw(shader);
		}

		private void LoadModel(string path)
		{
			using var importer = new AssimpContext();
			string fullPath = Path.Combine(AppContext.BaseDirectory, path);
			Directory = fullPath.Substring(0, fullPath.LastIndexOf('/'));
			Scene scene = importer.ImportFile(fullPath, PostProcessSteps.ValidateDataStructure | PostProcessSteps.Triangulate | PostProcessSteps.FlipUVs | PostProcessSteps.CalculateTangentSpace | PostProcessSteps.GenerateSmoothNormals);
			ProcessNode(scene.RootNode, scene);
		}

		private void ProcessNode(Node node, Scene scene)
		{
			for (int i = 0; i < node.MeshCount; i++)
			{
				var mesh = scene.Meshes[node.MeshIndices[i]];
				Meshes.Add(ProcessMesh(mesh, scene));
			}
			for (int i = 0; i < node.ChildCount; i++)
			{
				ProcessNode(node.Children[i], scene);
			}
		}

		private Mesh ProcessMesh(Assimp.Mesh aMesh, Scene scene)
		{
			Mesh mesh = new Mesh();
			for (int i = 0; i < aMesh.VertexCount; i++)
			{
				Vertex vertex = new Vertex
				{
					Position = new Vector3(aMesh.Vertices[i].X, aMesh.Vertices[i].Y, aMesh.Vertices[i].Z)
				};
				if (aMesh.HasNormals)
					vertex.Normals = new Vector3(aMesh.Normals[i].X, aMesh.Normals[i].Y, aMesh.Normals[i].Z);
				if (aMesh.HasTextureCoords(0))
				{
					vertex.TexCoords = new Vector2(aMesh.TextureCoordinateChannels[0][i].X, aMesh.TextureCoordinateChannels[0][i].Y);
					if (aMesh.HasTangentBasis)
					{
						vertex.Tangent = new Vector3(aMesh.Tangents[i].X, aMesh.Tangents[i].Y, aMesh.Tangents[i].Z);
						vertex.Bitangent = new Vector3(aMesh.BiTangents[i].X, aMesh.BiTangents[i].Y, aMesh.BiTangents[i].Z);
					}
				}
				else
				{
					vertex.TexCoords = Vector2.Zero;
				}
				mesh.Vertices = mesh.Vertices.Append(vertex).ToArray();
			}

			for (int i = 0; i < aMesh.FaceCount; i++)
			{
				for (int j = 0; j < aMesh.Faces[i].IndexCount; j++)
				{
					mesh.Indices = mesh.Indices.Append(aMesh.Faces[i].Indices[j]).ToArray();
				}
			}

			var material = scene.Materials[aMesh.MaterialIndex];

			List<Texture> diffuseMaps = LoadMaterialTextures(material, TextureType.Diffuse, "texture_diffuse");
			mesh.Textures.AddRange(diffuseMaps);

			List<Texture> specularMaps = LoadMaterialTextures(material, TextureType.Specular, "texture_specular");
			mesh.Textures.AddRange(specularMaps);

			List<Texture> normalMaps = LoadMaterialTextures(material, TextureType.Normals, "texture_normal");
			mesh.Textures.AddRange(normalMaps);

			List<Texture> heightMaps = LoadMaterialTextures(material, TextureType.Height, "texture_height");
			mesh.Textures.AddRange(heightMaps);

			mesh.SetupMesh();
			return mesh;
		}

		private List<Texture> LoadMaterialTextures(Material mat, TextureType type, string typeName)
		{
			var textures = new List<Texture>();
			for (int i = 0; i < mat.GetMaterialTextureCount(type); i++)
			{
				mat.GetMaterialTexture(type, i, out TextureSlot texSlot);
				bool skip = false;
				for (int j = 0; j < TexturesLoaded.Count; j++)
				{
					if (TexturesLoaded[j].Path == texSlot.FilePath)
					{
						textures.Add(TexturesLoaded[j]);
						skip = true;
						break;
					}
				}

				if (!skip)
				{
					Texture texture = new Texture()
					{
						ID = TextureFromFile(texSlot.FilePath),
						Type = typeName,
						Path = texSlot.FilePath,
					};
					textures.Add(texture);
					TexturesLoaded.Add(texture);
				}
			}

			return textures;
		}

		private int TextureFromFile(string path, bool gamma = false)
		{
			string fullPath = $"{Directory}/{path}";

			int texID = GL.GenTexture();

			StbImage.stbi_set_flip_vertically_on_load(0);
			var img = ImageResult.FromMemory(File.ReadAllBytes(fullPath));
			if (img != null)
			{
				PixelInternalFormat format = 0;
				PixelFormat pxFormat = 0;
				if (img.Comp == ColorComponents.Grey)
				{
					format = PixelInternalFormat.R8;
					pxFormat = PixelFormat.Red;
				}
				else if (img.Comp == ColorComponents.RedGreenBlue)
				{
					format = PixelInternalFormat.Rgb;
					pxFormat = PixelFormat.Rgb;
				}
				else if (img.Comp == ColorComponents.RedGreenBlueAlpha)
				{
					format = PixelInternalFormat.Rgba;
					pxFormat = PixelFormat.Rgba;
				}
				GL.BindTexture(TextureTarget.Texture2D, texID);
				GL.TexImage2D(TextureTarget.Texture2D, 0, format, img.Width, img.Height, 0, pxFormat, PixelType.UnsignedByte, img.Data);
				GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.ClampToEdge);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.ClampToEdge);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.LinearMipmapLinear);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
			}
			else
			{
				throw new Exception("Texture failed to load at " + fullPath);
			}
			return texID;
		}
	}
}
