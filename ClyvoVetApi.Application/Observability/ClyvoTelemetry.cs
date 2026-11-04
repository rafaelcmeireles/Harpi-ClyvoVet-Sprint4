using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ClyvoVetApi.Application.Observability
{
    /// <summary>
    /// Ponto central de instrumentação customizada da aplicação. Expõe:
    /// - um <see cref="ActivitySource"/> usado para criar spans na camada de Aplicação
    ///   (permitindo rastrear uma requisição enquanto ela atravessa Controller → Application → Infrastructure),
    /// - um <see cref="Meter"/> com métricas de negócio (violações de regra, duração das operações),
    ///   complementares às métricas automáticas de ASP.NET Core.
    /// </summary>
    public static class ClyvoTelemetry
    {
        public const string ServiceName = "ClyvoVetApi";
        public const string ActivitySourceName = "ClyvoVetApi.Application";
        public const string MeterName = "ClyvoVetApi";

        public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
        public static readonly Meter Meter = new(MeterName);

        public static readonly Counter<long> BusinessRuleViolations = Meter.CreateCounter<long>(
            "clyvovet.business_rule.violations",
            description: "Número de vezes que uma regra de negócio foi violada, por operação.");

        public static readonly Histogram<double> OperationDuration = Meter.CreateHistogram<double>(
            "clyvovet.application.operation.duration",
            unit: "ms",
            description: "Duração das operações da camada de Aplicação (casos de uso).");

        /// <summary>
        /// Inicia um span + cronômetro para uma operação da camada de Aplicação.
        /// Use dentro de um bloco <c>using</c> no início do método do serviço.
        /// </summary>
        public static OperationScope StartOperation(string operationName)
        {
            var activity = ActivitySource.StartActivity(operationName, ActivityKind.Internal);
            return new OperationScope(operationName, activity);
        }

        public sealed class OperationScope : IDisposable
        {
            private readonly string _operationName;
            private readonly Activity? _activity;
            private readonly Stopwatch _stopwatch;
            private bool _failed;

            internal OperationScope(string operationName, Activity? activity)
            {
                _operationName = operationName;
                _activity = activity;
                _stopwatch = Stopwatch.StartNew();
            }

            /// <summary>
            /// Marca a operação atual como uma violação de regra de negócio, incrementando
            /// o contador correspondente e anotando o span com o motivo.
            /// </summary>
            public void MarkAsBusinessRuleViolation(string reason)
            {
                _failed = true;
                _activity?.SetTag("clyvovet.business_rule_violation", reason);
                BusinessRuleViolations.Add(1, new KeyValuePair<string, object?>("operation", _operationName));
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                OperationDuration.Record(
                    _stopwatch.Elapsed.TotalMilliseconds,
                    new KeyValuePair<string, object?>("operation", _operationName),
                    new KeyValuePair<string, object?>("failed", _failed));
                _activity?.Dispose();
            }
        }
    }
}
