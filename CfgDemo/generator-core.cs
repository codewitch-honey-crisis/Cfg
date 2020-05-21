using C;
using System;
using System.Collections.Generic;

namespace LLkTest
{
	static class Helpers
	{
		public static void addToArrayFlat<T>(T el, string elf, IList<T> array, IList<string> arrayf)
		{
			if (!arrayf.Contains(elf))
			{
				arrayf.Add(elf);
				array.Add(el);
			}
		}
		public static int indexOf<T>(T el, IList<T> array)
		{
			var result = array.IndexOf(el);
			if (0 > result)
				throw new KeyNotFoundException();
			return result;
		}
	}
	
	enum PHStatus
	{
		OK = 0,
		FAILN = 1, // a fail terminal 
		FAILRD = 2, // a duplicate rule
		FAILRM = 3, // a missing rule
		FAILRL = 4  // a left recursive rule
	}

	// Parser Handler
	class ParserHandler
	{

		public CfgDocument cfg = null;
		public PHStatus status = PHStatus.OK;
		public string statusText = "";
		


		public void finish()
		{
			if (this.status != PHStatus.OK) return;

			// test duplicate rules
			if (!this._CheckForDuplicateRules()) return;

			// test the left recursion
			if (cfg.IsLeftRecursive)
				return;

		}

		bool _CheckForDuplicateRules()
		{
			CfgRule grulei, grulej;
			bool same;
			for (int ic=cfg.Rules.Count,i = 0; i < ic; ++i)
			{
				grulei = this.cfg.Rules[i];

				for (var j = 0; j < ic; ++j)
				{
					grulej = this.cfg.Rules[j];

					if (i == j) continue;
					same = grulei.Equals(grulej);
					if (same)
					{
						this.status = PHStatus.FAILRD;
						this.statusText = grulei.Left;
						return false;
					}
				}
			}
			return true;
		}

	}
	////
	// STANDARD LL(k) PARSING TABLE GENERATOR
	//////

	// Standard LL(k) Parsing Table Generator Status
	struct TGStatus
	{
		public const string OK = "ok";
		public const string ERROR = "error";
	}

	// LL(k) Table Follow Element
	struct FollowEl
	{
		public string N;
		public List<FirstKEl> sets;
		public FollowEl(string N, List<FirstKEl> sets)
		{
			this.N = N;
			this.sets = sets;
		}
	}
	// LL(k) Table Row
	struct LLkTRow
	{
		public FirstKEl u;
		public CfgRule prod;
		public List<FollowEl> follow;
		public LLkTRow(FirstKEl u, CfgRule grule, List<FollowEl> F)
		{
			this.u = u;
			this.prod = grule;
			this.follow = F;
		}
	}
	struct LLkT
	{
		public string N;
		public List<FirstKEl> L;
		public int number;
		public string name;
		public List<LLkTRow> rows;
		public LLkT(int count, string A, List<FirstKEl> L)
		{
			this.name = "T" + count;
			this.number = count;
			this.N = A;
			this.L = L;
			this.rows = new List<LLkTRow>();
		}
		public void addRow(LLkTRow ltrow)
		{
			this.rows.Add(ltrow);
		}
		public string toFlat()
		{
			var flat = "T:" + this.N + ",{";
			for (var i = 0; i < this.L.Count; i++)
			{
				for (var j = 0; j < this.L[i].str.Count; j++)
				{
					flat += this.L[i].str[j];
					if (j != this.L[i].str.Count - 1)
						flat += ":";
				}
				if (i != this.L.Count - 1)
					flat += ",";
			}
			flat += "}";
			return flat;
		}
	}
	// First(k) String
	struct FirstKEl
	{
		public int KRemaining;
		public int K;
		public List<string> str;
		public FirstKEl(int k)
		{
			this.KRemaining = k;
			this.K = 0;
			this.str = new List<string>();
		}
		public void addGEl(string gel)
		{
			this.KRemaining--;
			this.K++;
			this.str.Add(gel);
		}
		public FirstKEl clone()
		{
			var result = new FirstKEl(this.KRemaining);
			result.K = this.K;
			result.str = new List<string>(this.str);
			return result;
		}
		public string toFlat()
		{
			var flat = "";
			for (var i = 0; i < this.str.Count; i++)
			{
				flat += this.str[i];
				if (i != this.str.Count - 1)
					flat += ":";
			}
			return flat;
		}
	}

	// Standard LL(k) Parsing Table Element Type
	enum PTEType
	{
		ACCEPT = 0,
		POP = 1,
		EXPAND = 2
	}

	// Standard LL(k) Parsing Table Element
	struct PTEl
	{
		public PTEType type;
		public List<string> str;
		public CfgRule rule;
		public PTEl(PTEType type, List<string> str, CfgRule rule)
		{

			this.type = type;
			this.str = str;
			this.rule = rule;
		}
	}

	// Standard LL(k) Parsing Table First Index Type
	enum PTFIType
	{
		N = 1, // a nonterminal
		T = 2, // a terminal
		BOT = 3  // the bottom of a pushdown
	}

	// Standard LL(k) Parsing Table First Index
	struct PTFirstIn
	{
		PTFIType type;
		string value;
		public PTFirstIn(PTFIType type, string value)
		{
			this.type = type;
			this.value = value;
		}
		public string toFlat()
		{
			string flat = null;
			switch (this.type)
			{
				case PTFIType.N: flat = this.value; break;
				case PTFIType.T: flat = ":" + this.value; break;
				case PTFIType.BOT: flat = "|$"; break;
			}
			return flat;
		}
	}

	// Standard LL(k) Parsing Table Second Index Type
	enum PTSIType
	{
		STR = 1, // terminals
		END = 2  // the end of an input
	}

	// Standard LL(k) Parsing Table Second Index
	struct PTSecondIn
	{
		public PTSIType type;
		public List<string> str;
		public PTSecondIn(PTSIType type, List<string> str)
		{
			this.type = type;
			this.str = str;

		}
		public string toFlat()
		{
			var flat = "";
			switch (this.type)
			{
				case PTSIType.STR:
					for (var i = 0; i < this.str.Count; i++)
					{
						flat += this.str[i];
						if (i != this.str.Count - 1)
							flat += ":";
					}
					break;
				case PTSIType.END: flat = ""; break;
			}
			return flat;
		}
	}
	// Standard LL(k) Parsing Table
	class ParsingTable
	{
		public List<PTFirstIn> fi; // the first index
		public List<string> fif; // only values
		public List<PTSecondIn> si; // the second index
		public List<string> sif; // only values
		public List<List<List<PTEl>>> field;
		public ParsingTable()
		{
			this.fi = new List<PTFirstIn>();
			this.fif = new List<string>();
			this.si = new List<PTSecondIn>();
			this.sif = new List<string>();   // only values
			field = new List<List<List<PTEl>>>();
		}
		public void init(IList<string> T, int Tcounter, int k)
		{
			// the first index
			PTFirstIn nfi;
			for (var i = 0; i < Tcounter; i++)
			{
				nfi = new PTFirstIn(PTFIType.N, "T" + i);
				this.fi.Add(nfi);
				this.fif.Add(nfi.toFlat());
			}
			for (var i = 0; i < T.Count; i++)
			{
				nfi = new PTFirstIn(PTFIType.T, T[i]);
				this.fi.Add(nfi);
				this.fif.Add(nfi.toFlat());
			}
			nfi = new PTFirstIn(PTFIType.BOT, null);
			this.fi.Add(nfi);
			this.fif.Add(nfi.toFlat());

			// the second index
			PTSecondIn nsi;
			var ins = new List<int>();
			for (var ki = 0; ki < k; ki++)
			{
				ins.Add(0);
			}
			while (T.Count> ins[0])
			{
				nsi = new PTSecondIn(PTSIType.STR, new List<string>());
				for (var ki = 0; ki < k; ++ki)
				{
					if (ins[ki] < T.Count)
						nsi.str.Add(T[ins[ki]]);
				}
				++ins[k - 1];
				for (var ki = k - 1; ki >= 0; ki--)
				{
					if (ins[ki] > T.Count)
					{
						++ins[ki - 1];
						ins[ki] = 0;
					}
				}
				Helpers.addToArrayFlat(nsi, nsi.toFlat(), this.si, this.sif);
			}
			nsi = new PTSecondIn(PTSIType.END, null);
			this.si.Add(nsi);
			this.sif.Add(nsi.toFlat());

			// fields
			for (var i = 0; i < this.fi.Count; i++)
			{
				this.field.Add(new List<List<PTEl>>());
				for (var j = 0; j < this.si.Count; j++)
				{
					this.field[i].Add(new List<PTEl>());
				}
			}
		}
		public void addEl(string fiFlat, string siFlat, PTEl ptel)
		{
			int fi, si;
			fi = Helpers.indexOf(fiFlat, this.fif);
			si = Helpers.indexOf(siFlat, this.sif);
			this.field[fi][si].Add(ptel);
		}
		public string convSiSTRToFiFlat(PTSecondIn sel)
		{
			return ":" + sel.str[0];
		}
		public string convUToSiFlat(FirstKEl u)
		{
			var flat = "";
			for (var i = 0; i < u.str.Count; i++)
			{
				flat += u.str[i];
				if (i != u.str.Count - 1)
					flat += ":";
			}
			return flat;
		}
	}
	// Standard LL(k) Parsing Table Generator
	class TableGenerator
	{

		public CfgDocument cfg = null;
		public int k = 0;//: undefined,
		public int Tcounter = 0;
		public List<LLkT> LLks = null;
		public List<string> LLksf = null;
		public ParsingTable PT = null;
		public string status = TGStatus.OK;

		public void construct(CfgDocument cfg, int k)
		{
			this.cfg = cfg;
			this.k = k;
			this.Tcounter = 0;
			this.LLks = new List<LLkT>();
			this.LLksf = new List<string>();
			this.PT = new ParsingTable();
			this.status = TGStatus.OK;

			this.constructLLkTs();
			var tl = this.cfg.FillTerminals();
			tl.Remove("#ERROR");
			tl.Remove("#EOS");
			this.PT.init(tl, this.Tcounter, this.k);
			this.fillPT();

			this.checkValidity();
		}
		
		public void constructLLkTs()
		{
			//(1)
			var l = new List<FirstKEl>();
			l.Add(new FirstKEl(this.k));
			var t0 = this.constructLLkT(this.cfg.StartSymbol, l);
			this.LLks.Add(t0);

			//(2)
			var J = this.LLksf;
			J.Add(t0.toFlat());

			//(3)(4)
			LLkT tabi;
			LLkTRow rowj;
			FollowEl folk;
			LLkT newt;
			string newtf;
			for (var i = 0; i < this.LLks.Count; i++)
			{
				tabi = this.LLks[i];
				for (var j = 0; j < tabi.rows.Count; j++)
				{
					rowj = tabi.rows[j];
					for (var k = 0; k < rowj.follow.Count; k++)
					{
						folk = rowj.follow[k];

						newt = new LLkT(0, folk.N, folk.sets);
						newtf = newt.toFlat();
						if (!J.Contains(newtf))
						{
							newt = this.constructLLkT(folk.N, folk.sets);
							this.LLks.Add(newt);
							J.Add(newtf);
						}
					}
				}
			}
		}

		public LLkT constructLLkT(string N, List<FirstKEl> L)
		{
				
			var table = new LLkT(this.Tcounter, N, L);
			this.Tcounter++;

			List<FirstKEl> first, setu;
			CfgRule rulei;
			LLkTRow ltrow;
			List<FollowEl> follow;
			for (var i = 0; i < this.cfg.Rules.Count; i++)
			{
				rulei = this.cfg.Rules[i];

				// skip irrelevant rules
				if (rulei.Left!= N) continue;

				// compute u
				first = this.firstOp(rulei.Right,this.k);
				setu = this.firstPlusOp(first, L,this.k);

				// compute follow
				follow = this.followOp(rulei.Right, L);

				// add rows
				for (var j = 0; j < setu.Count; j++)
				{
					ltrow = new LLkTRow(setu[j], rulei, follow);
					table.addRow(ltrow);
				}
			}

			return table;
		}
		public IDictionary<string, string[]> FillFirsts(int k, IDictionary<string, string[]> result = null)
		{
			if (null == result)
				result = new Dictionary<string, string[]>();
			for (int ic = cfg.Rules.Count, i = 0; i < ic; ++i)
			{
				var rule = cfg.Rules[i];
				var fo = firstOp(rule.Right,k);
				
			}
			throw new NotImplementedException();
		}
		List<FirstKEl> firstOp(IList<string> right, int k)
		{
			var set = new List<FirstKEl>();
			set.Add(new FirstKEl(k));
			var set2 = new List<FirstKEl>();

			for (var i = 0; i < right.Count; i++)
			{
				for (var j = 0; j < set.Count; j++)
				{

					// only uncomplete
					if (set[j].KRemaining <= 0)
					{
						set2.Add(set[j]);
						continue;
					}

					// add terminals
					if (!cfg.IsNonTerminal(right[i]))
					{
						set[j].addGEl(right[i]);
						set2.Add(set[j]);
						continue;
					}

					// expand nonterminals
					set2.AddRange(this._firstOp_exp(set[j], right[i]));

				}
				set = set2;
				set2 = new List<FirstKEl>();
			}

			return set;
		}

		public List<FirstKEl> _firstOp_exp(FirstKEl el, string N)
		{
			var set = new List<FirstKEl>();
			set.Add(el.clone());
			var set2 = new List<FirstKEl>();
			var set3 = new List<FirstKEl>();

			for (var r = 0; r < this.cfg.Rules.Count; r++)
			{
				var cr = this.cfg.Rules[r];

				// skip irrelevant rules
				if (cr.Left != N) continue;

				for (var i = 0; i < cr.Right.Count; i++)
				{
					for (var j = 0; j < set.Count; j++)
					{

						// only uncomplete
						if (set[j].KRemaining <= 0)
						{
							set2.Add(set[j]);
							continue;
						}

						// add terminals
						if (!cfg.IsNonTerminal(cr.Right[i]))
						{
							set[j].addGEl(cr.Right[i]);
							set2.Add(set[j]);
							continue;
						}

						// expand nonterminals
						set2.AddRange(this._firstOp_exp(set[j], cr.Right[i]));

					}
					set = set2;
					set2 = new List<FirstKEl>();
				}

				set3.AddRange(set);
				set = new List<FirstKEl>();
				set.Add(el.clone());
				set2 = new List<FirstKEl>();
			}

			return set3;
		}

		public List<FirstKEl> firstPlusOp(List<FirstKEl> set1, List<FirstKEl> set2,int k)
		{
			int ip, jp;
			FirstKEl fel;
			var result = new List<FirstKEl>();
			var resultcheck = new List<string>();

			for (var i = 0; i < set1.Count; i++)
			{
				for (var j = 0; j < set2.Count; j++)
				{

					ip = 0; jp = 0; fel = new FirstKEl(k);
					for (var m = 0; m < this.k; m++)
					{
						if (ip < set1[i].str.Count)
						{
							fel.addGEl(set1[i].str[ip]);
							ip++;
							continue;
						}
						if (jp < set2[j].str.Count)
						{
							fel.addGEl(set2[j].str[jp]);
							jp++;
							continue;
						}
						break;
					}
					Helpers.addToArrayFlat(fel, fel.toFlat(), result, resultcheck);

				}
			}

			return result;
		}

		public List<FollowEl> followOp(IList<string> right, List<FirstKEl> L)
		{
			var result = new List<FollowEl>();
			string geli;
			List<string> rest;
			FollowEl follow;
			List<FirstKEl> first, setu;

			for (var i = 0; i < right.Count; i++)
			{
				geli = right[i];

				// skip terminals
				if (!cfg.IsNonTerminal(geli)) continue;

				// create rest
				rest = new List<string>();
				for (var j = i + 1; j < right.Count; j++)
				{
					rest.Add(right[j]);
				}

				// compute u
				first = this.firstOp(rest,this.k);
				setu = this.firstPlusOp(first, L,this.k);

				// add to the result
				follow = new FollowEl(geli, setu);
				result.Add(follow);
			}

			return result;
		}

		public string convNToTableName(string N, List<FirstKEl> L)
		{
			var t = new LLkT(0, N, L);
			var tf = t.toFlat();
			var i = Helpers.indexOf(tf, this.LLksf);
			var lt = this.LLks[i];
			return lt.name;
		}

		public void fillPT()
		{
			string fiv, siv;
			PTEl el;
			var PT = this.PT;

			//(1) expand
			LLkT tabi;
			LLkTRow rowj;
			string gelk;
			int nontl;
			string gelnew;
			for (var i = 0; i < this.LLks.Count; i++)
			{
				tabi = this.LLks[i];
				for (var j = 0; j < tabi.rows.Count; j++)
				{
					rowj = tabi.rows[j];

					el = new PTEl(PTEType.EXPAND,new List<string>(), rowj.prod);
					
					// convert the right side of a rule
					nontl = 0;
					for (var k = 0; k < rowj.prod.Right.Count; k++)
					{
						gelk = rowj.prod.Right[k];

						if (!cfg.IsNonTerminal(gelk))
						{
							el.str.Add(gelk);
						}
						else
						{
							gelnew = this.convNToTableName(gelk, rowj.follow[nontl].sets);
							el.str.Add(gelnew);
							nontl++;
						}
					}

					fiv = tabi.name;
					siv = PT.convUToSiFlat(rowj.u);
					PT.addEl(fiv, siv, el);
				}
			}

			//(2) pop
			PTSecondIn sii;
			for (var i = 0; i < PT.si.Count; i++)
			{
				sii = PT.si[i];
				if (sii.type != PTSIType.STR) continue;

				el = new PTEl(PTEType.POP, null, null);
				fiv = PT.convSiSTRToFiFlat(sii);
				siv = sii.toFlat();
				PT.addEl(fiv, siv, el);
			}

			//(3) accept
			PTFirstIn fie;
			PTSecondIn sie;
			el = new PTEl(PTEType.ACCEPT, null, null);
			fie = new PTFirstIn(PTFIType.BOT, null);
			sie = new PTSecondIn(PTSIType.END, null);
			fiv = fie.toFlat();
			siv = sie.toFlat();
			PT.addEl(fiv, siv, el);

			//(4)(5)
			//nothing
		}

		public void checkValidity()
		{
			var PT = this.PT;
			var field = this.PT.field;

			for (var i = 0; i < PT.fi.Count; i++)
			{
				for (var j = 0; j < PT.si.Count; j++)
				{
					if (field[i][j].Count> 1)
						this.status = TGStatus.ERROR;
				}
			}
		}
	}

}