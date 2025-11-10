using System;
using System.Collections.Generic;

namespace InsightLearn.Core.DTOs.Admin
{
    public class RealTimeMetricsDto
    {
        public int ActiveUsers { get; set; }
        public int ActiveSessions { get; set; }
        public int RequestsPerSecond { get; set; }
        public double AverageResponseTime { get; set; } // milliseconds
        public Dictionary<string, int> ActiveUsersByPage { get; set; } = new();
        public List<string> CurrentlyStreamingVideos { get; set; } = new();
        public int ActiveChatSessions { get; set; }
        public SystemHealthStatus SystemHealth { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }

    public class SystemHealthStatus
    {
        public ServiceStatus ApiStatus { get; set; } = new();
        public ServiceStatus DatabaseStatus { get; set; } = new();
        public ServiceStatus MongoDbStatus { get; set; } = new();
        public ServiceStatus RedisStatus { get; set; } = new();
        public ServiceStatus OllamaStatus { get; set; } = new();

        public double CpuUsage { get; set; } // percentage
        public double MemoryUsage { get; set; } // percentage
        public long MemoryUsedBytes { get; set; }
        public long MemoryTotalBytes { get; set; }

        public bool IsHealthy => ApiStatus.IsHealthy &&
                                  DatabaseStatus.IsHealthy &&
                                  MongoDbStatus.IsHealthy;
    }

    public class ServiceStatus
    {
        public string Name { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public double ResponseTime { get; set; } // milliseconds
        public string? ErrorMessage { get; set; }
        public DateTime LastChecked { get; set; }
    }
}