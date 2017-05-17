using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gale.LScripts.TinyPG
{
	public delegate LScript ApplyMetaDelegate(LScript meta_script);
	public class LScriptTree : ParseTree
	{
		public Dictionary<string, LScript> ContentReferences { get; private set; }
		public ApplyMetaDelegate ApplyMeta;

		public LScriptTree(Dictionary<string, LScript> content_references)
		{
			ContentReferences = content_references;
		}
		public LScriptTree(Dictionary<string, LScript> content_references, ApplyMetaDelegate meta_event)
		{
			ContentReferences = content_references;
			ApplyMeta = meta_event;
		}
		protected override object EvalEntry(ParseTree tree, params object[] paramlist)
		{
			string entry_name = Nodes.First(n => n.Token.Type == TokenType.WORD || n.Token.Type == TokenType.META).Token.Text;
			var payload = Nodes.Where(n => n.Token.Type == TokenType.Payload).FirstOrDefault();
			if (payload == null)
				return new LScript(entry_name);
			LScript entry = null;


			var f_node = payload.Nodes.Where(n => n.Token.Type == TokenType.Token).FirstOrDefault();
			if (f_node != null)
				entry = f_node.Eval(tree, entry_name) as LScript;
			else
			{
				f_node = payload.Nodes.First(n => n.Token.Type == TokenType.Group);
				var entries = f_node.Nodes.Where(n => n.Token.Type == TokenType.Entry)
					.Select(e => e.Eval(tree, paramlist) as LScript)
					.Where(e => e != null);
				entry = new ComplexLS(entry_name, entries.ToArray());
			}

			var content_node = payload.Nodes.Where(n => n.Token.Type == TokenType.NEWNAME).FirstOrDefault();
			if (content_node != null)
				ContentReferences[content_node.Token.Text.Substring(content_node.Token.Text.IndexOf('$'))] = entry;

			if (Nodes.Any(n => n.Token.Type == TokenType.META))
			{
				var got = ApplyMeta?.Invoke(entry);
				return got;
			}
			return entry;
		}

		protected override object EvalStart(ParseTree tree, params object[] paramlist)
		{
			var entries = Nodes.Where(n => n.Token.Type == TokenType.Entry)
				.Select(e => e.Eval(tree, paramlist) as LScript)
				.Where(e => e != null);
			return new ComplexLS("ROOT", entries.ToArray());
		}

		public override ParseNode CreateNode(Token token, string text)
			=> new LScriptTree(ContentReferences, ApplyMeta)
			{
				Token = token,
				text = text,
				Parent = this
			};


		protected override object EvalToken(ParseTree tree, params object[] paramlist)
		{
			var node = Nodes[0];
			if (node.Token.Type == TokenType.NUMBER)
				return new TokenLS<double>(paramlist[0] as string, double.Parse(node.Token.Text));
			else if (node.Token.Type == TokenType.QuoteToken)
				return new TokenLS<string>(paramlist[0] as string, (node.Eval(tree, paramlist) as string)
					.Replace("\r","").Replace("\t",""));
			else if (node.Token.Type == TokenType.PixelToken)
				return new TokenLS<Pixels>(paramlist[0] as string, new Pixels((int)node.Eval(tree, paramlist)));
			else if (node.Token.Type == TokenType.NameToken)
			{
				string name = node.Nodes.First(n => n.Token.Type == TokenType.NAME).Token.Text;
				LScript fnd;
				if (!ContentReferences.TryGetValue(name, out fnd))
					throw new Exception($"LScript Content Reference \"{name}\" not found.");
				if (paramlist[0] as string != fnd.Word)
					fnd = fnd.Clone(with_name: paramlist[0] as string);
				var edit = node.Nodes.Where(n => n.Token.Type == TokenType.NameEdit);
				if (edit.Any())
				{
					fnd = fnd.Clone();
					var fnd_c = fnd as ComplexLS;
					var comp = edit.First();
					var entries = comp.Nodes.Where(n => n.Token.Type == TokenType.Entry)
						.Select(n => n.Eval(tree, paramlist) as LScript).ToList();
					foreach (var old_entry in fnd_c.SubRunes)
						if (!entries.Any(e => e.Word == old_entry.Word))
							entries.Add(old_entry);
					var removes = comp.Nodes.Where(n => n.Token.Type == TokenType.REMOVE)
						.Select(n => new LScript(n.Token.Text)).ToArray();
					foreach (var to_remove in removes)
					{
						var e_fnd = entries.Where(e => e.Word == to_remove.Word).FirstOrDefault();
						if (e_fnd != null)
							entries.Remove(e_fnd);
					}
					return new ComplexLS(fnd_c.Word, entries.ToArray());
				}
				return fnd;
			}
			throw new Exception("LScript Content Type not Understood");
		}
	}
}
