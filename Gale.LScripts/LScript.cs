using System;
using System.Collections.Generic;
using System.Linq;
using Gale.LScripts.TinyPG;
using System.IO;

namespace Gale.LScripts
{
	public class LScript
	{
		public string Word { get; private set; }

		public LScript(string word)
		{
			Word = word;
		}


		public virtual LScript Clone( string with_name )
			=> new LScript(with_name);
		public virtual LScript Clone()
			=> new LScript(Word);

		public static Dictionary<string, LScript> ContentReferences { get; private set; } = new Dictionary<string, LScript>();
		public static ComplexLS CreateFrom(string input)
		{
			var s_tree = new LScriptTree(ContentReferences);
			s_tree.ApplyMeta += ImportMetaTags;
			s_tree.ApplyMeta += DoMetaMath;
			s_tree = AcquireParser().Parse(input, s_tree) as LScriptTree;
			return s_tree.Eval(null) as ComplexLS;
			//ContentReferences.Clear();
		}
		private static LScript ImportMetaTags(LScript meta_tag)
		{
			if (meta_tag.Word != "#IMPORT")
				return null;
			string file_name = meta_tag as TokenLS<string>;
			if (file_name == null)
				return null ;
			CreateFrom(File.ReadAllText(file_name));
			return null;
		}
		private static LScript DoMetaMath(LScript meta_tag)
		{
			if (meta_tag.Word != "#MATH")
				return null;
			var meta_complex = meta_tag as ComplexLS;
			double sum = 0.0;
			string name = null;
			foreach (var script in meta_complex.SubRunes)
			{
				sum = DoMath(script, sum, out string fnd_name);
				if (fnd_name != string.Empty)
					name = fnd_name;
			}
			return new TokenLS<double>(name, sum);
		}
		private static double DoMath(LScript operation, double sum, out string found_name)
		{
			var tok = operation as TokenLS<double>;
			found_name = string.Empty;
			switch( operation.Word )
			{
				case "IS": return tok.Value;
				case "MINUS": return sum - tok.Value;
				case "PLUS": return sum + tok.Value;
				case "DIVIDE": return sum - tok.Value;
				case "TIMES": return sum + tok.Value;
				default:
					found_name = operation.Word;
					return sum;
			}
		}

		private static Parser AcquireParser()
		{
			if (RuneParser == null)
				RuneParser = new Parser(new Scanner());
			return RuneParser;
		}
		private static Parser RuneParser;
	}
}
