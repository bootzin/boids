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
		private bool fogEnabled = true;
		private bool shouldPause;
		private bool autoPilot = true;
		private float deltaTime;
		private float cumulativeTime;
		private float currentFishModelIndex;
		private Vector3 leaderTarget = Vector3.Zero;
		private GameState State;

		public const int GroundSize = 6000;
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

			// done in fragment shader, can be turned off
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

			GL.Enable(EnableCap.CullFace);

			GL.Enable(EnableCap.DepthTest);
			GL.DepthFunc(DepthFunction.Less);

			GL.Enable(EnableCap.Multisample);

			Resize += (_, e) => GL.Viewport(0, 0, Width, Height);
			MouseWheel += OnMouseWheel;
			MouseMove += OnMouseMove;
			MouseDown += OnMouseDown;

			WindowState = WindowState.Maximized;
			CursorVisible = false;
			CursorGrabbed = false;
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

			Model towerModel = ResourceManager.LoadModel("resources/objects/ship/ship.obj", "tower");
			Model floorModel = ResourceManager.LoadModel("resources/objects/floor/floor.obj", "floor");
			Model sunModel = ResourceManager.LoadModel("resources/objects/other/sphere.obj", "sun");
			Model weedModel = ResourceManager.LoadModel("resources/objects/weed/weed.obj", "weed");

			Renderer3D = new Renderer3D();

			Tower = new Tower(towerModel, Vector3.Zero, 1, 2);

			Floor = new EngineObject()
			{
				Model = floorModel,
				Position = new Vector3(Vector3.Zero) { Y = GroundLevel },
				Size = new Vector3(GroundSize, 1, GroundSize)
			};

			Sun = new EngineObject()
			{
				Model = sunModel,
				Position = new Vector3(200, 1.5f * MaxHeight, -1100),
				Size = Vector3.One * 200
			};

			Tower.Position += new Vector3(0, Tower.Size.Y + GroundLevel, 0);


			EngineObjects.Add(Tower);
			EngineObjects.Add(Floor);

			LeaderBoid = new Boid(boidModels[0], GetRandomPosition(), Vector3.One * 30, GetRandomPosition());

			EngineObjects.Add(LeaderBoid);

			for (int i = 0; i < 50; i++)
				Boids.Add(new Boid(boidModels[0], GetRandomPosition(), Vector3.One * Utils.Random.Next(10, 16), GetRandomDir()));

			for (int i = 0; i < 100; i++)
			{
				var size = new Vector3(30000, Utils.Random.Next(20, 50) * 1000, 30000);
				var weed = new EngineObject()
				{
					Model = weedModel,
					Size = size,
					Position = new Vector3(GetRandomPosition(GroundSize)) { Y = GroundLevel }
				};
				EngineObjects.Add(weed);
			}

			UpdateFlockMiddle();

			EngineObjects.AddRange(Boids);

			Camera = new Camera(GetRandomPosition());

			var underwater = ResourceManager.LoadSound("resources/sounds/underwater.mp3", "underwater");
			SoundEngine.Instance.PlaySound(underwater);
		}

		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			base.OnUpdateFrame(e);
			if (State == GameState.Active)
			{
				deltaTime = (float)e.Time;
				cumulativeTime += deltaTime;
				currentFishModelIndex += 24 * deltaTime;

				if (autoPilot)
				{
					if (leaderTarget == Vector3.Zero || Vector3.Distance(LeaderBoid.Position, leaderTarget) < 2 * Boid.MAX_SPEED)
						leaderTarget = GetRandomPosition();

					LeaderBoid.MoveToPoint(leaderTarget, deltaTime);
				}
				else
				{
					LeaderBoid.CheckBoundaries();
					LeaderBoid.Velocity = Vector3.Clamp(LeaderBoid.Velocity, -Vector3.One * Boid.MAX_SPEED, Vector3.One * Boid.MAX_SPEED);
					LeaderBoid.CalculatePositionAndRotation(deltaTime);
				}

				Boids.ForEach(boid => ((Boid)boid).Move(Boids.Where(b => b != boid).ToList(), deltaTime));
				UpdateFlockMiddle();
				Camera.Update();

				if (shouldPause)
				{
					shouldPause = false;
					State = GameState.Paused;
					LogDebugInfo();
				}
			}
			ProcessEvents();
		}

		protected override void OnRenderFrame(FrameEventArgs e)
		{
			base.OnRenderFrame(e);

			var currBoidModel = ResourceManager.GetModel("fish_0000" + ((((int)currentFishModelIndex) % 39) + 10));
			Boids.ForEach(boid => boid.Model = currBoidModel);
			LeaderBoid.Model = currBoidModel;

			Renderer3D.RenderShadows(EngineObjects, ResourceManager.GetShader("shadow"), Width, Height, directionalLight);

			GL.ClearColor(60 / 255f, 100 / 255f, 120 / 255f, 1);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			ResourceManager.GetShader("textured").SetFloat("inputTime", cumulativeTime, true);
			Renderer3D.DrawModels(EngineObjects, ResourceManager.GetShader("textured"), Width, Height, directionalLight, fogEnabled);

			//Renderer3D.DrawSun(ResourceManager.GetShader("light"), (float)Width / Height);

			SwapBuffers();
		}

		protected override void OnKeyDown(KeyboardKeyEventArgs e)
		{
			base.OnKeyDown(e);

			// Move the camera when in free mode
			if (e.Key == Key.I)
				Camera.ProcessKeyboard(CameraMovement.FORWARD, deltaTime);
			if (e.Key == Key.K)
				Camera.ProcessKeyboard(CameraMovement.BACKWARD, deltaTime);
			if (e.Key == Key.J)
				Camera.ProcessKeyboard(CameraMovement.LEFT, deltaTime);
			if (e.Key == Key.L)
				Camera.ProcessKeyboard(CameraMovement.RIGHT, deltaTime);

			// change camera type
			if (e.Key == Key.Number1)
				Camera.SetCameraType(CameraType.Behind);
			if (e.Key == Key.Number2)
				Camera.SetCameraType(CameraType.Parallel);
			if (e.Key == Key.Number3)
				Camera.SetCameraType(CameraType.Tower);
			if (e.Key == Key.Number4)
				Camera.SetCameraType(CameraType.Free);

			// add boids/increase boid speed
			if (e.Key == Key.Plus)
			{
				if (e.Shift)
				{
					Boid.MAX_SPEED = Math.Min(800, Boid.MAX_SPEED + 10); // keeping max speed sane
					if (!autoPilot)
						LeaderBoid.Velocity = LeaderBoid.Velocity.Normalized() * Boid.MAX_SPEED;
				}
				else
				{
					Boid b = new Boid(ResourceManager.GetModel("fish_000010"), GetRandomPosition(), Vector3.One * Utils.Random.Next(10, 16), GetRandomDir());
					Boids.Add(b);
					EngineObjects.Add(b);
				}
			}

			//remove boids/decrease boid speed
			if (e.Key == Key.Minus)
			{
				if (e.Shift)
				{
					Boid.MAX_SPEED = Math.Max(50, Boid.MAX_SPEED - 10); // speeds lower than 50 start to get weird
					if (!autoPilot)
						LeaderBoid.Velocity = LeaderBoid.Velocity.Normalized() * Boid.MAX_SPEED;
				}
				else if (Boids.Count > 3)
				{
					int i = Utils.Random.Next(Boids.Count);
					EngineObject b = Boids[i];
					Boids.Remove(b);
					EngineObjects.Remove(b);
				}
			}

			// toggle light mode
			// can mess up with shadows
			if (e.Key == Key.T)
				directionalLight = !directionalLight;

			// enable/disable fog
			if (e.Key == Key.F)
				fogEnabled = !fogEnabled;

			// move light source, mostly for testing
			if (e.Key == Key.Left)
				Sun.Position -= Vector3.UnitZ * 50;
			if (e.Key == Key.Right)
				Sun.Position += Vector3.UnitZ * 50;
			if (e.Key == Key.Down)
				Sun.Position -= Vector3.UnitY * 50;
			if (e.Key == Key.Up)
				Sun.Position += Vector3.UnitY * 50;

			// enable/disable boid movement rules
			if (e.Key == Key.F1)
				Boid.Sep = !Boid.Sep;
			if (e.Key == Key.F2)
				Boid.Coh = !Boid.Coh;
			if (e.Key == Key.F3)
				Boid.Ali = !Boid.Ali;
			if (e.Key == Key.F4)
				Boid.Goal = !Boid.Goal;

			// move leader boid when autopilot is disabled
			if (!autoPilot)
			{
				if (e.Key == Key.W)
				{
					LeaderBoid.Velocity += LeaderBoid.Up * (Boid.MAX_SPEED / 20);
				}
				if (e.Key == Key.S)
				{
					LeaderBoid.Velocity -= LeaderBoid.Up * (Boid.MAX_SPEED / 20);
				}
				if (e.Key == Key.A)
				{
					LeaderBoid.Velocity -= LeaderBoid.Right * (Boid.MAX_SPEED / 20);
				}
				if (e.Key == Key.D)
				{
					LeaderBoid.Velocity += LeaderBoid.Right * (Boid.MAX_SPEED / 20);
				}
			}

			if (e.Key == Key.P)
			{
				// toggle auto/manual leader control
				if (e.Shift)
				{
					autoPilot = !autoPilot;
				}
				else
				{
					// pause/unpause
					if (State == GameState.Active)
						State = GameState.Paused;
					else
						State = GameState.Active;
				}
			}

			// quit
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

		private void OnMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.Button == MouseButton.Left)
			{
				if (State == GameState.Active)
					State = GameState.Paused;
				else
					State = GameState.Active;
			}
			if (e.Button == MouseButton.Right)
			{
				if (State == GameState.Active)
				{
					State = GameState.Paused;
					LogDebugInfo();
				}
				else
				{
					State = GameState.Active;
					shouldPause = true;
				}
			}
		}

		private void UpdateFlockMiddle()
		{
			FlockMiddlePos = Vector3.Zero;
			Boids.ForEach(boid => FlockMiddlePos += boid.Position);
			FlockMiddlePos /= Boids.Count;
		}

		private Vector3 GetRandomDir() => new Vector3(Utils.Random.Next(1000) / 1000f, Utils.Random.Next(1000) / 1000f, Utils.Random.Next(1000) / 1000f);

		private Vector3 GetRandomPosition(int? xzConstraint = null)
		{
			float boidX = Utils.Random.Next(xzConstraint ?? (GroundSize * 2 / 3)) - (GroundSize / 3);
			float boidY = Utils.Random.Next(2 * MinHeight, (MaxHeight - MinHeight) / 2);
			float boidZ = Utils.Random.Next(xzConstraint ?? (GroundSize * 2 / 3)) - (GroundSize / 3);

			if (Math.Abs(boidX) < Tower.Radius)
				boidX += 200 * Tower.Radius;
			if (Math.Abs(boidZ) < Tower.Radius)
				boidZ += 200 * Tower.Radius;

			return new Vector3(boidX, boidY, boidZ);
		}

		private void LogDebugInfo()
		{
			Console.WriteLine("==============================================================");
			Console.WriteLine("DEBUG INFO: ");
			Console.WriteLine("Boid Count: " + Boids.Count);
			Console.WriteLine("Separation: " + Boid.Sep);
			Console.WriteLine("Alignment: " + Boid.Ali);
			Console.WriteLine("Cohesion: " + Boid.Coh);
			Console.WriteLine("Follow Leader: " + Boid.Goal);
			Console.WriteLine("Max Boid Speed: " + Boid.MAX_SPEED);
			Console.WriteLine("Leader Boid:");
			Console.WriteLine("    Position: " + LeaderBoid.Position);
			Console.WriteLine("    Velocity: " + LeaderBoid.Velocity);
			Console.WriteLine("    Size: " + LeaderBoid.Size);
			Console.WriteLine("    Front: " + LeaderBoid.Front);
			Console.WriteLine("    Right: " + LeaderBoid.Right);
			Console.WriteLine("    Pitch: " + LeaderBoid.Pitch);
			Console.WriteLine("    Yaw: " + LeaderBoid.Yaw);
			Console.WriteLine("");
			Console.WriteLine("Boids: ");
			foreach (Boid b in Boids.OfType<Boid>())
			{
				Console.WriteLine("Boid");
				Console.WriteLine("    Position: " + b.Position);
				Console.WriteLine("    Velocity: " + b.Velocity);
				Console.WriteLine("    Size: " + b.Size);
				Console.WriteLine("    Front: " + b.Front);
				Console.WriteLine("    Right: " + b.Right);
				Console.WriteLine("    Pitch: " + b.Pitch);
				Console.WriteLine("    Yaw: " + b.Yaw);
			}
			Console.WriteLine("==============================================================");
			Console.WriteLine("");
		}
	}
}
