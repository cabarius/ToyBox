using System.Collections.Generic;

namespace ToyBox {
    public class RemappableInt {
        private readonly List<int> m_mapping = new List<int>();

        public void Add(int to) {
            m_mapping.Add(to);
        }

        public void Clear() {
            m_mapping.Clear();
        }

        public int To(int idx) {
            return m_mapping[idx];
        }

        public int From(int idx) {
            return m_mapping.IndexOf(idx);
        }
    }
}
