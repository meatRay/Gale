using Gale.LScripts;
using Gale.Visuals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gale.UIs
{
	class DialogueScreen
	{
		public const float ChatboxHeight = 2.0f;
		public UI Textbox { get; private set; }
		public UI Speaker { get; private set; }
		public UIManager UIContext { get; private set; }
		public Dialogue CurrentChat { get; private set; }

		public bool Active { get; private set; } = false;
		public bool Visible
		{
			get => _visible;
			set
			{
				_visible = value;
				Textbox.Visible = value;
				Speaker.Visible = value;
			}
		}
		bool _visible;

		// Skipping implementing in UIManager for debugs sake.
		public DialogueScreen(UIManager context)
		{
			UIContext = context;
			var img = context.Context.Content.MakeSprite("Img/ui_blank.png", 0, 0, 0);
			Textbox = new UI(img, 0.2f, 0.2f, context.Context.Content.GameContext.UISize.X - 2.4f, ChatboxHeight);
			Speaker = new UI(img);
			context.Elements.Add(Textbox);
			context.Elements.Add(Speaker);
			Visible = false;
		}

		public void Begin(Dialogue dialogue)
		{
			CurrentChat = dialogue;
			ReloadArt(UIContext.Context.Content);
			Visible = true;
			UIContext.Context.Cutscene = true;
			Active = true;
		}

		public void Advance()
		{
			if (CurrentChat.Advance())
			{
				Visible = false;
				UIContext.Context.Cutscene = false;
				Active = false;
			}
			else
				ReloadArt(UIContext.Context.Content);
		}

		public void ReloadArt(ContentManager content)
		{

			Textbox.Children = new IRender[1]
				{ content.Text.CompileString(CurrentChat.CurrentLine.Text, new OpenTK.Vector2(0.3f,ChatboxHeight-0.35f), content.GameContext.Window.RenderWorker) };
			((TextRender)Textbox.Children[0]).CalcFontSize(0.35f);
			UIContext.Elements.Remove(Speaker);
			Speaker = new UI(CurrentChat.CurrentLine.Voice.Moods[CurrentChat.CurrentLine.Mood]);
			Speaker.FitTo(UIContext.Context.UISize.X - 2.2f, 0, 2, false);
			UIContext.Elements.Add(Speaker);
		}
	}

	class Line
	{
		public string Text { get; private set; }
		public Actor Voice { get; private set; }
		public string Mood { get; private set; }

		public Line(string text, Actor voice, string mood)
		{
			Text = text;
			Voice = voice;
			Mood = mood;
		}
	}
	class Dialogue
	{
		public Line[] Lines { get; private set; }
		public bool Finished { get; private set; }
		public Line CurrentLine => Lines[_atline];
		int _atline;

		public Dialogue(Line[] dialogue_lines)
		{
			Lines = dialogue_lines;
		}
		public bool Advance()
		{
			if (_atline < Lines.Length - 1)
				++_atline;
			else
				return Finished = true;
			return false;
		}
		public void Reset()
		{
			_atline = 0;
			Finished = false;
		}

		public static Dialogue FromScript(ComplexLS script, ContentManager content)
		{
			Dictionary<LScript, Actor> actors = new Dictionary<LScript, Actor>();
			Stack<Line> lines = new Stack<Line>();
			foreach (var line in script.ReadAll<ComplexLS>("LINE"))
			{
				Actor actor;
				var scriptactor = line.Read("ACTOR");
				if (scriptactor == null)
					actor = lines.Peek().Voice;
				else if (!actors.TryGetValue(scriptactor, out actor))
				{
					actor = Actor.FromScript(scriptactor, content);
					actors[scriptactor] = actor;
				}
				string text = line.Read<TokenLS<string>>("TEXT") ?? lines.Peek().Text;
				string mood = line.Read<TokenLS<string>>("MOOD") ?? lines.Peek().Mood;
				lines.Push(new Line(text.Replace("\r","").Replace("\t",""), actor, mood));
			}
			return new Dialogue(lines.Reverse().ToArray());
		}
	}
	class Actor
	{
		public string Name { get; private set; }
		public Dictionary<string, Sprite> Moods { get; private set; }

		public Actor(string name)
		{
			Name = name;
			Moods = new Dictionary<string, Sprite>();
		}
		public static Actor FromScript(LScript script, ContentManager content)
			=> FromScript(script as ComplexLS, content);
		public static Actor FromScript(ComplexLS script, ContentManager content)
		{
			string name = script.ReadToken<string>("NAME");
			var actor = new Actor(name);
			foreach (var mood in script.Read<ComplexLS>("MOODS").ReadAll<ComplexLS>("MOOD"))
			{
				var moodname = mood.ReadToken<string>("NAME");
				var img = content.MakeSprite(mood.Read<ComplexLS>("SPRITE"));
				actor.Moods[moodname] = img;
			}
			return actor;
		}
	}
}
