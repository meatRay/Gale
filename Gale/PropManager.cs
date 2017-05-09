using Box2DX.Collision;
using Box2DX.Common;
using Box2DX.Dynamics;
using Gale.LScripts;
using Gale.Props;
using Gale.Visuals;
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
			var sprtscript = template_script.Read<ComplexLS>("SPRITE");
			Sprite sprt;
			if (sprtscript != null)
				sprt = LevelContext.Context.Content.MakeSprite(template_script.Read<ComplexLS>("SPRITE"));
			else
				sprt = LevelContext.Context.Content.MakeAnimation(template_script.Read<ComplexLS>("ANIMATION"));
			var name_s = template_script.Read<TokenLS<string>>("NAME");
			string name = name_s != null ? name_s.Value : "UNKNOWN";
			var desc_s = template_script.Read<TokenLS<string>>("DESC");
			string desc = desc_s != null ? desc_s.Value : "";
			float mass = (float)template_script.Read<TokenLS<double>>("MASS");
			bool canmove = template_script.ReadAll<LScript>("CANMOVE").Any();
			bool collides = template_script.ReadAll<LScript>("COLLIDES").Any();
			float friction = 1.0f;
			var s_friction = template_script.Read<TokenLS<double>>("FRICTION");
			if (s_friction != null)
				friction = (float)s_friction.Value;
			var width = template_script.Read<TokenLS<Pixels>>("WIDTH");
			var height = template_script.Read<TokenLS<Pixels>>("HEIGHT");
			if (width != null && height != null)
				return new PropTemplate(sprt, name, desc, mass, friction, collides, canmove,
					width.Value / Sprite.TileSize, height.Value / Sprite.TileSize);
			else
				return new PropTemplate(sprt, name, desc, mass, friction, collides, canmove);
		}

		public void Destroy(Prop prop)
		{
			LevelContext.Context.PhysicsContext.DestroyBody(prop.Physics);
			_props.Remove(prop);
		}

		public Prop SpawnProp(ComplexLS prop_script)
		{
			var template = MakePropTemplate(prop_script.Read<ComplexLS>("TEMPLATE"));
			float x = (float)prop_script.Read<TokenLS<double>>("X");
			float y = (float)prop_script.Read<TokenLS<double>>("Y");
			var z = prop_script.Read<TokenLS<double>>("Z");

			bool makeplayer = prop_script.Read<LScript>("PLAYER") != null;
			var ontrigger = prop_script.Read<ComplexLS>("ON_TRIGGER");

			var prop = SpawnProp(template, new Vec2(x, y), makeplayer, ontrigger != null);
			prop.OnInteract = prop_script.Read<ComplexLS>("ON_INTERACT");
			prop.OnTrigger = ontrigger;
			prop.ZPosition = z != null ? (float)z.Value : 0.0f;
			return prop;
		}

		public Prop SpawnProp(PropTemplate template, Vec2 position, bool make_player = false, bool is_trigger = false)
		{
			Body physics = null;
			Shape shape = null;
			if (template.Image.Height > 0)
			{
				physics = LevelContext.Context.PhysicsContext.CreateBody(new BodyDef()
				{
					LinearDamping = template.Mass * 10f * template.Friction,
					FixedRotation = true,
					Position = position,
				});
				var box = new PolygonDef() { IsSensor = is_trigger, Density = template.CanMove ? 1.0f : 0.0f, Friction = template.Friction };
				box.Filter.MaskBits = (ushort)(template.Collides ? 1 : 0);
				box.SetAsBox(template.PhysicsBounds.X / 2f, template.PhysicsBounds.Y / 2f, new Vec2(template.PhysicsBounds.X / 2, template.PhysicsBounds.Y / 2f), 0.0f);
				shape = physics.CreateShape(box);
				physics.SetMassFromShapes();
			}
			var prop = new Prop(template, LevelContext.Context.ActiveLevel, physics, shape);
			_props.Add(prop);
			physics?.SetUserData(prop);

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
