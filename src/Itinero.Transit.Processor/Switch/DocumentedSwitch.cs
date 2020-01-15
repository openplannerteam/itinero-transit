using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Itinero.Transit.Processor.Switch
{
    /// <summary>
    /// A documented switch contains all flags explicitly,
    /// in order to be able to generate documentation and to make parsing easier 
    /// </summary>
    abstract class DocumentedSwitch
    {
        /// <summary>
        /// The names of the switch
        /// </summary>
        public readonly string[] Names;

        /// <summary>
        /// What does this switch do?
        /// </summary>
        public readonly string Documentation;

        /// <summary>
        /// 
        /// Give a list of expected and optional arguments.
        /// E.g. (for write-geojson), this would be:
        /// [( "file", true, "The file where the geojson will be written to")
        /// , ("left", false, "The minimum latitude")
        /// , ("right", false, ...)
        /// , ("top", false, ...)
        /// , ("bottom", false, ...)]
        ///
        /// Indicating an must-have argument 'file' and four optional arguments
        /// </summary>
        private readonly List<(List<string> argNames, bool isObligated, string comment, string defaultValue)>
            _extraParams;

        public List<string> OptionNames => _extraParams.SelectMany(param => param.argNames).ToList();


        /// <summary>
        /// Should this switch be clearly showed in the documentation?
        /// </summary>
        /// <returns></returns>
        public readonly bool SwitchIsStable;

        protected DocumentedSwitch(
            string[] names, string about,
            List<(List<string> argName, bool isObligated, string comment, string defaultValue)> extraParams,
            bool isStable
        )
        {
            Names = names;
            Documentation = about;
            _extraParams = extraParams;
            SwitchIsStable = isStable;
        }


        /// <summary>
        /// Converts the command line arguments into a dictionary, using ExtraParams.
        /// Unnamed parameters are matched with the beginning of the list
        ///
        /// E.g. --write-geojson left=123 someFile right=456 ...
        /// will match someFile with the first 'file' argument
        ///
        /// The resulting dictionary will 
        /// 
        /// </summary>
        /// <returns></returns>
        internal Dictionary<string, string> ParseExtraParams(List<string> arguments)
        {
            var result = new Dictionary<string, string>();

            var expected = _extraParams;
            var index = 0;

            var used = new HashSet<int>();

            // First, handle all the named arguments, thus every element containing '='
            // We _translate_ the argument name to the first index in the according list
            for (var i = 0; i < arguments.Count; i++)
            {
                var argument = arguments[i];
                if (!SplitKeyValue(argument, out var key, out var value)) continue;
                // The argument is of the format 'file=abc'


                var found = false;
                foreach (var (argNames, _, _, _) in expected)
                {
                    foreach (var argName in argNames)
                    {
                        // ReSharper disable once InvertIf
                        if (argName.Equals(key))
                        {
                            found = true;
                            key = argNames[0];
                            break;
                        }

                        if (found)
                        {
                            break;
                        }
                    }
                }

                if (!found)
                {
                    throw new ArgumentException(
                        $"Found a parameter with key {key}, but no such parameter is defined for this switch.\n\n{Help()}");
                }


                result.Add(key, value);
                used.Add(i);
            }


            // Now, we handle all the boolean arguments, thus every element starting with '-', e.g. '-some-flag'
            // We _translate_ the argument name to the first index in the according list
            for (var i = 0; i < arguments.Count; i++)
            {
                var argument = arguments[i];
                if (used.Contains(i)) continue;

                if (!argument.StartsWith("-")) continue;
                // The argument is of the format '-flag'

                var key = argument.Substring(1);
                var found = false;
                foreach (var (argNames, _, _, _) in expected)
                {
                    foreach (var argName in argNames)
                    {
                        // ReSharper disable once InvertIf
                        if (argName.Equals(key))
                        {
                            found = true;
                            key = argNames[0];
                            break;
                        }

                        if (found)
                        {
                            break;
                        }
                    }
                }

                if (!found)
                {
                    throw new ArgumentException(
                        $"Found a flag with key {key}, but no such flag is defined for this switch.\n\n{Help()}");
                }


                result.Add(key, "true");
                used.Add(i);
            }


            // Now, we handle the resting elements without a name
            for (var i = 0; i < arguments.Count; i++)
            {
                var argument = arguments[i];
                if (used.Contains(i)) continue;

                // We found an argument which does not use "="
                // Was it used already?
                var name = expected[index].argNames[0];
                while (result.ContainsKey(name))
                {
                    index++;
                    if (index >= expected.Count)
                    {
                        throw new ArgumentException(
                            $"Too many arguments are given for the switch {Names[0]}, only expects {expected.Count} but got {arguments.Count()} instead");
                    }

                    name = expected[index].argNames[0];
                }

                result.Add(name, argument);
                used.Add(i);
            }


            // Did we use all arguments?
            if (arguments.Count > used.Count)
            {
                var unused = string.Join(", ",
                    arguments.Where((str, i) => !used.Contains(i)));
                throw new ArgumentException(
                    $"{arguments.Count - used.Count} arguments in switch {Names[0]} were not used: \n  {unused}");
            }


            // At last, do we have all obligated parameters?
            // And we fill out the default values if not obligated
            foreach (var (name, obligated, _, defaultValue) in expected)
            {
                if (obligated && !result.ContainsKey(name[0]))
                {
                    throw new ArgumentException($"The argument '{name[0]}' is missing");
                }

                if (!result.ContainsKey(name[0]))
                {
                    // Add default of optional parameter
                    result.Add(name[0], defaultValue);
                }
            }

            return result;
        }

        public string MarkdownName()
        {
            var text = "";
            text += Names[0];
            if (Names.Length > 1)
            {
                text += " (";
                for (var i = 1; i < Names.Length; i++)
                {
                    text += Names[i] + ", ";
                }

                text = text.Substring(0, text.Length - 2);
                text += ")";
            }


            return text;
        }

        private bool IsMulti()
        {
            return this is IMultiTransitDbSource || this is IMultiTransitDbModifier || this is IMultiTransitDbSink;
        }

        private bool IsSource()
        {
            return this is IMultiTransitDbSource || this is ITransitDbSource;
        }

        private bool IsModifier()
        {
            return this is IMultiTransitDbModifier || this is ITransitDbModifier;
        }

        private bool IsSink()
        {
            return this is IMultiTransitDbSink || this is ITransitDbSink;
        }

        /// <summary>
        /// Creates a help text for this switch
        /// </summary>
        /// <returns></returns>
// ReSharper disable once MemberCanBeProtected.Global
        public string Help(bool markdown = false)
        {
            var text = "";
            if (markdown)
            {
                text += "#### " + MarkdownName();
            }
            else
            {
                text += string.Join(", ", Names);
            }

            text += "\n\n";
            text += "This switch is ";
            if (!SwitchIsStable)
            {
                text += "**experimental**, ";
            }

            if (IsMulti())
            {
                text += "multi-compatible, ";
            }

            if (IsSource())
            {
                text += "a transitdb-source, ";
            }


            if (IsModifier())
            {
                text += "a transitdb-modifier, ";
            }

            if (IsSink())
            {
                text += "a transitdb-sink, ";
            }

            text = text.Substring(0, text.Length - 2);

            text += "\n\n";
            text += "   " + Documentation + "\n\n";

            if (markdown)
            {
                if (_extraParams.Count == 0)
                {
                    text += "\n\n*This switch does not need parameters*\n";
                }
                else
                {
                    text += "| Parameter  | Default value | Explanation       |\n";
                    text += "|----------- | ------------- | ----------------- |\n";
                }
            }

            foreach (var (argNames, isObligated, comment, defaultValue) in _extraParams)
            {
                var argName = string.Join(", ", argNames);


                if (markdown)
                {
                    if (isObligated)
                    {
                        text += $"| **{argName}** | _Obligated param_ | {comment} | ";
                    }
                    else
                    {
                        var defV = String.IsNullOrEmpty(defaultValue) ? "_NA_" : $"`{defaultValue}`";
                        text += $"| {argName} | {defV}| {comment} | ";
                    }
                }
                else
                {
                    text += $"   {argName}=*\n\t{comment}";
                    if (isObligated)
                    {
                        text += " (Obligated)";
                    }
                    else
                    {
                        text += $" (Optional, default is {defaultValue})";
                    }
                }

                text += "\n";
            }

            return text;
        }

        /// <summary>
        /// Returns true if the given string contains a key value like 'key=value'.
        /// </summary>
        public static bool SplitKeyValue(string keyValueString, out string key, out string value)
        {
            key = null;
            value = null;
            if (keyValueString.Count(x => x == '=') == 1)
            {
                // there is only one '=' sign here.
                int idx = keyValueString.IndexOf('=');
                if (idx > 0 && idx < keyValueString.Length - 1)
                {
                    key = keyValueString.Substring(0, idx);
                    value = keyValueString.Substring(idx + 1, keyValueString.Length - (idx + 1));
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the given string contains one or more comma seperated values.
        /// </summary>
        public static bool SplitValuesArray(string valuesArray, out string[] values)
        {
            values = valuesArray.Split(',');
            return true;
        }

        /// <summary>
        /// Returns true if the given string value represent true.
        /// </summary>
        internal static bool IsTrue(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   (value.ToLowerInvariant() == "yes" ||
                    value.ToLowerInvariant() == "true");
        }

        /// <summary>
        /// Parses an integer from the given value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static int? Parse(string value)
        {
            if (int.TryParse(value, out var val))
            {
                return val;
            }

            return null;
        }

    
    }
}