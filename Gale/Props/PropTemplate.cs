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

        public PropTemplate(Sprite image, string name)
        {
            Image = image;
            Name = name;
        }


        public float Speed = 2f;
    }
}
