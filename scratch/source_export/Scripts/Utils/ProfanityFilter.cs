using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace GameClient.Utils
{
    public static class ProfanityFilter
    {
        private static readonly List<string> _badWords = new List<string> {
            "fuck", "shit", "bitch", "đụ", "địt", "đm", "vcl", "loz"
        };

        private static readonly Dictionary<char, string> _leetspeakMap = new Dictionary<char, string>
        {
            {'a', "[a@4^]"}, {'e', "[e3]"}, {'i', "[i1!|]"}, {'o', "[o0]"}, {'u', "[uµ]"},
            {'c', "[c(k]"}, {'s', "[s5$]"}, {'đ', "[đd]"}
        };

        private static List<Regex> _compiledRegexes = new List<Regex>();
        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized) return;

            _compiledRegexes.Clear();
            foreach (var word in _badWords)
            {
                string pattern = "";
                foreach (char c in word)
                {
                    if (_leetspeakMap.ContainsKey(c))
                        pattern += _leetspeakMap[c] + @"\W*"; // \W* cho phép ký tự đặc biệt/khoảng trắng xen giữa
                    else
                        pattern += Regex.Escape(c.ToString()) + @"\W*";
                }
                _compiledRegexes.Add(new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled));
            }
            _initialized = true;
        }

    }
}
