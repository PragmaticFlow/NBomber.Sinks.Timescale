namespace NBomber.Sinks.Timescale.Contracts;

static class ColumnNames
{
    public const string Time = "time";
    public const string SessionId = "session_id";
    public const string CurrentOperation = "current_operation";
    public const string NodeInfo = "node_info";
    public const string TestSuite = "test_suite";
    public const string TestName = "test_name";
    public const string Scenario = "scenario";
    public const string Step = "step";
    public const string OkStepStats = "ok_step_stats";
    public const string FailStepStats = "fail_step_stats";
}