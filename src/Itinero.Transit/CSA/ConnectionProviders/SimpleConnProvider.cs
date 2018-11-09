using System;

namespace Itinero.Transit
{
    public class SimpleConnProvider : IConnectionsProvider
    {
        private readonly ITimeTable _tt;

        public SimpleConnProvider(ITimeTable tt)
        {
            _tt = tt;
        }
        
        public ITimeTable GetTimeTable(Uri id)
        {
            return _tt;
        }

        public Uri TimeTableIdFor(DateTime includedTime)
        {
            return _tt.Id();
        }
    }
}