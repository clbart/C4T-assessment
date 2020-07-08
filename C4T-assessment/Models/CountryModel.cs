using System;
using System.Collections.Generic;

namespace C4T_assessment
{
    public class CountryModel
    {
        public string Name { get; set; }

        public string Code { get; set; }

        public List<RegionalBloc> RegionalBlocs { get; set; }

        public string BrowserName { get; set; }

        public DateTime TimeStamp { get; set; }
    }

    public class RegionalBloc
    {
        public string Name { get; set; }

        public string Code { get; set; }

        public List<string> Countries { get; set; }
    }

    public class CountryRequest
    {
        public string Name { get; set; }
    }
}
