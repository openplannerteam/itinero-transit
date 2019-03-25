using System;

namespace Itinero.Transit.Data
{
    /// <inheritdoc />
    /// <summary>
    /// An enumerator is an object which has all the fields of an IConnection, but changes those fields when
    /// 'MoveNext' is called.
    /// </summary>
    public interface IConnectionEnumerator : IConnection
    {

        bool MoveNext(DateTime? dateTime = null);
        bool MovePrevious(DateTime? dateTime = null);

    }
}