using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPropertyManager.Data;
using RentalPropertyManager.Models;
using RentalPropertyManager.ViewModels;

namespace RentalPropertyManager.Controllers
{
    [Route("[controller]")]
    [Route("api/[controller]")]
    [Authorize(Roles = "PropertyManager")]
    public class PropertiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PropertiesController> _logger;

        public PropertiesController(ApplicationDbContext context, ILogger<PropertiesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// GET /properties - Display all properties (View)
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            return View("Index");
        }

        /// <summary>
        /// GET /properties/list - Get all properties as JSON
        /// </summary>
        [HttpGet("list")]
        public async Task<IActionResult> GetList()
        {
            try
            {
                var properties = await _context.Properties
                    .AsNoTracking()
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                return Ok(properties);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching properties: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching properties" });
            }
        }

        /// <summary>
        /// GET /properties/createmodal - Return create modal partial view
        /// </summary>
        [HttpGet("createmodal")]
        public IActionResult CreateModal()
        {
            var viewModel = new PropertyFormViewModel();
            return PartialView("_PropertyModal", viewModel);
        }

        /// <summary>
        /// POST /properties/create - Create a new property (AJAX form submission)
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] PropertyFormViewModel viewModel)
        {
            try
            {
                _logger.LogInformation($"Create property called with: Name={viewModel?.Name}, StreetAddress={viewModel?.StreetAddress}, City={viewModel?.City}, State={viewModel?.State}, ZipCode={viewModel?.ZipCode}");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                    _logger.LogWarning($"Create validation failed with errors: {string.Join("; ", errors)}");
                    return BadRequest(new { success = false, errors = errors });
                }

                var property = new Property
                {
                    Name = viewModel.Name,
                    StreetAddress = viewModel.StreetAddress,
                    City = viewModel.City,
                    State = viewModel.State,
                    ZipCode = viewModel.ZipCode,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Properties.Add(property);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Property created successfully with ID: {property.Id}");

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating property: {ex.Message}");
                return BadRequest(new { success = false, error = "Error creating property. Please try again." });
            }
        }

        /// <summary>
        /// GET /properties/{id}/editmodal - Return edit modal partial view
        /// </summary>
        [HttpGet("{id}/editmodal")]
        public async Task<IActionResult> EditModal(int id)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property == null)
            {
                return NotFound();
            }

            var viewModel = new PropertyFormViewModel
            {
                Id = property.Id,
                Name = property.Name,
                StreetAddress = property.StreetAddress,
                City = property.City,
                State = property.State,
                ZipCode = property.ZipCode
            };

            return PartialView("_PropertyModal", viewModel);
        }

        /// <summary>
        /// POST /properties/{id}/edit - Update an existing property (AJAX form submission)
        /// </summary>
        [HttpPost("{id}/edit")]
        public async Task<IActionResult> Edit(int id, [FromBody] PropertyFormViewModel viewModel)
        {
            try
            {
                _logger.LogInformation($"Edit property called with ID:{id}, viewModel.Id={viewModel?.Id}, Name={viewModel?.Name}");

                if (id != viewModel.Id)
                {
                    _logger.LogWarning($"Edit ID mismatch: URL id={id}, viewModel.Id={viewModel?.Id}");
                    return BadRequest(new { success = false, error = "Property ID mismatch" });
                }

                var property = await _context.Properties.FindAsync(id);
                if (property == null)
                {
                    _logger.LogWarning($"Edit property not found with ID: {id}");
                    return NotFound();
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                    _logger.LogWarning($"Edit validation failed for ID {id} with errors: {string.Join("; ", errors)}");
                    return BadRequest(new { success = false, errors = errors });
                }

                property.Name = viewModel.Name;
                property.StreetAddress = viewModel.StreetAddress;
                property.City = viewModel.City;
                property.State = viewModel.State;
                property.ZipCode = viewModel.ZipCode;

                _context.Properties.Update(property);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Property updated successfully with ID: {id}");

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating property: {ex.Message}");
                return BadRequest(new { success = false, error = "Error updating property. Please try again." });
            }
        }

        /// <summary>
        /// POST /properties/{id}/delete - Delete a property
        /// </summary>
        [HttpPost("{id}/delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var property = await _context.Properties.FindAsync(id);
                if (property == null)
                {
                    return NotFound();
                }

                // Check if property has units
                var unitCount = await _context.Units.CountAsync(u => u.PropertyID == id);
                if (unitCount > 0)
                {
                    return BadRequest(new { success = false, error = "Cannot delete property with existing units" });
                }

                _context.Properties.Remove(property);
                await _context.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting property: {ex.Message}");
                return StatusCode(500, new { success = false, error = "Error deleting property" });
            }
        }
    }
}
