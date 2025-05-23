using Grpc.Core;
using GrpcPlayground.Infrastructure;
using Metrics;

namespace GrpcPlayground.Services
{
    public sealed class SystemMetricsGrpcService
    : SystemMetrics.SystemMetricsBase
    {
        private readonly MetricsSampler _sampler;
        private readonly ILogger<SystemMetricsGrpcService> _log;

        public SystemMetricsGrpcService(MetricsSampler sampler,
                                        ILogger<SystemMetricsGrpcService> log)
        {
            _sampler = sampler;
            _log = log;
        }

        public override async Task StreamMetrics(
            Google.Protobuf.WellKnownTypes.Empty request,
            IServerStreamWriter<MetricsReply> responseStream,
            ServerCallContext context)
        {
            await foreach (var msg in _sampler.Stream(context.CancellationToken))
            {
                await responseStream.WriteAsync(msg);
            }

            _log.LogInformation("SystemMetrics stream closed");
        }
    }
}
