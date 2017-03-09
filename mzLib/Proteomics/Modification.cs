﻿using System.Text;
using System.Linq;

namespace Proteomics
{
    public class Modification
    {
        #region Public Fields

        public readonly string id;

        #endregion Public Fields

        #region Public Constructors

        public Modification(string id)
        {
            this.id = id;
        }

        #endregion Public Constructors

        #region Public Methods

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ID   " + id);
            return sb.ToString();
        }

        public virtual bool Equals(Modification m)
        {
            return this.id == m.id;
        }

        public virtual int GetCustomHashCode()
        {
            return sum_string_chars(this.id);
        }

        #endregion Public Methods

        #region Private Methods

        private int sum_string_chars(string s)
        {
            if (s == null) return 0;
            int sum = int.MinValue + s.ToCharArray().Sum(c => 37 * c);
            return sum != 0 ? sum : sum + 1;
        }

        #endregion Private Methods
    }
}