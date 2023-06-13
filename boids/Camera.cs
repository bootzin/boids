using OpenTK.Mathematics;
using System;

namespace boids
{
	public enum CameraMovement
	{
		FORWARD,
		BACKWARD,
		LEFT,
		RIGHT
	}

	public enum CameraType
	{
		Behind,
		Parallel,
		Tower,
		Free
	}

	public class Camera
	{
		public Vector3 Position { get; set; }
		public Vector3 Front { get; set; }
		public Vector3 Up { get; set; }
		public Vector3 Right { get; set; }
		public Vector3 WorldUp { get; set; }
		public float Yaw { get; set; }
		public float Pitch { get; set; }
		public float MovementSpeed { get; set; }
		public float MouseSensivity { get; set; }
		public float Zoom { get; set; }
		public CameraType Type { get; set; }

		private bool Movable;
		private bool Orientable;
		private Vector3 TowerCameraPosition = new Vector3(-150, 333 * Engine.Tower.Height, 1);
		private const int CameraDistanceFactor = 10;
		private const int BoidDistance = 200;

		public Camera(Vector3? pos = null, Vector3? up = null, float? yaw = null, float? pitch = null, CameraType type = CameraType.Tower)
		{
			Position = pos ?? new Vector3(0, Engine.MinHeight, 0);
			WorldUp = up ?? Vector3.UnitY;
			Yaw = yaw ?? -90f;
			Pitch = pitch ?? 0f;
			Zoom = 90f;
			MovementSpeed = 200f;
			MouseSensivity = .8f;
			Front = -Vector3.UnitZ;
			SetCameraType(type);
			UpdateCameraVectors();
			OrientToBoids();
		}

		public void Update()
		{
			switch (Type)
			{
				case CameraType.Behind:
				case CameraType.Parallel:
					PositionCamera(Type);
					OrientToBoids();
					break;
				case CameraType.Tower:
					OrientToBoids();
					break;
				case CameraType.Free:
					break;
			}
		}

		public void SetCameraType(CameraType type)
		{
			Type = type;
			switch (type)
			{
				case CameraType.Behind:
				case CameraType.Parallel:
					Movable = false;
					Orientable = false;
					PositionCamera(type);
					OrientToBoids();
					break;
				case CameraType.Tower:
					Movable = false;
					Orientable = false;
					Position = TowerCameraPosition;
					OrientToBoids();
					break;
				case CameraType.Free:
					Movable = true;
					Orientable = true;
					var vertRads = Math.Asin(-Front.Y);
					Pitch = Utils.Rad2Deg(vertRads);
					Yaw = Utils.Rad2Deg(Math.Acos(-Front.Z / Math.Cos(vertRads)));
					UpdateCameraVectors();
					break;
			}
		}

		private void OrientToBoids()
		{
			Vector3 front;
			//if (Type == CameraType.Parallel || Type == CameraType.Behind)
				front = Engine.LeaderBoid.Position + (Engine.LeaderBoid.Size / 2) - Position;
			//else
			//	front = Engine.FlockMiddlePosAbsolute - Position;
			Front = front.Normalized();
			Up = Engine.LeaderBoid.Up;
			Right = Vector3.Cross(Front, Up).Normalized();
		}

		private void PositionCamera(CameraType type)
		{
			var leader = Engine.LeaderBoid;

			if (type == CameraType.Parallel)
			{
				Vector3 right = leader.Right;

				right *= BoidDistance + (CameraDistanceFactor * Math.Min(Math.Max((Engine.Boids.Count + 1), 50), 75));

				Position = leader.Position - right;
				Position = new Vector3(Position.X, Math.Clamp(Position.Y, Engine.MinHeight, Engine.MaxHeight), Position.Z);
				return;
			}

			Vector3 front = leader.Front;
			front *= BoidDistance + (CameraDistanceFactor * 2 * Math.Min(Math.Max((Engine.Boids.Count + 1), 50), 75));

			Position = leader.Position - front;
			Position = new Vector3(Position.X, Math.Clamp(Position.Y, Engine.MinHeight, Engine.MaxHeight), Position.Z);
		}

		public Matrix4 GetViewMatrix()
		{
			return Matrix4.LookAt(Position, Position + Front, Up);
		}

		public void ProcessKeyboard(CameraMovement dir, float dt)
		{
			if (!Movable)
				return;

			float velocity = MovementSpeed * dt;
			switch (dir)
			{
				case CameraMovement.FORWARD:
					Position += Front * velocity;
					break;
				case CameraMovement.BACKWARD:
					Position -= Front * velocity;
					break;
				case CameraMovement.LEFT:
					Position -= Right * velocity;
					break;
				case CameraMovement.RIGHT:
					Position += Right * velocity;
					break;
			}

			Position = new Vector3(Position.X, Math.Clamp(Position.Y, Engine.MinHeight, Engine.MaxHeight), Position.Z);
		}

		public void ProcessMouse(float xOffset, float yOffset, bool constraintPitch = true)
		{
			if (!Orientable)
				return;

			Yaw += xOffset * MouseSensivity;
			Pitch += yOffset * MouseSensivity;

			if (constraintPitch)
			{
				Pitch = Math.Clamp(Pitch, -89, 89);
			}

			UpdateCameraVectors();
		}

		public void ProcessMouseScroll(float yOffset)
		{
			Zoom -= yOffset;
			if (Zoom < 1)
				Zoom = 1;
			if (Zoom > 90)
				Zoom = 90;
		}

		private void UpdateCameraVectors()
		{
			float x = (float)Math.Cos(Utils.Deg2Rad(Yaw)) * (float)Math.Cos(Utils.Deg2Rad(Pitch));
			float y = (float)Math.Sin(Utils.Deg2Rad(Pitch));
			float z = (float)Math.Sin(Utils.Deg2Rad(Yaw)) * (float)Math.Cos(Utils.Deg2Rad(Pitch));
			Front = new Vector3(x, y, z).Normalized();
			Right = Vector3.Cross(Front, WorldUp).Normalized();
			Up = Vector3.Cross(Right, Front).Normalized();
		}
	}
}
