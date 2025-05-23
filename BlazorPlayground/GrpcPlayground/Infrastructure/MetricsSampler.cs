using Metrics;
using System.Diagnostics;
using System.Threading.Channels;

namespace GrpcPlayground.Infrastructure
{
    public sealed class MetricsSampler : IAsyncDisposable
    {
        private readonly Timer _timer;
        private readonly Channel<MetricsReply> _channel = Channel.CreateUnbounded<MetricsReply>();
        private long _lastProcTicks;
        private DateTime _lastSampleUtc;

        public MetricsSampler()
        {
            var p = Process.GetCurrentProcess();
            _lastProcTicks = p.TotalProcessorTime.Ticks;
            _lastSampleUtc = DateTime.UtcNow;

            _timer = new Timer(_ =>
            {
                var msg = Sample();
                _channel.Writer.TryWrite(msg);
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
        }

        private MetricsReply Sample()
        {
            var p = Process.GetCurrentProcess();
            var now = DateTime.UtcNow;

            var deltaTicks = p.TotalProcessorTime.Ticks - _lastProcTicks;
            var deltaMs = (now - _lastSampleUtc).TotalMilliseconds;
            var cpu = deltaTicks / (10_000 * deltaMs * Environment.ProcessorCount);

            _lastProcTicks = p.TotalProcessorTime.Ticks;
            _lastSampleUtc = now;

            return new MetricsReply
            {
                CpuPercent = cpu,
                WorkingSetMb = p.WorkingSet64 / 1024d / 1024d,
                UnixTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        public IAsyncEnumerable<MetricsReply> Stream(CancellationToken ct)
            => _channel.Reader.ReadAllAsync(ct);

        public async ValueTask DisposeAsync()
        {
            _timer.Dispose();
            _channel.Writer.Complete();
            await _channel.Reader.Completion;
        }
    }
}
