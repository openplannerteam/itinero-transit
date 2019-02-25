using System;

namespace Itinero.Transit.IO.LC
{
    public class LoggingOptions
    {
        private readonly Action<(int currentCount, int batchTarget, int batchNummer, int nrOfBatches)>
            _onAdded;

        private readonly int _triggerEvery;

        public LoggingOptions(Action<(int currentCount, int batchTarget, int batchNummer, int nrOfBatches)> onAdded,
            int triggerEvery = 100)
        {
            _onAdded = onAdded;
            _triggerEvery = triggerEvery;
        }

        internal void Ping(int currentCount, int batchTarget, int batchNr, int batchCount)
        {
            if (currentCount % _triggerEvery == 0)
            {
                _onAdded.Invoke((currentCount, batchTarget, batchNr, batchCount));
            }
        }
    }
}