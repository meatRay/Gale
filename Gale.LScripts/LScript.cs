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

		public static Dictionary<string, LScript> ContentReferences { get; private set; } = new Dictionary<string, LScript>();
		public static ComplexLS CreateFrom(string input)
		{
			var s_tree = new LScriptTree(ContentReferences);
			s_tree.ParseMetaLScript += ImportMetaTags;
			s_tree = AcquireParser().Parse(input, s_tree) as LScriptTree;
			return s_tree.Eval(null) as ComplexLS;
		}
		private static void ImportMetaTags(object sender, LScript meta_tag)
		{
			if (meta_tag.Word != "#IMPORT")
				return;
			string file_name = meta_tag as TokenLS<string>;
			if (file_name == null)
				return;
			CreateFrom(File.ReadAllText(file_name));
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
