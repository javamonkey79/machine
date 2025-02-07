﻿namespace SIL.Machine.WebApi.Configuration;

public class ClearMLOptions
{
    public const string Key = "ClearML";

    public string ApiServer { get; set; } = "http://localhost:8008";
    public string Queue { get; set; } = "default";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public TimeSpan BuildPollingTimeout { get; set; } = TimeSpan.FromSeconds(2);
    public int MaxStep { get; set; } = 500_000;
    public string RootProject { get; set; } = "Machine";
}
