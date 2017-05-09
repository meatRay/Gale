using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gale.LScripts
{
	public class ComplexLS : LScript
	{
		public LScript[] SubRunes { get; private set; }
		public ComplexLS(string word, LScript[] subrunes)
			: base(word)
		{
			SubRunes = subrunes;
		}

		public virtual LScript Clone(string with_word, LScript[] with_subrunes)
			=> new ComplexLS(with_word, with_subrunes);
		public override LScript Clone(string with_word)
			=> new ComplexLS(with_word, SubRunes.Select(s => s.Clone()).ToArray());
		public override LScript Clone()
			=> new ComplexLS(Word, SubRunes.Select(s => s.Clone()).ToArray());

		public LScript Read(string rune_name)
			=> SubRunes.Where(r => r.Word.Equals(rune_name, StringComparison.OrdinalIgnoreCase))
				.FirstOrDefault();
		public IEnumerable<LScript> ReadAll(string rune_name)
			=> SubRunes.Where(r => r.Word.Equals(rune_name, StringComparison.OrdinalIgnoreCase));

		public R Read<R>(string rune_name) where R : LScript
			=> Read(rune_name) as R;
		public IEnumerable<R> ReadAll<R>(string rune_name) where R : LScript
			=> ReadAll(rune_name).Cast<R>();

		public R ReadToken<R>(string rune_name)
			=> Read(rune_name) as TokenLS<R>;
		public IEnumerable<R> ReadAllToken<R>(string rune_name)
			=> ReadAll(rune_name).Select(r => (r as TokenLS<R>).Value);
	}
}
