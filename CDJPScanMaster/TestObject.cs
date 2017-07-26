using DRBDB.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDJPScanMaster
{
    public class TestObject
    {
        TXItem testTx;
        Function testFunc;

        public TestObject(TXItem txItem, List<TXItem> children)
        {
            testTx = txItem;
            ChildTXItems = children;
        }

        public TestObject(Function func, List<TXItem> children)
        {
            testFunc = func;
            ChildTXItems = children;
        }

        public List<TXItem> ChildTXItems { get; private set; }

        public override string ToString()
        {
            if (testFunc != null && testFunc.Name != null) return testFunc.Name.ResourceString;
            if (testTx != null && testTx.Name != null) return testTx.Name.ResourceString;
            return "(BARF)";
        }
    }
}
