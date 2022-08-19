using Jupiter.VMU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Testing
{
    public class ListenerClass
    {
        public ListenerClass(SourceClass source)
        {
            source.Subscribe(p => p.ValueToSet, v => this.WriteLine(v));
        }

        private void WriteLine(bool v)
        {
            Console.WriteLine($"ListenerClass: Value changed to {v}");
        }
    }
}