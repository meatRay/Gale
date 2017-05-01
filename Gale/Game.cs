using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Box2DX.Collision;
using Box2DX.Common;
using Box2DX.Dynamics;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using Gale.Props;
using Gale.LScripts;
using System.IO;

namespace Gale
{
	class Game : IDisposable
	{
		public World PhysicsContext { get; private set; }
		public AABB AABBBounds;
		public Prop Player;
		public Render Display;
		public Vector2 Camera;
		public ContentManager Content { get; private set; }

		public Game(Render display_context)
		{
			Display = display_context;
			BindDisplay();
			AABBBounds = new AABB() { LowerBound = new Vec2(-100.0f, -100.0f), UpperBound = new Vec2(100.0f, 100.0f) };
			PhysicsContext = new World(AABBBounds, new Vec2(0.0f, 0.0f), false);
			Content = new ContentManager(this);
		}

		private void BindDisplay()
		{
			Display.UpdateFrame += Update;
			Display.MouseDown += OnMouseDown;
			Display.RenderFrame += Render;
		}
		private void UnbindDisplay()
		{
			Display.UpdateFrame -= Update;
			Display.MouseDown -= OnMouseDown;
			Display.RenderFrame -= Render;
		}

		protected void OnMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (!investigatemode && e.Button == MouseButton.Left)
			{
				var pos = new Vector4(
					(e.X - (Display.Width * 0.5f)) / (Display.Width * 0.5f),
					-((e.Y - (Display.Height * 0.5f)) / (Display.Height * 0.5f)),
					0.0f, 1.0f);
				Player.MoveTo((inv_proj * pos).Xy + Camera);
			}
			else if (e.Button == MouseButton.Middle)
			{
				investigatemode = !investigatemode;
				_music = investigatemode ? 0.2f : 0.0f;
			}
			else if (e.Button == MouseButton.Right)
			{
				var pos = ((inv_proj * new Vector4(
					(e.X - (Display.Width * 0.5f)) / (Display.Width * 0.5f),
					-((e.Y - (Display.Height * 0.5f)) / (Display.Height * 0.5f)),
					0.0f, 1.0f)).Xy + Camera).CreateVec2();
				Prop f_prop = null;
				foreach (var prop in ActiveLevel.Props.All)
					if (prop.PhysicsShape.TestPoint(prop.Physics.GetXForm(), pos))
					{
						f_prop = prop;
						break;
					}
				if (f_prop != null)
				{
					f_prop.Interact();
				}
			}
		}
		Prop Selector;
		double select_timr = 0.0;
		bool investigatemode = false;
		public void Update(object sender, FrameEventArgs e)
		{
			ActiveLevel.Update(e.Time);
			var pos = Player.GetPosition();
			var r_pos = new Vector2(pos.X, pos.Y);
			Camera += Vector2.Clamp((r_pos - Camera), new Vector2(-1), new Vector2(1)) * (float)e.Time * 2;
			PhysicsContext.Step((float)e.Time, 5, 5);
			Selector.Update(e.Time);
			if (investigatemode && (select_timr -= e.Time) <= 0)
			{
				select_timr = 0.0;
				var mouse = ((inv_proj * new Vector4(
						(Display.Mouse.X - (Display.Width * 0.5f)) / (Display.Width * 0.5f),
						-((Display.Mouse.Y - (Display.Height * 0.5f)) / (Display.Height * 0.5f)),
						0.0f, 1.0f)).Xy + Camera).CreateVec2();

				foreach (var prop in ActiveLevel.Props.All)
					if (prop.PhysicsShape.TestPoint(prop.Physics.GetXForm(), mouse))
					{
						Display.ShaderProgram.Highlight = Selector.Image;
						Selector.MoveTo(prop.GetPosition().CreateVector2() + new Vector2(prop.Image.UnitSize.X / 2.0f, prop.Image.UnitDepth / 2.0f));
						break;
					}
			}
			Selector.Visible = investigatemode;
		}
		Matrix4 inv_proj;
		Stopwatch timr = new Stopwatch();
		float _music;
		public void Render(object sender, FrameEventArgs e)
		{
			timr.Start();
			var proj = Matrix4.CreateOrthographic(Gale.Render.ViewTiles * Display.AspectRatio, Gale.Render.ViewTiles, -10.0f, 10.0f);

			inv_proj = proj.Inverted();

			var view = Matrix4.CreateTranslation(-Camera.X, -Camera.Y, 0.0f);

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			GL.UseProgram(Display.ShaderProgram.ShaderID);
			GL.UniformMatrix4(Display.ShaderProgram.ProjectionLocation, false, ref proj);
			GL.UniformMatrix4(Display.ShaderProgram.ViewLocation, false, ref view);

			GL.Uniform1(Display.ShaderProgram.MusicLocation, _music);

			var matrix = Matrix4.Identity;
			GL.UniformMatrix4(Display.ShaderProgram.ModelMatLocation, false, ref matrix);
			GL.Uniform1(Display.ShaderProgram.ZLocation, 0.0f);
			ActiveLevel.Render(Display.ShaderProgram);
			Display.SwapBuffers();
			Display.Title = $"FPS: {(1.0 / e.Time).ToString("F")}";
			timr.Stop();
			if (timr.ElapsedMilliseconds < 15)
				Thread.Sleep((1000 / 60) - (int)timr.ElapsedMilliseconds);
			timr.Reset();
		}
		public Level ActiveLevel;
		static void Main(string[] args)
		{
			using (var render = new Render())
			{
				using (var vert = Shader.Step.CompileFrom(ShaderResources.default_vert_shader, ShaderType.VertexShader))
				using (var frag = Shader.Step.CompileFrom(ShaderResources.default_frag_shader, ShaderType.FragmentShader))
					render.ShaderProgram = Shader.CompileFrom(vert, frag);
				using (var game = new Game(render))
				{
					game.Display = render;

					var script = LScript.CreateFrom(File.ReadAllText("Data/Office/room.txt"));
					game.ActiveLevel = game.Content.MakeLevel(script.Read<ComplexLS>("ROOM"));
					game.Selector = game.ActiveLevel.Props.SpawnProp(
						new PropTemplate(game.Content.MakeSprite("Img/selection.png", 64), "Selection"
						, 1.0f, 1.0f, false, true), Vec2.Zero);
					game.Selector.Speed = 100f;
					game.Selector.ZPosition = -1.0f;
					render.Run(60);
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
					UnbindDisplay();
					ActiveLevel.Dispose();
				}
				disposedValue = true;
			}
		}
		public void Dispose() => Dispose(true);
		#endregion
	}
}
