using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gale.Props
{
    class PropTemplate
    {
        public float ZPosition = 0.0f;
        public Sprite Image { get; private set; }
        public string Name { get; private set; }
        public float Mass { get; private set; }
        public float Friction { get; private set; }
        public bool Collides { get; private set; }
        public bool CanMove { get; private set; }

        public PropTemplate(Sprite image, string name, float mass, float friction, bool collides, bool can_move)
        {
            Image = image;
            Name = name;
            Mass = mass;
            Friction = friction;
            Collides = collides;
            CanMove = can_move;
        }


        public float Speed = 2f;
    }
}
