using OpenTK;
using OpenTK.Graphics.OpenGL;
using StbImageSharp;
using System;
using System.IO;

namespace boids
{
	public class Renderer3D
	{
		private int oceanHeightTex, oceanNormalTex, lightMapTex;
		private Vector3 lightPos = new Vector3(-20, 2 * Engine.MaxHeight, -30);

		public Shader CausticShaders { get; set; }
		public Renderer3D(Shader causticShaders)
		{
			CausticShaders = causticShaders;
			Init();
		}

		private void Init()
		{
			oceanHeightTex = GL.GenTexture();
			oceanNormalTex = GL.GenTexture();
			lightMapTex = GL.GenTexture();

			string[] texturePaths = new string[] { "sine_wave_height.png", "sine_wave_normal.png", "light_map.png" };
			int[] texIds = new int[] { oceanHeightTex, oceanNormalTex, lightMapTex };

			for (int i = 0; i < texIds.Length; i++)
			{
				GL.BindTexture(TextureTarget.Texture2D, texIds[i]);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.Repeat);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.Repeat);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);

				StbImage.stbi_set_flip_vertically_on_load(0);
				var img = ImageResult.FromMemory(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "resources/textures", texturePaths[i])));
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
					GL.TexImage2D(TextureTarget.Texture2D, 0, format, img.Width, img.Height, 0, pxFormat, PixelType.UnsignedByte, img.Data);
					GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
					GL.BindTexture(TextureTarget.Texture2D, 0);
				}
			}
		}

		public void DrawModel(Model model, Shader shader, Vector3 position, Vector3 size, float pitch, float yaw, Vector3 color, float aspectRatio)
		{
			shader.Use();
			shader.SetMatrix4("projection", Matrix4.CreatePerspectiveFieldOfView(Utils.Deg2Rad(Engine.Camera.Zoom), aspectRatio, 0.1f, 8000f));
			shader.SetMatrix4("view", Engine.Camera.GetViewMatrix());

			var modelMatrix = Matrix4.CreateScale(size);
			modelMatrix *= Matrix4.CreateRotationX(Utils.Deg2Rad(pitch));
			modelMatrix *= Matrix4.CreateRotationY(Utils.Deg2Rad(yaw));
			modelMatrix *= Matrix4.CreateTranslation(position);

			shader.SetMatrix4("model", modelMatrix);

			model.Draw(shader);
		}

		public void DrawModelWithCaustics(Model model, Shader shader, Vector3 position, Vector3 size, float pitch, float yaw, Vector3 color, float aspectRatio)
		{
			shader.Use();
			shader.SetMatrix4("projection", Matrix4.CreatePerspectiveFieldOfView(Utils.Deg2Rad(Engine.Camera.Zoom), aspectRatio, 0.1f, 8000f));
			shader.SetMatrix4("view", Engine.Camera.GetViewMatrix());

			shader.SetVector3f("lightColor", Vector3.Zero);
			shader.SetVector3f("lightPos", lightPos);
			shader.SetVector3f("viewPos", Engine.Camera.Position);

			GL.ActiveTexture(TextureUnit.Texture29);
			GL.BindTexture(TextureTarget.Texture2D, oceanHeightTex);
			shader.SetInteger("oceanHeight", 29);

			GL.ActiveTexture(TextureUnit.Texture30);
			GL.BindTexture(TextureTarget.Texture2D, oceanNormalTex);
			shader.SetInteger("oceanHeight", 30);

			GL.ActiveTexture(TextureUnit.Texture31);
			GL.BindTexture(TextureTarget.Texture2D, lightMapTex);
			shader.SetInteger("oceanHeight", 31);

			var modelMatrix = Matrix4.CreateScale(size);
			modelMatrix *= Matrix4.CreateRotationX(Utils.Deg2Rad(pitch));
			modelMatrix *= Matrix4.CreateRotationY(Utils.Deg2Rad(yaw));
			modelMatrix *= Matrix4.CreateTranslation(position);

			shader.SetMatrix4("model", modelMatrix);

			model.Draw(shader);
		}
	}
}
