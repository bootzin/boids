using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;

namespace boids
{
	public class Renderer3D
	{
		private int depthMapFBO, shadowMap;
		private Matrix4 lightSpaceMatrix;
		private const int ShadowResolution = 8192;

		public Renderer3D()
		{
			Init();
		}

		private void Init()
		{
			depthMapFBO = GL.GenFramebuffer();
			shadowMap = GL.GenTexture();

			GL.BindTexture(TextureTarget.Texture2D, shadowMap);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, ShadowResolution, ShadowResolution, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.ClampToBorder);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.ClampToBorder);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[] { 1f, 1f, 1f, 1f });

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, depthMapFBO);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, shadowMap, 0);
			GL.DrawBuffer(DrawBufferMode.None);
			GL.ReadBuffer(ReadBufferMode.None);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
		}

		public void DrawModels(List<EngineObject> objects, Shader shader, int width, int height, bool directionalLight = false, bool fogEnabled = true)
		{
			shader.Use();
			shader.SetVector3f("lightColor", Vector3.One);
			shader.SetVector4f("lightPos", directionalLight ? new Vector4(-Engine.Sun.Position, 0) : new Vector4(Engine.Sun.Position, 1));
			shader.SetVector2f("attenuation", 0.00014f / 10, 0.000007f / 100);
			//shader.SetVector2f("cutOff", (float)Math.Cos(Utils.Deg2Rad(35.5f)), (float)Math.Cos(Utils.Deg2Rad(40.5f)));
			shader.SetVector3f("viewPos", Engine.Camera.Position);
			shader.SetMatrix4("lightSpaceMatrix", lightSpaceMatrix);

			shader.SetVector3f("fogColor", new Vector3(60 / 255f, 100 / 255f, 120 / 255f));
			shader.SetInteger("fogEnabled", fogEnabled ? 1 : 0);
			shader.SetFloat("fogDensity", .00055f);

			shader.SetMatrix4("projection", Matrix4.CreatePerspectiveFieldOfView(Utils.Deg2Rad(Engine.Camera.Zoom), (float)width / height, 0.1f, 8000f));
			shader.SetMatrix4("view", Engine.Camera.GetViewMatrix());

			shader.SetInteger("shadowMap", 31);

			foreach (var obj in objects)
			{
				GL.ActiveTexture(TextureUnit.Texture31);
				GL.BindTexture(TextureTarget.Texture2D, shadowMap);
				DrawModel(obj.Model, shader, obj.Position, obj.Size, obj.Pitch, obj.Yaw, obj.Color);
			}
		}

		public void DrawModel(Model model, Shader shader, Vector3 position, Vector3 size, float pitch, float yaw, Vector3 color)
		{
			var modelMatrix = Matrix4.CreateScale(size);
			modelMatrix *= Matrix4.CreateRotationX(Utils.Deg2Rad(pitch));
			modelMatrix *= Matrix4.CreateRotationY(Utils.Deg2Rad(yaw));
			modelMatrix *= Matrix4.CreateTranslation(position);

			shader.SetMatrix4("model", modelMatrix);

			model.Draw(shader);
		}

		public void DrawSun(Shader shader, float aspectRatio)
		{
			var sun = Engine.Sun;
			shader.Use();
			shader.SetMatrix4("projection", Matrix4.CreatePerspectiveFieldOfView(Utils.Deg2Rad(Engine.Camera.Zoom), aspectRatio, 0.1f, 8000f));
			shader.SetMatrix4("view", Engine.Camera.GetViewMatrix());

			var modelMatrix = Matrix4.CreateScale(sun.Size);
			modelMatrix *= Matrix4.CreateTranslation(sun.Position);
			shader.SetMatrix4("model", modelMatrix);
			sun.Model.Draw(shader);
		}

		public void RenderShadows(List<EngineObject> objects, Shader shader, int width, int height, bool directionalLight = false)
		{
			const float near = .1f;
			const float far = 8000;
			//var lightProjection = Matrix4.CreatePerspectiveFieldOfView(Utils.Deg2Rad(45), (float)width / height, 1, 7.5f);
			Matrix4 lightProjection = Matrix4.CreateOrthographicOffCenter(-Engine.GroundSize, Engine.GroundSize, -Engine.GroundSize, Engine.GroundSize, near, far);
			Matrix4 lightView = Matrix4.LookAt(Engine.Sun.Position, Vector3.Zero, Vector3.UnitY);
			lightSpaceMatrix = lightView * lightProjection;

			shader.SetMatrix4("lightSpaceMatrix", lightSpaceMatrix, true);

			GL.Viewport(0, 0, ShadowResolution, ShadowResolution);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, depthMapFBO);
			GL.Clear(ClearBufferMask.DepthBufferBit);
			DrawModels(objects, shader, width, height, directionalLight); // shadows don't work very well with directional lights
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

			GL.Viewport(0, 0, width, height);
		}
	}
}
