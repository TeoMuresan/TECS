using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
    /// <summary>
    /// Class containing identifier properties needed for compilation.
    /// </summary>
    public class IdentifierProperties
    {
        public string Type
        {
            get;
            set;
        }

        public IdentifierCategory Kind
        {
            get;
            set;
        }

        public int RunningIndex
        {
            get;
            set;
        }

        public IdentifierProperties(string type, IdentifierCategory kind, int scopeIndex)
        {
            Type = type;
            Kind = kind;
            RunningIndex = scopeIndex;
        }
    }

    /// <summary>
    /// Class containing subroutine properties needed for compilation.
    /// </summary>
    public class SubroutineProperties
    {
        public string Name
        {
            get;
            set;
        }

        public bool IsVoid
        {
            get;
            set;
        }

        public Keyword Kind
        {
            get;
            set;
        }

        // The labels need to be unique only at the subroutine level.
        // The VM translator ensures the labels are globally unique by prepending the subroutine name to them.
        public int IfLabelIndex
        {
            get;
            set;
        }

        public int WhileLabelIndex
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Provides a symbol table abstraction.
    /// The symbol table associates the identifier names found in the program with identifier properties 
    /// needed for compilation: type, kind, and running index.
    /// The symbol table for Jack programs has two nested scopes (class/subroutine).
    /// </summary>
    public static class SymbolTable
    {
        private static Dictionary<IdentifierCategory, int> runningIndexesDictionary;
        private static Dictionary<string, IdentifierProperties> classScopeDictionary;
        private static Dictionary<string, IdentifierProperties> subroutineScopeDictionary;        

        /// <summary>
        /// Creates a new empty symbol table.
        /// </summary>
        static SymbolTable()
        {
            // Initialize the class and subroutine scopes.
            classScopeDictionary = new Dictionary<string, IdentifierProperties>();
            subroutineScopeDictionary = new Dictionary<string, IdentifierProperties>();
            runningIndexesDictionary = new Dictionary<IdentifierCategory, int>();

            // Initialize the running-index counter for each identifier type.            
            runningIndexesDictionary[IdentifierCategory.VAR] = -1;
            runningIndexesDictionary[IdentifierCategory.ARG] = -1;
            runningIndexesDictionary[IdentifierCategory.STATIC] = -1;
            runningIndexesDictionary[IdentifierCategory.FIELD] = -1;            
        }

        /// <summary>
        /// Starts a new class scope (i.e. resets the class' symbol table).
        /// </summary>
        public static void StartClass()
        {
            runningIndexesDictionary[IdentifierCategory.STATIC] = -1;
            runningIndexesDictionary[IdentifierCategory.FIELD] = -1;
            classScopeDictionary.Clear();
        }

        /// <summary>
        /// Starts a new subroutine scope (i.e. resets the subroutine’s symbol table).
        /// </summary>
        /// <param name="isMethod"> True, if subroutine is method; false, otherwise.</param>
        public static void StartSubroutine(bool isMethod)
        {
            runningIndexesDictionary[IdentifierCategory.VAR] = -1;
            int i = -1;
            // If subroutine is method, argi becomes arg(i+1), where argi is the i-th argument in the argument list.
            if (isMethod)
            {
                i = 0;
            }
            runningIndexesDictionary[IdentifierCategory.ARG] = i;
            subroutineScopeDictionary.Clear();
        }        

        /// <summary>
        /// Defines a new identifier of a given name, type, and kind and assigns it a running index.
        /// STATIC and FIELD identifiers have a class scope.
        /// ARG and VAR identifiers have a subroutine scope.
        /// </summary>
        /// <param name="name"> The identifier name.</param>
        /// <param name="type"> The identifier type.</param>
        /// <param name="kind"> The identifier kind.</param>
        public static void Define(string name, string type, IdentifierCategory kind)
        {
            int runningIndex = ++runningIndexesDictionary[kind];
            IdentifierProperties props = new IdentifierProperties(type, kind, runningIndex);

            switch (kind)
            {
                case IdentifierCategory.STATIC:
                case IdentifierCategory.FIELD:
                    classScopeDictionary.Add(name, props);
                    break;
                case IdentifierCategory.ARG:
                case IdentifierCategory.VAR:
                    subroutineScopeDictionary.Add(name, props);
                    break;
                default:
                    break;
            }                        
        }

        /// <summary>
        /// Returns the number of variables of the given kind already defined in the current scope.
        /// If kind=ARG and subroutine=METHOD, returns number_of_arguments_in_argument_list + 1.
        /// </summary>
        /// <param name="kind"> The identifier kind.</param>
        /// <returns></returns>
        public static int VarCount(IdentifierCategory kind)
        {
            return runningIndexesDictionary[kind] + 1;
        }

        /// <summary>
        /// Returns the kind of the specified identifier in the current scope.
        /// If the identifier is unknown in the current scope, returns NONE.
        /// </summary>
        /// <param name="name"> The identifier name.</param>
        /// <returns></returns>
        public static IdentifierCategory KindOf(string name)
        {
            IdentifierCategory idKind;
            IdentifierProperties idProps;
            if (classScopeDictionary.TryGetValue(name, out idProps) || subroutineScopeDictionary.TryGetValue(name, out idProps))
            {
                idKind = idProps.Kind;
            }
            else
            {
                idKind = IdentifierCategory.NONE;
            }

            return idKind;
        }

        /// <summary>
        /// Returns the type of the specified identifier in the current scope.
        /// Should be called only when KindOf() is not NONE.
        /// </summary>
        /// <param name="name"> The identifier name.</param>
        /// <returns></returns>
        public static string TypeOf(string name)
        {
            string idType;
            IdentifierProperties idProps;
            if (classScopeDictionary.TryGetValue(name, out idProps) || subroutineScopeDictionary.TryGetValue(name, out idProps))
            {
                idType = idProps.Type;
            }
            else
            {
                idType = "";
            }

            return idType;
        }

        /// <summary>
        /// Returns the index assigned to the specified identifier in the current scope.
        /// Should be called only when KindOf() is not NONE.
        /// </summary>
        /// <param name="name"> The identifier name.</param>
        /// <returns></returns>
        public static int IndexOf(string name)
        {
            int idIndex;
            IdentifierProperties idProps;
            if (classScopeDictionary.TryGetValue(name, out idProps) || subroutineScopeDictionary.TryGetValue(name, out idProps))
            {
                idIndex = idProps.RunningIndex;
            }
            else
            {
                idIndex = -1;
            }

            return idIndex;
        }
    }
}
