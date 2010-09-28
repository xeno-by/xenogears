﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using XenoGears.Assertions;
using XenoGears.Functional;
using XenoGears.Strings;

namespace XenoGears.Formats
{
    public partial class Json
    {
        public static dynamic ParseOrDefault(String json)
        {
            return ParseOrDefault(json, null as Json);
        }

        public static dynamic ParseOrDefault(String json, dynamic @default)
        {
            return ParseOrDefault(json, () => @default);
        }

        public static dynamic ParseOrDefault(String json, Func<dynamic> @default)
        {
            try { return Parse(json); }
            catch { return new Json((@default ?? (() => null))()); }
        }

        public static dynamic Parse(String s)
        {
            s = s ?? String.Empty;
            s = s.Replace(@"//.*$", "", RegexOptions.Multiline);
            s = s.Replace(@"/\*.*?\*/", "", RegexOptions.Singleline);
            s = s.Trim();
            s.AssertNotEmpty();

            var json = new Json();
            if (s[0] == '{')
            {
                json._my_state = State.Object;
                var contents = s.AssertExtract(@"^\s*\{\s*(?<contents>.*)\s*\}\s*$", RegexOptions.Singleline);
                var parts = DisassembleJsonObject(contents.Trim()).ToReadOnly();

                parts.ForEach(part =>
                {
                    var map1 = part.Parse(@"^\s*""(?<key>.*?)""\s*:\s*(?<value>.*)\s*$");
                    var map2 = part.Parse(@"^\s*'(?<key>.*?)'\s*:\s*(?<value>.*)\s*$");
                    var map3 = part.Parse(@"^\s*(?<key>.*?)\s*:\s*(?<value>.*)\s*$");
                    var map = (map1 ?? map2 ?? map3).AssertNotNull();

                    var key = map["key"].Trim();
                    var s_value = map["value"].Trim();
                    var value = Parse(s_value);
                    json.Add(key, value);
                });
            }
            else if (s[0] == '[')
            {
                json._my_state = State.Array;
                var contents = s.AssertExtract(@"^\s*\[\s*(?<contents>.*)\s*\]\s*$", RegexOptions.Singleline);
                var parts = DisassembleJsonObject(contents.Trim()).ToReadOnly();
                parts.ForEach((part, i) => json[i] = Parse(part));
            }
            else
            {
                var primitive = ((Func<Object>)(() =>
                {
                    if (s == "null") return null;
                    if (s == "false") return false;
                    if (s == "true") return true;

                    if (s.Matches(@"^'.*'$"))
                    {
                        return s.FromJsonString();
                    }
                    else if (s.Matches("^\".*\"$"))
                    {
                        return s.FromJsonString();
                    }
                    else
                    {
                        int integer;
                        if (int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out integer))
                        {
                            return integer;
                        }

                        double floatingPoint;
                        if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out floatingPoint))
                        {
                            return floatingPoint;
                        }

                        // todo. maybe add custom converters, e.g. for datetime
                        // todo. maybe even integrate this with V8, but what for?
                        throw AssertionHelper.Fail();
                    }
                }))();

                json._my_state = State.Primitive;
                json._my_primitive = primitive;
            }

            return json;
        }

        // Splits JSON lists and object definitions into structural parts.
        // String.Split method won't work for lists of objects.
        private static IEnumerable<String> DisassembleJsonObject(String s)
        {
            var brackBalance = 0;
            var braceBalance = 0;
            var squote = false;
            var dquote = false;
            var buffer = new StringBuilder();

            foreach (var c in s)
            {
                switch (c)
                {
                    case '[':
                        ++brackBalance;
                        break;
                    case ']':
                        --brackBalance;
                        break;
                    case '{':
                        ++braceBalance;
                        break;
                    case '}':
                        --braceBalance;
                        break;
                    case '\'':
                        squote = !squote;
                        break;
                    case '\"':
                        dquote = !dquote;
                        break;
                    default:
                        if (c == ',' && brackBalance == 0 && braceBalance == 0 && !squote && !dquote)
                        {
                            yield return buffer.ToString().Trim();
                            buffer = new StringBuilder();
                            continue;
                        }
                        break;
                }

                buffer.Append(c);
            }

            var lastElement = buffer.ToString().Trim();
            if (!String.IsNullOrEmpty(lastElement))
            {
                yield return lastElement;
            }
        }
    }
}
