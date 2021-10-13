using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityModManagerNet;
using ModKit.Utility;

namespace ModKit {
    public interface IMenuPage {
        string Name { get; }

        int Priority { get; }

        void OnGUI(UnityModManager.ModEntry modEntry);
    }

    public interface IMenuTopPage : IMenuPage { }

    public interface IMenuSelectablePage : IMenuPage { }

    public interface IMenuBottomPage : IMenuPage { }

    public class MenuManager : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #region Fields
        private int _tabIndex;
        public int tabIndex {
            get { return _tabIndex; }
            set { _tabIndex = value; NotifyPropertyChanged(); }
        }
        private readonly List<IMenuTopPage> _topPages = new();
        private readonly List<IMenuSelectablePage> _selectablePages = new();
        private readonly List<IMenuBottomPage> _bottomPages = new();
        private static Exception caughtException = null;

        #endregion

        #region Toggle

        public void Enable(UnityModManager.ModEntry modEntry, Assembly _assembly) {
            foreach (var type in _assembly.GetTypes()
                .Where(type => !type.IsInterface && !type.IsAbstract && typeof(IMenuPage).IsAssignableFrom(type))) {
                if (typeof(IMenuTopPage).IsAssignableFrom(type))
                    _topPages.Add(Activator.CreateInstance(type, true) as IMenuTopPage);

                if (typeof(IMenuSelectablePage).IsAssignableFrom(type))
                    _selectablePages.Add(Activator.CreateInstance(type, true) as IMenuSelectablePage);

                if (typeof(IMenuBottomPage).IsAssignableFrom(type))
                    _bottomPages.Add(Activator.CreateInstance(type, true) as IMenuBottomPage);
            }

            static int comparison(IMenuPage x, IMenuPage y) => x.Priority - y.Priority;
            _topPages.Sort(comparison);
            _selectablePages.Sort(comparison);
            _bottomPages.Sort(comparison);

            modEntry.OnGUI += OnGUI;
        }

        public void Disable(UnityModManager.ModEntry modEntry) {
            modEntry.OnGUI -= OnGUI;

            _topPages.Clear();
            _selectablePages.Clear();
            _bottomPages.Clear();
        }

        #endregion

        private void OnGUI(UnityModManager.ModEntry modEntry) {
            var hasPriorPage = false;
            try {
                if (caughtException != null) {
                    GUILayout.Label("ERROR".Red().Bold() + $": caught exception {caughtException}");
                    if (GUILayout.Button("Reset".Orange().Bold(), GUILayout.ExpandWidth(false))) {
                        caughtException = null;
                    }
                    return;
                }
                var e = Event.current;
                UI.userHasHitReturn = e.keyCode == KeyCode.Return;
                UI.focusedControlName = GUI.GetNameOfFocusedControl();


                if (_topPages.Count > 0) {
                    foreach (var page in _topPages) {
                        if (hasPriorPage)
                            GUILayout.Space(10f);
                        page.OnGUI(modEntry);
                        hasPriorPage = true;
                    }
                }

                if (_selectablePages.Count > 0) {
                    if (_selectablePages.Count > 1) {
                        if (hasPriorPage)
                            GUILayout.Space(10f);
                        tabIndex = GUILayout.Toolbar(tabIndex, _selectablePages.Select(page => page.Name).ToArray());

                        GUILayout.Space(10f);
                    }

                    _selectablePages[tabIndex].OnGUI(modEntry);
                    hasPriorPage = true;
                }

                if (_bottomPages.Count > 0) {
                    foreach (var page in _bottomPages) {
                        if (hasPriorPage)
                            GUILayout.Space(10f);
                        page.OnGUI(modEntry);
                        hasPriorPage = true;
                    }
                }
            }
            catch (Exception e) {
                Console.Write($"{e}");
                caughtException = e;
            }
        }
    }
}