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
        public bool IsHardError;

        public override string ToString()
        {
            return $"[{Type}]\n" +
                   $"    {MessageText.Replace("\n", "\n    ")}\n" +
                   $"    > {Connection.ToJson()}\n";
        }
    }

    public static class MessageExtensions
    {
        public static Dictionary<string, (uint count, bool isHardError)> CountTypes(this IEnumerable<Message> msgs)
        {
            var allTypes = new Dictionary<string, (uint count, bool isHardError)>();
            foreach (var msg in msgs)
            {
                if (!allTypes.ContainsKey(msg.Type))
                {
                    allTypes[msg.Type] = (1, msg.IsHardError);
                }
                else
                {
                    var (count, isHardError) = allTypes[msg.Type];
                    allTypes[msg.Type] = (count + 1, isHardError || msg.IsHardError);
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
        List<Message> Validate(TransitDb tdb, bool relax);

        /// <summary>
        /// Gives information about the validation
        /// </summary>
        /// <returns></returns>
        string About { get; }

        string Name { get; }
    }
}