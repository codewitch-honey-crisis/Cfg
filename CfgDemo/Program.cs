using C;
using System;
using System.Collections.Generic;

namespace CfgDemo
{
	class Program
	{
		static void Main(string[] args)
		{
			if(0==args.Length)
			{
				Console.Error.WriteLine("Must specify input CFG");
				return;
			}
			var cfg = CfgDocument.ReadFrom(args[0]);
			Console.WriteLine(cfg.ToString());
			Console.WriteLine();
			// not-necessary but faster access since we're not modifying:

			cfg.RebuildCache();
			Console.WriteLine("See: http://hackingoff.com/compilers/ll-1-parser-generator");
			Console.WriteLine();
			Console.WriteLine("CFG has {0} rules composed of {1} non-terminals and {2} terminals for a total of {3} symbols" ,cfg.Rules.Count, cfg.FillNonTerminals().Count, cfg.FillTerminals().Count, cfg.FillSymbols().Count);
			Console.WriteLine();

			Console.Write("Terminals:");
			foreach(var t in cfg.FillTerminals())
			{
				Console.Write(" ");
				Console.Write(t);
			}
			Console.WriteLine();
			Console.WriteLine();

			// compute the various aspects of the CFG
			var predict = cfg.FillPredict();
			// var firsts = cfg.FillFirsts(); // we don't need this because we have predict
			var follows = cfg.FillFollows();

			// enum some stuff
			foreach(var nt in cfg.FillNonTerminals())
			{
				Console.WriteLine(nt+" has the following rules:");
				foreach(var ntr in cfg.FillNonTerminalRules(nt))
				{
					Console.Write("\t");
					Console.WriteLine(ntr);
				}
				Console.WriteLine();
				Console.WriteLine( nt + " has the following PREDICT:");
				foreach (var t in predict[nt])
				{
					Console.Write("\t");
					Console.WriteLine((t.Symbol??"<empty>")+" - "+t.Rule);
				}
				Console.WriteLine();
				// PREDICT makes this redundant
				//Console.WriteLine(nt + " has the following FIRSTS:");
				//foreach (var t in firsts[nt])
				//{
				//	Console.Write("\t");
				//	Console.WriteLine(t);
				//}
				//Console.WriteLine();
				Console.WriteLine(nt + " has the following FOLLOWS:");
				foreach (var t in follows[nt])
				{
					Console.Write("\t");
					Console.WriteLine(t);
				}
				Console.WriteLine();

			}

			// now lets parse some stuff

			Console.WriteLine("Building simple parse table");

			// the parse table is simply nested dictionaries where each outer key is a non-terminal
			// and the inner key is each terminal, where they map to a single rule.
			// lookups during parse are basically rule=parseTable[<topOfStack>][<currentToken>]
			var parseTable = new Dictionary<string, Dictionary<string, CfgRule>>();
			foreach (var nt in cfg.FillNonTerminals())
			{
				var d = new Dictionary<string, CfgRule>();
				parseTable.Add(nt, d);
				foreach(var p in predict[nt])
				{
					if(null!=p.Symbol)
					{
						CfgRule or;
						if(d.TryGetValue(p.Symbol,out or))
						{
							Console.Error.WriteLine("First-first conflict between " + p.Rule + " and " + or);
						} else
							d.Add(p.Symbol, p.Rule);
					} else
					{
						foreach(var f in follows[nt])
						{
							CfgRule or;
							if (d.TryGetValue(f, out or))
							{
								Console.Error.WriteLine("First-follows conflict between " + p.Rule + " and " + or);
							}
							else
								d.Add(f, p.Rule);
						}
					}
				}
			}

			#region Build a Lexer for our parser - out of scope of the CFG project but necessary
			Console.WriteLine("Building simple lexer");
			var fas = new FA[] 
			{
				FA.Literal("+","add"),
				FA.Literal("*","mul"),
				FA.Literal("(","lparen"),
				FA.Literal(")","rparen"),
				FA.Repeat(FA.Set("0123456789"), "int") 
			};
			
			var lexer = new FA();
			for(var i = 0;i<fas.Length;i++)
				lexer.EpsilonTransitions.Add(fas[i]);
			Console.WriteLine();
			#endregion

			var text = "(1+3)*2";
			
			Console.WriteLine("Creating tokenizer");
			var tokenizer = new Tokenizer(lexer, text);
			Console.WriteLine("Creating parser");
			var parser = new Parser(parseTable, tokenizer, "Expr");
			Console.WriteLine();
			Console.WriteLine("Parsing " + text);
			Console.WriteLine(parser.ParseSubtree());
		}
	}
}
