using System;
using System.Collections.Generic;

namespace InsightLearn.Core.DTOs.Admin
{
    public class ChartDataDto
    {
        public string ChartType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<DataPoint> DataPoints { get; set; } = new();
        public List<string> Labels { get; set; } = new();
        public List<DataSeries> Series { get; set; } = new();
        public ChartOptions Options { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    public class DataPoint
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public DateTime? Date { get; set; }
        public string? Category { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class DataSeries
    {
        public string Name { get; set; } = string.Empty;
        public List<decimal> Values { get; set; } = new();
        public string Color { get; set; } = string.Empty;
        public string Type { get; set; } = "line"; // line, bar, area, etc.
    }

    public class ChartOptions
    {
        public bool ShowLegend { get; set; } = true;
        public bool ShowGrid { get; set; } = true;
        public bool ShowTooltips { get; set; } = true;
        public string YAxisLabel { get; set; } = string.Empty;
        public string XAxisLabel { get; set; } = string.Empty;
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public bool Stacked { get; set; }
        public bool Responsive { get; set; } = true;
    }

    public class HeatmapData
    {
        public List<string> XLabels { get; set; } = new(); // e.g., days of week
        public List<string> YLabels { get; set; } = new(); // e.g., hours of day
        public List<List<decimal>> Values { get; set; } = new(); // 2D array of values
        public decimal MinValue { get; set; }
        public decimal MaxValue { get; set; }
    }

    public class GeographicData
    {
        public string Country { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public decimal RevenueAmount { get; set; }
        public double Percentage { get; set; }
    }
}