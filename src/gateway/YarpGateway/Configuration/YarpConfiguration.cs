namespace YarpGateway.Configuration;

public class YarpConfiguration
{
    public const string SectionName = "ReverseProxy";

    public Dictionary<string, ClusterConfig> Clusters { get; set; } = new();
    public Dictionary<string, RouteConfig> Routes { get; set; } = new();
}

public class ClusterConfig
{
    public Dictionary<string, DestinationConfig> Destinations { get; set; } = new();
    public HealthCheckConfig? HealthCheck { get; set; }
    public LoadBalancingPolicy? LoadBalancingPolicy { get; set; }
}

public class DestinationConfig
{
    public string Address { get; set; } = string.Empty;
}

public class HealthCheckConfig
{
    public bool Enabled { get; set; }
    public string Path { get; set; } = "/health";
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}

public class LoadBalancingPolicy
{
    public string Mode { get; set; } = "RoundRobin"; // RoundRobin, LeastRequests, Random
}

public class RouteConfig
{
    public string ClusterId { get; set; } = string.Empty;
    public MatchConfig Match { get; set; } = new();
    public Dictionary<string, string>? Metadata { get; set; }
}

public class MatchConfig
{
    public string Path { get; set; } = string.Empty;
    public List<string>? Methods { get; set; }
}
