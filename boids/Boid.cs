﻿using OpenTK;
using System;
using System.Collections.Generic;

namespace boids
{
	public class Boid : EngineObject
	{
		public bool Sep { get; set; } = true;
		public bool Coh { get; set; } = true;
		public bool Ali { get; set; } = true;
		public Vector3 Front { get; set; }
		public Vector3 Up { get; set; }
		public Vector3 Right { get; set; }

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

		public void Move(List<EngineObject> objects, float dt, int width, int height)
		{
			Vector3 separation = Separation(objects, Sep);
			Vector3 cohesion = Cohesion(objects, Coh);
			Vector3 alignment = Alignment(objects, Ali);

			Velocity += separation + cohesion + alignment;
			Console.WriteLine(Velocity);
			Velocity = Vector3.Clamp(Velocity, -Vector3.One * 50, Vector3.One * 50);

			Vector3 oldPos = Position;

			Position += Velocity * dt;

			Front = (Position - oldPos).Normalized();
			var vertRads = Math.Asin(-Front.Y);
			Pitch = Utils.Rad2Deg(vertRads);
			Yaw = Utils.Rad2Deg(Math.Acos(-Front.Z / Math.Cos(vertRads)));

			Right = new Vector3((float)Math.Cos(Utils.Deg2Rad(Yaw)), 0, (float)Math.Sin(Utils.Deg2Rad(Yaw)));
		}

		private Vector3 Separation(List<EngineObject> objects, bool sep)
		{
			if (!sep)
				return Vector3.Zero;
			Vector3 ret = Vector3.Zero;
			objects.ForEach(obj =>
			{
				if (Vector3.Distance(obj.Position, Position) < 1)
					ret -= obj.Position - Position;
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

		private Vector3 Boundaries(int width, int height)
		{
			var t = Vector3.Clamp(Velocity, new Vector3(-1f), new Vector3(1));
			return t;
		}
	}
}