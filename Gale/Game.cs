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
using Gale.UIs;

namespace Gale
{
	class Game : IDisposable
	{
		public World PhysicsContext { get; private set; }
		public AABB Bounds;
		public Prop Player;
		public Display Window;
		public Vector2 Camera;
		public Dictionary<string, int> Journal { get; private set; } = new Dictionary<string, int>();
		public ContentManager Content { get; private set; }
		public UIManager GUI { get; private set; }
		public Vector2 UISize { get; private set; }
		public Random RNG { get; private set; }

		(Prop Highlight, Prop Glass) Selector;
		Prop _selected;
		double select_timr = 0.1;
		bool investigatemode = false;
		string _titleroot;

		public Game(Display window)
		{
			_titleroot = window.Title;
			Window = window;
			UISize = new Vector2(10, 10 / Window.AspectRatio);
			BindDisplay();
			Bounds = new AABB() { LowerBound = new Vec2(-100.0f, -100.0f), UpperBound = new Vec2(100.0f, 100.0f) };
			PhysicsContext = new World(Bounds, new Vec2(0.0f, 0.0f), false);
			Content = new ContentManager(this);
			GUI = new UIManager(this);
			PhysicsContext.SetContactListener(new TriggerCallback(Trigger));
			RNG = new Random();
		}
		private void Trigger(Prop prop)
		{
			if (prop.OnTrigger != null)
				triggers.AddRange(prop.OnTrigger.SubRunes);
		}
		private List<LScript> triggers = new List<LScript>();
		private void DoEvent(LScript step)
		{
			if (step.Word == "WRITE" && step is ComplexLS write)
			{
				GUI.Studyline = Content.Text.CompileString(write.ReadToken<string>("TEXT"),
					new Vector2(0.1f, 0.65f), Window.RenderWorker);
				GUI.Studybox.Children[0] = GUI.Studyline;
				GUI.Studyline.CalcFontSize(0.35f);
				GUI.DisplayStudy(true, time: write.ReadToken<double>("FOR"));
			}
			else if (step.Word == "WAIT" && step is TokenLS<double> time)
				waiting_time = time;
			else if (step.Word == "JOURNAL" && step is ComplexLS journal)
			{
				string entry = journal.Read<TokenLS<string>>("ENTRY");
				Journal[entry] = (int)journal.Read<TokenLS<double>>("TO");
			}
			else if (step.Word == "IFJOURNAL" && step is ComplexLS if_journal)
			{
				string entry = if_journal.Read<TokenLS<string>>("ENTRY");
				int to_check = (int)if_journal.Read<TokenLS<double>>("IS");
				if ((Journal.TryGetValue(entry, out int val) || to_check == 0) && val == to_check)
					foreach (var j_event in if_journal.Read<ComplexLS>("THEN").SubRunes)
						DoEvent(j_event);
				else
					foreach (var j_event in if_journal.Read<ComplexLS>("ELSE").SubRunes)
						DoEvent(j_event);
			}
			else if (step.Word == "DIALOGUE" && step is ComplexLS chat)
				GUI.Dialogue.Begin(Dialogue.FromScript(chat, Content));
			else if (step.Word == "SPAWN" && step is ComplexLS complex)
				ActiveLevel.Props.SpawnProp(complex.Read<ComplexLS>("PROP"));
			else if (step.Word == "RANDOM" && step is ComplexLS random)
				DoEvent(random.SubRunes[RNG.Next(random.SubRunes.Length)]);
			else if (step.Word == "CUTSCENE" && step is TokenLS<string> cutscene)
			{
				if (bool.TryParse(cutscene, out Cutscene))
				{
					Player.MoveTo(Player.GetPosition().CreateVector2());
					investigatemode = false;
				}
			}
			else if (step.Word == "MOVE" && step is ComplexLS movement)
			{
				string p_name = movement.Read<TokenLS<string>>("PROP");
				var prop = ActiveLevel.Props.All.Where(p => p.Name == p_name).FirstOrDefault();
				if (prop != null)
					prop.MoveTo(new Vector2((float)movement.Read<TokenLS<double>>("X"), (float)movement.Read<TokenLS<double>>("Y")));
			}
			else if (step.Word == "DESTROY" && step is TokenLS<string> destroy_name)
				ActiveLevel.Props.Destroy(ActiveLevel.Props.All.Where(p => p.Name == destroy_name).FirstOrDefault());
			else if (step.Word == "MAKEPLAYER" && step is TokenLS<string> player_name)
				Player = ActiveLevel.Props.All.Where(p => p.Name == player_name).FirstOrDefault() ?? Player;
		}
		private void BindDisplay()
		{
			Window.UpdateFrame += Update;
			Window.MouseDown += OnMouseDown;
			Window.RenderFrame += Render;
			Window.KeyDown += OnKeyDown;
		}
		private void UnbindDisplay()
		{
			Window.UpdateFrame -= Update;
			Window.MouseDown -= OnMouseDown;
			Window.RenderFrame -= Render;
			Window.KeyDown -= OnKeyDown;
		}
		public bool Cutscene = false;
		protected void OnKeyDown(object sender, KeyboardKeyEventArgs e)
		{
			if (e.Key == Key.I)
			{
				investigatemode = !investigatemode;
				if (!investigatemode)
					GUI.DisplayStudy(false);
				_music = investigatemode ? 0.4f : 0.0f;
			}
		}
		protected void OnMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (Cutscene)
			{
				if (GUI.Dialogue.Active && e.Button == MouseButton.Left)
					GUI.Dialogue.Advance();
			}
			else if (e.Button == MouseButton.Middle)
			{
				investigatemode = !investigatemode;
				if (!investigatemode)
					GUI.DisplayStudy(false);
				_music = investigatemode ? 0.4f : 0.0f;
			}
			else
			{
				bool fnd_any = false;
				var pos = ((inv_proj * new Vector4(
					(e.X - (Window.Width * 0.5f)) / (Window.Width * 0.5f),
					-((e.Y - (Window.Height * 0.5f)) / (Window.Height * 0.5f)),
					0.0f, 1.0f)).Xy + Camera).CreateVec2();
				foreach (var prop in ActiveLevel.Props.All)
					if (prop != Selector.Highlight && prop != Selector.Glass)
						if (prop.PhysicsShape.TestPoint(prop.Physics.GetXForm(), pos))
						{
							if (investigatemode && e.Button == MouseButton.Left && !string.IsNullOrEmpty(prop.Desc))
								DoEvent(new ComplexLS("WRITE", new LScript[2]{
									new TokenLS<string>("TEXT", prop.Desc),
									new TokenLS<double>("FOR", 5.0)}));
							else if (!investigatemode)
							{
								if (e.Button == MouseButton.Right)
									Console.WriteLine($"{prop.Name} : ({prop.GetPosition().X}, {prop.GetPosition().Y})");
								else
								{
									if (prop.OnInteract == null)
										break;
									var dif = prop.GetPosition() - Player.GetPosition();
									dif.Normalize();
									Player.MoveTo((prop.GetPosition() - (dif*2.0f)).CreateVector2());
									foreach (var action in prop.OnInteract.SubRunes)
										DoEvent(action);
									break;
								}
							}
						}
				if (!investigatemode && e.Button == MouseButton.Left && !fnd_any)
					Player.MoveTo(pos.CreateVector2());
			}

			//}
		}
		double waiting_time = 0.0;
		public void Update(object sender, FrameEventArgs e)
		{
			if (waiting_time > 0.0)
				waiting_time -= e.Time;
			else if (triggers.Count > 0)
			{
				var t = triggers.First();
				DoEvent(t);
				triggers.Remove(t);
			}

			if (investigatemode && _selected != null)
			{
				Selector.Highlight.MoveTo(_selected.GetPosition().CreateVector2() + new Vector2(_selected.Image.UnitSize.X / 2.0f, _selected.Image.UnitDepth / 2.0f));
				Selector.Glass.MoveTo(_selected.GetPosition().CreateVector2() + new Vector2(_selected.Image.UnitSize.X / 2.0f, _selected.Image.UnitSize.Y - 0.5f));
			}
			GUI.Update(e.Time);
			ActiveLevel.Update(e.Time);
			var r_pos = Player.GetPosition().CreateVector2() + (Player.Image.UnitSize / 2);
			Camera += Vector2.Clamp((r_pos - Camera), new Vector2(-1), new Vector2(1)) * (float)e.Time * 2;
			PhysicsContext.Step((float)e.Time, 5, 5);
			Selector.Highlight.Update(e.Time);
			Selector.Glass.Update(e.Time);
			if (investigatemode && (select_timr -= e.Time) <= 0)
			{
				select_timr = 0.1;
				var mouse = ((inv_proj * new Vector4(
						(Window.Mouse.X - (Window.Width * 0.5f)) / (Window.Width * 0.5f),
						-((Window.Mouse.Y - (Window.Height * 0.5f)) / (Window.Height * 0.5f)),
						0.0f, 1.0f)).Xy + Camera).CreateVec2();
				bool fnd = false;
				foreach (var prop in ActiveLevel.Props.All)
					if (prop != Selector.Highlight && prop != Selector.Glass &&
						!string.IsNullOrEmpty(prop.Desc) &&
						prop.PhysicsShape.TestPoint(prop.Physics.GetXForm(), mouse))
					{
						fnd = true;
						Window.RenderWorker.ShaderProgram.Highlight = prop.Image;
						_selected = prop;
						break;
					}
				_selected = fnd ? _selected : null;
				Selector.Highlight.Visible = fnd;
				Selector.Glass.Visible = fnd;
			}
			else if (!investigatemode)
			{
				Selector.Highlight.MoveTo(Player.GetPosition().CreateVector2());
				Selector.Glass.MoveTo(Player.GetPosition().CreateVector2());
				Selector.Highlight.Visible = false;
				Selector.Glass.Visible = false;
			}
		}
		Matrix4 inv_proj;
		Stopwatch timr = new Stopwatch();
		float _music;
		double fps = 0.0;
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

			rendercontext.ShaderProgram.Model.Write(Matrix4.Identity);
			ActiveLevel.Render(rendercontext);
			rendercontext.BuildUIScene();
			GUI.Render(rendercontext);
			rendercontext.Display();
			fps = (fps + (1.0 / e.Time)) / 2.0;
			Window.Title = $"{_titleroot} FPS: {fps.ToString("F")}";
			timr.Stop();
			if (timr.ElapsedMilliseconds < 15)
				Thread.Sleep((1000 / 60) - (int)timr.ElapsedMilliseconds);
			timr.Reset();
		}
		public Level ActiveLevel;
		static void Main(string[] args)
		{
			var gm = LScript.CreateFrom(File.ReadAllText("Data/game.lscp")).Read<ComplexLS>("GAME");
			var constants = gm.Read<ComplexLS>("CONSTANTS");

			Sprite.TileSize = constants.ReadToken<Pixels>("TILESIZE");
			Sprite.UseLinear = constants.ReadToken<string>("FILTER") == "linear";
			Display.ViewTiles = (float)constants.ReadToken<double>("VIEWTILES");
			string loadlevel = gm.ReadToken<string>("LOADLEVEL");

			var wndw = constants.Read<ComplexLS>("WINDOW");
			using (var window = new Display(wndw.ReadToken<Pixels>("WIDTH"), wndw.ReadToken<Pixels>("HEIGHT"), wndw.ReadToken<string>("TITLE")))
			{
				using (var vert = Shader.Step.CompileFrom(ShaderResources.default_vert_shader, ShaderType.VertexShader))
				using (var frag = Shader.Step.CompileFrom(ShaderResources.default_frag_shader, ShaderType.FragmentShader))
					window.RenderWorker.ShaderProgram = Shader.CompileFrom(vert, frag);
				using (var game = new Game(window))
				{
					game.Window = window;

					var script = LScript.CreateFrom(File.ReadAllText(loadlevel));
					game.ActiveLevel = game.Content.MakeLevel(script.Read<ComplexLS>("ROOM"));
					var back = game.ActiveLevel.Props.SpawnProp(
						new PropTemplate(game.Content.MakeSprite("Img/selection.png", 64, 0, 0), "Selection", ""
						, 1.0f, 1.0f, false, true), Vec2.Zero);
					back.Speed = 100f;
					back.ZPosition = -1.0f;
					var front = game.ActiveLevel.Props.SpawnProp(
						new PropTemplate(game.Content.MakeSprite("Img/magnify.png", 64, 0, 0), "Magnify", ""
						, 1.0f, 1.0f, false, true), Vec2.Zero);
					front.Speed = 100f;
					front.ZPosition = 9.0f;
					game.Selector = (back, front);

					var text = TextService.FromFile(Environment.ExpandEnvironmentVariables("%WINDIR%/Fonts/Arial.ttf"), game.Window.RenderWorker);
					game.Content.Text = text;
					//game.GUI.Cheat();
					var ui_scripts = gm.Read<ComplexLS>("UI");
					foreach (var uisc in ui_scripts.ReadAll<ComplexLS>("ELEMENT"))
						game.GUI.SpawnUI(uisc);
					//var box = new UI(game.Content.MakeSprite("Img/ui_blank.png", 0, 0, 0), 0.0f, 0.0f, game.UISize.X, 1);
					//game.GUI.Elements.Add(box);

					//var line = text.CompileString("Hellopq, World!", new Vector2(0.2f, 0.2f), game.Window.RenderWorker);
					//box.Children = new IRender[1] { line };
					game.GUI.DisplayStudy(false);
					game.Camera = game.Player.GetPosition().CreateVector2();
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
					GUI.Dispose();
				}
				disposedValue = true;
			}
		}
		public void Dispose() => Dispose(true);
		#endregion
	}
}
