using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Data
{
    public interface IOperatorDb : IDatabaseReader<OperatorId, Operator>, IClone<IOperatorDb>
    {
        void PostProcess();
        
        long Count { get; }
    }
}