using System;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using OpenTK.Graphics;

using OpenTK.Input;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using Box2DX.Dynamics;
using Box2DX.Collision;
using Box2DX.Common;

namespace Gale
{
	class Render : GameWindow
	{
		public Render()
			: base(800, // initial width
				600, // initial height
				GraphicsMode.Default,
				"dreamstatecoding",  // initial title
				GameWindowFlags.Default,
				DisplayDevice.Default,
				4, // OpenGL major version
				4, // OpenGL minor version
				GraphicsContextFlags.ForwardCompatible)
		{
			Props = new List<Prop>();
			Effects = new List<IRender>();
			AABBBounds = new AABB() { LowerBound = new Vec2(-100.0f, -100.0f), UpperBound = new Vec2(100.0f, 100.0f) };
			PhysicsContext = new World(AABBBounds, new Vec2(0.0f, 0.0f), false);
		}
		public World PhysicsContext;
		public AABB AABBBounds;
		public Shader ShaderProgram;
		public List<Prop> Props;
		public List<IRender> Effects;
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			GL.ClearColor(0.1f, 0.1f, 0.3f, 0f);
			//GL.Enable(EnableCap.DepthTest);
			//GL.DepthFunc(DepthFunction.Less);
			//GL.Disable(EnableCap.CullFace);
			//GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
			//GL.PatchParameter(PatchParameterInt.PatchVertices, 3);
			GL.Enable(EnableCap.Texture2D);
			GL.Enable(EnableCap.Blend);

			//GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.BlendEquation(BlendEquationMode.FuncAdd);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
		}

		float _time = 0.0f;
		const float ViewTiles = 8.0f;
		public float AspectRatio => (float)Width / Height;
		public Vector2 Camera = new Vector2(-0.0f, -0.0f);
		Stopwatch timr = new Stopwatch();
		protected override void OnRenderFrame(FrameEventArgs e)
		{
			timr.Start();
			base.OnRenderFrame(e);
			_time += (float)e.Time;
			var proj = Matrix4.CreateOrthographic(ViewTiles * AspectRatio, ViewTiles, -10.0f, 10.0f);
			//var proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver3 * 2.0f, AspectRatio, 0.1f, 1000f);
			inv_proj = proj.Inverted();

			var view = Matrix4.CreateTranslation(-Camera.X, -Camera.Y, 0.0f);

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			GL.UseProgram(ShaderProgram.ShaderID);
			GL.UniformMatrix4(ShaderProgram.ProjectionLocation, false, ref proj);
			GL.UniformMatrix4(ShaderProgram.ViewLocation, false, ref view);

			var matrix = Matrix4.Identity;
			GL.UniformMatrix4(ShaderProgram.ModelMatLocation, false, ref matrix);
			GL.Uniform1(ShaderProgram.ZLocation, 0.0f);
			Backdrop?.Render(ShaderProgram);
			// Order gets to be expensive over 10k props probably.  Find a cleaner solution!!
			foreach (var sprite in Props.OrderByDescending(p => p.GetPosition().Y + p.ZPosition))
				sprite.Render(ShaderProgram);
			SwapBuffers();
			Title = $"FPS: {(1.0 / e.Time).ToString("F")}";
			timr.Stop();
			if (timr.ElapsedMilliseconds < 15)
				Thread.Sleep((1000 / 60) - (int)timr.ElapsedMilliseconds);
			timr.Reset();
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);
			var pos = new Vector4(
				(e.X - (Width * 0.5f)) / (Width * 0.5f),
				-((e.Y - (Height * 0.5f)) / (Height * 0.5f)),
				0.0f, 1.0f);
			Player.MoveTo((inv_proj * pos).Xy + Camera);
		}

		Matrix4 inv_proj;
		public Sprite Backdrop;

		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			base.OnUpdateFrame(e);
			foreach (var prop in Props)
				prop.Update(e.Time);
			var pos = Player.GetPosition();
			var r_pos = new Vector2(pos.X, pos.Y);
			Camera+=Vector2.Clamp((r_pos-Camera), new Vector2(-1), new Vector2(1))*(float)e.Time * 2;
			PhysicsContext.Step((float)e.Time, 5, 5);
		}

		protected override void OnResize(EventArgs e)
		{
			GL.Viewport(0, 0, Width, Height);
		}
		public Prop Player;
		static void Main(string[] args)
		{
			using (var render = new Render())
			{
				Console.WriteLine(GL.GetString(StringName.Version));

				using (var vert = Shader.Step.CompileFrom(ShaderResources.default_vert_shader, ShaderType.VertexShader))
				using (var frag = Shader.Step.CompileFrom(ShaderResources.default_frag_shader, ShaderType.FragmentShader))
					render.ShaderProgram = Shader.CompileFrom(vert, frag);
				//render.Effects.Add(DotSet.FromFile(render.ShaderProgram, @"C:/Temp/data.txt"));
				var sprite = Sprite.FromFile("Data/Character/Gale_Dribble.png", render.ShaderProgram);
				var sprite2 = Sprite.FromFile("Resources/table_huge.png", render.ShaderProgram);
				render.Backdrop = Sprite.FromFile("Data/Img/woodfloor.png", render.ShaderProgram);
				render.Props = new List<Prop>() {
					new Prop(render.PhysicsContext, sprite,100.0f / Sprite.TileSize, 1.0f) { Context = render },
					new Prop(render.PhysicsContext, sprite2,24.0f / Sprite.TileSize, 10.0f) { Context = render } };
				render.VSync = VSyncMode.Off;
				render.Player=render.Props[0];

				//var audio = new OpenTK.Audio.AudioContext();
				var msc = new System.Media.SoundPlayer();

				render.Run(60);
			}
		}

		protected override void Dispose(bool manual)
		{
			if (!IsDisposed)
			{
				ShaderProgram.Dispose();
				Backdrop?.Dispose();
				foreach (var sprite in Props)
					sprite.Image.Dispose();
			}
			base.Dispose(manual);
		}
	}

	class Prop : IRender
	{
		public Render Context;
		public Vec2 GetPosition() => Physics.GetPosition(); //{ get; private set; } = new Vector2(1, 1);
		public Vector2 TargetPosition { get; private set; } = new Vector2(1, 1);
		public float ZPosition = 0.0f;
		public float Height;
		public Sprite Image;
		public bool IsMoving { get; private set; }
		public Prop(World physics_context, Sprite image, float height, float mass)
		{
			Image = image;
			Height = height;
			Physics = physics_context.CreateBody(new BodyDef() { LinearDamping = mass, FixedRotation = true });
			var box = new PolygonDef() { Density = 1.0f };
			box.SetAsBox(image.UnitSize.X/2f, (image.UnitSize.Y-Height)/2f, new Vec2(image.UnitSize.X/2, (image.UnitSize.Y-(Height/2f))/-2f), 0.0f);
			PhysicsShape = Physics.CreateShape(box);
			Physics.SetMassFromShapes();
		}
		public void Render(Shader shader_program)
		{
			var pos = GetPosition();
			var matrix = Matrix4.CreateTranslation(pos.X, pos.Y, 0.0f);
			GL.UniformMatrix4(shader_program.ModelMatLocation, false, ref matrix);
			GL.Uniform1(shader_program.ZLocation, ZPosition);
			Image.Render(shader_program);
		}

		public void MoveTo(Vector2 target_position, bool from_sprite_center = true)
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
		public void Update(double delta_time)
		{
			if (IsMoving)
			{
				var pos = GetPosition();
				var dif = TargetPosition - new Vector2(pos.X, pos.Y);
				var len = dif.LengthFast;
				if (len > 0.0001)
				{
					var scale = Speed;// * (float)delta_time;
					if (scale < len)
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
