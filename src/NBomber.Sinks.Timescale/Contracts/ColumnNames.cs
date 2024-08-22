namespace NBomber.Sinks.Timescale.Contracts;

public static class ColumnNames
{
    public const string Time = "time";
    public const string ScenarioTimestamp = "scenario_timestamp";
    public const string SessionId = "session_id";
    public const string CurrentOperation = "current_operation";
    
    public const string NodeInfo = "node_info";
    public const string TestSuite = "test_suite";
    public const string TestName = "test_name";
    
    public const string Scenario = "scenario";
    public const string Step = "step";
    
    public const string AllReqCount = "all_req_count";
    public const string AllDataAll = "all_data_all";
    
    public const string OkReqCount = "ok_req_count";
    public const string OkReqRps = "ok_req_rps";
    public const string OkLatencyMax = "ok_latency_max";
    public const string OkLatencyMean = "ok_latency_mean";
    public const string OkLatencyMin = "ok_latency_min";
    public const string OkLatencyStdDev = "ok_latency_std_dev";
    public const string OkLatencyP50 = "ok_latency_p50";
    public const string OkLatencyP75 = "ok_latency_p75";
    public const string OkLatencyP95 = "ok_latency_p95";
    public const string OkLatencyP99 = "ok_latency_p99";
    public const string OkDataMin = "ok_data_min";
    public const string OkDataMean = "ok_data_mean";
    public const string OkDataMax = "ok_data_max";
    public const string OkDataAll = "ok_data_all";
    public const string OkDataP50 = "ok_data_p50";
    public const string OkDataP75 = "ok_data_p75";
    public const string OkDataP95 = "ok_data_p95";
    public const string OkDataP99 = "ok_data_p99";
    public const string OkStatusCodes = "ok_status_codes";
    public const string OkLatencyCount = "ok_latency_count";
    
    public const string FailReqCount = "fail_req_count";
    public const string FailReqRps = "fail_req_rps";
    public const string FailLatencyMax = "fail_latency_max";
    public const string FailLatencyMean = "fail_latency_mean";
    public const string FailLatencyMin = "fail_latency_min";
    public const string FailLatencyStdDev = "fail_latency_std_dev";
    public const string FailLatencyP50 = "fail_latency_p50";
    public const string FailLatencyP75 = "fail_latency_p75";
    public const string FailLatencyP95 = "fail_latency_p95";
    public const string FailLatencyP99 = "fail_latency_p99";
    public const string FailDataMin = "fail_data_min";
    public const string FailDataMean = "fail_data_mean";
    public const string FailDataMax = "fail_data_max";
    public const string FailDataAll = "fail_data_all";
    public const string FailDataP50 = "fail_data_p50";
    public const string FailDataP75 = "fail_data_p75";
    public const string FailDataP95 = "fail_data_p95";
    public const string FailDataP99 = "fail_data_p99";
    public const string FailStatusCodes = "fail_status_codes";
    public const string FailLatencyCount = "fail_latency_count";
    
    public const string SimulationValue = "simulation_value";
}
