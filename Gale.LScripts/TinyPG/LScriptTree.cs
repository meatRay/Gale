using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gale.LScripts.TinyPG
{
	public class LScriptTree : ParseTree
	{
		public Dictionary<string, LScript> ContentReferences { get; private set; }
		public EventHandler<LScript> ParseMetaLScript;
		public LScriptTree(Dictionary<string, LScript> content_references)
		{
			ContentReferences = content_references;
		}
		public LScriptTree(Dictionary<string, LScript> content_references, EventHandler<LScript> events)
		{
			ContentReferences = content_references;
			ParseMetaLScript = events;
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
				ParseMetaLScript?.Invoke(this, entry);
				return null;
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
			=> new LScriptTree(ContentReferences, ParseMetaLScript)
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
				return new TokenLS<string>(paramlist[0] as string, node.Eval(tree, paramlist) as string);
			else if (node.Token.Type == TokenType.PixelToken)
				return new TokenLS<Pixels>(paramlist[0] as string, new Pixels((int)node.Eval(tree, paramlist)));
			else if (node.Token.Type == TokenType.NAME)
			{
				if (ContentReferences.TryGetValue(node.Token.Text, out LScript found))
					return /*new TokenLS<LScript>(paramlist[0] as string, */found/*)*/;
				throw new Exception($"LScript Content Reference \"{node.Token.Text}\" not found.");
			}
			throw new Exception("LScript Content Type not Understood");
		}
	}
}
