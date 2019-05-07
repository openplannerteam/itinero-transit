using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data.Attributes;

namespace Itinero.Transit.Data.Aggregators
{
    
    
    
    
    
    public class TripReaderAggregator : ITripReader
    {
        private ITrip _currentTrip;
        public string GlobalId => _currentTrip.GlobalId;

        public TripId Id => _currentTrip.Id;

        public IAttributeCollection Attributes => _currentTrip.Attributes;


        private readonly IEnumerable<ITripReader> _readers;



        public static ITripReader CreateFrom(IEnumerable<ITripReader> readers)
        {
            if (!readers.Any())
            {
                throw new ArgumentException("At least one ITripReader is needed to aggregate them");
            }

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (readers.Count() == 1)
            {
                return readers.First();
            }
            
            return new TripReaderAggregator(readers);
            
            
        }

        public TripReaderAggregator(IEnumerable<ITripReader> readers)
        {
            _readers = readers;
        }


        public bool MoveTo(TripId tripId)
        {
            foreach (var reader in _readers)
            {
                // ReSharper disable once InvertIf
                if (reader.MoveTo(tripId))
                {
                    _currentTrip = reader;
                    return true;
                }
            }

            return false;
        }
    }
}