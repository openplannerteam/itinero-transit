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
            if (_tt.Id() == id)
            {
                return _tt;
            }

            return null;
        }

        public Uri TimeTableIdFor(DateTime includedTime)
        {
            return _tt.Id();
        }
    }
}