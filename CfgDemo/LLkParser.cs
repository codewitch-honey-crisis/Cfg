using LLkTest;
using System;
using System.Collections.Generic;
using System.Text;

namespace CfgDemo
{
	class LLkParser : IDisposable
	{
		private struct _Entry
		{
			public int TableId;
			public string Symbol;
			public bool IsEndSymbol;
			public _Entry(int tableId)
			{
				TableId = tableId;
				Symbol = null;
				IsEndSymbol = false;

			}
			public _Entry(string symbol,bool isEndSymbol)
			{
				TableId = -1;
				Symbol = symbol;
				IsEndSymbol = isEndSymbol;

			}
			public override string ToString()
			{
				if (null != Symbol)
				{
					if (IsEndSymbol)
						return "#END " + Symbol;
					return Symbol;
				}
				return "T" + TableId.ToString();
			}
		}
		// this is silly, but we're doing this because of the way it was factored
		// stuff was ported from JS code that wasn't really meant for this
		TableGenerator _tg;
		Stack<_Entry> _stack;
		IEnumerator<Token> _input;
		Token _errorToken;
		IList<Token> _current;
		LLNodeType _nodeType;
		public LLkParser(TableGenerator tg,IEnumerable<Token> input)
		{
			_tg = tg;
			_input = input.GetEnumerator();
			_stack = new Stack<_Entry>();
			_nodeType = LLNodeType.Initial;
			_current = new List<Token>();
		}
		public void Close()
		{
			if (null != _input)
			{
				_input.Dispose();
				_input = null;
			}
		}
		public LLNodeType NodeType {
			get {
				return _nodeType;
			}
		}
		void IDisposable.Dispose() { Close(); }
		public bool Read()
		{
			var n = NodeType;
			if (LLNodeType.Error == n && 0==_current.Count)
			{
				_errorToken.Symbol = null;
				_stack.Clear();
				return true;
			}
			if (LLNodeType.Initial == n)
			{
				_stack.Push(new _Entry(0)); // start at T0
				var kk = 0;
				// read k tokens from the input
				while(_input.MoveNext() && kk < _tg.k)
				{
					_current.Add(_input.Current);
					++kk;
				}
				return true;
			}
			// clear the error status
			_errorToken.Symbol = null; 
			if (0 < _stack.Count)
			{
				var entry = _stack.Peek();
				var sid = _stack.Peek();
				if (entry.IsEndSymbol)
				{
					_nodeType = LLNodeType.EndNonTerminal;
					_stack.Pop();
					return true;
				}
				if (entry.Symbol == _input.Current.Symbol) // terminal
				{
					// lex the next token
					_input.MoveNext();

					_stack.Pop();
					return true;
				}
				// non-terminal
				Dictionary<string, CfgRule> d;
				if (_parseTable.TryGetValue(sid, out d))
				{
					CfgRule rule;
					if (d.TryGetValue(_input.Current.Symbol, out rule))
					{
						_stack.Pop();

						// store the end non-terminal marker for later
						_stack.Push(string.Concat("#END ", sid));

						// push the rule's derivation onto the stack in reverse order
						var ic = rule.Right.Count;
						for (var i = ic - 1; 0 <= i; --i)
						{
							sid = rule.Right[i];
							_stack.Push(sid);
						}
						return true;
					}
					_Panic();
					return true;
				}
				_Panic();
				return true;
			}
			// last symbol must be the end of the input stream or there's a problem
			if ("#EOS" != _input.Current.Symbol)
			{
				_Panic();
				return true;
			}
			return false;
		}
		void _Panic()
		{
			throw new Exception("Parse error");
		}
		void _CheckDisposed()
		{
			if (null == _input)
				throw new ObjectDisposedException(GetType().Name);
		}
	}
}
