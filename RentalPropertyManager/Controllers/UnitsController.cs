using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RentalPropertyManager.Data;
using RentalPropertyManager.Models;
using RentalPropertyManager.ViewModels;

namespace RentalPropertyManager.Controllers
{
    [Route("[controller]")]
    public class UnitsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UnitsController> _logger;

        public UnitsController(ApplicationDbContext context, ILogger<UnitsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// GET /units - Display units management page (PropertyManager only)
        /// </summary>
        [Authorize(Roles = "PropertyManager")]
        [HttpGet]
        public IActionResult Index()
        {
            return View("Index");
        }

        /// <summary>
        /// GET /units/browse - Browse available units view (Public)
        /// </summary>
        [HttpGet("browse")]
        [AllowAnonymous]
        public IActionResult Browse()
        {
            return View("Browse");
        }

        /// <summary>
        /// GET /units/list - Get all available units as JSON
        /// </summary>
        [HttpGet("list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetList()
        {
            try
            {
                var units = await _context.Units
                    .Include(u => u.Property)
                    .Include(u => u.UnitType)
                    .AsNoTracking()
                    .Select(u => new UnitBrowseViewModel
                    {
                        UnitID = u.Id,
                        PropertyName = u.Property.Name,
                        UnitNumber = u.UnitNumber,
                        Bedrooms = u.Bedrooms,
                        MonthlyRent = u.MonthlyRent,
                        UnitTypeName = u.UnitType.UnitTypeName,
                        IsAvailable = true
                    })
                    .ToListAsync();

                return Ok(units);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error browsing units: {ex.Message}");
                return StatusCode(500, new { message = "Error browsing units" });
            }
        }

        /// <summary>
        /// GET /units/createmodal - Return create modal partial view (PropertyManager only)
        /// </summary>
        [Authorize(Roles = "PropertyManager")]
        [HttpGet("createmodal")]
        public async Task<IActionResult> CreateModal()
        {
            try
            {
                var viewModel = new UnitFormViewModel();
                await PopulateUnitTypeDropdown(viewModel);
                await PopulatePropertyDropdown(viewModel);
                return PartialView("_UnitModal", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading create modal: {ex.Message}");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// POST /units/create - Create a new unit (AJAX form submission, PropertyManager only)
        /// </summary>
        [Authorize(Roles = "PropertyManager")]
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] UnitFormViewModel viewModel)
        {
            try
            {
                _logger.LogInformation($"Create unit called with PropertyID: {viewModel.PropertyID}, UnitNumber: {viewModel.UnitNumber}");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning($"Model validation failed: {string.Join(", ", errors)}");
                    await PopulateUnitTypeDropdown(viewModel);
                    await PopulatePropertyDropdown(viewModel);
                    return BadRequest(new { success = false, errors = errors });
                }

                // Validate UnitType is active for new units
                var unitType = await _context.UnitTypes.FindAsync(viewModel.UnitTypeID);
                if (unitType == null || !unitType.ActiveBool)
                {
                    await PopulateUnitTypeDropdown(viewModel);
                    await PopulatePropertyDropdown(viewModel);
                    return BadRequest(new { success = false, error = "Selected unit type is inactive and cannot be assigned." });
                }

                // Validate Property exists
                var property = await _context.Properties.FindAsync(viewModel.PropertyID);
                if (property == null)
                {
                    await PopulateUnitTypeDropdown(viewModel);
                    await PopulatePropertyDropdown(viewModel);
                    return BadRequest(new { success = false, error = "Selected property does not exist." });
                }

                var unit = new Unit
                {
                    PropertyID = viewModel.PropertyID,
                    UnitNumber = viewModel.UnitNumber,
                    Bedrooms = viewModel.Bedrooms,
                    MonthlyRent = viewModel.MonthlyRent,
                    UnitTypeID = viewModel.UnitTypeID
                };

                _context.Units.Add(unit);
                await _context.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating unit: {ex.Message}");
                await PopulateUnitTypeDropdown(viewModel);
                await PopulatePropertyDropdown(viewModel);
                return BadRequest(new { success = false, error = "Error creating unit. Please try again." });
            }
        }

        /// <summary>
        /// GET /units/{id}/editmodal - Return edit modal partial view (PropertyManager only)
        /// </summary>
        [Authorize(Roles = "PropertyManager")]
        [HttpGet("{id}/editmodal")]
        public async Task<IActionResult> EditModal(int id)
        {
            try
            {
                var unit = await _context.Units
                    .Include(u => u.Property)
                    .Include(u => u.UnitType)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (unit == null)
                {
                    return NotFound();
                }

                var viewModel = new UnitFormViewModel
                {
                    Id = unit.Id,
                    PropertyID = unit.PropertyID,
                    PropertyName = unit.Property?.Name ?? string.Empty,
                    UnitNumber = unit.UnitNumber,
                    Bedrooms = unit.Bedrooms,
                    MonthlyRent = unit.MonthlyRent,
                    UnitTypeID = unit.UnitTypeID
                };

                await PopulateUnitTypeDropdown(viewModel, unit.UnitTypeID);
                await PopulatePropertyDropdown(viewModel);
                return PartialView("_UnitModal", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading edit modal: {ex.Message}");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// POST /units/{id}/edit - Update an existing unit (AJAX form submission, PropertyManager only)
        /// </summary>
        [Authorize(Roles = "PropertyManager")]
        [HttpPost("{id}/edit")]
        public async Task<IActionResult> Edit(int id, [FromBody] UnitFormViewModel viewModel)
        {
            try
            {
                _logger.LogInformation($"Edit unit {id} called with PropertyID: {viewModel.PropertyID}, UnitNumber: {viewModel.UnitNumber}");

                if (id != viewModel.Id)
                {
                    _logger.LogWarning($"Unit ID mismatch: URL id {id} != viewModel.Id {viewModel.Id}");
                    return BadRequest(new { success = false, error = "Unit ID mismatch" });
                }

                var unit = await _context.Units
                    .Include(u => u.UnitType)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (unit == null)
                {
                    _logger.LogWarning($"Unit with ID {id} not found");
                    return NotFound();
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning($"Model validation failed: {string.Join(", ", errors)}");
                    await PopulateUnitTypeDropdown(viewModel, unit.UnitTypeID);
                    await PopulatePropertyDropdown(viewModel);
                    return BadRequest(new { success = false, errors = errors });
                }

                // Validate UnitType is active OR is the unit's existing type
                var unitType = await _context.UnitTypes.FindAsync(viewModel.UnitTypeID);
                if (unitType == null || (!unitType.ActiveBool && unitType.Id != unit.UnitTypeID))
                {
                    await PopulateUnitTypeDropdown(viewModel, unit.UnitTypeID);
                    await PopulatePropertyDropdown(viewModel);
                    return BadRequest(new { success = false, error = "Selected unit type is inactive and cannot be assigned." });
                }

                unit.PropertyID = viewModel.PropertyID;
                unit.UnitNumber = viewModel.UnitNumber;
                unit.Bedrooms = viewModel.Bedrooms;
                unit.MonthlyRent = viewModel.MonthlyRent;
                unit.UnitTypeID = viewModel.UnitTypeID;

                _context.Units.Update(unit);
                await _context.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating unit: {ex.Message}");
                await PopulateUnitTypeDropdown(viewModel);
                await PopulatePropertyDropdown(viewModel);
                return BadRequest(new { success = false, error = "Error updating unit. Please try again." });
            }
        }

        /// <summary>
        /// POST /units/{id}/delete - Delete a unit (PropertyManager only)
        /// </summary>
        [Authorize(Roles = "PropertyManager")]
        [HttpPost("{id}/delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var unit = await _context.Units.FindAsync(id);
                if (unit == null)
                {
                    return NotFound();
                }

                _context.Units.Remove(unit);
                await _context.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting unit: {ex.Message}");
                return StatusCode(500, new { success = false, error = "Error deleting unit" });
            }
        }

        /// <summary>
        /// Helper method to populate the UnitType dropdown with active types
        /// For edit operations, also includes the current unit's type if inactive
        /// </summary>
        private async Task PopulateUnitTypeDropdown(UnitFormViewModel viewModel, int? currentUnitTypeId = null)
        {
            // Get all active unit types
            var activeUnitTypes = await _context.UnitTypes
                .Where(ut => ut.ActiveBool)
                .OrderBy(ut => ut.UnitTypeName)
                .ToListAsync();

            var selectItems = activeUnitTypes
                .Select(ut => new SelectListItem
                {
                    Value = ut.Id.ToString(),
                    Text = ut.UnitTypeName,
                    Selected = ut.Id == viewModel.UnitTypeID
                })
                .ToList();

            // If editing and current unit has an inactive type, add it to the list
            if (currentUnitTypeId.HasValue && currentUnitTypeId.Value != 0)
            {
                var currentUnitType = await _context.UnitTypes.FindAsync(currentUnitTypeId.Value);
                if (currentUnitType != null && !currentUnitType.ActiveBool)
                {
                    // Check if it's not already in the list
                    if (!selectItems.Any(si => si.Value == currentUnitTypeId.ToString()))
                    {
                        selectItems.Add(new SelectListItem
                        {
                            Value = currentUnitType.Id.ToString(),
                            Text = $"{currentUnitType.UnitTypeName} (Inactive)",
                            Selected = currentUnitType.Id == viewModel.UnitTypeID
                        });
                    }
                }
            }

            viewModel.AvailableUnitTypes = selectItems;
        }

        private async Task PopulatePropertyDropdown(UnitFormViewModel viewModel)
        {
            // Get all properties for the dropdown
            var properties = await _context.Properties
                .OrderBy(p => p.Name)
                .ToListAsync();

            var selectItems = properties
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name,
                    Selected = p.Id == viewModel.PropertyID
                })
                .ToList();

                         viewModel.AvailableProperties = selectItems;
                    }
                }
            }
