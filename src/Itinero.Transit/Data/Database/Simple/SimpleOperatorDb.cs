using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Data.Simple
{
    public class SimpleOperatorDb : SimpleDb<OperatorId, Operator>, IOperatorDb, IClone<SimpleOperatorDb>
    {
        public SimpleOperatorDb(uint dbId) : base(dbId)
        {
        }

        public SimpleOperatorDb(SimpleDb<OperatorId, Operator> copyFrom) : base(copyFrom)
        {
        }

        public void PostProcess()
        {
        }

        public SimpleOperatorDb Clone()
        {
            return new SimpleOperatorDb(this);
        }


        IOperatorDb IClone<IOperatorDb>.Clone()
        {
            return Clone();
        }
    }
}