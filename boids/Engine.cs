using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Collections.Generic;
using System.Linq;

namespace boids
{
	public sealed class Engine : GameWindow
	{
		public static Camera Camera { get; set; } = new Camera(new Vector3(0, 2, 3));
		private float lastX;
		private float lastY;
		private bool firstMouse = true;
		private float deltaTime;

		private const int GroundSize = 2000;
		private const int GroundLevel = 2000;


		public List<EngineObject> Boids { get; set; } = new List<EngineObject>();

		public Renderer3D Renderer3D { get; set; }

		public Engine(int width, int height, string title) : base(width, height, GraphicsMode.Default, title)
		{
			GL.Viewport(0, 0, width, height);

			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

			GL.Enable(EnableCap.CullFace);

			GL.Enable(EnableCap.DepthTest);
			GL.DepthFunc(DepthFunction.Less);

			Resize += (_, e) => GL.Viewport(0, 0, Width, Height);
			MouseWheel += OnMouseWheel;
			MouseMove += OnMouseMove;

			Init();
		}

		private void OnMouseMove(object sender, MouseMoveEventArgs e)
		{
			if (firstMouse)
			{
				lastX = e.X;
				lastY = e.Y;
				firstMouse = false;
			}

			float xOff = e.X - lastX;
			float yOff = lastY - e.Y;
			lastX = e.X;
			lastY = e.Y;
			Camera.ProcessMouse(xOff, yOff);
		}

		private void OnMouseWheel(object sender, MouseWheelEventArgs e)
		{
			Camera.ProcessMouseScroll(e.Delta);
		}

		private void Init()
		{
			ResourceManager.LoadShader("shaders/textured.vert", "shaders/textured.frag", "textured");
			Model boidModel = ResourceManager.LoadModel("resources/objects/fish/fish.obj", "fish");

			Renderer3D = new Renderer3D();

			Boids.Add(new Boid(boidModel, Vector3.Zero, Vector3.One * .05f));
			Boids.Add(new Boid(boidModel, Vector3.One, Vector3.One * .05f));
			Boids.Add(new Boid(boidModel, -Vector3.One, Vector3.One * .05f));
		}

		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			base.OnUpdateFrame(e);
			deltaTime = (float)e.Time;

			Boids.ForEach(boid => ((Boid)boid).Move(Boids.Where(b => b != boid).ToList(), deltaTime, Width, Height));

			ProcessEvents();
		}

		protected override void OnRenderFrame(FrameEventArgs e)
		{
			base.OnRenderFrame(e);
			GL.ClearColor(.85f, .85f, .85f, 1);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			var shader = ResourceManager.GetShader("textured");
			foreach (Boid boid in Boids)
			{
				Renderer3D.DrawModel(boid.Model, shader, boid.Position, boid.Size, boid.Color, (float)Width / Height);
			}

			SwapBuffers();
		}

		protected override void OnKeyDown(KeyboardKeyEventArgs e)
		{
			base.OnKeyDown(e);

			if (e.Key == Key.W)
				Camera.ProcessKeyboard(CameraMovement.FORWARD, deltaTime);
			if (e.Key == Key.S)
				Camera.ProcessKeyboard(CameraMovement.BACKWARD, deltaTime);
			if (e.Key == Key.A)
				Camera.ProcessKeyboard(CameraMovement.LEFT, deltaTime);
			if (e.Key == Key.D)
				Camera.ProcessKeyboard(CameraMovement.RIGHT, deltaTime);
			if (e.Key == Key.Q || e.Key == Key.Escape)
				Close();
		}
	}
}
