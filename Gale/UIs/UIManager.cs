using Gale.LScripts;
using Gale.UIs;
using Gale.Visuals;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gale
{
	class UIManager : IDisposable, IRender
	{
		public Game Context { get; private set; }
		public DialogueScreen Dialogue { get; private set; }
		public List<UI> Elements;

		public UI Studybox;
		public TextRender Studyline;
		public UIManager(Game context)
		{
			Context = context;
			Elements = new List<UI>();
			Dialogue = new DialogueScreen(this);
		}
		public void Cheat()
		{
			var actor = new Actor("Gale");
			actor.Moods["idle"] = Context.Content.MakeSprite("Img/Character/Gale_Dribble.png", 0, 0, 0);
			var line = new Line("Hello, world!", actor, "idle");
			var line2 = new Line("Here is some more text just for you.\nAnd some more text.\nAnd some more.", actor, "idle");
			var chat = new Dialogue(new Line[2] { line, line2 });
			Dialogue.Begin(chat);
		}

		public void SpawnUI(ComplexLS ui_script)
			=> Elements.Add(MakeUI(ui_script));
		public UI MakeUI(ComplexLS ui_script)
		{
			var sprite = Context.Content.MakeSprite(ui_script.Read<ComplexLS>("SPRITE"));
			float left = (float)ui_script.ReadToken<double>("LEFT");
			float right = (float)ui_script.ReadToken<double>("RIGHT");
			float top = (float)ui_script.ReadToken<double>("TOP");
			float bottom = (float)ui_script.ReadToken<double>("BOTTOM");
			var id = ui_script.Read<TokenLS<string>>("ID");
			var ui = new UI(sprite, left, bottom, right, top);
			ui.Visible = ui_script.Read("HIDDEN") == null;
			List<IRender> uis = new List<IRender>();
			foreach (var suic in ui_script.Read<ComplexLS>("CHILDREN").SubRunes)
			{
				if (suic.Word == "ELEMENT")
					uis.Add(MakeUI(suic as ComplexLS));
				else if (suic.Word == "TEXT")
					uis.Add(MakeText(suic as ComplexLS, right - left, 0.7f));
			}
			ui.Children = uis.ToArray();
			if (id != null)
				AssignUI(ui, id);
			return ui;
		}
		public TextRender MakeText(ComplexLS text_script, float line_length, float font_size)
		{
			float x = (float)text_script.ReadToken<double>("X");
			float y = (float)text_script.ReadToken<double>("Y");
			var str = Context.Content.Text.CompileString(
				text_script.ReadToken<string>("LINE"),
				new Vector2(x, y),
				Context.Window.RenderWorker,
				line_length / font_size);
			str.CalcFontSize(font_size);
			return str;
		}
		private void AssignUI(UI ui, string to)
		{
			switch (to)
			{
				case "descbar":
					Studybox = ui;
					Studyline = ui.Children[0] as TextRender;
					break;
				default: break;
			}
		}

		public void Update(double delta_time)
		{
			if (_studying && (_studyleft -= delta_time) <= 0.0)
				DisplayStudy(false);
		}

		double _studyleft;
		bool _studying;
		public void DisplayStudy(bool study_on, double time = 0.0)
		{
			Studybox.Visible = study_on;
			Studyline.Visible = study_on;
			_studyleft = time;
			_studying = study_on;
		}

		public void Render(Renderer render_context)
		{
			foreach (var ui in Elements)
			{
				render_context.ShaderProgram.Model.Default = Matrix4.Identity;
				ui.Render(render_context);
			}
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
					foreach (var ui in Elements)
						ui.Image.Dispose();
				disposedValue = true;
			}
		}
		public void Dispose()
			=> Dispose(true);
		#endregion
	}
}