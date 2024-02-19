using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ModKit {
    public static class Translater {
        public static Dictionary<string, string> cachedTranslations = new();

#if true
        public static async Task MassTranslate(List<string> strings) {
            using (var client = new HttpClient()) {
                var fromLanguage = "ru";//Russian
                var toLanguage = "en";//English
                var text = String.Join(" | ", strings);
                Mod.Log($"{text}");
                var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={fromLanguage}&tl={toLanguage}&dt=t";
                // Serialize our concrete class into a JSON String
                var stringPayload = JsonConvert.SerializeObject(text);
                var content = new StringContent(stringPayload, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);
                var result = await response.Content.ReadAsStringAsync();
                // how do I best ask for this ^^^ and parse it out?
                // store the text => translation in the cachedTranslations dict
            }
        }

#else

        private const int maxQuerySize = 2000;
        public static async Task MassTranslate(List<string> strings) {
            using (var client = new HttpClient()) {
                string accum = "";
                foreach (var text in strings) {
                    if (accum.Length + text.Length < maxQuerySize - 4) {
                        accum += $"\\{text}//";
                    }
                    else {
                        RawMassTranslate(accum);
                        accum = "";
                    }
                }
                if (accum.Length > 0) {
                    RawMassTranslate(accum);
                    accum = "";
                }
            }
        }
        private static void RawMassTranslate(string text) {
            var fromLanguage = "ru";//Russian
            var toLanguage = "en";//English
            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={fromLanguage}&tl={toLanguage}&dt=t&q={Uri.EscapeDataString(text)}";
            var webClient = new WebClient {
                Encoding = System.Text.Encoding.UTF8
            };
            try {
                var result = webClient.DownloadString(url);
                Mod.Log($"response: {result}");
                result = result.Substring(4, result.IndexOf("\"", 4, StringComparison.Ordinal) - 4);

                cachedTranslations[text] = result;
            }
            catch (Exception e) {
                Mod.Log(url);
                Mod.Error(e);
            }
        }
#endif
        public static String Translate(this string text) {
            if (cachedTranslations.TryGetValue(text, out var value))
                return value;
#if true
            var fromLanguage = "ru";//Russian
            var toLanguage = "en";//English
            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={fromLanguage}&tl={toLanguage}&dt=t&q={Uri.EscapeDataString(text)}";
            var webClient = new WebClient {
                Encoding = Encoding.UTF8
            };
            try {
                var result = webClient.DownloadString(url);
                result = result.Substring(4, result.IndexOf("\"", 4, StringComparison.Ordinal) - 4);
                cachedTranslations[text] = result;
                return result;
            }
            catch (Exception e) {
                Mod.Log(url);
                Mod.Error(e);
                return text;
            }
#else
            var toLanguage = "en";
            var fromLanguage = "ru";
            var uriBuilder = new UriBuilder {
                Scheme = "https",
                Host = "translate.googleapis.com",
                Path = "translate_a/single",
                Query = $"client=gtx&sl={fromLanguage}&tl={toLanguage}&dt=t&q={Uri.EscapeDataString(text)}"
            };
            var request = (HttpWebRequest)WebRequest.Create(uriBuilder.Uri);

            var response = (HttpWebResponse)request.GetResponse();
            Mod.Log($"response: {response}");
            var translation = new StreamReader(response.GetResponseStream()).ReadToEnd();
            Mod.Log($"{text} => {translation}");
            return translation;
#endif

#if true
#else
            var toLanguage = "en";
            var fromLanguage = "ru";
            var inputText = "Все люди рождаются свободными и равными в своем достоинстве и правах. Они наделены разумом и совестью и должны поступать в отношении друг друга в духе братства.";

            var uriBuilder = new UriBuilder
            {
                Scheme = "https",
                Host = "translate.googleapis.com",
                Path = "translate_a/single",
                Query = $"client=gtx&sl={fromLanguage}&tl={toLanguage}&dt=t&q={Uri.EscapeDataString(inputText)}"
            };
            var request = (HttpWebRequest)WebRequest.Create(uriBuilder.Uri);

            var response = (HttpWebResponse)request.GetResponse();

            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            Console.WriteLine(responseString);

            
            -----------------
            var fromLanguage = "ru";//Russian
            var toLanguage = "en";//English
            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={fromLanguage}&tl={toLanguage}&dt=t&q={Uri.EscapeDataString(text)}";
            var webClient = new WebClient {
                Encoding = System.Text.Encoding.UTF8
            };
            try {
                var result = webClient.DownloadString(url);
                result = result.Substring(4, result.IndexOf("\"", 4, StringComparison.Ordinal) - 4);
                cachedTranslations[text] = result;
                return result;
            }
            catch (Exception e) {
                Mod.Log(url);
                Mod.Error(e);
                return text;
            }

            -----------------

            var request = (HttpWebRequest)WebRequest.Create("http://www.example.com/recepticle.aspx");

var response = (HttpWebResponse)request.GetResponse();

var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();


            var toLanguage = "en";//English
            var fromLanguage = "ru";//Russian
            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={fromLanguage}&tl={toLanguage}&dt=t&q={HttpUtility.UrlEncode(text)}";
            var webClient = new WebClient {
                Encoding = System.Text.Encoding.UTF8
            };
            var result = webClient.DownloadString(url);
            try {
                result = result.Substring(4, result.IndexOf("\"", 4, StringComparison.Ordinal) - 4);
                return result;
            }
            catch {
                return "Error";
            }
#endif
        }
    }
}
