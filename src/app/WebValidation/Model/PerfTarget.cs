using System.Collections.Generic;

namespace WebValidation.Model
{
    public class PerfTarget
    {
        public string Category { get; set; }
        public List<double> Quartiles { get; set; }
    }
}
