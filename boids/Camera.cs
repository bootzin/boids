using OpenTK;
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

		public Camera(Vector3? pos = null, Vector3? up = null, float? yaw = null, float? pitch = null)
		{
			Position = pos ?? Vector3.Zero;
			WorldUp = up ?? Vector3.UnitY;
			Yaw = yaw ?? -90f;
			Pitch = pitch ?? 0f;
			Zoom = 90f;
			MovementSpeed = 2.5f;
			MouseSensivity = 0.1f;
			Front = -Vector3.UnitZ;
			UpdateCameraVectors();
		}

		public Matrix4 GetViewMatrix()
		{
			return Matrix4.LookAt(Position, Position + Front, Up);
		}

		public void ProcessKeyboard(CameraMovement dir, float dt)
		{
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
		}

		public void ProcessMouse(float xOffset, float yOffset, bool constraintPitch = true)
		{
			xOffset *= MouseSensivity;
			yOffset *= MouseSensivity;

			Yaw += xOffset;
			Pitch += yOffset;

			if (constraintPitch)
			{
				if (Pitch > 89)
					Pitch = 89;
				if (Pitch < -89)
					Pitch = -89;
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
