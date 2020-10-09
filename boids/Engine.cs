using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace boids
{
	public sealed class Engine : GameWindow
	{
		public static Camera Camera { get; set; }
		private float lastX;
		private float lastY;
		private bool firstMouse = true;
		private bool directionalLight;
		private float deltaTime;
		private float currentFishModelIndex;
		private Vector3 leaderTarget = Vector3.Zero;

		public const int GroundSize = 4000;
		public const int GroundLevel = -20;
		public const int MinHeight = 200;
		public const int MaxHeight = 1500;
		public static Vector3 FlockMiddlePos { get; private set; }
		public static Vector3 FlockMiddlePosAbsolute => LeaderBoid.Position + FlockMiddlePos;

		public static List<EngineObject> Boids { get; set; } = new List<EngineObject>();
		public List<EngineObject> EngineObjects { get; set; } = new List<EngineObject>();
		public static Boid LeaderBoid { get; set; }
		public static Tower Tower { get; set; }
		public EngineObject Floor { get; set; }
		public static EngineObject Sun { get; set; }

		public Renderer3D Renderer3D { get; set; }

		public Engine(int width, int height, string title) : base(width, height, new GraphicsMode(new ColorFormat(32), 16, 0, 4, new ColorFormat(0), 2, false), title)
		{
			GL.Viewport(0, 0, width, height);

			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

			GL.Enable(EnableCap.CullFace);

			GL.Enable(EnableCap.DepthTest);
			GL.DepthFunc(DepthFunction.Less);

			GL.Enable(EnableCap.Multisample);

			Resize += (_, e) => GL.Viewport(0, 0, Width, Height);
			MouseWheel += OnMouseWheel;
			MouseMove += OnMouseMove;

			Init();
		}

		private void Init()
		{
			ResourceManager.LoadShader("shaders/textured.vert", "shaders/textured.frag", "textured");
			ResourceManager.LoadShader("shaders/normal.vert", "shaders/normal.frag", "normal");
			ResourceManager.LoadShader("shaders/normal2.vert", "shaders/normal2.frag", "normal2");
			ResourceManager.LoadShader("shaders/light.vert", "shaders/light.frag", "light");
			ResourceManager.LoadShader("shaders/shadow.vert", "shaders/shadow.frag", "shadow");

			List<Model> boidModels = new List<Model>();
			for (int i = 10; i < 50; i++)
			{
				boidModels.Add(ResourceManager.LoadModel("resources/objects/fish/fish_0000" + i + ".obj", "fish_0000" + i));
			}

			Model towerModel = ResourceManager.LoadModel("resources/objects/crate/crate1.obj", "tower");
			Model floorModel = ResourceManager.LoadModel("resources/objects/floor/floor.obj", "floor");
			Model sunModel = ResourceManager.LoadModel("resources/objects/other/sphere.obj", "sun");

			Renderer3D = new Renderer3D();

			Tower = new Tower(towerModel, Vector3.Zero, 100, 300);

			Floor = new EngineObject()
			{
				Model = floorModel,
				Position = new Vector3(Vector3.Zero) { Y = GroundLevel },
				Size = new Vector3(GroundSize, 1, GroundSize)
			};

			Sun = new EngineObject()
			{
				Model = sunModel,
				Position = new Vector3(200, 1.5f * MaxHeight, -300),
				Size = Vector3.One * 200
			};

			Tower.Position += new Vector3(0, Tower.Size.Y + GroundLevel, 0);

			EngineObjects.Add(Tower);
			EngineObjects.Add(Floor);

			LeaderBoid = new Boid(boidModels[0], GetRandomPosition(), Vector3.One * 30f, GetRandomPosition());

			EngineObjects.Add(LeaderBoid);

			for (int i = 0; i < 50; i++)
				Boids.Add(new Boid(boidModels[0], GetRandomPosition(), Vector3.One * 10f, GetRandomDir()));
			UpdateFlockMiddle();

			EngineObjects.AddRange(Boids);

			Camera = new Camera(GetRandomPosition());
		}

		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			base.OnUpdateFrame(e);
			deltaTime = (float)e.Time;

			if (leaderTarget == Vector3.Zero || Vector3.Distance(LeaderBoid.Position, leaderTarget) < 200)
				leaderTarget =  GetRandomPosition();

			LeaderBoid.MoveToPoint(leaderTarget, deltaTime);

			Boids.ForEach(boid => ((Boid)boid).Move(EngineObjects.Where(b => b != boid).ToList(), deltaTime));
			UpdateFlockMiddle();
			Camera.Update();
			ProcessEvents();
		}

		protected override void OnRenderFrame(FrameEventArgs e)
		{
			base.OnRenderFrame(e);

			var currBoidModel = ResourceManager.GetModel("fish_0000" + ((((int)currentFishModelIndex) % 39) + 10));
			Boids.ForEach(boid => boid.Model = currBoidModel);
			LeaderBoid.Model = currBoidModel;
			currentFishModelIndex += .3333f;

			GL.ClearColor(.1f, .1f, .1f, 1);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			Renderer3D.RenderShadows(EngineObjects, ResourceManager.GetShader("shadow"), Width, Height, directionalLight);

			GL.ClearColor(60 / 255f, 100 / 255f, 120 / 255f, 1);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			Renderer3D.DrawModels(EngineObjects, ResourceManager.GetShader("textured"), Width, Height, directionalLight);

			Renderer3D.DrawSun(ResourceManager.GetShader("light"), (float)Width / Height);

			Renderer3D.RenderCaustics();

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
			if (e.Key == Key.Number1)
				Camera.SetCameraType(CameraType.Behind);
			if (e.Key == Key.Number2)
				Camera.SetCameraType(CameraType.Parallel);
			if (e.Key == Key.Number3)
				Camera.SetCameraType(CameraType.Tower);
			if (e.Key == Key.Number4)
				Camera.SetCameraType(CameraType.Free);
			if (e.Key == Key.Plus)
				Camera.MouseSensivity += 0.05f;
			if (e.Key == Key.Minus)
				Camera.MouseSensivity -= 0.05f;
			if (e.Key == Key.T)
				directionalLight = !directionalLight;

			if (e.Key == Key.Left)
				Sun.Position -= Vector3.UnitZ * 50;
			if (e.Key == Key.Right)
				Sun.Position += Vector3.UnitZ * 50;
			if (e.Key == Key.Down)
				Sun.Position -= Vector3.UnitY * 50;
			if (e.Key == Key.Up)
				Sun.Position += Vector3.UnitY * 50;

			if (e.Key == Key.Q || e.Key == Key.Escape)
				Close();
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

		private void UpdateFlockMiddle()
		{
			FlockMiddlePos = Vector3.Zero;
			Boids.ForEach(boid => FlockMiddlePos += boid.Position);
			FlockMiddlePos /= Boids.Count;
		}

		private Vector3 GetRandomDir() => new Vector3(Utils.Random.Next(1000) / 1000f, Utils.Random.Next(1000) / 1000f, Utils.Random.Next(1000) / 1000f);

		private Vector3 GetRandomPosition()
		{
			float boidX = Utils.Random.Next(GroundSize) - (GroundSize / 2);
			float boidY = Utils.Random.Next((int)1.5 * MinHeight, (MaxHeight - MinHeight) / 2);
			float boidZ = Utils.Random.Next(GroundSize) - (GroundSize / 2);

			if (Math.Abs(boidX) < Tower.Radius)
				boidX += 2 * Tower.Radius;
			if (Math.Abs(boidZ) < Tower.Radius)
				boidZ += 2 * Tower.Radius;

			return new Vector3(boidX, boidY, boidZ);
		}
	}
}
