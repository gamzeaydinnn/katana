using System.Text.Json.Serialization;

namespace Katana.Core.DTOs;

public class ManufacturingOrderDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty; // NOT_STARTED, BLOCKED, IN_PROGRESS, DONE

    [JsonPropertyName("order_no")]
    public string OrderNo { get; set; } = string.Empty;

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("planned_quantity")]
    public decimal PlannedQuantity { get; set; }

    [JsonPropertyName("actual_quantity")]
    public decimal ActualQuantity { get; set; }

    [JsonPropertyName("batch_transactions")]
    public List<ManufacturingOrderBatchTransactionDto> BatchTransactions { get; set; } = new();

    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("order_created_date")]
    public DateTime? OrderCreatedDate { get; set; }

    [JsonPropertyName("done_date")]
    public DateTime? DoneDate { get; set; }

    [JsonPropertyName("production_deadline_date")]
    public DateTime? ProductionDeadlineDate { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("is_linked_to_sales_order")]
    public bool? IsLinkedToSalesOrder { get; set; }

    [JsonPropertyName("sales_order_id")]
    public long? SalesOrderId { get; set; }

    [JsonPropertyName("sales_order_row_id")]
    public long? SalesOrderRowId { get; set; }

    [JsonPropertyName("sales_order_delivery_deadline")]
    public DateTime? SalesOrderDeliveryDeadline { get; set; }

    [JsonPropertyName("ingredient_availability")]
    public string? IngredientAvailability { get; set; }

    [JsonPropertyName("total_cost")]
    public decimal? TotalCost { get; set; }

    [JsonPropertyName("total_planned_time")]
    public long? TotalPlannedTime { get; set; }

    [JsonPropertyName("total_actual_time")]
    public long? TotalActualTime { get; set; }

    [JsonPropertyName("material_cost")]
    public decimal? MaterialCost { get; set; }

    [JsonPropertyName("subassemblies_cost")]
    public decimal? SubassembliesCost { get; set; }

    [JsonPropertyName("operations_cost")]
    public decimal? OperationsCost { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}

public class ManufacturingOrderBatchTransactionDto
{
    [JsonPropertyName("batch_id")]
    public long BatchId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }
}

public class ManufacturingOrderCreateRequest
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "NOT_STARTED";

    [JsonPropertyName("order_no")]
    public string OrderNo { get; set; } = string.Empty;

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("planned_quantity")]
    public decimal PlannedQuantity { get; set; }

    [JsonPropertyName("actual_quantity")]
    public decimal? ActualQuantity { get; set; }

    [JsonPropertyName("order_created_date")]
    public DateTime? OrderCreatedDate { get; set; }

    [JsonPropertyName("production_deadline_date")]
    public DateTime? ProductionDeadlineDate { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("batch_transactions")]
    public List<ManufacturingOrderBatchTransactionDto>? BatchTransactions { get; set; }
}

public class ManufacturingOrderUpdateRequest
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("order_no")]
    public string? OrderNo { get; set; }

    [JsonPropertyName("variant_id")]
    public long? VariantId { get; set; }

    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    [JsonPropertyName("planned_quantity")]
    public decimal? PlannedQuantity { get; set; }

    [JsonPropertyName("actual_quantity")]
    public decimal? ActualQuantity { get; set; }

    [JsonPropertyName("order_created_date")]
    public DateTime? OrderCreatedDate { get; set; }

    [JsonPropertyName("production_deadline_date")]
    public DateTime? ProductionDeadlineDate { get; set; }

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; set; }

    [JsonPropertyName("done_date")]
    public DateTime? DoneDate { get; set; }

    [JsonPropertyName("batch_transactions")]
    public List<ManufacturingOrderBatchTransactionDto>? BatchTransactions { get; set; }
}

public class ManufacturingOrderListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("order_no")]
    public string? OrderNo { get; set; }

    [JsonPropertyName("location_id")]
    public long? LocationId { get; set; }

    [JsonPropertyName("is_linked_to_sales_order")]
    public bool? IsLinkedToSalesOrder { get; set; }

    [JsonPropertyName("include_deleted")]
    public bool? IncludeDeleted { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("created_at_min")]
    public DateTime? CreatedAtMin { get; set; }

    [JsonPropertyName("created_at_max")]
    public DateTime? CreatedAtMax { get; set; }

    [JsonPropertyName("updated_at_min")]
    public DateTime? UpdatedAtMin { get; set; }

    [JsonPropertyName("updated_at_max")]
    public DateTime? UpdatedAtMax { get; set; }
}

/// <summary>
/// Make-to-order üretim emri oluşturma isteği.
/// </summary>
public class ManufacturingOrderMakeToOrderRequest
{
    [JsonPropertyName("sales_order_row_id")]
    public long SalesOrderRowId { get; set; }

    [JsonPropertyName("create_subassemblies")]
    public bool CreateSubassemblies { get; set; }
}

/// <summary>
/// Üretim emrini satış siparişi satırından unlink etme isteği.
/// </summary>
public class ManufacturingOrderUnlinkRequest
{
    [JsonPropertyName("sales_order_row_id")]
    public long SalesOrderRowId { get; set; }
}

/// <summary>
/// Üretim emri üretim kaydı (manufacturing_order_production) DTO'su.
/// </summary>
public class ManufacturingOrderProductionDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("manufacturing_order_id")]
    public long ManufacturingOrderId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("production_date")]
    public DateTime? ProductionDate { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [JsonPropertyName("ingredients")]
    public List<ManufacturingOrderProductionIngredientDto>? Ingredients { get; set; }

    [JsonPropertyName("operations")]
    public List<ManufacturingOrderProductionOperationDto>? Operations { get; set; }
}

public class ManufacturingOrderProductionIngredientDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("manufacturing_order_id")]
    public long ManufacturingOrderId { get; set; }

    [JsonPropertyName("manufacturing_order_recipe_row_id")]
    public long ManufacturingOrderRecipeRowId { get; set; }

    [JsonPropertyName("production_id")]
    public long ProductionId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("production_date")]
    public DateTime? ProductionDate { get; set; }

    [JsonPropertyName("cost")]
    public decimal? Cost { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}

public class ManufacturingOrderProductionOperationDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("location_id")]
    public long LocationId { get; set; }

    [JsonPropertyName("manufacturing_order_id")]
    public long ManufacturingOrderId { get; set; }

    [JsonPropertyName("manufacturing_order_operation_id")]
    public long ManufacturingOrderOperationId { get; set; }

    [JsonPropertyName("production_id")]
    public long ProductionId { get; set; }

    [JsonPropertyName("time")]
    public long Time { get; set; }

    [JsonPropertyName("production_date")]
    public DateTime? ProductionDate { get; set; }

    [JsonPropertyName("cost")]
    public decimal? Cost { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}

public class ManufacturingOrderProductionListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("manufacturing_order_ids")]
    public List<long>? ManufacturingOrderIds { get; set; }

    [JsonPropertyName("include_deleted")]
    public bool? IncludeDeleted { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("created_at_min")]
    public DateTime? CreatedAtMin { get; set; }

    [JsonPropertyName("created_at_max")]
    public DateTime? CreatedAtMax { get; set; }

    [JsonPropertyName("updated_at_min")]
    public DateTime? UpdatedAtMin { get; set; }

    [JsonPropertyName("updated_at_max")]
    public DateTime? UpdatedAtMax { get; set; }
}

public class ManufacturingOrderProductionCreateRequest
{
    [JsonPropertyName("manufacturing_order_id")]
    public long ManufacturingOrderId { get; set; }

    [JsonPropertyName("completed_quantity")]
    public decimal CompletedQuantity { get; set; }

    [JsonPropertyName("completed_date")]
    public DateTime? CompletedDate { get; set; }

    [JsonPropertyName("is_final")]
    public bool? IsFinal { get; set; }

    [JsonPropertyName("ingredients")]
    public List<ManufacturingOrderProductionIngredientDto>? Ingredients { get; set; }

    [JsonPropertyName("operations")]
    public List<ManufacturingOrderProductionOperationDto>? Operations { get; set; }

    [JsonPropertyName("serial_numbers")]
    public List<long>? SerialNumbers { get; set; }
}

public class ManufacturingOrderProductionUpdateRequest
{
    [JsonPropertyName("production_date")]
    public DateTime? ProductionDate { get; set; }
}

/// <summary>
/// Üretim emri production ingredient update isteği.
/// </summary>
public class ManufacturingOrderProductionIngredientUpdateRequest
{
    [JsonPropertyName("batch_transactions")]
    public List<ManufacturingOrderBatchTransactionDto>? BatchTransactions { get; set; }
}

public class ManufacturingOrderOperationRowDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty; // NOT_STARTED, BLOCKED, IN_PROGRESS, PAUSED, COMPLETED

    [JsonPropertyName("rank")]
    public int? Rank { get; set; }

    [JsonPropertyName("manufacturing_order_id")]
    public long ManufacturingOrderId { get; set; }

    [JsonPropertyName("operation_id")]
    public long? OperationId { get; set; }

    [JsonPropertyName("operation_name")]
    public string? OperationName { get; set; }

    [JsonPropertyName("resource_id")]
    public long? ResourceId { get; set; }

    [JsonPropertyName("resource_name")]
    public string? ResourceName { get; set; }

    [JsonPropertyName("assigned_operators")]
    public List<ManufacturingOrderOperatorDto>? AssignedOperators { get; set; }

    [JsonPropertyName("completed_by_operators")]
    public List<ManufacturingOrderOperatorDto>? CompletedByOperators { get; set; }

    [JsonPropertyName("active_operator_id")]
    public long? ActiveOperatorId { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; } // process, setup, perUnit, fixed

    [JsonPropertyName("planned_time_parameter")]
    public long? PlannedTimeParameter { get; set; }

    [JsonPropertyName("total_actual_time")]
    public long? TotalActualTime { get; set; }

    [JsonPropertyName("planned_cost_per_unit")]
    public decimal? PlannedCostPerUnit { get; set; }

    [JsonPropertyName("total_actual_cost")]
    public decimal? TotalActualCost { get; set; }

    [JsonPropertyName("cost_parameter")]
    public decimal? CostParameter { get; set; }

    [JsonPropertyName("group_boundary")]
    public int? GroupBoundary { get; set; }

    [JsonPropertyName("is_status_actionable")]
    public bool? IsStatusActionable { get; set; }

    [JsonPropertyName("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}

public class ManufacturingOrderOperatorDto
{
    [JsonPropertyName("operator_id")]
    public long OperatorId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}

public class ManufacturingOrderOperationRowCreateRequest
{
    [JsonPropertyName("manufacturing_order_id")]
    public long ManufacturingOrderId { get; set; }

    [JsonPropertyName("operation_id")]
    public long? OperationId { get; set; }

    [JsonPropertyName("operation_name")]
    public string? OperationName { get; set; }

    [JsonPropertyName("resource_id")]
    public long? ResourceId { get; set; }

    [JsonPropertyName("resource_name")]
    public string? ResourceName { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; } // process, setup, perUnit, fixed

    [JsonPropertyName("planned_time_parameter")]
    public long? PlannedTimeParameter { get; set; }

    [JsonPropertyName("cost_parameter")]
    public decimal? CostParameter { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "NOT_STARTED";

    [JsonPropertyName("assigned_operators")]
    public List<ManufacturingOrderOperatorDto>? AssignedOperators { get; set; }
}

public class ManufacturingOrderOperationRowListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("manufacturing_order_id")]
    public long? ManufacturingOrderId { get; set; }

    [JsonPropertyName("include_deleted")]
    public bool? IncludeDeleted { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("created_at_min")]
    public DateTime? CreatedAtMin { get; set; }

    [JsonPropertyName("created_at_max")]
    public DateTime? CreatedAtMax { get; set; }

    [JsonPropertyName("updated_at_min")]
    public DateTime? UpdatedAtMin { get; set; }

    [JsonPropertyName("updated_at_max")]
    public DateTime? UpdatedAtMax { get; set; }
}

/// <summary>
/// Üretim emri operasyon satırı güncelleme isteği.
/// </summary>
public class ManufacturingOrderOperationRowUpdateRequest
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("rank")]
    public int? Rank { get; set; }

    [JsonPropertyName("operation_id")]
    public long? OperationId { get; set; }

    [JsonPropertyName("operation_name")]
    public string? OperationName { get; set; }

    [JsonPropertyName("resource_id")]
    public long? ResourceId { get; set; }

    [JsonPropertyName("resource_name")]
    public string? ResourceName { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("planned_time_parameter")]
    public long? PlannedTimeParameter { get; set; }

    [JsonPropertyName("total_actual_time")]
    public long? TotalActualTime { get; set; }

    [JsonPropertyName("planned_cost_per_unit")]
    public decimal? PlannedCostPerUnit { get; set; }

    [JsonPropertyName("total_actual_cost")]
    public decimal? TotalActualCost { get; set; }

    [JsonPropertyName("cost_parameter")]
    public decimal? CostParameter { get; set; }

    [JsonPropertyName("group_boundary")]
    public int? GroupBoundary { get; set; }

    [JsonPropertyName("assigned_operators")]
    public List<ManufacturingOrderOperatorDto>? AssignedOperators { get; set; }

    [JsonPropertyName("completed_by_operators")]
    public List<ManufacturingOrderOperatorDto>? CompletedByOperators { get; set; }

    [JsonPropertyName("active_operator_id")]
    public long? ActiveOperatorId { get; set; }

    [JsonPropertyName("completed_at")]
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Üretim emri recipe satırı DTO'su.
/// </summary>
public class ManufacturingOrderRecipeRowDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("manufacturing_order_id")]
    public long ManufacturingOrderId { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("planned_quantity_per_unit")]
    public decimal PlannedQuantityPerUnit { get; set; }

    [JsonPropertyName("total_actual_quantity")]
    public decimal? TotalActualQuantity { get; set; }

    [JsonPropertyName("ingredient_availability")]
    public string? IngredientAvailability { get; set; }

    [JsonPropertyName("ingredient_expected_date")]
    public DateTime? IngredientExpectedDate { get; set; }

    [JsonPropertyName("batch_transactions")]
    public List<ManufacturingOrderBatchTransactionDto>? BatchTransactions { get; set; }

    [JsonPropertyName("cost")]
    public decimal? Cost { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}

public class ManufacturingOrderRecipeRowCreateRequest
{
    [JsonPropertyName("manufacturing_order_id")]
    public long ManufacturingOrderId { get; set; }

    [JsonPropertyName("variant_id")]
    public long VariantId { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("planned_quantity_per_unit")]
    public decimal PlannedQuantityPerUnit { get; set; }

    [JsonPropertyName("total_actual_quantity")]
    public decimal? TotalActualQuantity { get; set; }

    [JsonPropertyName("batch_transactions")]
    public List<ManufacturingOrderBatchTransactionDto>? BatchTransactions { get; set; }
}

public class ManufacturingOrderRecipeRowUpdateRequest
{
    [JsonPropertyName("variant_id")]
    public long? VariantId { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("planned_quantity_per_unit")]
    public decimal? PlannedQuantityPerUnit { get; set; }

    [JsonPropertyName("total_actual_quantity")]
    public decimal? TotalActualQuantity { get; set; }

    [JsonPropertyName("batch_transactions")]
    public List<ManufacturingOrderBatchTransactionDto>? BatchTransactions { get; set; }
}

public class ManufacturingOrderRecipeRowListQuery
{
    [JsonPropertyName("ids")]
    public List<long>? Ids { get; set; }

    [JsonPropertyName("manufacturing_order_id")]
    public long? ManufacturingOrderId { get; set; }

    [JsonPropertyName("variant_id")]
    public long? VariantId { get; set; }

    [JsonPropertyName("ingredient_availability")]
    public string? IngredientAvailability { get; set; }

    [JsonPropertyName("include_deleted")]
    public bool? IncludeDeleted { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("created_at_min")]
    public DateTime? CreatedAtMin { get; set; }

    [JsonPropertyName("created_at_max")]
    public DateTime? CreatedAtMax { get; set; }

    [JsonPropertyName("updated_at_min")]
    public DateTime? UpdatedAtMin { get; set; }

    [JsonPropertyName("updated_at_max")]
    public DateTime? UpdatedAtMax { get; set; }
}
