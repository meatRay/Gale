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

		public virtual LScript Clone(string with_name, T with_value)
			=> new TokenLS<T>(with_name, with_value);
		public override LScript Clone(string with_name)
			=> new TokenLS<T>(with_name, Value);
		public override LScript Clone()
			=> new TokenLS<T>(Word, Value);

		public static implicit operator T(TokenLS<T> t) => t.Value;

		public T Value { get; private set; }
	}
}
