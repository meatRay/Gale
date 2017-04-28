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
            var name_s = template_script.Read<TokenLS<string>>("NAME");
            string name = name_s != null ? name_s.Value : "UNKNOWN";
            float mass = (float)template_script.Read<TokenLS<double>>("MASS");
            bool canmove = template_script.ReadAll<LScript>("CANMOVE").Any();
            bool collides = template_script.ReadAll<LScript>("COLLIDES").Any();
            float friction = 1.0f;
            var s_friction = template_script.Read<TokenLS<double>>("FRICTION");
            if (s_friction != null)
                friction = (float)s_friction.Value;
            return new PropTemplate(sprt, name, mass, friction, collides, canmove);
        }

        public Prop SpawnProp(ComplexLS prop_script)
        {
            var template = MakePropTemplate(prop_script.Read<ComplexLS>("TEMPLATE"));
            float x = (float)prop_script.Read<TokenLS<double>>("X");
            float y = (float)prop_script.Read<TokenLS<double>>("Y");

            bool makeplayer = prop_script.ReadAll<LScript>("PLAYER").Any();

            return SpawnProp(template, new Vec2(x, y), makeplayer);
        }

        public Prop SpawnProp(PropTemplate template, Vec2 position, bool make_player = false)
        {
            var physics = LevelContext.Context.PhysicsContext.CreateBody(new BodyDef()
            {
                LinearDamping = template.Mass * 10f * template.Friction,
                FixedRotation = true,
                Position = position,
            });
            var box = new PolygonDef() { Density = template.CanMove ? 1.0f : 0.0f, Friction = template.Friction };
            box.Filter.MaskBits = (ushort)(template.Collides ? 1 : 0);
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
