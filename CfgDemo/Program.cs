using C;
using System;


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
			// not-necessary but faster access since we're not modifying:
			cfg.RebuildCache();
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
			var predict = cfg.FillPredict();
			var firsts = cfg.FillFirsts();
			var follows = cfg.FillFollows();
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
		}
	}
}
