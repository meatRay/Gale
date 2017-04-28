using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gale.LScripts
{
    public class TokenLS<T> : LScript
    {
        public TokenLS(string r_name, T token_value)
            : base(r_name)
        {
            Value = token_value;
        }

        public static implicit operator T(TokenLS<T> t) => t.Value;

        public T Value { get; private set; }
    }
}
