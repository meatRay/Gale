using Box2DX.Collision;
using Box2DX.Common;
using Box2DX.Dynamics;
using Gale.Visuals;
using Gale.LScripts;
using Gale.Props;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gale
{
	class ContentManager
	{
		public Game GameContext { get; private set; }
		public TextService Text;
		public Dictionary<string, Sprite> ActiveTextures { get; private set; }
		public ContentManager(Game game_context)
		{
			GameContext = game_context;
			ActiveTextures = new Dictionary<string, Sprite>();
		}

		public Level MakeLevel(ComplexLS level_script)
		{
			var bounds = level_script.Read<ComplexLS>("BOUNDS");
			Vector2 min, max;
			min.X = (float)bounds.Read<TokenLS<double>>("LEFT");
			min.Y = (float)bounds.Read<TokenLS<double>>("BOTTOM");
			max.X = (float)bounds.Read<TokenLS<double>>("RIGHT");
			max.Y = (float)bounds.Read<TokenLS<double>>("TOP");
			var boundsbody = GameContext.PhysicsContext.CreateBody(new BodyDef() { FixedRotation = true });
			var def = new PolygonDef();
			float hrangey = (max.Y - min.Y) / 2;
			float hrangex = (max.X - min.X) / 2;
			def.SetAsBox(1, hrangey, new Vec2(min.X - 1, hrangey), 0);
			boundsbody.CreateShape(def);
			def.SetAsBox(1, hrangey, new Vec2(max.X + 1, hrangey), 0);
			boundsbody.CreateShape(def);
			def.SetAsBox(hrangex, 1, new Vec2(hrangex, min.Y - 1), 0);
			boundsbody.CreateShape(def);
			def.SetAsBox(hrangex, 1, new Vec2(hrangex, max.Y + 1), 0);
			boundsbody.CreateShape(def);
			var lvl = new Level(GameContext, null);
			lvl.Backdrop = MakeSprite(level_script.Read<ComplexLS>("BACKDROP"));
			foreach (var sprop in level_script.Read<ComplexLS>("PROPS").ReadAll<ComplexLS>("PROP"))
				lvl.Props.SpawnProp(sprop);
			return lvl;
		}

		public Sprite MakeAnimation(ComplexLS script)
		{
			string filename = script.Read<TokenLS<string>>("FILENAME");
			if (ActiveTextures.TryGetValue(filename, out Sprite texture))
				return texture;
			var seqs = new List<AnimationSequence>();
			int handle, w, h;
			handle = Sprite.LoadHandle(filename, out w, out h);
			foreach (var scriptsequence in script.ReadAll<ComplexLS>("SEQUENCE"))
			{
				string name = scriptsequence.ReadToken<string>("NAME");
				var dirs = AnimationDirections.None;
				foreach (var scriptdir in scriptsequence.Read<ComplexLS>("DIRECTIONS").SubRunes)
					if (Enum.TryParse<AnimationDirections>(scriptdir.Word, true, out var fnddir))
						dirs |= fnddir;
				var locs = new List<(int X, int Y)>();
				foreach (var scriptloc in scriptsequence.ReadAll<ComplexLS>("STEP"))
					locs.Add((scriptloc.ReadToken<Pixels>("X"), scriptloc.ReadToken<Pixels>("Y")));
				var scriptoffset = scriptsequence.Read<ComplexLS>("OFFSET");
				var offset_x = scriptoffset != null ? scriptoffset.Read<TokenLS<Pixels>>("X") : null;
				var offset_y = scriptoffset != null ? scriptoffset.Read<TokenLS<Pixels>>("Y") : null;
				seqs.Add(Sprite.MakeSteps(name,
					locs,
					(scriptsequence.ReadToken<Pixels>("WIDTH"), scriptsequence.ReadToken<Pixels>("HEIGHT")),
					(offset_x != null ?offset_x.Value : 0, offset_y != null ? offset_y.Value : 0),
					(w,h), dirs));
			}
			var animation = new Animation(seqs.ToArray(), handle);
			return ActiveTextures[filename] = animation;
		}

		public Sprite MakeSprite(ComplexLS sprite_script)
		{
			var depth = sprite_script.Read<TokenLS<Pixels>>("DEPTH");
			var offset = sprite_script.Read<ComplexLS>("OFFSET");
			var offset_x = offset != null ? offset.Read<TokenLS<Pixels>>("X") : null;
			var offset_y = offset != null ? offset.Read<TokenLS<Pixels>>("Y") : null;
			return MakeSprite(sprite_script.Read<TokenLS<string>>("FILENAME"),
				depth != null ? depth.Value : 0,
				offset_x != null ? offset_x.Value : 0,
				offset_y != null ? offset_y.Value : 0);
		}
		public Sprite MakeSprite(string file_name, int z_depth, int offset_x, int offset_y, bool embedded_resource = false)
		{
			if (ActiveTextures.TryGetValue(file_name, out Sprite texture))
				return texture;
			return ActiveTextures[file_name] = Sprite.FromFile(file_name,
				GameContext.Window.RenderWorker.ShaderProgram, z_depth, offset_x, offset_y);
		}
	}
}
