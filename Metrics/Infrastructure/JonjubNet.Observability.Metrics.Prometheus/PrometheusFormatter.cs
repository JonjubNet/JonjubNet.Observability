using System.Linq;
using JonjubNet.Observability.Metrics.Core;
using JonjubNet.Observability.Metrics.Core.Interfaces;
using JonjubNet.Observability.Metrics.Core.MetricTypes;

namespace JonjubNet.Observability.Metrics.Prometheus
{
    /// <summary>
    /// Formateador de métricas en formato Prometheus
    /// </summary>
    public class PrometheusFormatter : IMetricFormatter
    {
        public string Format => "Prometheus";

        public string FormatMetrics(IReadOnlyList<MetricPoint> points)
        {
            var output = new System.Text.StringBuilder();

            foreach (var point in points)
            {
                output.Append(FormatMetricPoint(point));
                output.Append('\n'); // Usar \n explícito para compatibilidad con Prometheus
            }

            return output.ToString();
        }

        /// <summary>
        /// Formatea métricas desde el registry
        /// Usa \n explícito para compatibilidad con Prometheus (evita \r\n de Windows)
        /// </summary>
        public string FormatRegistry(MetricRegistry registry)
        {
            var output = new System.Text.StringBuilder();

            // Formatear contadores
            foreach (var counter in registry.GetAllCounters().Values)
            {
                var allValues = counter.GetAllValues();
                
                // Detectar si hay valores con tags
                bool hasTaggedValues = allValues.Keys.Any(k => !string.IsNullOrEmpty(k));
                bool hasUntaggedValue = allValues.ContainsKey(string.Empty);
                
                // Si hay valores con tags Y sin tags, ignorar el valor sin tags (Prometheus no permite mezclar)
                if (hasTaggedValues && hasUntaggedValue)
                {
                    // Filtrar el valor sin tags
                    allValues = allValues.Where(kvp => !string.IsNullOrEmpty(kvp.Key))
                                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                }

                if (allValues.Count == 0)
                    continue;

                output.Append($"# HELP {counter.Name} {counter.Description}\n");
                output.Append($"# TYPE {counter.Name} counter\n");

                foreach (var (key, value) in allValues)
                {
                    var labels = FormatLabels(ParseKey(key));
                    output.Append($"{counter.Name}{labels} {value}\n");
                }
            }

            // Formatear gauges
            foreach (var gauge in registry.GetAllGauges().Values)
            {
                var allValues = gauge.GetAllValues();
                
                // Detectar si hay valores con tags
                bool hasTaggedValues = allValues.Keys.Any(k => !string.IsNullOrEmpty(k));
                bool hasUntaggedValue = allValues.ContainsKey(string.Empty);
                
                // Si hay valores con tags Y sin tags, ignorar el valor sin tags
                if (hasTaggedValues && hasUntaggedValue)
                {
                    allValues = allValues.Where(kvp => !string.IsNullOrEmpty(kvp.Key))
                                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                }

                if (allValues.Count == 0)
                    continue;

                output.Append($"# HELP {gauge.Name} {gauge.Description}\n");
                output.Append($"# TYPE {gauge.Name} gauge\n");

                foreach (var (key, value) in allValues)
                {
                    var labels = FormatLabels(ParseKey(key));
                    output.Append($"{gauge.Name}{labels} {value}\n");
                }
            }

            // Formatear histogramas
            foreach (var histogram in registry.GetAllHistograms().Values)
            {
                var allData = histogram.GetAllData();
                
                if (allData.Count == 0)
                    continue;

                output.Append($"# HELP {histogram.Name} {histogram.Description}\n");
                output.Append($"# TYPE {histogram.Name} histogram\n");

                foreach (var (key, data) in allData)
                {
                    var labels = ParseKey(key);
                    foreach (var bucket in histogram.Buckets)
                    {
                        var bucketLabels = new Dictionary<string, string>(labels) { ["le"] = bucket.ToString() };
                        var count = data.BucketCounts[Array.IndexOf(histogram.Buckets, bucket)];
                        output.Append($"{histogram.Name}_bucket{FormatLabels(bucketLabels)} {count}\n");
                    }
                    output.Append($"{histogram.Name}_sum{FormatLabels(labels)} {data.Sum}\n");
                    output.Append($"{histogram.Name}_count{FormatLabels(labels)} {data.Count}\n");
                }
            }

            // Formatear summaries
            foreach (var summary in registry.GetAllSummaries().Values)
            {
                var allData = summary.GetAllData();
                
                if (allData.Count == 0)
                    continue;

                output.Append($"# HELP {summary.Name} {summary.Description}\n");
                output.Append($"# TYPE {summary.Name} summary\n");

                foreach (var (key, data) in allData)
                {
                    var labels = ParseKey(key);
                    var quantiles = data.GetQuantiles();
                    foreach (var quantile in quantiles)
                    {
                        var quantileLabels = new Dictionary<string, string>(labels) { ["quantile"] = quantile.Key.ToString() };
                        output.Append($"{summary.Name}{FormatLabels(quantileLabels)} {quantile.Value}\n");
                    }
                    output.Append($"{summary.Name}_sum{FormatLabels(labels)} {data.Sum}\n");
                    output.Append($"{summary.Name}_count{FormatLabels(labels)} {data.Count}\n");
                }
            }

            return output.ToString();
        }

        private string FormatMetricPoint(MetricPoint point)
        {
            var labels = FormatLabels(point.Tags);
            return $"{point.Name}{labels} {point.Value}";
        }

        private string FormatLabels(Dictionary<string, string>? labels)
        {
            if (labels == null || labels.Count == 0)
                return string.Empty;

            var labelStrings = labels.Select(kvp => $"{kvp.Key}=\"{kvp.Value}\"");
            return "{" + string.Join(",", labelStrings) + "}";
        }

        private Dictionary<string, string> ParseKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return new Dictionary<string, string>();

            var result = new Dictionary<string, string>();
            var pairs = key.Split(',');
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=');
                if (parts.Length == 2)
                {
                    result[parts[0]] = parts[1];
                }
            }
            return result;
        }
    }
}

