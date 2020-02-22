using PrerenderCleaner;
using System;
using Xunit;

namespace Prerender.Test
{
    public class UnitTest1
    {
        [Fact]
        public void DoRun()
        {
            var func = new Function();
            func.StartCleaning(null);
        }
    }
}
