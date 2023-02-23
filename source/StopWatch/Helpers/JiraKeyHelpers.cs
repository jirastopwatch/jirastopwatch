/**
 * Copyright 2023 Y. Meyer-Norwood
 * Copyright 2020 Dan Tulloh
 * Copyright 2016 Carsten Gehling
 *
 * For a full list of contributing authors, see:
 *
 *     https://jirastopwatch.github.io/contributors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at:
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Text.RegularExpressions;

namespace StopWatch
{
    static public class JiraKeyHelpers
    {
        public static string ParseUrlToKey(string text)
        {
            // If text contains a key, extract and return it
            Match m = new Regex(@"[A-Z0-9_]+-\d+", RegexOptions.IgnoreCase).Match(text);

            if (m.Success)
                return m.Value;

            // otherwise return original text
            return text;
        }
    }
}

