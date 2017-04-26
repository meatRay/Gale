using Box2DX.Collision;
using Box2DX.Common;
using Box2DX.Dynamics;
using Gale.LScripts;
using Gale.Props;
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
            var lvl = new Level(GameContext);
            lvl.Backdrop = MakeSprite(level_script.Read<ComplexLS>("BACKDROP"));
            foreach (var sprop in level_script.Read<ComplexLS>("PROPS").ReadAll<ComplexLS>("PROP"))
                lvl.Props.SpawnProp(sprop);
            return lvl;
        }

        public Sprite MakeSprite(ComplexLS sprite_script)
        {
            var depth = sprite_script.Read<TokenLS>("DEPTH");
            return MakeSprite(sprite_script.Read<TokenLS>("FILENAME").Read<string>(),
                depth != null ? int.Parse(depth.Read<string>().TrimEnd('p', 'x')) : 0);
        }
        public Sprite MakeSprite(string file_name, int z_depth, bool embedded_resource = false)
        {
            if (ActiveTextures.TryGetValue(file_name, out Sprite texture))
                return texture;
            return ActiveTextures[file_name] = Sprite.FromFile(file_name, GameContext.Display.ShaderProgram, z_depth);
        }
    }
}
