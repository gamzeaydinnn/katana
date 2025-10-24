using System.Collections.Concurrent;
using Katana.Data.Models;

namespace Katana.Infrastructure.Services
{
    // Simple in-memory queue to hold audit/error entries when DB is unavailable
    public class PendingDbWriteQueue
    {
        private readonly ConcurrentQueue<AuditLog> _auditQueue = new ConcurrentQueue<AuditLog>();

        public void EnqueueAudit(AuditLog audit)
        {
            if (audit == null) return;
            _auditQueue.Enqueue(audit);
        }

        public bool TryDequeue(out AuditLog? audit)
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
