using System;
using System.Collections.Generic;
using System.Linq;
using Gale.LScripts.TinyPG;

namespace Gale.LScripts
{
	public class LScript
	{
		public string Word { get; private set; }

		public LScript(string word)
		{
			Word = word;
		}
		
		public static ComplexRune CreateFrom( string input, string file_name )
		{
			RuneParser = new Parser(new Scanner());
			var tree = RuneParser.Parse(input, file_name);
			return tree.Eval(null) as ComplexRune;
		}

		private static Parser RuneParser;
	}
	public class TokenRune : LScript
	{
		public TokenRune(string r_name, object token)
			: base(r_name)
		{
			_token = token;
		}
		public T Read<T>()
			=> (T)_token;

		private object _token;
	}
	public class ComplexRune : LScript
	{
		public LScript[] SubRunes { get; private set; }
		public ComplexRune(string word, LScript[] subrunes)
			: base(word)
		{
			SubRunes = subrunes;
		}

		public LScript Read(string rune_name)
			=> SubRunes.Where(r => r.Word.Equals(rune_name, StringComparison.OrdinalIgnoreCase))
				.FirstOrDefault();
		public IEnumerable<LScript> ReadAll(string rune_name)
			=> SubRunes.Where(r => r.Word.Equals(rune_name, StringComparison.OrdinalIgnoreCase));

		public R Read<R>(string rune_name) where R : LScript
			=> Read(rune_name) as R;
		public IEnumerable<R> ReadAll<R>(string rune_name) where R : LScript
			=> ReadAll(rune_name).Cast<R>();
	}
}
