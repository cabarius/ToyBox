using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;

namespace ModBase
{
    public class Menu
    {
        public interface IPage
        {
            void OnGUI(UnityModManager.ModEntry modEntry);
        }

        public interface IToggleablePage
        {
            string Name { get; }

            int Priority { get; }

            void OnGUI(UnityModManager.ModEntry modEntry);
        }

        #region Fields

        private Assembly _assembly;

        private int _tabIndex;
        private IPage _topPage;
        private List<IToggleablePage> _pages;

        #endregion

        public Menu(UnityModManager.ModEntry modEntry, Assembly assembly)
        {
            _assembly = assembly;
        }

        #region Toggle

        public void Enable(UnityModManager.ModEntry modEntry, IPage topPage = null)
        {
            _pages = _assembly.GetTypes()
                .Where(type => !type.IsInterface && !type.IsAbstract && typeof(IToggleablePage).IsAssignableFrom(type))
                .Select(page => Activator.CreateInstance(page, true) as IToggleablePage).ToList();

            _topPage = topPage;

            modEntry.OnGUI += OnGUI;
        }

        public void Disable(UnityModManager.ModEntry modEntry)
        {
            modEntry.OnGUI -= OnGUI;
        }

        #endregion

        private void OnGUI(UnityModManager.ModEntry modEntry)
        {
            if (_topPage != null)
            {
                _topPage.OnGUI(modEntry);
                GUILayout.Space(10f);
            }

            if (_pages.Count > 1)
            {
                _pages.Sort((x, y) => x.Priority - y.Priority);
                _tabIndex = GUILayout.Toolbar(_tabIndex, _pages.Select(page => page.Name).ToArray());
                GUILayout.Space(10f);
            }

            if (_pages.Count != 0)
                _pages[_tabIndex].OnGUI(modEntry);
        }
    }
}
