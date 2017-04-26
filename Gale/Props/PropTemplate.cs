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
        public Sprite Image;

        public PropTemplate(Sprite image)
        {
            Image = image;
        }


        public float Speed = 2f;
    }
}
