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
using Gale.Visuals;

namespace Gale
{
	class Game : IDisposable
	{
		public World PhysicsContext { get; private set; }
		public AABB AABBBounds;
		public Prop Player;
		public Display Window;
		public Vector2 Camera;
		public ContentManager Content { get; private set; }
		public List<UI> UIElements { get; private set; }

		public Game(Display window)
		{
			Window = window;
			BindDisplay();
			AABBBounds = new AABB() { LowerBound = new Vec2(-100.0f, -100.0f), UpperBound = new Vec2(100.0f, 100.0f) };
			PhysicsContext = new World(AABBBounds, new Vec2(0.0f, 0.0f), false);
			Content = new ContentManager(this);
			UIElements = new List<UI>();
		}

		private void BindDisplay()
		{
			Window.UpdateFrame += Update;
			Window.MouseDown += OnMouseDown;
			Window.RenderFrame += Render;
		}
		private void UnbindDisplay()
		{
			Window.UpdateFrame -= Update;
			Window.MouseDown -= OnMouseDown;
			Window.RenderFrame -= Render;
		}

		protected void OnMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (!investigatemode && e.Button == MouseButton.Left)
			{
				var pos = new Vector4(
					(e.X - (Window.Width * 0.5f)) / (Window.Width * 0.5f),
					-((e.Y - (Window.Height * 0.5f)) / (Window.Height * 0.5f)),
					0.0f, 1.0f);
				Player.MoveTo((inv_proj * pos).Xy + Camera);
			}
			else if (e.Button == MouseButton.Middle)
			{
				investigatemode = !investigatemode;
				_music = investigatemode ? 0.4f : 0.0f;
			}
			else if (investigatemode && e.Button == MouseButton.Left)
			{
				var pos = ((inv_proj * new Vector4(
					(e.X - (Window.Width * 0.5f)) / (Window.Width * 0.5f),
					-((e.Y - (Window.Height * 0.5f)) / (Window.Height * 0.5f)),
					0.0f, 1.0f)).Xy + Camera).CreateVec2();
				foreach (var prop in ActiveLevel.Props.All)
					if (prop.PhysicsShape.TestPoint(prop.Physics.GetXForm(), pos))
					{
						prop.Interact();
						break;
					}

			}
		}
		(Prop, Prop) Selector;
		double select_timr = 0.0;
		bool investigatemode = false;
		public void Update(object sender, FrameEventArgs e)
		{
			ActiveLevel.Update(e.Time);
			var pos = Player.GetPosition();
			var r_pos = new Vector2(pos.X, pos.Y);
			Camera += Vector2.Clamp((r_pos - Camera), new Vector2(-1), new Vector2(1)) * (float)e.Time * 2;
			PhysicsContext.Step((float)e.Time, 5, 5);
			Selector.Item1.Update(e.Time);
			Selector.Item2.Update(e.Time);
			if (investigatemode && (select_timr -= e.Time) <= 0)
			{
				select_timr = 0.0;
				var mouse = ((inv_proj * new Vector4(
						(Window.Mouse.X - (Window.Width * 0.5f)) / (Window.Width * 0.5f),
						-((Window.Mouse.Y - (Window.Height * 0.5f)) / (Window.Height * 0.5f)),
						0.0f, 1.0f)).Xy + Camera).CreateVec2();

				foreach (var prop in ActiveLevel.Props.All)
					if (prop != Selector.Item1 && prop != Selector.Item2 &&
						prop.OnInteract != null &&
						prop.PhysicsShape.TestPoint(prop.Physics.GetXForm(), mouse))
					{
						Selector.Item1.Visible = true;
						Selector.Item2.Visible = true;
						Window.RenderWorker.ShaderProgram.Highlight = prop.Image;
						Selector.Item1.MoveTo(prop.GetPosition().CreateVector2() + new Vector2(prop.Image.UnitSize.X / 2.0f, prop.Image.UnitDepth / 2.0f));
						Selector.Item2.MoveTo(prop.GetPosition().CreateVector2() + new Vector2(prop.Image.UnitSize.X / 2.0f, prop.Image.UnitSize.Y - 0.5f));
						break;
					}
			}
			else if (!investigatemode)
			{
				Selector.Item1.MoveTo(Player.GetPosition().CreateVector2());
				Selector.Item2.MoveTo(Player.GetPosition().CreateVector2());
				Selector.Item1.Visible = false;
				Selector.Item2.Visible = false;
			}
		}
		Matrix4 inv_proj;
		Stopwatch timr = new Stopwatch();
		float _music;
		public void Render(object sender, FrameEventArgs e)
		{
			timr.Start();
			var rendercontext = Window.RenderWorker;

			rendercontext.Begin();
			rendercontext.RebuildGameProjection();
			rendercontext.RebuildGameView(Camera);
			rendercontext.BuildGameScene();

			inv_proj = rendercontext.GameProjection.Inverted();

			rendercontext.ShaderProgram.Music.Default = _music;

			var matrix = Matrix4.Identity;
			GL.UniformMatrix4(rendercontext.ShaderProgram.ModelMatLocation, false, ref matrix);
			ActiveLevel.Render(rendercontext);

			rendercontext.BuildUIScene();
			foreach (var ui in UIElements)
				ui.Render(rendercontext);

			rendercontext.Display();
			Window.Title = $"FPS: {(1.0 / e.Time).ToString("F")}";
			timr.Stop();
			if (timr.ElapsedMilliseconds < 15)
				Thread.Sleep((1000 / 60) - (int)timr.ElapsedMilliseconds);
			timr.Reset();
		}
		public Level ActiveLevel;
		static void Main(string[] args)
		{
			using (var window = new Display())
			{
				using (var vert = Shader.Step.CompileFrom(ShaderResources.default_vert_shader, ShaderType.VertexShader))
				using (var frag = Shader.Step.CompileFrom(ShaderResources.default_frag_shader, ShaderType.FragmentShader))
					window.RenderWorker.ShaderProgram = Shader.CompileFrom(vert, frag);
				using (var game = new Game(window))
				{
					game.Window = window;

					var script = LScript.CreateFrom(File.ReadAllText("Data/Office/room.txt"));
					game.ActiveLevel = game.Content.MakeLevel(script.Read<ComplexLS>("ROOM"));
					var back = game.ActiveLevel.Props.SpawnProp(
						new PropTemplate(game.Content.MakeSprite("Img/selection.png", 64), "Selection"
						, 1.0f, 1.0f, false, true), Vec2.Zero);
					back.Speed = 100f;
					back.ZPosition = -1.0f;
					var front = game.ActiveLevel.Props.SpawnProp(
						new PropTemplate(game.Content.MakeSprite("Img/magnify.png", 64), "Magnify"
						, 1.0f, 1.0f, false, true), Vec2.Zero);
					front.Speed = 100f;
					front.ZPosition = 1.0f;
					game.Selector = (back, front);

					game.UIElements.Add(new UI(game.Content.MakeSprite("Img/magnify.png", 0), 0, 0, 1f, 0.1f));

					window.Run(60);
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
