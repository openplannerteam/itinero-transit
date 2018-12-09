using System;
using Itinero.Transit.Tests.Functional.Performance;
using Xunit;

// ReSharper disable UnusedMember.Global

namespace Itinero.Transit.Tests.Functional
{
    /// <summary>
    /// Abstract definition of a functional test.
    /// </summary>
    /// <typeparam name="TOut"></typeparam>
    /// <typeparam name="TIn"></typeparam>
    public abstract class FunctionalTest<TOut, TIn>
    {
        /// <summary>
        /// Gets the name of this test.
        /// </summary>
        protected virtual string Name => this.GetType().Name;

        /// <summary>
        /// Gets or sets the track performance track.
        /// </summary>
        public bool TrackPerformance { get; set; } = true;

        /// <summary>
        /// Gets or sets the logging flag.
        /// </summary>
        public bool Log { get; set; } = true;

        /// <summary>
        /// Executes this test.
        /// </summary>
        /// <returns>The output.</returns>
        public virtual TOut Run()
        {
            return this.Run(default(TIn));
        }

        /// <summary>
        /// Executes this test for the given input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>The output.</returns>
        public virtual TOut Run(TIn input)
        {
            if (this.TrackPerformance)
            {
                Func<TIn, PerformanceTestResult<TOut>> executeFunc = (i) => new PerformanceTestResult<TOut>(this.Execute(i));
                return executeFunc.TestPerf<TIn, TOut>(this.Name, input);
            }
            else
            {
                return this.Execute(input);
            }
        }

        /// <summary>
        /// Executes this test for the given input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>The output.</returns>
        protected abstract TOut Execute(TIn input);

        // TODO: reconsider using xunit here.
        
        /// <summary>
        /// Asserts that the given value is true.
        /// </summary>
        /// <param name="value">The value to verify.</param>
        protected void True(bool value)
        {
            Assert.True(value);
        }

        /// <summary>
        /// Write a log event with the Informational level.
        /// </summary>
        /// <param name="message">The log message.</param>
        protected void Information(string message)
        {
            if (!this.Log) return;
            Serilog.Log.Information(message);
        }
    }
}