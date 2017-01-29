using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembler
{
    /// <summary>
    /// Keeps a correspondence between symbolic labels and numeric addresses.
    /// </summary>
    public class SymbolTable
    {
        public IDictionary<string, int> dictionary;

        public SymbolTable()
        {
            dictionary = new Dictionary<string, int>();
            SetPredefinedSymbols();
        }

        public void AddEntry(string symbol, int address)
        {
            dictionary.Add(symbol, address);
        }

        public bool Contains(string symbol)
        {
            return dictionary.ContainsKey(symbol);
        }

        public int GetAddress(string symbol)
        {
            return dictionary[symbol];
        }

        private void SetPredefinedSymbols()
        {
            dictionary.Add("SP", 0);
            dictionary.Add("LCL", 1);
            dictionary.Add("ARG", 2);
            dictionary.Add("THIS", 3);
            dictionary.Add("THAT", 4);
            dictionary.Add("SCREEN", 16384);
            dictionary.Add("KBD", 24576);
            for (int i = 0; i <= 15; i++)
            {
                dictionary.Add("R" + i, i);
            }
        }
    }
}
