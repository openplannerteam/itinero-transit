using System;
using System.Collections.Generic;
using Itinero.Transit.Data;

namespace Itinero.Transit.Processor.Switch.Misc
{
    class GarbageCollect : DocumentedSwitch,
        IMultiTransitDbModifier, IMultiTransitDbSink, IMultiTransitDbSource
    {
        private static readonly string[] _names = {"--garbage-collect", "--gc"};

        private static string About =
            "Run garbage collection. This is for debugging";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>();

        private const bool IsStable = false;


        public GarbageCollect
            () : base(_names, About, _extraParams, IsStable)
        {
        }

        private static void Run()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }


        public void Use(Dictionary<string, string> __, List<TransitDbSnapShot> _)
        {
            Run();
        }

        public List<TransitDbSnapShot> Generate(Dictionary<string, string> parameters)
        {
            Run();
            return new List<TransitDbSnapShot>();
        }

        public List<TransitDbSnapShot> Modify(Dictionary<string, string> parameters, List<TransitDbSnapShot> transitDbs)
        {
            Run();
            return transitDbs;
        }
    }
}