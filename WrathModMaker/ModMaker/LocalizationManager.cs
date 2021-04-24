using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityModManagerNet;

namespace ModMaker
{
    public interface ILanguage
    {
        string Language { get; set; }

        Version Version { get; set; }

        string Contributors { get; set; }

        string HomePage { get; set; }

        Dictionary<string, string> Strings { get; set; }

        T Deserialize<T>(TextReader reader);

        void Serialize<T>(TextWriter writer, T obj);
    }

    public class LocalizationManager<TDefaultLanguage>
        where TDefaultLanguage : class, ILanguage, new()
    {
        private string _localFolderPath;
        private TDefaultLanguage _localDefault;
        private TDefaultLanguage _local;

        public string Language {
            get {
                if (IsDefault)
                    return _localDefault.Language;
                else
                    return _local.Language;
            }
        }

        public Version Version {
            get {
                if (IsDefault)
                    return _localDefault.Version;
                else
                    return _local.Version;
            }
        }

        public string Contributors {
            get {
                if (IsDefault)
                    return _localDefault.Contributors;
                else
                    return _local.Contributors;
            }
        }

        public string HomePage {
            get {
                if (IsDefault)
                    return _localDefault.HomePage;
                else
                    return _local.HomePage;
            }
        }

        public bool IsDefault => _local == null;

        public string FileName { get; private set; }

        public string this[string key] {
            get {
                if (IsDefault ?
                    _localDefault.Strings.TryGetValue(key, out string text) :
                    _local.Strings.TryGetValue(key, out text))
                    return text;
                else
                    return "$Missing$" + key;
            }
        }

        public void Enable(UnityModManager.ModEntry modEntry)
        {
            char separator = Path.DirectorySeparatorChar;
            _localFolderPath = modEntry.Path + "Localization" + separator;
            _localDefault = new TDefaultLanguage { Version = modEntry.Version };
        }

        public void Disable(UnityModManager.ModEntry modEntry)
        {
            _localFolderPath = null;
            _localDefault = null;
            _local = null;
            FileName = null;
        }

        public string[] GetFileNames(string searchPattern)
        {
            try
            {
                if (Directory.Exists(_localFolderPath))
                {
                    string[] files = Directory.GetFiles(_localFolderPath, searchPattern);
                    for (int i = 0; i < files.Length; i++)
                    {
                        files[i] = Path.GetFileName(files[i]);
                    }
                    return files;
                }
            }
            catch
            {
            }
            return new string[0];
        }

        public void Reset()
        {
            _local = null;
            FileName = null;
        }

        public void Sort()
        {
            if (_local != null)
            {
                Dictionary<string, string> temp = new Dictionary<string, string>();
                foreach (string key in _localDefault.Strings.Keys)
                {
                    if (_local.Strings.TryGetValue(key, out string text))
                        temp[key] = text;
                    else
                        temp[key] = _localDefault.Strings[key];
                }
                _local.Strings = temp;
            }
        }

        public bool Import(string fileName, Action<Exception> onError = null)
        {
            try
            {
                string path = _localFolderPath + fileName;

                if (File.Exists(path))
                {
                    using (StreamReader reader = new StreamReader(path))
                    {
                        _local = _localDefault.Deserialize<TDefaultLanguage>(reader);
                    }

                    FileName = fileName;

                    foreach (string key in _localDefault.Strings.Keys.Except(_local.Strings.Keys))
                    {
                        _local.Strings[key] = _localDefault.Strings[key];
                    }

                    return true;
                }
            }
            catch (Exception e)
            {
                onError(e);
            }

            return false;
        }

        public bool Export(string fileName, Action<Exception> onError = null)
        {
            try
            {
                if (!Directory.Exists(_localFolderPath))
                {
                    Directory.CreateDirectory(_localFolderPath);
                }

                string path = _localFolderPath + fileName;

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                if (!File.Exists(path))
                {
                    using (StreamWriter writer = new StreamWriter(path))
                    {
                        _localDefault.Serialize(writer, IsDefault ? _localDefault : _local);
                    }

                    return true;
                }
            }
            catch (Exception e)
            {
                onError(e);
            }

            return false;
        }
    }
}