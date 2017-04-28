using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gale.LScripts.TinyPG
{
    class LScriptTree : ParseTree
    {
        public Dictionary<string, LScript> ContentReferences { get; private set; }
        public LScriptTree(Dictionary<string, LScript> content_references)
        {
            ContentReferences = content_references;
        }
        protected override object EvalEntry(ParseTree tree, params object[] paramlist)
        {
            string entry_name = Nodes.First(n => n.Token.Type == TokenType.WORD).Token.Text;
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
                    .Select(e => e.Eval(tree, paramlist) as LScript);
                entry = new ComplexLS(entry_name, entries.ToArray());
            }

            var content_node = payload.Nodes.Where(n => n.Token.Type == TokenType.NAME).FirstOrDefault();
            if (content_node != null)
                ContentReferences[content_node.Token.Text] = entry;
            return entry;
        }

        protected override object EvalStart(ParseTree tree, params object[] paramlist)
        {
            var entries = Nodes.Where(n => n.Token.Type == TokenType.Entry)
                .Select(e => e.Eval(tree, paramlist) as LScript);
            return new ComplexLS("ROOT", entries.ToArray());
        }

        public override ParseNode CreateNode(Token token, string text)
        {
            LScriptTree node = new LScriptTree(ContentReferences);
            node.Token = token;
            node.text = text;
            node.Parent = this;
            return node;
        }


        protected override object EvalToken(ParseTree tree, params object[] paramlist)
        {
            var node = Nodes[0];
            if (node.Token.Type == TokenType.NUMBER)
                return new TokenLS<double>(paramlist[0] as string, double.Parse(node.Token.Text));
            else if (node.Token.Type == TokenType.QUOTE)
                return new TokenLS<string>(paramlist[0] as string, node.Token.Text.Substring(1, node.Token.Text.Length - 2));
            else if (node.Token.Type == TokenType.NUMBER)
                return new TokenLS<Pixels>(paramlist[0] as string, new Pixels(int.Parse(node.Token.Text)));
            else if (node.Token.Type == TokenType.NAME)
            {
                if (ContentReferences.TryGetValue(node.Token.Text, out LScript found))
                    return new TokenLS<LScript>(paramlist[0] as string, found);
                throw new Exception($"LScript Content Reference \"{node.Token.Text}\" not found.");
            }
            throw new Exception("LScript Content Type not Understood");
        }
    }
}
