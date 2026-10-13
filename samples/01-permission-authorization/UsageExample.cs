// SAMPLE 1 — usage (excerpt from InventoryController)
// Source: Cafi Manager (private repository). Excerpt published for portfolio review.
// 
// One endpoint serves two logical roles: the inventory manager and a staff-consumption operator.
// ---------------------------------------------------------------------------

    [HttpPost("staff-consumption/product")]
    [RequireAnyPermission(PermissionCodes.InventoryManage, PermissionCodes.StaffConsumptionManage)]
    public async Task<IActionResult> StaffProductConsumption(
        [FromBody] StaffProductConsumptionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await inventoryService.ApplyStaffProductConsumptionAsync(userId, request, cancellationToken);
        return Ok(ApiResponse<StaffProductConsumptionResultDto>.Ok(result));
    }

    // ...

    [HttpGet("staff-consumption/today")]
    [RequireAnyPermission(PermissionCodes.InventoryView, PermissionCodes.StaffConsumptionView, PermissionCodes.StaffConsumptionManage)]
