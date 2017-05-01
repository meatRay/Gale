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

namespace Gale.Props
{
	public static class VectorExtensions
	{
		public static Vector2 CreateVector2(this Vec2 physics_vec)
			=> new Vector2(physics_vec.X, physics_vec.Y);
		public static Vec2 CreateVec2(this Vector2 display_vec)
			=> new Vec2(display_vec.X, display_vec.Y);
	}
	class Prop : PropTemplate, IRender, IDisposable
	{
		public ComplexLS OnInteract { get; set; }
		public Level LevelContext { get; private set; }
		public Vec2 GetPosition() => Physics.GetPosition();
		public Vector2 TargetPosition { get; private set; } = new Vector2(1, 1);
		public bool IsMoving { get; private set; }
		public bool Visible = true;

		public Prop(PropTemplate template, Level level_context, Body physics, Shape physics_shape)
			: base(template.Image, template.Name, template.Mass, template.Friction, template.Collides, template.CanMove)
		{
			Image.AcquireUse();
			LevelContext = level_context;
			Physics = physics;
			PhysicsShape = physics_shape;
		}

		public void Render(Shader shader_program)
		{
			if (Visible)
			{
				var pos = GetPosition();
				_translation = Matrix4.CreateTranslation(pos.X, pos.Y, 0.0f);
				GL.UniformMatrix4(shader_program.ModelMatLocation, false, ref _translation);
				GL.Uniform1(shader_program.ZLocation, ZPosition);
				Image.Render(shader_program);
			}
		}

		public void Interact()
		{
			if (OnInteract == null)
				return;
			Console.WriteLine(OnInteract.Read<TokenLS<string>>("WRITE"));
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
		public Body Physics;
		public Shape PhysicsShape;

		/*Since it's a struct, 
		 * does this keep the Render function from allocating new space for a mat4 each render pass?
		 * just silly experiments.*/
		private Matrix4 _translation;

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
