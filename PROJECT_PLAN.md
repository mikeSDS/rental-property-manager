# Property Management Application: Prioritized Version Roadmap & GitHub Agent Implementation Plan (v0.1 -> v1.0)

## Overview & Architecture Specifications
This document outlines the comprehensive, prioritized software development roadmap for the ASP.NET Core 10 MVC Property Management Web Application (`RentalPropertyManager`). It integrates all technical requirements, domain model specifications, role-based security architectures, database seeding rules, and automated test specifications.

**Core Priority Strategy**: All **non-bonus core requirements** (Authentication, Property & Unit CRUD, Unit Availability, Rental Application Wizard, Database Application Filtering, Property Manager Review Workflow, 12-Month Lease Generation, and Bogus Data Seeding) are completed first in **v0.1 through v0.6**. 

At **v0.6**, the core system is 100% feature-complete, fully functional, and independently testable. Optional **bonus features** (Manager Notes, Soft Validation Saving, Paged Grid View Component with OpenAPI, Review Queue, and Multi-Applicant Co-signing with Optimistic Concurrency) are isolated into subsequent versions (**v0.7 through v1.0**).

---

## Mandatory Testing & Data Seeding Requirements

All version releases and implementation specifications MUST explicitly fulfill these core requirements:

1. **Idempotent Full Database Seeding**:
   - The database should be seeded idempotently with lookups, property managers, applicants, properties, units, and applications in every status.
2. **Bogus Data Generation**:
   - Seed your database with data using Bogus for .NET.
3. **Automated Business Logic Unit Tests**:
   - Add unit tests for business logic.

---

## Domain & Security Architecture Specifications

### 1. Identity Roles-Only Security Architecture
- All authorization relies **100% on native ASP.NET Core Identity Roles** (`PropertyManager` and `Applicant`).
- Controller endpoints and Razor Pages must be decorated strictly with `[Authorize(Roles = "PropertyManager")]` or `[Authorize(Roles = "Applicant")]` (or `[Authorize]` for shared endpoints like unit browsing).
- Custom user property checks (like legacy `UserType`) are strictly prohibited for authorization decisions.

### 2. UnitType Architecture & Active/Inactive State Specifications
The application enforces a dynamic lookup architecture for `UnitType` records using an `ActiveBool` status flag:

#### Standard Rental Unit Type Names
- Studio / Efficiency
- Traditional Multi-Bedroom (1BR, 2BR, 3BR+)
- Loft
- Townhome
- Multiplex Unit (Duplex / Triplex / Fourplex)
- Penthouse
- Mixed-Use Residential
- Specialized Housing Unit

> **Domain Architecture Clarification**: The number of bedrooms (`Bedrooms` integer property on the `Unit` entity) is an independent numerical attribute (e.g., 0, 1, 2, 3+) and is **NOT** a `UnitType`. `UnitType` is a category lookup entity representing structural/architectural layouts.

#### Active/Inactive State & Business Selection Rules
- **Active State (`ActiveBool == true`)**: Unit types available for selection when creating new units or updating existing unit records.
- **Inactive State (`ActiveBool == false`)**: Historical unit types retained for data integrity and existing unit references.
- **Selection Rules**:
  1. **Existing Unit Display**: Units assigned an inactive unit type must continue to display their assigned `UnitType` correctly across all list, table, and detail views to prevent historical data loss.
  2. **Create Dropdown Rule**: Dropdowns when creating a new unit MUST filter to display **active unit types only**.
  3. **Edit Dropdown Rule**: Dropdowns when editing an existing unit display active unit types PLUS the unit's currently assigned inactive unit type (if applicable) so the view renders without resetting or corrupting data.
  4. **Server-Side Validation**: Server controllers MUST validate and reject any `POST`/`PUT` requests that attempt to assign an inactive `UnitTypeID` to new units or change an existing unit's type to a different inactive `UnitType`.

---

## Machine-Readable Agent Execution Metadata
```yaml
project_name: PropertyManagementSystem
framework: .NET 10 ASP.NET Core MVC
database: SQL Server / Entity Framework Core 10 (Code-First)
auth_framework: ASP.NET Core Identity Roles
data_seeding: Bogus for .NET (Idempotent Startup Seeding)
frontend_architecture: Razor Views, Partial Views, View Components, Bootstrap 5, External JS Modules in wwwroot/js/
version_roadmap:
  - v0.1: [CORE] Solution Foundation, Database Context & Identity Setup
  - v0.2: [CORE] Property & Unit Management (Modals, Active UnitType Server Rules & Browsing)
  - v0.3: [CORE] Single-Applicant Rental Application Wizard, Section Partials & Residence History Modals
  - v0.4: [CORE] Application Submission, Active Lease Check & Database Application List Filtering
  - v0.5: [CORE] Property Manager Review Workflow, Audit Timeline & 12-Month Lease Issuance
  - v0.6: [CORE MILESTONE] Bogus Idempotent Seeding & Core Automated Test Suite (100% Core Complete)
  - v0.7: [BONUS] Manager Notes Security Isolation & Draft Soft Validation Error Preservation
  - v0.8: [BONUS] Paged/Sorted Grid View Component (OpenAPI JSON Endpoint) & Review Queue
  - v1.0: [BONUS MILESTONE] Multi-Applicant Co-Signing & Optimistic Concurrency Control
```

---

## Version v0.1: [CORE] Solution Foundation, Database Context & Identity Setup

### 1. Objective
Establish the solution structure, Entity Framework Core SQL Server DB context, ASP.NET Core Identity user/role management (`PropertyManager` and `Applicant`), and base lookup tables.

### 2. Business Functions & User Stories
- **System Admin / Setup**: Configure ASP.NET Core 10 MVC web app with `_Layout.cshtml` global shell, Bootstrap 5 navigation, and flash message toast notifications.
- **User Authentication**: Implement user registration, login, and logout. During registration, the user selects their system role: `Applicant` or `PropertyManager`.
- **Database Initialization**: Apply EF Core database migrations automatically on app startup. Idempotently seed `UnitType` lookup values (`ActiveBool = true/false`).

### 3. Business Rules & Constraints
- Users must belong to either the `Applicant` or `PropertyManager` ASP.NET Core Identity role.
- Roles must be enforced via `[Authorize(Roles = "PropertyManager")]` and `[Authorize(Roles = "Applicant")]` on controllers/actions.
- DB migrations and lookup seeding must execute idempotently on startup.

### 4. Models Needed
#### Entity Framework Core Entities
- `ApplicationUser`: Extends `IdentityUser`. Properties: `CreatedAt` (`DateTime`).
- `UnitType`: `Id` (int PK), `UnitTypeName` (string, required, max 100), `ActiveBool` (bool, default true).

#### ViewModels & DTOs
- `RegisterViewModel`: `Email` (Required, EmailAddress), `Password` (Required), `ConfirmPassword` (Compare Password), `RoleChoice` (`Applicant` or `PropertyManager`).
- `LoginViewModel`: `Email` (Required, EmailAddress), `Password` (Required), `RememberMe` (bool).

### 5. Controllers & UI / Razor Components
- `AccountController` / `Pages/Account`: `Register` (GET/POST), `Login` (GET/POST), `Logout` (POST).
- Views: `Views/Account/Register.cshtml`, `Views/Account/Login.cshtml`, `Views/Shared/_Layout.cshtml`.

### 6. Required Tests & Assertions
- **Unit Tests (`RegisterViewModelTests.cs`, `UnitTypeTests.cs`)**:
  - Validate password complexity, required role selection, and `UnitType` domain defaults.
- **Integration Tests (`AuthenticationTests.cs`, `DatabaseStartupTests.cs`)**:
  - Assert successful user registration assigns correct ASP.NET Core Identity Role (`Applicant` vs `PropertyManager`).
  - Assert DB migrations apply cleanly and `UnitType` lookup table is seeded idempotently on startup.

---

## Version v0.2: [CORE] Property & Unit Management (Modals, Active UnitType Server Rules & Browsing)

### 1. Objective
Implement Property Manager CRUD for Properties and Units using Bootstrap partial view modals, enforce server-side `UnitType` active/inactive validation rules, and enable Applicants and Managers to browse available units.

### 2. Business Functions & User Stories
- **Property Management**: Property Manager can view all properties, create new properties, edit existing properties, and remove properties via Bootstrap partial view modals.
- **Unit Management**: Property Manager can add, edit, and remove units associated with a property (`UnitNumber`, `Bedrooms`, `MonthlyRent`, `UnitTypeID`) via modal forms.
- **UnitType Lookup Enforcement**: Manage unit type selection. Inactive unit types remain displayed on existing units but cannot be selected when creating or updating any unit.
- **Applicant & Manager Unit Browsing**: Users can browse a list of properties and units, search/filter by bedrooms or max rent, and initiate a rental application.

### 3. Business Rules & Constraints
- Only users in the `PropertyManager` role can manage properties and units (`[Authorize(Roles = "PropertyManager")]`).
- **Server-Side UnitType Rule**: On `POST`/`PUT` of a Unit, the server MUST reject the submission if the selected `UnitTypeId` has `ActiveBool == false`. Existing units assigned an inactive `UnitTypeId` retain it visually in the UI.
- `UnitNumber` must be unique per property.
- `Bedrooms` must be `>= 0`, `MonthlyRent` must be `> 0` (configured with EF Core `.HasPrecision(18, 2)`).
- **Client JS Architecture**: Modal forms MUST be HTML-only (no inline `<script>` tags). Client AJAX logic lives in `wwwroot/js/modal-handler.js` using external script loading and event delegation. Form validation failures (`HTTP 400`) re-render partial HTML with `text-danger` error spans; valid saves (`HTTP 200`) return JSON `{ success: true }` and refresh the grid.

### 4. Models Needed
#### Entity Framework Core Entities
- `Property`: `Id` (int PK), `Name` (string, required, max 150), `StreetAddress` (string, required, max 200), `City` (string, required, max 100), `State` (string, required, max 50), `ZipCode` (string, required, max 20), `CreatedAt` (DateTime), `ManagerNotes` (string, nullable). Navigation: `ICollection<Unit> Units`.
- `Unit`: `Id` (int PK), `PropertyID` (int FK), `UnitNumber` (string, required, max 50), `Bedrooms` (int, required), `MonthlyRent` (decimal, required, precision 18,2), `UnitTypeID` (int FK), `ManagerNotes` (string, nullable). Navigation: `Property Property`, `UnitType UnitType`.

#### ViewModels & DTOs
- `PropertyFormViewModel` / `PropertyModalViewModel`: `Id`, `Name`, `StreetAddress`, `City`, `State`, `ZipCode`, `ManagerNotes`.
- `UnitFormViewModel` / `UnitModalViewModel`: `Id`, `PropertyID`, `UnitNumber`, `Bedrooms`, `MonthlyRent`, `UnitTypeID`, `AvailableUnitTypes` (SelectList containing active types for dropdowns, plus current inactive type if editing).
- `UnitBrowseViewModel`: `UnitID`, `PropertyID`, `PropertyName`, `PropertyAddress`, `UnitNumber`, `Bedrooms`, `MonthlyRent`, `UnitTypeName`, `IsAvailable`.

### 5. Controllers & UI / Razor Components
- `PropertiesController`: `Index` (GET list), `CreateModal` (GET partial / POST JSON), `EditModal` (GET partial / POST JSON), `Delete` (POST).
- `UnitsController`: `CreateModal` (GET partial / POST JSON), `EditModal` (GET partial / POST JSON), `Delete` (POST), `Browse` (GET for Applicants/Managers).
- Views & Partials: `Views/Properties/Index.cshtml`, `Views/Properties/_PropertyModal.cshtml`, `Views/Units/_UnitModal.cshtml`, `Views/Units/Browse.cshtml`, `wwwroot/js/modal-handler.js`.

### 6. Required Tests & Assertions
- **Unit Tests (`UnitServiceTests.cs`)**:
  - Assert server validation fails if `UnitTypeId` is inactive during Unit creation/edit.
  - Assert existing units with inactive `UnitTypeId` pass read/display validation.
- **Integration Tests (`PropertyManagerAccessTests.cs`)**:
  - Assert `Applicant` role receives HTTP 403 Forbidden when attempting to access `PropertiesController` or `UnitsController` POST actions.
- **UI Modal Validation Tests (`UnitModalValidationTests.cs`)**:
  - Assert invalid modal post returns partial view HTML containing `text-danger` validation span elements (HTTP 400).
  - Assert valid modal post returns JSON success `{ success = true }` (HTTP 200).

---

## Version v0.3: [CORE] Single-Applicant Rental Application Wizard, Section Partials & Residence History Modals

### 1. Objective
Build the single-page multi-section rental application wizard driven by one view model, utilizing section partial views, standalone external JavaScript (`wwwroot/js/application-wizard.js`), and residence history modal CRUD.

### 2. Business Functions & User Stories
- **Applicant Wizard**: Single-page application experience with 3 sections:
  1. *Section 1: Applicant Information* (Name, Phone, Email, Current Address).
  2. *Section 2: Residence History* (List of prior residences added/edited/removed via modal).
  3. *Section 3: Summary* (Read-only view of both sections and Submit button).
- **Navigation Logic**: Single form posting to one controller action.
  - `Continue`: Validates current section. Saves to DB only if valid, then displays next section. Re-renders current section with errors if invalid.
  - `Back`: Returns to previous section without saving.
  - `Submit`: Available strictly from Summary section after both sections are successfully saved.
- **Read-Only vs Editable State**: The section partial renders editable form inputs if Application status is `Draft` or `Returned`; renders read-only display text for all other statuses (`Submitted`, `Approved`, `Denied`, `Withdrawn`). Server rejects posts to non-editable applications.

### 3. Business Rules & Constraints
- Only 1 view model (`ApplicationWizardViewModel`) drives the single-page application wizard.
- Residences must be managed through modal popups (`_ResidenceModal.cshtml`). Server validation MUST enforce `MoveOutDate >= MoveInDate` when `MoveOutDate` is provided.
- Controllers must validate application status server-side before accepting section updates (`Status` MUST be `Draft` or `Returned`).
- All partial views contain HTML markup ONLY—NO inline `<script>` tags. Client logic lives in `wwwroot/js/application-wizard.js` with a `wireUpResidenceModal()` helper.

### 4. Models Needed
#### Entity Framework Core Entities
- `Application`: `Id` (int PK), `UnitID` (int FK), `ApplicantUserID` (string FK to AspNetUsers), `Date` (DateTime), `ApplicationStatusID` (int FK), `ManagerNotes` (string, nullable). Navigation: `Unit Unit`, `ApplicationStatus ApplicationStatus`, `Applicant Applicant`, `ICollection<ResidenceHistory> ResidenceHistories`.
- `Applicant`: `Id` (int PK), `ApplicationID` (int FK), `UserID` (string FK), `Name` (string, required, max 100), `Phone` (string, required, max 20), `Email` (string, required, max 100), `CurrentAddress` (string, required, max 200).
- `ApplicantsXRef`: Junction entity `ApplicationID` (int FK), `ApplicantID` (int FK).
- `ResidenceHistory`: `Id` (int PK), `ApplicationID` (int FK), `ApplicantID` (int FK), `Street` (string, required, max 200), `City` (string, required, max 100), `State` (string, required, max 50), `Zip` (string, required, max 20), `LandlordName` (string, required, max 100), `LandlordPhone` (string, required, max 20), `MoveInDate` (DateTime, required), `MoveOutDate` (DateTime, nullable).
- `ApplicationStatus`: `Id` (int PK), `Name` (string: `Draft`, `Submitted`, `Returned`, `Approved`, `Denied`, `Withdrawn`, `Under Review`).

#### ViewModels & DTOs
- `ApplicationWizardViewModel`: `ApplicationID`, `UnitID`, `UnitNumber`, `PropertyName`, `MonthlyRent`, `CurrentStep` (1, 2, or 3), `IsReadOnly`, `StatusName`, `ApplicantInfo` (`ApplicantFormViewModel`), `Residences` (`List<ResidenceHistoryViewModel>`).
- `ApplicantFormViewModel` / `ApplicantProfileViewModel`: `ApplicantID`, `ApplicationID`, `Name`, `Phone`, `Email`, `CurrentAddress`.
- `ResidenceHistoryViewModel` / `ResidenceModalViewModel`: `ResidenceID`, `ApplicationID`, `ApplicantID`, `Street`, `City`, `State`, `Zip`, `LandlordName`, `LandlordPhone`, `MoveInDate`, `MoveOutDate`.

### 5. Controllers & UI / Razor Components
- `ApplicationController`: `Create` (GET - starts application in `Draft`), `Wizard` (GET/POST - handles `Continue`, `Back`, step rendering), `ResidenceModal` (GET partial), `SaveResidence` (POST partial modal), `DeleteResidence` (POST), `ResidenceTablePartial` (GET partial).
- Views & Partials: `Views/Application/Wizard.cshtml`, `Views/Application/Partials/_ApplicantInfoSection.cshtml`, `Views/Application/Partials/_ResidenceHistorySection.cshtml`, `Views/Application/Partials/_ResidenceTable.cshtml`, `Views/Application/Partials/_SummarySection.cshtml`, `Views/Application/Modals/_ResidenceModal.cshtml`, `wwwroot/js/application-wizard.js`.

### 6. Required Tests & Assertions
- **Unit Tests (`ApplicationWizardTests.cs`)**:
  - Assert `Continue` on Step 1 validates required fields and advances `CurrentStep` from 1 to 2 when valid.
  - Assert `Back` button navigates from Step 2 to Step 1 without invoking DB save.
  - Assert server validation fails if `MoveOutDate < MoveInDate` on residence history.
  - Assert `IsReadOnly = true` when `ApplicationStatus` is `Submitted`, `Approved`, `Denied`, or `Withdrawn`.
- **Integration Tests (`ApplicationSecurityTests.cs`, `ResidenceModalTests.cs`)**:
  - Assert HTTP POST to update an application in `Submitted` or `Approved` status returns HTTP 400/403.
  - Assert HTTP POST to update another user's application returns HTTP 403 Forbidden.
  - Assert adding a residence history item updates the DB table and re-renders the Step 2 residence table partial.

---

## Version v0.4: [CORE] Application Submission, Active Lease Check & Database Application List Filtering

### 1. Objective
Implement final application submission, server-side active lease checks on submission, and the database-filtered Application List for Applicants and Property Managers.

### 2. Business Functions & User Stories
- **Application Submission**: Applicant clicks `Submit` on the Summary page. System transitions status from `Draft`/`Returned` to `Submitted`.
- **Active Lease Check at Submission**: When applicant clicks `Submit`, server verifies that the target Unit does not currently have an active lease covering today's date (`StartDate <= Today <= EndDate`). If an active lease exists, reject submission with a user-friendly error message.
- **Application List**:
  - Filtered list of applications by `Status` and `Property`.
  - Filtering MUST be performed in SQL/database via EF Core `IQueryable`, NOT in memory (`.Where(a => a.Unit.PropertyID == propertyId && a.ApplicationStatusID == statusId)`).
  - Applicants see only their own submitted/draft applications; Property Managers see all applications across all properties.

### 3. Business Rules & Constraints
- Application submission must verify unit lease availability.
- Database query for application list MUST execute SQL `WHERE` clauses for filtering.
- Applicants receive HTTP 403 Forbidden if attempting to query applications owned by other users.

### 4. Models Needed
#### ViewModels & DTOs
- `ApplicationListFilterViewModel`: `SelectedPropertyID` (int?), `SelectedStatusID` (int?), `AvailableProperties` (SelectList), `AvailableStatuses` (SelectList).
- `ApplicationListItemViewModel`: `ApplicationID`, `ApplicantName`, `PropertyName`, `UnitNumber`, `SubmissionDate`, `StatusName`, `CanEdit`.

### 5. Controllers & UI / Razor Components
- `ApplicationController`: `Submit` (POST action with active lease verification).
- `ApplicationListController`: `Index` (GET with query string filters `propertyId` and `statusId`).
- Views: `Views/ApplicationList/Index.cshtml`, `Views/ApplicationList/_ApplicationFilterBar.cshtml`.

### 6. Required Tests & Assertions
- **Unit Tests (`LeaseValidationServiceTests.cs`)**:
  - Assert submission is rejected with error when unit has an active lease (`StartDate <= Today <= EndDate`).
- **Integration Tests (`ApplicationListDatabaseFilterTests.cs`, `ApplicationListAuthorizationTests.cs`)**:
  - Capture generated EF Core SQL log and assert SQL contains `WHERE` clauses for `PropertyID` and `StatusID`.
  - Assert Applicant user receives ONLY their own applications in query results.

---

## Version v0.5: [CORE] Property Manager Review Workflow, Audit Timeline & 12-Month Lease Issuance

### 1. Objective
Build the Property Manager review modal (`Approve`, `Return`, `Deny`), status transition handling, status history timeline display (`ActionHistory` / `Review`), and automatic 12-month lease creation upon approval.

### 2. Business Functions & User Stories
- **Property Manager Review**: Property Manager opens a `Submitted` application and completes a review through a modal:
  - Select Outcome: `Approve`, `Return`, or `Deny`.
  - Comment: Optional for `Approve`, **MANDATORY** for `Return` and `Deny`.
- **Automated Lease Issuance**: Approving an application automatically generates a `Lease` for the unit starting today for a 12-month term (`StartDate = Today`, `EndDate = Today.AddMonths(12)`), copying `MonthlyRent` directly from `Unit.MonthlyRent` at lease signing.
- **Approval Active Lease Check**: At the moment of approval, server checks if an active lease already exists for the unit. If an active lease exists, reject approval with an error message.
- **Application Status History & Audit Timeline**: Application view displays a chronological history of status changes and review outcomes (Who, When, Outcome Status, Comment, ActionType).
- **Applicant Withdrawal**: Applicant can withdraw a submitted application (status transitions to `Withdrawn`).

### 3. Business Rules & Constraints
- Terminal Application Statuses: `Approved`, `Denied`, `Withdrawn`.
- Comments are mandatory when outcome is `Return` or `Deny`.
- Approval check prevents double-leasing a unit.
- A unit whose lease term covers today (`StartDate <= Today <= EndDate`) is marked unavailable for new leases and applications.

### 4. Models Needed
#### Entity Framework Core Entities
- `Lease`: `Id` (int PK), `UnitID` (int FK), `ApplicationID` (int FK), `StartDate` (DateTime), `EndDate` (DateTime), `MonthlyRent` (decimal, copied from `Unit.MonthlyRent` at lease signing), `CreatedAt` (DateTime), `ManagerNotes` (string, nullable).
- `Review`: `Id` (int PK), `ApplicationID` (int FK), `UserID` (string FK), `ReviewDate` (DateTime), `OutcomeApplicationStatusID` (int FK), `Comment` (string).
- `ActionType`: `Id` (int PK), `Name` (string: `Submission`, `StartReview`, `CompleteReview`, `AddProperty`, `EditProperty`, `RemoveProperty`, `AddApplication`, etc.).
- `ActionHistory`: `Id` (int PK), `ActionTypeID` (int FK), `UserID` (string FK), `Date` (DateTime), `ApplicationID` (int, nullable FK), `UnitID` (int, nullable FK), `PropertyID` (int, nullable FK), `FromStatus` (string), `ToStatus` (string), `FromObject` (string JSON), `ToObject` (string JSON).

#### ViewModels & DTOs
- `ReviewModalViewModel`: `ApplicationID`, `OutcomeStatusID`, `Comment` (Required if Return/Deny), `AvailableOutcomes` (SelectList: Approved, Returned, Denied).
- `StatusHistoryItemViewModel`: `ReviewDate`, `ReviewerName`, `FromStatusName`, `ToStatusName`, `Comment`, `ActionTypeName`.

### 5. Controllers & UI / Razor Components
- `ReviewController`: `ReviewModal` (GET partial), `CompleteReview` (POST action with lease creation logic).
- View Components: `Views/Shared/Components/StatusHistoryTimeline/Default.cshtml` (`StatusHistoryTimelineViewComponent`).
- Views & Modals: `Views/Review/_ReviewModal.cshtml`.

### 6. Required Tests & Assertions
- **Unit Tests (`ReviewWorkflowTests.cs`, `LeaseGenerationTests.cs`)**:
  - Assert `CompleteReview` fails model validation if outcome is `Return` or `Deny` and `Comment` is empty.
  - Assert `Approve` outcome creates a `Lease` record with exact 12-month duration (`EndDate == StartDate.AddMonths(12)`) and copies `Unit.MonthlyRent`.
- **Integration Tests (`DoubleLeasePreventionTests.cs`, `StatusTimelineTests.cs`)**:
  - Assert approving an application for a unit that already has an active lease fails and returns an error message.
  - Assert review actions create `Review` and `ActionHistory` records displayed in the `StatusHistoryTimelineViewComponent`.

---

## Version v0.6: [CORE MILESTONE] Bogus Idempotent Seeding & Core Automated Test Suite (100% Core Complete)

### 1. Objective
Implement idempotent database seeding using Bogus for .NET and execute the complete automated test suite verifying all core application requirements.

### 2. Business Functions & User Stories
- **Automated Data Seeding**: On application startup, seed the database using Bogus with:
  - Roles: `Applicant`, `PropertyManager`.
  - Accounts: Default Property Manager (`manager@realestate.com`) and Applicant (`applicant@realestate.com`).
  - Lookup Data: `UnitType` (Active and Inactive types), `ApplicationStatus` lookups (`Draft`, `Submitted`, `Returned`, `Approved`, `Denied`, `Withdrawn`, `Under Review`), `ActionType` lookups.
  - Domain Data: Properties, Units, Applications in **EVERY status** (`Draft`, `Submitted`, `Returned`, `Approved`, `Denied`, `Withdrawn`), Residence Histories, and active Leases.
- **Strict Idempotency**: Seeding script (`DbInitializer.cs`) runs safely on every startup without creating duplicate records or throwing unique constraint exceptions, guarded by explicit `.Any()`, `RoleExistsAsync`, and `FindByEmailAsync` checks.

### 3. Business Rules & Constraints
- Seeding must use Bogus for realistic mock data generation.
- Database must contain applications in every status upon initial seed.
- Re-running `DbInitializer.InitializeAsync()` repeatedly MUST leave total row counts constant.

### 4. Models Needed
- `DbInitializer` / `BogusDataSeeder`: C# static seeding utility utilizing `Bogus.Faker`.

### 5. Required Tests & Assertions
- **Integration Suite (`SeedingIdempotencyTests.cs`, `CoreRequirementCoverageTests.cs`)**:
  - Run `DbInitializer.InitializeAsync()` twice in succession; assert total row counts for Properties, Units, Users, Applications, and Leases remain unchanged and 0 exceptions are thrown.
  - Assert database contains at least one application for every `ApplicationStatus` (`Draft`, `Submitted`, `Returned`, `Approved`, `Denied`, `Withdrawn`).

> **CORE MILESTONE VERIFICATION**: At v0.6, all non-bonus functional and technical requirements of the assessment are 100% complete, fully functional, and verified by unit/integration tests.

---

## Version v0.7: [BONUS] Manager Notes Security Isolation & Draft Soft Validation Error Preservation

### 1. Objective
Implement Property Manager Notes across entities with strict DTO security isolation (Bonus #3) and soft section validation error preservation in Draft mode (Bonus #4).

### 2. Business Functions & User Stories
- **Bonus #3 - Manager Notes**: Property Managers can view and edit private notes on `Property`, `Unit`, `Application`, and `Lease`. Notes are strictly hidden from Applicants.
- **Bonus #4 - Draft Soft Validation**: Allow applicants to save wizard sections even when fields fail validation. Errors are stored and highlighted per field. The Summary page lists all outstanding blocking errors, and `Submit` remains disabled until all errors are cleared.

### 3. Business Rules & Constraints
- **Security Rule (Bonus #3)**: `ManagerNotes` must NEVER be included in any ViewModel rendered or returned to an Applicant endpoint.
- **Draft Rule (Bonus #4)**: `Application.IsDraftSavedWithErrors` flag set to true when section has validation errors.

### 4. Models Needed
#### Entity Modifications
- Add `ManagerNotes` (string, nullable) to `Property`, `Unit`, `Application`, `Lease`.
- Add `SectionValidationErrorsJson` (string, JSON) to `Application`.

#### ViewModels & DTOs
- `ManagerNoteEditDTO`: `EntityName`, `EntityID`, `NoteText`.
- `DraftSummaryErrorViewModel`: `SectionName`, `FieldName`, `ErrorMessage`.

### 5. Controllers & UI / Razor Components
- `ManagerNotesController`: `UpdateNote` (POST action restricted to `PropertyManager`).
- UI Extensions: `_ManagerNotesPartial.cshtml` rendered only inside Property Manager views.

### 6. Required Tests & Assertions
- **Security Unit Tests (`ManagerNotesSecurityTests.cs`)**:
  - Inspect all `Applicant` ViewModels via reflection and assert `ManagerNotes` property is completely absent.
- **Unit Tests (`DraftSoftValidationTests.cs`)**:
  - Assert section saves to DB with invalid email format when in Draft status, but Summary page flags error and disables Submit button.

---

## Version v0.8: [BONUS] Paged/Sorted Grid View Component (OpenAPI JSON Endpoint) & Review Queue

### 1. Objective
Implement database paging/sorting for the Application List extracted into a reusable Razor Grid View Component driven by an OpenAPI JSON endpoint (Bonus #1) and a Property Manager Review Queue (Bonus #2).

### 2. Business Functions & User Stories
- **Bonus #1 - Reusable Grid View Component**: Reusable Razor grid view component for the application list, powered by an API JSON endpoint returning paged rows and total filtered count. Documented with OpenAPI (Swashbuckle/Scalar).
- **Bonus #2 - Review Queue**: Property Manager can claim a submitted application (status transitions to `Under Review`), locking it for review, and release it back to the queue if uncompleted.

### 3. Business Rules & Constraints
- Paging and sorting MUST be executed in SQL (`.Skip()`, `.Take()`, `.OrderBy()`).
- Review Queue claiming prevents two managers from reviewing the same application simultaneously.

### 4. Models Needed
#### ViewModels & DTOs
- `PagedResultDTO<T>`: `List<T> Items`, `int TotalCount`, `int PageIndex`, `int PageSize`.
- `GridQueryParameters`: `int Page = 1`, `int PageSize = 10`, `string SortColumn`, `string SortDirection`, `int? PropertyId`, `int? StatusId`.

### 5. Controllers & UI / Razor Components
- `Api/ApplicationGridApiController`: OpenAPI-documented JSON endpoint for paged application list data.
- View Component: `ApplicationGridViewComponent` (`Views/Shared/Components/ApplicationGrid/Default.cshtml`).
- `ReviewQueueController`: `Claim` (POST), `Release` (POST).

### 6. Required Tests & Assertions
- **Unit & Integration Tests (`PagedGridSqlTests.cs`, `ReviewQueueConcurrencyTests.cs`)**:
  - Assert generated SQL contains `OFFSET` and `FETCH NEXT` clauses.
  - Assert claiming an already claimed application by a second manager returns HTTP 409 Conflict.

---

## Version v1.0: [BONUS MILESTONE] Multi-Applicant Co-Signing & Optimistic Concurrency Control

### 1. Objective
Implement multi-applicant support per application via email invitations (Bonus #5) and EF Core optimistic concurrency control (`RowVersion`) to prevent stale overwrites.

### 2. Business Functions & User Stories
- **Bonus #5 - Multi-Applicant Co-Signing**: Allow multiple applicants on a single application. Primary applicant invites co-applicant by email. Any linked applicant can view and edit sections.
- **Optimistic Concurrency**: Concurrent edits to the same section by two applicants detect stale data and reject the second save with a message asking the user to reload.

### 3. Business Rules & Constraints
- Section saves enforce EF Core `[Timestamp]` / `RowVersion` concurrency checks.
- Ownership checks verify current user is linked to the application via `ApplicationApplicant` / `ApplicantsXRef` junction entity.

### 4. Models Needed
#### Entity Framework Core Entities
- `ApplicationApplicant` / `ApplicantsXRef` (Junction): `Id` (int PK), `ApplicationID` (int FK), `ApplicantID` (int FK), `IsPrimary` (bool), `InvitationEmail` (string).
- Entity modification to `Application`: Add `byte[] RowVersion` concurrency token (`[Timestamp]`).
- All existing entities: `Property`, `Unit`, `UnitType`, `Lease`, `Application`, `Applicant`, `ApplicantsXRef`, `ResidenceHistory`, `ApplicationStatus`, `Review`, `ActionType`, `ActionHistory`.

#### ViewModels & DTOs
- `InviteCoApplicantViewModel`: `ApplicationID`, `CoApplicantEmail`.

### 5. Controllers & UI / Razor Components
- `CoApplicantController`: `Invite` (POST action sending invitation link).
- Modals: `Views/CoApplicant/_InviteModal.cshtml`.

### 6. Required Tests & Assertions
- **Unit & Integration Tests (`OptimisticConcurrencyTests.cs`, `MultiApplicantAccessTests.cs`)**:
  - Simulate two simultaneous section saves with identical `RowVersion`; assert second save throws `DbUpdateConcurrencyException` and returns a HTTP 409 stale data reload message.
  - Assert both primary and co-applicant can view and edit the application wizard sections.

---

## Final Verification Checklist for GitHub Coding Agent

1. **`dotnet build`**: 0 Errors, 0 Warnings across all solution projects.
2. **`dotnet test`**: 100% pass rate across Unit and Integration test suites.
3. **Database Migrations**: `dotnet ef database update` executes cleanly on fresh SQL Server instance.
4. **Idempotent Seeding**: Run app twice in succession; verify database row counts remain completely stable without duplicate records or primary key exceptions.
5. **Security Audit**: Verify `[Authorize(Roles = "PropertyManager")]` on all management/review endpoints and `ManagerNotes` completely omitted from applicant views.
