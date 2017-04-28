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
		
        public static Dictionary<string, LScript> ContentReferences { get; private set; }
		public static ComplexLS CreateFrom( string input, string file_name )
		{
			RuneParser = new Parser(new Scanner());
            var s_tree = new LScriptTree(ContentReferences);
			var tree = RuneParser.Parse(input, s_tree) as LScriptTree;
			return tree.Eval(null) as ComplexLS;
		}

		private static Parser RuneParser;
	}
}
