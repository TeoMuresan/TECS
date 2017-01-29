using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembler
{
    /// <summary>
    /// Translates Hack assembly language mnemonics into binary codes.
    /// </summary>
    public static class Code
    {
        /// <summary>
        /// Returns the binary representation of address on 15 bits.
        /// </summary>
        /// <param name="address"> String of decimal address.</param>
        /// <returns> 15-bits binary code for address.</returns>
        public static string Address(string decAddress)
        {
            string binAddress = Convert.ToString(Convert.ToInt32(decAddress, 10), 2);
            return binAddress.PadLeft(Command.AddressNumberOfBits, '0');
        }

        /// <summary>
        /// Returns the binary code of the dest mnemonic.
        /// </summary>
        /// <param name="destMnemonic"> Mnemonic for dest field.</param>
        /// <returns> Binary code for mnemonic.</returns>
        public static string Dest(string destMnemonic)
        {
            return Command.destMnemonics[destMnemonic];
        }

        /// <summary>
        /// Returns the binary code of the comp mnemonic.
        /// </summary>
        /// <param name="destMnemonic"> Mnemonic for comp field.</param>
        /// <returns> Binary code for mnemonic.</returns>
        public static string Comp(string compMnemonic)
        {
            return Command.compMnemonics[compMnemonic];
        }

        /// <summary>
        /// Returns the binary code of the jump mnemonic.
        /// </summary>
        /// <param name="destMnemonic"> Mnemonic for jump field.</param>
        /// <returns> Binary code for mnemonic.</returns>
        public static string Jump(string jumpMnemonic)
        {
            return Command.jumpMnemonics[jumpMnemonic];
        }

        /// <summary>
        /// Assembles a machine code A-instruction using the binary code for the address.
        /// </summary>
        /// <param name="address"> The memory address given in decimal.</param>
        /// <returns> 16-bit string representing the binary code for an A-instruction.</returns>
        public static string AssembleAInstruction(string address)
        {
            return "0" + Address(address);
        }

        /// <summary>
        /// Assembles a machine code C-instruction using the translated binary codes for the fields.        
        /// </summary>
        /// <param name="compMnemonic"> Mnemonic for comp field.</param>
        /// <param name="destMnemonic"> Mnemonic for dest field.</param>
        /// <param name="jumpMnemonic"> Mnemonic for jump field.</param>
        /// <returns> 16-bit string representing the binary code for a C-instruction.</returns>
        public static string AssembleCInstruction(string compMnemonic, string destMnemonic, string jumpMnemonic)
        {
            return "111" + Comp(compMnemonic) + Dest(destMnemonic) + Jump(jumpMnemonic);
        }        
    }
}
