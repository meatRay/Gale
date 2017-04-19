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

namespace Gale
{
	class Prop : IRender
	{
		public Render Context;
		public Vec2 GetPosition() => Physics.GetPosition(); //{ get; private set; } = new Vector2(1, 1);
		public Vector2 TargetPosition { get; private set; } = new Vector2(1, 1);
		public float ZPosition = 0.0f;
		public float Height;
		public Sprite Image;
		public bool IsMoving { get; private set; }
		public Prop( World physics_context, Sprite image, float height, float mass )
		{
			Image = image;
			Height = height;
			Physics = physics_context.CreateBody(new BodyDef() { LinearDamping = mass, FixedRotation = true });
			var box = new PolygonDef() { Density = 1.0f };
			box.SetAsBox(image.UnitSize.X / 2f, (image.UnitSize.Y - Height) / 2f, new Vec2(image.UnitSize.X / 2, (image.UnitSize.Y - (Height / 2f)) / -2f), 0.0f);
			PhysicsShape = Physics.CreateShape(box);
			Physics.SetMassFromShapes();
		}
		public void Render( Shader shader_program )
		{
			var pos = GetPosition();
			var matrix = Matrix4.CreateTranslation(pos.X, pos.Y, 0.0f);
			GL.UniformMatrix4(shader_program.ModelMatLocation, false, ref matrix);
			GL.Uniform1(shader_program.ZLocation, ZPosition);
			Image.Render(shader_program);
		}

		public void MoveTo( Vector2 target_position, bool from_sprite_center = true )
		{
			TargetPosition = target_position;
			if (from_sprite_center)
			{
				var centr = new Vector2(Image.UnitSize.X, Image.UnitSize.Y - Height);
				TargetPosition -= centr / 2.0f;
			}
			IsMoving = true;
		}

		public float Speed = 1f;
		public Body Physics;
		public Shape PhysicsShape;
		public void Update( double delta_time )
		{
			if (IsMoving)
			{
				var pos = GetPosition();
				var dif = TargetPosition - new Vector2(pos.X, pos.Y);
				var len = dif.LengthFast;
				if (len > 0.0001)
				{
					var scale = Speed;// * (float)delta_time;
					if (true || scale < len)
					{
						dif.NormalizeFast();
						dif *= scale;
					}
					Physics.SetLinearVelocity(new Vec2(dif.X, dif.Y));
					//Position += dif;
				}
				else
					IsMoving = false;
			}
		}
	}
}
