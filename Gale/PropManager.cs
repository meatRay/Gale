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
    class PropManager : IDisposable
    {
        public Level LevelContext { get; private set; }
        public IEnumerable<Prop> All => _props;
        public PropManager(Level level_context)
        {
            LevelContext = level_context;
            _props = new List<Prop>();
        }

        public PropTemplate MakePropTemplate(ComplexLS template_script)
        {
            var sprt = LevelContext.Context.Content.MakeSprite(template_script.Read<ComplexLS>("SPRITE"));
            return new PropTemplate(sprt);
        }

        public Prop SpawnProp(ComplexLS prop_script)
        {
            var template = MakePropTemplate(prop_script.Read<ComplexLS>("TEMPLATE"));
            var x = (float)prop_script.Read<TokenLS>("X").Read<double>();
            var y = (float)prop_script.Read<TokenLS>("Y").Read<double>();
            var mass = (float)prop_script.Read<TokenLS>("MASS").Read<double>();
            var canmove = prop_script.ReadAll<LScript>("CANMOVE").Any();
            var collides = prop_script.ReadAll<LScript>("COLLIDES").Any();
            var makeplayer = prop_script.ReadAll<LScript>("PLAYER").Any();
            return SpawnProp(template, new Vec2(x, y), mass, canmove, collides, make_player: makeplayer);
        }

        public Prop SpawnProp(PropTemplate template, Vec2 position, float mass = 1f, bool can_move = true, bool collides = true, float friction = 1f, bool make_player = false)
        {
            var physics = LevelContext.Context.PhysicsContext.CreateBody(new BodyDef()
            {
                LinearDamping = mass * 10f * friction,
                FixedRotation = true,
                Position = position,
            });
            var box = new PolygonDef() { Density = can_move ? 1.0f : 0.0f, Friction = friction };
            box.Filter.MaskBits = (ushort)(collides ? 1 : 0);
            box.SetAsBox(template.Image.UnitSize.X / 2f, template.Image.UnitDepth / 2f, new Vec2(template.Image.UnitSize.X / 2, template.Image.UnitDepth / 2f), 0.0f);
            var shape = physics.CreateShape(box);
            physics.SetMassFromShapes();
            var prop = new Prop(template, LevelContext.Context.ActiveLevel, physics, shape);
            _props.Add(prop);
            if (make_player)
                LevelContext.Context.Player = prop;
            return prop;
        }

        private List<Prop> _props;

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    foreach (var prop in _props)
                        prop.Dispose();
                disposedValue = true;
            }
        }
        public void Dispose() => Dispose(true);
        #endregion
    }
}
