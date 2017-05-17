using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Box2DX.Collision;
using Box2DX.Common;
using Box2DX.Dynamics;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using Gale.LScripts;
using Gale.Visuals;

namespace Gale.Props
{
	public static class VectorExtensions
	{
		public static Vector2 CreateVector2(this Vec2 physics_vec)
			=> new Vector2(physics_vec.X, physics_vec.Y);
		public static Vec2 CreateVec2(this Vector2 display_vec)
			=> new Vec2(display_vec.X, display_vec.Y);
		public static AnimationDirections CalcDirection(this Vec2 physics_vec)
		{
			int x = (int)System.Math.Round(physics_vec.X);
			int y = (int)System.Math.Round(physics_vec.Y);
			if (y == 1)
				return AnimationDirections.North;
			else if (x == -1)
				return AnimationDirections.West;
			else if (y == -1)
				return AnimationDirections.South;
			else
				return AnimationDirections.East;
		}
	}
	class Prop : PropTemplate, IRender, IDisposable
	{
		public ComplexLS OnInteract { get; set; }
		public ComplexLS OnTrigger { get; set; }
		public Level LevelContext { get; private set; }
		public Vec2 GetPosition() => Physics.GetPosition();
		public Vector2 TargetPosition { get; private set; } = new Vector2(1, 1);
		public bool IsMoving { get; private set; }
		public bool Visible = true;
		public Body Physics;
		public Shape PhysicsShape;
		bool isanimated = false;
		Vec2 lastposition;
		AnimationDirections lastdirection = AnimationDirections.South;

		public Prop(PropTemplate template, Level level_context, Body physics, Shape physics_shape)
			: base(template.Image, template.Name, template.Desc, template.Mass, template.Friction, template.Collides, template.CanMove)
		{
			Image.AcquireUse();
			if (Image is Animation animation)
			{
				isanimated = true;
				animation.UseSequence("idle", AnimationDirections.South);
			}
			LevelContext = level_context;
			Physics = physics;
			PhysicsShape = physics_shape;
			lastposition = GetPosition();
		}

		public void Render(Renderer render_context)
		{
			if (Visible)
			{
				var pos = GetPosition();
				float rt = Display.ViewTiles / 1024f;
				render_context.ShaderProgram.Model.Push(Matrix4.CreateTranslation(pos.X, pos.Y, 0.0f));
				render_context.ShaderProgram.Model.Write();
				GL.Uniform1(render_context.ShaderProgram.ZLocation, ZPosition);
				Image.Render(render_context);
				render_context.ShaderProgram.Model.Pop();
			}
		}

		public void MoveTo(Vector2 target_position, bool from_sprite_center = true)
		{
			TargetPosition = target_position;
			if (from_sprite_center)
			{
				var centr = new Vector2(Image.UnitSize.X, Image.UnitDepth);
				TargetPosition -= centr / 2.0f;
			}
			IsMoving = true;
		}

		public void Update(double delta_time)
		{
			if (isanimated && !Physics.IsSleeping())
			{
				var pos = GetPosition() - lastposition;
				if (pos.LengthSquared() >= 0.01f)
				{
					lastposition = GetPosition();
					pos.Normalize();
					var dir = pos.CalcDirection();
					if (dir != lastdirection)
					{
						((Animation)Image).UseSequence("idle", dir);
						lastdirection = dir;
					}
				}
			}
			if (IsMoving)
			{
				var pos = GetPosition();
				var dif = TargetPosition - pos.CreateVector2();
				var len = dif.LengthFast;
				if (len > 0.01)
				{
					var scale = Speed;// * (float)delta_time;
					if (len > scale * (float)delta_time)
					{
						dif.NormalizeFast();
						dif *= scale;
					}
					else
						dif /= (float)delta_time;
					Physics.SetLinearVelocity(dif.CreateVec2());
					//Position += dif;
				}
				else
				{
					IsMoving = false;
					if (!Collides)
						Physics.SetLinearVelocity(Vec2.Zero);
				}
			}
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					Image.ReleaseUse();
					Image.Dispose();
					PhysicsShape.Dispose();
					Physics.Dispose();
				}
				disposedValue = true;
			}
		}
		public void Dispose() => Dispose(true);
		#endregion
	}
}
