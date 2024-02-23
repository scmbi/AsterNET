using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Text;
using System.Threading;

namespace AsterNET.IO
{
    public class Simultaneous : IDisposable
    {
        private int _counter = 0;

        public IDisposable Run()
        {
            _counter++;
            return this;
        }

        public override string ToString()
            => _counter.ToString();

        public void Dispose()
            =>  _counter--;
    }
}
