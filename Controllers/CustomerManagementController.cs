using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/customer-management")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN,Admin")]
    public class CustomerManagementController : ControllerBase
    {
        private readonly ICustomerManagementService _customerService;
        private readonly ILogger<CustomerManagementController> _logger;

        public CustomerManagementController(ICustomerManagementService customerService, ILogger<CustomerManagementController> logger)
        {
            _customerService = customerService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous] // Remove later when testing is done
        public async Task<IActionResult> GetAllCustomers()
        {
            try
            {
                var customers = await _customerService.GetAllCustomersAsync();
                return Ok(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get customers");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPatch("{id}/status")]
        [AllowAnonymous]
        public async Task<IActionResult> ToggleStatus(int id, [FromBody] CustomerStatusUpdateRequestDto request)
        {
            try
            {
                var success = await _customerService.ToggleCustomerStatusAsync(id, request.Status);
                if (!success) return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle status");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            try
            {
                var success = await _customerService.DeleteCustomerAsync(id);
                if (!success) return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete customer");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
