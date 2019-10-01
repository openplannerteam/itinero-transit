using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Processor.Validator
{
    public struct Message
    {
        /// <summary>
        /// The connection causing the warning (if applicable)
        /// </summary>
        public Connection Connection;

        public string MessageText;
        public string Type;

        public override string ToString()
        {
            return $"{Connection?.GlobalId ?? ""} \t[{Type}]: {MessageText}";
        }
    }

    public static class MessageExtensions
    {
        public static Dictionary<string, uint> CountTypes(this IEnumerable<Message> msgs)
        {
            var allTypes = new Dictionary<string, uint>();
            foreach (var msg in msgs)
            {
                if (!allTypes.ContainsKey(msg.Type))
                {
                    allTypes[msg.Type] = 1;
                }
                else
                {
                    allTypes[msg.Type]++;
                }
            }

            return allTypes;
        }

        public static void PrintType(this IEnumerable<Message> msgs, string typeToPrint, int total, int cutoff)
        {
            var i = 0;
            foreach (var msg in msgs)
            {
                if (msg.Type != typeToPrint)
                {
                    continue;
                }
                
                
                i++;
                Console.WriteLine(msg);

                if (i >= cutoff && total - cutoff != 0)
                {
                    Console.WriteLine(
                        $"... printed {cutoff}, {total - cutoff} more results of type {typeToPrint} omitted ...");
                    break;
                }
            }
        }
    }

    public interface IValidation
    {
        /// <summary>
        /// Generates a list of warnings/errors for the transitDb
        /// </summary>
        /// <returns></returns>
        List<Message> Validate(TransitDb tdb);

        /// <summary>
        /// Gives information about the validation
        /// </summary>
        /// <returns></returns>
        string About { get; }

        string Name { get; }
    }
}