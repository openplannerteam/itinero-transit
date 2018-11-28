using System;

namespace Itinero.Transit.Tests.Functional.Performance
{
    /// <summary>
    /// Extension methods for the performance info class.
    /// </summary>
    public static class PerformanceInfoConsumerExtensions
    {
        /// <summary>
        /// Tests performance for the given action.
        /// </summary>
        public static void TestPerf(this Action action, string name)
        {
            var info = new PerformanceInfoConsumer(name);
            info.Start();
            action();
            info.Stop(string.Empty);
        }

        /// <summary>
        /// Tests performance for the given action.
        /// </summary>
        public static void TestPerf(this Action action, string name, int count)
        {
            var info = new PerformanceInfoConsumer(name + " x " + count.ToInvariantString(), 10000, count);
            info.Start();
            var message = string.Empty;
            while (count > 0)
            {
                action();
                count--;
            }

            info.Stop(message);
        }

        /// <summary>
        /// Tests performance for the given action.
        /// </summary>
        public static void TestPerf(this Func<string> action, string name)
        {
            var info = new PerformanceInfoConsumer(name);
            info.Start();
            var message = action();
            info.Stop(message);
        }

        /// <summary>
        /// Tests performance for the given action.
        /// </summary>
        public static void TestPerf(this Func<string> action, string name, int count)
        {
            var info = new PerformanceInfoConsumer(name + " x " + count.ToInvariantString(), 10000);
            info.Start();
            var message = string.Empty;
            while (count > 0)
            {
                message = action();
                count--;
            }

            info.Stop(message);
        }

        /// <summary>
        /// Tests performance for the given function.
        /// </summary>
        public static T TestPerf<T>(this Func<PerformanceTestResult<T>> func, string name)
        {
            var info = new PerformanceInfoConsumer(name);
            info.Start();
            var res = func();
            info.Stop(res.Message);
            return res.Result;
        }

        /// <summary>
        /// Tests performance for the given function.
        /// </summary>
        public static T TestPerf<T>(this Func<PerformanceTestResult<T>> func, string name, int count)
        {
            var info = new PerformanceInfoConsumer(name + " x " + count.ToInvariantString(), 10000);
            info.Start();
            PerformanceTestResult<T> res = null;
            while (count > 0)
            {
                res = func();
                count--;
            }

            info.Stop(res.Message);
            return res.Result;
        }

        /// <summary>
        /// Tests performance for the given function.
        /// </summary>
        public static TResult TestPerf<T, TResult>(this Func<T, PerformanceTestResult<TResult>> func, string name, T a)
        {
            var info = new PerformanceInfoConsumer(name);
            info.Start();
            var res = func(a);
            info.Stop(res.Message);
            return res.Result;
        }
    }

    /// <summary>
    /// An object containing feedback from a tested function.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PerformanceTestResult<T>
    {
        /// <summary>
        /// Creates a new peformance test result.
        /// </summary>
        /// <param name="result"></param>
        public PerformanceTestResult(T result)
        {
            this.Result = result;
        }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the result.
        /// </summary>
        public T Result { get; set; }
    }
}