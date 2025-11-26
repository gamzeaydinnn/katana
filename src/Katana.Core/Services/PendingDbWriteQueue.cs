using System.Collections.Concurrent;

namespace Katana.Core.Services
{
    
    public class PendingAuditInfo
    {
        public string ActionType { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string PerformedBy { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Details { get; set; }
        public string? Changes { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    public class PendingDbWriteQueue
    {
        private readonly ConcurrentQueue<PendingAuditInfo> _auditQueue = new ConcurrentQueue<PendingAuditInfo>();

        public void EnqueueAudit(PendingAuditInfo audit)
        {
            if (audit == null) return;
            _auditQueue.Enqueue(audit);
        }

        public bool TryDequeue(out PendingAuditInfo? audit)
        {
            if (_auditQueue.TryDequeue(out var item))
            {
                audit = item;
                return true;
            }
            audit = null;
            return false;
        }

        public int Count => _auditQueue.Count;
    }
}
