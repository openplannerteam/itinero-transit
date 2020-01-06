using System;
using System.Collections.Generic;
using Itinero.Transit.Logging;

namespace Itinero.Transit.Tests.Functional.Utils
{
    public abstract class FunctionalTestWithInput<TIn> : FunctionalTest
    {
        public TIn Input { get; set; }

        public void Run(TIn input)
        {
            Input = input;
            Run();
        }


        public void RunOverMultiple(List<TIn> inputs)
        {
            if (inputs[0] is FunctionalTest)
            {
                Information($"Running {inputs.Count} embedded tests");
            }
            else
            {
                Information($"Running test {Name} with {inputs.Count} inputs");
            }


            var i = 0;

            var failed = 0;
            var report = $"";

            // Run test over all the inputs
            foreach (var input in inputs)
            {
                i++;
                try
                {
                    Run(input);
                    Log.Information($"Test {i}/{inputs.Count} finished successful");
                }
                catch (Exception e)
                {
                    failed++;
                    var msg = $"Test {Name} failed for input {i}\n\n {input.ToString()}\n\n with message {e}\n{e.StackTrace}";
                    if (input is FunctionalTest ft)
                    {
                        msg = $"Test {ft.Name} ({i}/{inputs.Count}) failed with message {e}";
                    }

                    Information(msg);
                    report += "\n\n---------------\n\n" + msg;
                }

                
            }

            if (failed == 0)
            {
                // All tests were successful!
                return;
            }

            report = $"{failed} tests failed:\n{report}";
            Information(report);
            throw new Exception(report);
        }
    }
}