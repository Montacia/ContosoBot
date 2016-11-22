using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Contoso_Bot
{
    public class intent
    {
        public class TopScoringIntent
        {
            public string intent { get; set; }
            public double score { get; set; }
        }

        public class RootObject
        {
            public string query { get; set; }
            public TopScoringIntent topScoringIntent { get; set; }
            public List<object> entities { get; set; }
        }
    }
}