using System;
using System.Collections.Generic;

namespace Itinero.IO.LC
{
    /// <inheritdoc />
    ///  <summary>
    ///  This class is one of the centerpieces of the intermodal aspect.
    ///  We have multiple sources of connections, namely the various PT operators.
    ///  They all provide their timetables at various schedules.
    ///  This class takes multiple such timetables and 'fuses' them into an synthetic table.
    ///  </summary>
    public class ConnectionProviderMerger : IConnectionsProvider
    {
        public static readonly string SyntheticUri = "https://pt.anyways.eu/connections?departureTime=";


        private readonly List<IConnectionsProvider> _sources;
        private readonly List<ITimeTable> _curTables = new List<ITimeTable>();


        public ConnectionProviderMerger(params IConnectionsProvider[] sources)
        : this(new List<IConnectionsProvider>(sources))
        {
            
        }
        
        public ConnectionProviderMerger(List<IConnectionsProvider> sources)
        {
            _sources = sources;
            foreach (var _ in sources)
            {
                _curTables.Add(null);
            }
        }


        public ITimeTable GetTimeTable(Uri uri)
        {
            var id = uri.ToString();
            if (!id.StartsWith(SyntheticUri))
            {
                throw new ArgumentException("Not a synthetic URI-ID");
            }

            id = id.Substring(SyntheticUri.Length);

            var time = DateTime.Parse(id);

            for (var i = 0; i < _sources.Count; i++)
            {
                var idTime = _sources[i].TimeTableIdFor(time);
                if (_curTables[i] != null && _curTables[i].Id().ToString().Equals(id))
                {
                    // We already have this table, no need to download it once more
                    continue;
                }

                _curTables[i] = _sources[i].GetTimeTable(idTime);
            }

            return new SyntheticTimeTable(_curTables, uri);
        }

        public Uri TimeTableIdFor(DateTime includedTime)
        {
            return new Uri($"{SyntheticUri}{includedTime:O}");
        }
    }
}