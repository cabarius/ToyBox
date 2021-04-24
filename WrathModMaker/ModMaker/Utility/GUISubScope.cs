using System;
using UnityEngine;

namespace ModMaker.Utility
{
    public class GUISubScope : IDisposable
    {
        public GUISubScope() : this(null) { }

        public GUISubScope(string subtitle)
        {
            if (!string.IsNullOrEmpty(subtitle))
                GUILayout.Label(subtitle.Bold());
            GUILayout.BeginHorizontal();
            GUILayout.Space(10f);
            GUILayout.BeginVertical();
        }

        public void Dispose()
        {
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }
}
