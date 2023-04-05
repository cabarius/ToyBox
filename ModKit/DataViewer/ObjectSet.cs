using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Collections.Generic;


namespace ModKit.DataViewer {
    public interface IObjectSet {
        /// <summary> check the existence of an object. </summary>
        /// <returns> true if object is exist, false otherwise. </returns>
        bool IsExist(object obj);

        /// <summary> if the object is not in the set, add it in. else do nothing. </summary>
        /// <returns> true if successfully added, false otherwise. </returns>
        bool Add(object obj);
    }

    public sealed class ObjectSetUsingConditionalWeakTable : IObjectSet {
        /// <summary> unit test on object set. </summary>
        internal static void Main() {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            ObjectSetUsingConditionalWeakTable objSet = new ObjectSetUsingConditionalWeakTable();
            for (int i = 0; i < 10000000; ++i) {
                object obj = new object();
                if (objSet.IsExist(obj)) { Console.WriteLine("bug!!!"); }
                if (!objSet.Add(obj)) { Console.WriteLine("bug!!!"); }
                if (!objSet.IsExist(obj)) { Console.WriteLine("bug!!!"); }
            }
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
        }

        public bool IsExist(object obj) {
            return objectSet.TryGetValue(obj, out tryGetValue_out0);
        }

        public bool Add(object obj) {
            if (IsExist(obj)) {
                return false;
            }
            else {
                objectSet.Add(obj, null);
                return true;
            }
        }

        /// <summary> internal representation of the set. (only use the key) </summary>
        private ConditionalWeakTable<object, object> objectSet = new ConditionalWeakTable<object, object>();

        /// <summary> used to fill the out parameter of ConditionalWeakTable.TryGetValue(). </summary>
        private static object tryGetValue_out0 = null;
    }

    [Obsolete("It will crash if there are too many objects and ObjectSetUsingConditionalWeakTable get a better performance.")]
    public sealed class ObjectSetUsingObjectIDGenerator : IObjectSet {
        /// <summary> unit test on object set. </summary>
        internal static void Main() {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            ObjectSetUsingObjectIDGenerator objSet = new ObjectSetUsingObjectIDGenerator();
            for (int i = 0; i < 10000000; ++i) {
                object obj = new object();
                if (objSet.IsExist(obj)) { Console.WriteLine("bug!!!"); }
                if (!objSet.Add(obj)) { Console.WriteLine("bug!!!"); }
                if (!objSet.IsExist(obj)) { Console.WriteLine("bug!!!"); }
            }
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
        }


        public bool IsExist(object obj) {
            bool firstTime;
            idGenerator.HasId(obj, out firstTime);
            return !firstTime;
        }

        public bool Add(object obj) {
            bool firstTime;
            idGenerator.GetId(obj, out firstTime);
            return firstTime;
        }


        /// <summary> internal representation of the set. </summary>
        private ObjectIDGenerator idGenerator = new ObjectIDGenerator();
    }
}
