using System;
using System.Collections.Generic;
using Katana.Business.Enums;

namespace Katana.Business.Mappers
{
    /// <summary>
    /// Koza/Luca belge türü eşleştirmeleri (fatura/irsaliye).
    /// </summary>
    public static class DocumentTypeMapper
    {
        private static readonly Dictionary<InvoiceType, (int BelgeTurId, int BelgeTurDetayId)> InvoiceMapping = new()
        {
            { InvoiceType.AlimFaturasi, (16, 69) },
            { InvoiceType.SatisFaturasi, (18, 76) },
            { InvoiceType.SatisIade, (17, 72) },
            { InvoiceType.AlimIade, (19, 79) },
            { InvoiceType.ProformaAlim, (16, 94) },
            { InvoiceType.ProformaSatis, (18, 108) },
            { InvoiceType.KurFarkiAlim, (16, 93) },
            { InvoiceType.KurFarkiSatis, (18, 107) }
        };

        private static readonly Dictionary<WaybillType, (int BelgeTurId, int BelgeTurDetayId)> WaybillMapping = new()
        {
            { WaybillType.AlimIrsaliyesi, (1, 1) },
            { WaybillType.SatisIrsaliyesi, (2, 52) },
            { WaybillType.AlimIade, (3, 9) },
            { WaybillType.SatisIade, (4, 25) },
            { WaybillType.DahiliSevk, (2, 178) }
        };

        public static (int BelgeTurId, int BelgeTurDetayId) GetInvoiceTypeIds(InvoiceType type)
        {
            if (InvoiceMapping.TryGetValue(type, out var val)) return val;
            throw new ArgumentOutOfRangeException(nameof(type), $"InvoiceType mapping not found for {type}");
        }

        public static (int BelgeTurId, int BelgeTurDetayId) GetWaybillTypeIds(WaybillType type)
        {
            if (WaybillMapping.TryGetValue(type, out var val)) return val;
            throw new ArgumentOutOfRangeException(nameof(type), $"WaybillType mapping not found for {type}");
        }
    }
}
