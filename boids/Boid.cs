using OpenTK;
using System;
using System.Collections.Generic;

namespace boids
{
	public class Boid : EngineObject
	{
		public bool Sep { get; set; } = true;
		public bool Goal { get; set; } = true;
		public bool Coh { get; set; } = true;
		public bool Ali { get; set; } = true;
		public Vector3 Front { get; set; }
		public Vector3 Up { get; set; }
		public Vector3 Right { get; set; }

		private const int MAX_SPEED = 120;

		public Boid(Model model, Vector3 position, Vector3 size, Vector3 front, Vector3? up = null, Vector3? color = null)
		{
			Model = model;
			Position = position;
			Size = size;
			Color = color ?? Vector3.One;
			Up = up ?? Vector3.UnitY;
			Front = front.Normalized();

			var vertRads = Math.Asin(-Front.Y);
			Pitch = Utils.Rad2Deg(vertRads);
			Yaw = Utils.Rad2Deg(Math.Acos(-Front.Z / Math.Cos(vertRads)));

			Right = new Vector3((float)Math.Cos(Utils.Deg2Rad(Yaw)), 0, (float)Math.Sin(Utils.Deg2Rad(Yaw)));
		}

		public void Move(List<EngineObject> objects, float dt)
		{
			Vector3 separation = Separation(objects, Sep);
			Vector3 cohesion = Cohesion(objects, Coh);
			Vector3 alignment = Alignment(objects, Ali);
			Vector3 objective = Objective(Goal);

			Velocity += separation + cohesion + alignment + objective;
			CheckBoundaries();
			Velocity = Vector3.Clamp(Velocity, -Vector3.One * MAX_SPEED, Vector3.One * MAX_SPEED);

			Vector3 oldPos = Position;

			Position += Velocity * dt;

			Front = (Position - oldPos).Normalized();
			Pitch = Math.Clamp(Utils.Rad2Deg(Math.Asin(-Front.Y)), -75, 75);
			Yaw = Math.Clamp(Utils.Rad2Deg(Math.Atan2(Front.X, Front.Z)), -179, 179);

			Right = Vector3.Cross(Front, Engine.Camera.WorldUp).Normalized();
			//Up = Vector3.Cross(Right, Front).Normalized();
		}

		public void MoveToPoint(Vector3 destination, float dt)
		{
			Velocity += Objective(true, destination);
			CheckBoundaries();
			Velocity = Vector3.Clamp(Velocity, -Vector3.One * MAX_SPEED, Vector3.One * MAX_SPEED);

			Vector3 oldPos = Position;

			Position += Velocity * dt;

			Front = (Position - oldPos).Normalized();
			Pitch = Math.Clamp(Utils.Rad2Deg(Math.Asin(-Front.Y)), -89, 89);
			Yaw = Math.Clamp(Utils.Rad2Deg(Math.Atan2(Front.X, Front.Z)), -179, 179);
			Right = Vector3.Cross(Front, Engine.Camera.WorldUp).Normalized();
		}

		private Vector3 Objective(bool goal, Vector3? objective = null)
		{
			if (!goal)
				return Vector3.Zero;
			return ((objective ?? (Engine.LeaderBoid.Position + (Engine.LeaderBoid.Size / 2))) - (Position + (Size / 2))) / 50;
		}

		private Vector3 Separation(List<EngineObject> objects, bool sep)
		{
			if (!sep)
				return Vector3.Zero;
			Vector3 ret = Vector3.Zero;
			objects.ForEach(obj =>
			{
				if (Vector3.Distance(obj.Position, Position) < 200)
					ret -= (obj.Position - Position) / 25;
			});
			return ret;
		}

		private Vector3 Cohesion(List<EngineObject> objects, bool coh)
		{
			if (!coh)
				return Vector3.Zero;
			Vector3 ret = Vector3.Zero;
			objects.ForEach(obj => ret += obj.Position);
			ret /= (objects.Count - 1);
			return (ret - Position) / 100;
		}

		private Vector3 Alignment(List<EngineObject> objects, bool ali)
		{
			if (!ali)
				return Vector3.Zero;
			Vector3 ret = Vector3.Zero;
			objects.ForEach(obj => ret += obj.Velocity);
			ret /= (objects.Count - 1);
			return (ret - Velocity) / 8;
		}

		private void CheckBoundaries()
		{
			if (Position.X < -Engine.GroundSize)
				Velocity = new Vector3(Velocity) { X = 1 };
			if (Position.X > Engine.GroundSize)
				Velocity = new Vector3(Velocity) { X = -1 };

			if (Position.Y < Engine.MinHeight)
				Velocity = new Vector3(Velocity) { Y = 1 };
			if (Position.Y > Engine.MaxHeight)
				Velocity = new Vector3(Velocity) { Y = -1 };

			if (Position.Z < -Engine.GroundSize)
				Velocity = new Vector3(Velocity) { Z = 1 };
			if (Position.Z > Engine.GroundSize)
				Velocity = new Vector3(Velocity) { Z = -1 };
		}
	}
}
