using ScanMaster.Database.Converters;
using ScanMaster.Database.Helpers;
using System;

namespace ScanMaster.Database.Objects
{
    public class StateDataScaler : DataScaler
    {
        public uint ID { get; private set; }
        public uint Mask { get; private set; }
        public StateOperator Operator { get; private set; }
        public StateDataScaler(uint id, uint mask, byte op)
        {
            ID = id;
            Mask = mask;
            if(Enum.IsDefined(typeof(StateOperator), op))
            {
                Operator = (StateOperator)op;
            }
        }

        bool EvaluateMask(int data)
        {
            switch(Operator)
            {
                case StateOperator.EQUAL:
                    return data == Mask;
                case StateOperator.NOT_EQUAL:
                    return data != Mask;
                case StateOperator.GREATER:
                    return data > Mask;
                case StateOperator.LESS:
                    return data < Mask;
                case StateOperator.MASK_ZERO:
                    return (data & Mask) == 0;
                case StateOperator.MASK_NOT_ZERO:
                    return (data & Mask) != 0;
                case StateOperator.UNKNOWN:
                default:
                    return false;
            }
        }

        public void ScaleData(DataDisplay container)
        {
            container.ScaledIntData = EvaluateMask(container.RawIntData) ? 1 : 0;
        }
    }
}
