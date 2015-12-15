using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AzureAlerting.Models
{
    public class AlertModel
    {
        public string status { get; set; }
        public Context context { get; set; }
        public Properties properties { get; set; }
    }

    public class Condition
    {
        public string metricName { get; set; }
        public string metricUnit { get; set; }
        public string metricValue { get; set; }
        public string threshold { get; set; }
        public string windowSize { get; set; }
        public string timeAggregation { get; set; }
        public string @operator { get; set; }
    }

    public class Context
    {
        public string timestamp { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string conditionType { get; set; }
        public Condition condition { get; set; }
        public string subscriptionId { get; set; }
        public string resourceGroupName { get; set; }
        public string resourceName { get; set; }
        public string resourceType { get; set; }
        public string resourceId { get; set; }
        public string resourceRegion { get; set; }
        public string portalLink { get; set; }
    }

    public class Properties
    {
        public string key1 { get; set; }
        public string key2 { get; set; }
    }

    public class Update
    {
        [Required]
        [MaxLength(140)]
        public string Status { get; set; }
        public DateTime Date { get; set; }
    }
}