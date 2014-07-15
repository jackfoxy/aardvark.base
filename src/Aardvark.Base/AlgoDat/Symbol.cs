﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace Aardvark.Base
{
    public struct Symbol : IEquatable<Symbol>, IComparable<Symbol>, IComparable
    {
        public readonly int Id;

        #region Internal Constructor

        /// <summary>
        /// DO NOT USE THIS CONSTRUCTOR!
        /// Use Create(...) instead.
        /// </summary>
        internal Symbol(int id)
        {
            Id = id;
        }

        #endregion

        #region Static Creators

        public static Symbol Create(string str)
        {
            return SymbolManager.GetSymbol(str);
        }

        public static Symbol CreateNewGuid()
        {
            return SymbolManager.GetSymbol(Guid.NewGuid());
        }

        public static Symbol Create(Guid guid)
        {
            return SymbolManager.GetSymbol(guid);
        }

        public static readonly Symbol Empty = default(Symbol);

        #endregion

        #region Properties

        /// <summary>
        /// Returns true if the Symbol is negative.
        /// For details on negative symbols see the
        /// unary minus operator.
        /// </summary>
        public bool IsNegative
        {
            get { return Id < 0; }
        }

        /// <summary>
        /// Returns true if the Symbol is not negative.
        /// For details on negative symbols see the
        /// unary minus operator.
        /// </summary>
        public bool IsPositive
        {
            get { return Id > 0; }
        }

        public bool IsNotEmpty
        {
            get { return Id != 0; }
        }

        public bool IsEmpty
        {
            get { return Id == 0; }
        }

        #endregion

        #region Overrides

        public override int GetHashCode()
        {
            return Id;
        }

        public override bool Equals(object obj)
        {
            if (obj is Symbol)
            {
                var symbol = (Symbol)obj;
                return (Id == symbol.Id);
            }
            return false;
        }

        public override string ToString()
        {
            return SymbolManager.GetString(Id);
        }

        public Guid ToGuid()
        {
            return SymbolManager.GetGuid(Id);
        }

        #endregion

        #region IEquatable<Symbol> Members

        public bool Equals(Symbol other)
        {
            return Id == other.Id;
        }

        #endregion

        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj is Symbol) return Id.CompareTo(((Symbol)obj).Id);
            else throw new NotSupportedException(string.Format("Cannot compare symbol to {0}", obj));
        }

        #endregion

        #region IComparable<Symbol> Members

        public int CompareTo(Symbol other)
        {
            return Id.CompareTo(other.Id);
        }

        #endregion

        #region Operators

        public static bool operator ==(Symbol a, Symbol b)
        {
            return a.Id == b.Id;
        }

        public static bool operator !=(Symbol a, Symbol b)
        {
            return a.Id != b.Id;
        }

        /// <summary>
        /// Creates a negative symbol from an ordinary symbol.
        /// Negative symbols have no string representation, they
        /// are however useful to store a second value in a
        /// dictionary.
        /// </summary>
        public static Symbol operator -(Symbol symbol)
        {
            return new Symbol(-symbol.Id);
        }

        #endregion

        #region Conversion

        public static implicit operator Symbol(string str)
        {
            return Create(str);
        }

        #endregion
    }

    public interface ITypedSymbol
    {
        Symbol GetSymbol();
        Type GetSymbolType();
    }

    /// <summary>
    /// A typed symbol is a symbol that is associated with
    /// a type at compile time. This can be used in Dicts
    /// to associate each key with a value type.
    /// </summary>
    public struct TypedSymbol<T> : ITypedSymbol
    {
        public readonly Symbol Symbol;

        #region Constructor

        public TypedSymbol(string str)
        {
            Symbol = str;
        }

        public TypedSymbol(Symbol symbol)
        {
            Symbol = symbol;
        }

        #endregion

        #region ITypedSymbol Members

        public Symbol GetSymbol()
        {
            return Symbol;
        }

        public Type GetSymbolType()
        {
            return typeof(T);
        }

        #endregion

        #region Conversion

        public static implicit operator TypedSymbol<T>(string str)
        {
            return new TypedSymbol<T>(str);
        }

        #endregion
    }

    public static class SymbolExtensions
    {
        public static TypedSymbol<T> WithType<T>(this Symbol symbol)
        {
            return new TypedSymbol<T>(symbol);
        }

        /// <summary>
        /// Returns the result of .ToString() of an objects as Symbol.
        /// </summary>
        public static Symbol ToSymbol(this object self)
        {
            return Symbol.Create(self.ToString());
        }
    }


    internal static class SymbolManager
    {
        private static Dict<string, int> s_stringDict = new Dict<string, int>(1024);
        private static Dict<Guid, int> s_guidDict = new Dict<Guid, int>(1024);
        private static List<string> s_allStrings = new List<string>(1024);
        private static List<Guid> s_allGuids = new List<Guid>(1024);
        private static SpinLock s_lock = new SpinLock();


        static SymbolManager()
        {
            s_allStrings.Add(string.Empty);
            s_allGuids.Add(Guid.Empty);
        }

        internal static Symbol GetSymbol(Guid guid)
        {
            int id;
            var locked = false;
            try
            {
                s_lock.Enter(ref locked);
                if (!s_guidDict.TryGetValue(guid, out id))
                {
                    id = s_allStrings.Count;
                    var str = guid.ToString();
                    s_guidDict.Add(guid, id);
                    s_stringDict.Add(str, id);
                    s_allStrings.Add(str);
                    s_allGuids.Add(guid);
                }
            }
            finally { if (locked) s_lock.Exit(); }
            return new Symbol(id);
        }

        internal static Symbol GetSymbol(string str)
        {
            if (string.IsNullOrEmpty(str))
                return default(Symbol);

            int id;
            int hash = str.GetHashCode(); // hashcode computation outside spinlock
            var locked = false;
            try
            {
                s_lock.Enter(ref locked);
                if (!s_stringDict.TryGetValue(str, hash, out id))
                {
                    id = s_allStrings.Count;
                    s_stringDict.Add(str, hash, id);
                    s_allStrings.Add(str);
                    s_allGuids.Add(Guid.Empty);
                }
            }
            finally { if (locked) s_lock.Exit(); }
            return new Symbol(id);
        }

        internal static Guid GetGuid(int id)
        {
            if (id > 0)
            {
                var locked = false;
                try
                {
                    s_lock.Enter(ref locked);
                    return s_allGuids[id];
                }
                finally { if (locked) s_lock.Exit(); }
            }
            return Guid.Empty;
        }

        internal static string GetString(int id)
        {
            var locked = false;
            if (id > 0)
            {
                try
                {
                    s_lock.Enter(ref locked);
                    return s_allStrings[id];
                }
                finally { if (locked) s_lock.Exit(); }
            }
            else if (id < 0)
            {
                string str;
                try
                {
                    s_lock.Enter(ref locked);
                    str = s_allStrings[-id];
                }
                finally { if (locked) s_lock.Exit(); }
                return "-" + str;
            }
            return string.Empty;
        }
    }

}
