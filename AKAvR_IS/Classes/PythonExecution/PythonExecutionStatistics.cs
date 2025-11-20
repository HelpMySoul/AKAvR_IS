namespace AKAvR_IS.Classes.PythonExecution
{
    public class PythonExecutionStatistics
    {
        public int TotalExecutions { get; set; }
        public int SuccessfulExecutions { get; set; }
        public int FailedExecutions { get; set; }
        public TimeSpan TotalExecutionTime { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public DateTime LastExecutionDate { get; set; }
    }
}
