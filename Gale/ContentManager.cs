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

		public Sprite MakeSprite(ComplexLS sprite_script)
		{
			var depth = sprite_script.Read<TokenLS<Pixels>>("DEPTH");
			return MakeSprite(sprite_script.Read<TokenLS<string>>("FILENAME"),
				depth != null ? depth.Value : 0);
		}
		public Sprite MakeSprite(string file_name, int z_depth, bool embedded_resource = false)
		{
			if (ActiveTextures.TryGetValue(file_name, out Sprite texture))
				return texture;
			return ActiveTextures[file_name] = Sprite.FromFile(file_name, GameContext.Window.RenderWorker.ShaderProgram, z_depth);
		}
	}
}
