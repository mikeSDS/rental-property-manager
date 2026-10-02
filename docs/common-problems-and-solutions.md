# Common Problems and Solutions

## Problem 1: Modal Form Submission Handlers Not Attaching (innerHTML Script Execution)

### Symptoms
- Modal opens correctly, but clicking "Save"/"Submit" does nothing
- No network requests are sent when form is submitted
- Form submit handler never fires

### Root Cause
When modal HTML is injected into the DOM via `innerHTML`, any `<script>` tags embedded in that HTML are **intentionally not executed** by the browser (security feature). This means:
- `<script src="path/to/file.js"></script>` tags don't load
- Inline `<script>` blocks don't execute
- Form event listeners attached in the partial never get wired up

### Solution
Move the form submission logic out of the partial view's embedded scripts and into the **parent page's JavaScript file**, which loads normally via `<script src>` tag. After injecting the modal HTML:

1. **In parent page script** (e.g., `units-index.js`): Define the form handler function
2. **After innerHTML injection**, manually call `addEventListener` to wire up the form submit listener
3. **Do NOT rely on** embedded `<script>` tags in the partial to execute

**Example Fix:**
```javascript
// units-index.js (loaded by Index.cshtml via <script src>)
async function editUnit(id) {
	const response = await fetch(`/units/${id}/editmodal`);
	const html = await response.text();
	document.getElementById('unitModalContainer').innerHTML = html;

	// Manually wire up the submit handler AFTER injection
	const form = document.getElementById('unitForm');
	if (form) {
		form.removeEventListener('submit', handleUnitFormSubmit);
		form.addEventListener('submit', handleUnitFormSubmit);
	}

	const modal = new bootstrap.Modal(document.getElementById('unitModal'));
	modal.show();
}

async function handleUnitFormSubmit(e) {
	e.preventDefault();
	// Form submission logic here
}
```

**Remove from partial:**
- Delete `<script src="~/js/unit-modal.js"></script>` 
- Delete inline `<script>` block that calls `initializeUnitForm()`
- These are dead code since they never execute

---

## Problem 2: Database Seeding Not Reflecting on App Restart

### Symptoms
- Changed seeding code in `DbInitializer.cs`
- App still shows old data
- New unit types not appearing in dropdown

### Root Cause
The seeding logic includes idempotent checks like `if (!dbContext.UnitTypes.Any())`. Once the table has data, the seeding code never runs again, even if you restart the app. If the old data is still in the database, new seeding code is bypassed.

### Solution
**Clear the relevant tables without losing user data:**

Use the SQL script at `docs/delete database while keeping users.sql`:

```sql
DELETE FROM Units;
DELETE FROM Properties;
DELETE FROM UnitTypes;
```

Run this in Visual Studio's **SQL Server Object Explorer** → **New Query**, then restart the app (F5). The DbInitializer will detect empty tables and re-seed with the new data.

**Never use:**
- `dotnet ef database drop --force` (deletes everything including users)
- Manual database file deletion (same problem)

---

## Problem 3: Units/Entities Filtered Out Even When They Should Display

### Symptoms
- Units with certain types don't appear in the browse list
- Dropdown shows fewer items than expected
- Data seems to disappear after editing

### Root Cause
Overly restrictive filters in LINQ queries, often filtering by a related entity's status rather than the entity itself.

**Common mistake:**
```csharp
var units = await _context.Units
	.Where(u => u.UnitType.ActiveBool)  // ❌ Hides units with INACTIVE types
	.ToListAsync();
```

This filters out **entire units** because their **UnitType is inactive**. But the unit itself is valid and should display—only the type status affects whether it can be reassigned to other units.

### Solution
Remove the overly restrictive filter. Show all units; let the **dropdown validation** control what types can be selected, not the display logic.

```csharp
var units = await _context.Units  
	// ✅ No type filter—show ALL units regardless of type status
	.Select(u => new UnitBrowseViewModel { /** **/ })
	.ToListAsync();
```

Use `Where` only for:
- Filtering by unit status (if units have their own active flag)
- Filtering by property ownership
- Filtering by **user role/authorization**

**NOT for:**
- Filtering by related entity's active status (unless that makes business sense and is clearly documented)

---

## Problem 4: Browse/List Pages Show "Error Loading" or Blank Data

### Symptoms
- Browse units page shows error message or spinning loader
- Data table/grid is empty
- Network request succeeds (200 OK) but page still shows error

### Root Cause
Usually one of:

1. **JavaScript fetch URL mismatch** - Page calls `/api/units/list` but controller endpoint is `/units/list` (or vice versa)
2. **HTML/JSON response mismatch** - Page expects JSON but server returns HTML (or error page)
3. **Form handler not attached** (see Problem 1)
4. **CORS or authorization error** - Response is 401/403 but silently fails

### Solution

**Step 1: Check the fetch URL in browser DevTools**
- Open DevTools (F12) → **Network** tab
- Trigger the browse/list action
- Look for the fetch request to the server
- Verify the URL matches the controller route

**Step 2: Inspect the response**
- Click the network request
- View the **Response** tab
- If it's HTML (contains `<html>`, `<!DOCTYPE`), the URL is wrong
- If it's JSON, check the structure matches what the page expects

**Step 3: Verify the controller endpoint**
```csharp
[HttpGet("list")]  // Route is /units/list, not /api/units/list
public async Task<IActionResult> GetList() { /** **/ }
```

**Step 4: Check authorization**
- If the page requires user login, verify the user is authenticated
- Check `[Authorize]` attributes on the controller action

---

## Problem 5: UnitType Seeding Only Has Placeholder Names

### Symptoms
- Dropdown shows "Active" and "Inactive" instead of real unit types
- Seeding contradicts project plan
- Business user confused by generic names

### Root Cause
Initial seeding used placeholder names instead of the real unit type names from the project plan.

### Solution
Update `DbInitializer.cs` seeding to use real unit type names:

```csharp
var unitTypes = new List<UnitType>
{
	// Active (available for selection)
	new UnitType { UnitTypeName = "Studio / Efficiency", ActiveBool = true },
	new UnitType { UnitTypeName = "Traditional Multi-Bedroom", ActiveBool = true },
	new UnitType { UnitTypeName = "Loft", ActiveBool = true },
	new UnitType { UnitTypeName = "Townhome", ActiveBool = true },
	new UnitType { UnitTypeName = "Multiplex Unit (Duplex / Triplex / Fourplex)", ActiveBool = true },
	new UnitType { UnitTypeName = "Mixed-Use Residential", ActiveBool = true },
	// Inactive (retained for historical data)
	new UnitType { UnitTypeName = "Penthouse", ActiveBool = false },
	new UnitType { UnitTypeName = "Specialized Housing Unit", ActiveBool = false }
};
```

Clear the database and reseed (use the SQL script to preserve users).

**Key Understanding:**
- **UnitType** = Structure/style (Studio, Loft, Townhome, etc.)
- **Bedrooms** = Separate numeric field on Unit (1, 2, 3, 4, etc.)
- These are independent; don't confuse them

---

## Problem 6: Model Relationships Configured Incorrectly (Direct vs. Junction Table)

### Symptoms
- Created models with direct foreign key references that should use a junction table
- Schema doesn't match the domain design
- Code references properties that don't exist on the model
- Migration creates wrong table structure
- Tests fail because model shape is incorrect

### Root Cause
The relationship requirement (one-to-many, many-to-many, etc.) was misunderstood or implemented directly instead of through a cross-reference/junction entity. Common mistakes:

**Mistake 1: Direct many-to-many instead of junction table**
```csharp
// ❌ WRONG: Direct collection reference
public class Application
{
    public ICollection<Applicant> Applicants { get; set; }  // No xref table
}

public class Applicant
{
    public Application Application { get; set; }  // Direct back-reference
}
```

This breaks the domain model when an `Applicant` can belong to multiple `Application`s with unique metadata per relationship (e.g., `IsPrimary` status, `ResidenceHistories` per application).

**Mistake 2: Attaching child entities to wrong parent**
```csharp
// ❌ WRONG: ResidenceHistory attached directly to Application
public class Application
{
    public ICollection<ResidenceHistory> ResidenceHistories { get; set; }
}

public class ResidenceHistory
{
    public int ApplicationID { get; set; }
    public int ApplicantID { get; set; }
    // ❌ But which one owns the residence? Confusion and cascade issues.
}
```

When `ResidenceHistory` should hang off the junction (`ApplicationApplicant`), not the application directly.

### Solution
**Step 1: Identify the relationship type from the domain model**

Check `docs/RealEstate objects.md` or `PROJECT_PLAN.md`:
- If an entity can relate to another in multiple unique ways → use a **junction table**
- If metadata needed per relationship (flags, dates, ownership info) → use a **junction table**
- If it's truly one-to-many with no special data → use direct FK

**Step 2: Create the junction entity**

```csharp
public class ApplicationApplicant
{
    public int Id { get; set; }
    public int ApplicationID { get; set; }
    public int ApplicantID { get; set; }
    public bool IsPrimary { get; set; }  // Metadata per relationship

    public Application Application { get; set; }
    public Applicant Applicant { get; set; }
    public ICollection<ResidenceHistory> ResidenceHistories { get; set; }
}
```

**Step 3: Update the primary entities**

```csharp
public class Application
{
    public ICollection<ApplicationApplicant> ApplicationApplicants { get; set; }  // ✅ Junction
    // Remove: ICollection<Applicant> Applicants
    // Remove: ICollection<ResidenceHistory> ResidenceHistories
}

public class Applicant
{
    public ICollection<ApplicationApplicant> ApplicationApplicants { get; set; }  // ✅ Junction
    // Remove: Application Application
}

public class ResidenceHistory
{
    public int ApplicationApplicantID { get; set; }  // ✅ Points to junction
    public ApplicationApplicant ApplicationApplicant { get; set; }
    // Remove: int ApplicationID (indirect)
    // Remove: int ApplicantID (indirect)
}
```

**Step 4: Configure in DbContext**

```csharp
builder.Entity<ApplicationApplicant>(entity =>
{
    entity.HasOne(x => x.Application)
        .WithMany(a => a.ApplicationApplicants)
        .HasForeignKey(x => x.ApplicationID)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(x => x.Applicant)
        .WithMany(a => a.ApplicationApplicants)
        .HasForeignKey(x => x.ApplicantID)
        .OnDelete(DeleteBehavior.Restrict);

    entity.HasIndex(x => new { x.ApplicationID, x.ApplicantID }).IsUnique();
});

builder.Entity<ResidenceHistory>(entity =>
{
    entity.HasOne(r => r.ApplicationApplicant)
        .WithMany(x => x.ResidenceHistories)
        .HasForeignKey(r => r.ApplicationApplicantID)
        .OnDelete(DeleteBehavior.Cascade);
});
```

**Step 5: Update queries and seeding**

Replace direct collection access:
```csharp
// ❌ OLD
var applicant = application.Applicants.First();

// ✅ NEW
var link = application.ApplicationApplicants.First(x => x.IsPrimary);
var applicant = link.Applicant;
```

Helper methods for readability:
```csharp
private ApplicationApplicant GetPrimaryLink(Application app) =>
    app.ApplicationApplicants.First(x => x.IsPrimary);

private IEnumerable<ResidenceHistory> GetAllResidences(Application app) =>
    app.ApplicationApplicants.SelectMany(x => x.ResidenceHistories);
```

**Step 6: Remove and regenerate the migration**

```bash
dotnet ef migrations remove --force
dotnet ef migrations add <YourMigrationName>
dotnet ef database update
```

If the migration fails due to NullReferenceException in EF Core's type mapper, the Designer.cs snapshot may be corrupted. **Delete the manually-created migration files and regenerate.**

### Key Learning
**Always consult the domain model spec first** before creating relationships. If the spec mentions a "cross-reference table" or "xref," it's a junction table—never model it as a direct collection on both sides.

---

## Problem 7: Adding Notes/Comments Fields to the Wrong Entity (ManagerNotes on Lease)

### Symptoms
A `ManagerNotes` field was added to `Lease` (in the model, DbContext config, and migration) even though the domain spec does not define it there. The error later arose: `'Lease' does not contain a definition for 'ManagerNotes'`.

### Root Cause
When a concept like "manager notes" appears in the spec, it's tempting to add it directly to the entity that needs review (e.g., `Lease`). However, the spec defines ManagerNotes as its own entity: it references objects by `EntityName` and `EntityID`, not as a field on each table.

**How ManagerNotes works:**
- `ManagerNotes` is a standalone table with columns: `Id`, `EntityName` (e.g., "Lease", "Application"), `EntityID`, `Notes`, `CreatedAt`, `AuthorID`.
- When a manager adds notes to a lease, you insert a `ManagerNotes` row with `EntityName = "Lease"` and `EntityID = lease.Id`.
- Code never directly accesses `lease.ManagerNotes`; instead, you query `context.ManagerNotes.Where(m => m.EntityName == "Lease" && m.EntityID == lease.Id)`.

This pattern allows one table to hold notes for *any* entity type without bloating the schema.

### Solution
- **Do NOT** add `ManagerNotes`, `Notes`, or similar comment fields to `Lease`, `Application`, `Review`, or other entities.
- If you need a note on an entity, insert a `ManagerNotes` row that references it by name and ID.
- Review comments live on `Review.Comment` (which is a real column because it's part of the review decision). Do not duplicate or move comments to `Lease`.
- Keep `ApplicationDbContext` and migrations clean: only add fields that the domain model defines on that entity.

### Key Learning
**Separate the concept of comment/note storage from the entity being noted.** The ManagerNotes table uses a generic "EntityName + EntityID" pattern so you don't have to repeat a notes column on every table. This keeps the schema lean and makes it easy to add notes to new entity types without migrations.

---

## Quick Reference: When to Use What

| Problem | Tool | Command |
|---------|------|---------|
| Clear data, keep users | SQL Query | `DELETE FROM Units; DELETE FROM Properties; DELETE FROM UnitTypes;` |
| Clear entire DB (lose users) | EF CLI | `dotnet ef database drop --force` |
| View/edit data in VS | SQL Server Object Explorer | View → SQL Server Object Explorer (Ctrl+\, Ctrl+S) |
| Debug form submission | Browser DevTools | F12 → Network tab → Filter by Fetch/XHR |
| Debug script not loaded | Browser DevTools | F12 → Console → Check for errors |
| Check if modal form wired | Browser DevTools | F12 → Elements → Edit modal div → Inspect onclick/submit handlers |

---

## Testing Checklist

After fixing any of these problems:

1. **Build project**: `dotnet build` (0 errors)
2. **Run tests**: `dotnet test` (all pass)
3. **Manual verification** (F5 in VS):
   - Create a new entity (e.g., Create Unit)
   - Edit an existing entity
   - Browse/list all entities
   - Verify inactive-type handling (dropdowns, display)
4. **Check console**: F12 → Console tab (no red errors)
5. **Check network**: F12 → Network → Verify fetch URLs and responses are JSON

