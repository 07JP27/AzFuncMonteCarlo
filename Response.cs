namespace AzFuncMonteCarlo
{
    public class Response
    {
        public double SimulatedMedianValue { get; set; }
        public double SimulatedAverageValue { get; set; }
        public double SimulatedModeValue { get; set; }        
        public double DurationSecond { get; set; }
        public List<double> Iterations { get; set; } = new List<double>();
    }
}