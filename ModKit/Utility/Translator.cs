using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web;

namespace ModKit {
    public static class Translater {
        public static String Translate(this string text) {
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
        }
    }
}
