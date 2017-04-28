using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gale.LScripts
{
    public struct Pixels
    {
        public int Amt { get; }

        public Pixels(int amt)
        {
            Amt = amt;
        }

        public static implicit operator int(Pixels p) => p.Amt;
    }
}
