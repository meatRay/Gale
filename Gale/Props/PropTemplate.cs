using Gale.Visuals;
using OpenTK;

namespace Gale.Props
{
	class PropTemplate
	{
		public float ZPosition = 0.0f;
		public Sprite Image { get; private set; }
		public string Name { get; private set; }
		public string Desc { get; private set; }
		public float Mass { get; private set; }
		public float Friction { get; private set; }
		public bool Collides { get; private set; }
		public bool CanMove { get; private set; }
		public Vector2 PhysicsBounds { get; private set; }

		public PropTemplate(Sprite image, string name, string desc, float mass, float friction, bool collides, bool can_move)
			: this(image, name, desc, mass, friction, collides, can_move, image.UnitSize.X, image.UnitDepth)
		{ }
		public PropTemplate(Sprite image, string name, string desc, float mass, float friction, bool collides, bool can_move,
			float width, float height)
		{
			Image = image;
			Name = name;
			Desc = desc;
			Mass = mass;
			Friction = friction;
			Collides = collides;
			CanMove = can_move;
			PhysicsBounds = new Vector2(width, height);
		}

		public float Speed = 2f;
	}
}
