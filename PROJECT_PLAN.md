# Property Management Application: Prioritized Version Roadmap & GitHub Agent Implementation Plan (v0.1 -> v1.0)

## Overview & Architecture Specifications
This document outlines the revised, prioritized software development roadmap for the ASP.NET Core 10 MVC Property Management Web Application. 

**Core Priority Strategy**: All **non-bonus core requirements** (Authentication, Property & Unit CRUD, Unit Availability, Rental Application Wizard, Database Application Filtering, Property Manager Review Workflow, 12-Month Lease Generation, and Bogus Data Seeding) are completed first in **v0.1 through v0.6**. 

At **v0.6**, the core system is 100% feature-complete, fully functional, and independently testable. Optional **bonus features** (Manager Notes, Soft Validation Saving, Paged Grid View Component with OpenAPI, Review Queue, and Multi-Applicant Co-signing with Optimistic Concurrency) are isolated into subsequent versions (**v0.7 through v1.0**).

---

## Machine-Readable Agent Execution Metadata
```yaml
project_name: PropertyManagementSystem
framework: .NET 10 ASP.NET Core MVC
database: SQL Server / Entity Framework Core 10 (Code-First)
auth_framework: ASP.NET Core Identity
data_seeding: Bogus for .NET
frontend_architecture: Razor Views, Partial Views, View Components, Browser-Native ES6 Modules
version_roadmap:
  - v0.1: [CORE] Solution Foundation, Database Context & Identity Setup
  - v0.2: [CORE] Property & Unit Management (Modals, Active UnitType Server Rules & Browsing)
  - v0.3: [CORE] Rental Application Single-Page Wizard, Section Partials & Residence History Modals
  - v0.4: [CORE] Application Submission, Active Lease Check & DB Application List Filtering
  - v0.5: [CORE] Property Manager Review Workflow, Audit Timeline & 12-Month Lease Issuance
  - v0.6: [CORE MILESTONE] Bogus Idempotent Seeding & Core Automated Test Suite (100% Core Complete)
  - v0.7: [BONUS] Manager Notes Security Isolation & Draft Soft Validation Error Preservation
  - v0.8: [BONUS] Paged/Sorted Grid View Component (OpenAPI JSON Endpoint) & Review Queue
  - v1.0: [BONUS MILESTONE] Multi-Applicant Co-Signing & Optimistic Concurrency Control
```

---

## Version v0.1: [CORE] Solution Foundation, Database Context & Identity Setup

### 1. Objective
Establish the solution structure, Entity Framework Core SQL Server DB context, ASP.NET Core Identity user/role management, and base lookup tables.

### 2. Business Functions & User Stories
- **System Admin / Setup**: Configure ASP.NET Core 10 MVC web app with `_Layout.cshtml` global shell, Bootstrap 5 navigation, and flash message toast notifications.
- **User Authentication**: Implement user registration, login, and logout. During registration, the user selects their system role: `Applicant` or `PropertyManager`.
- **Database Initialization**: Apply EF Core database migrations automatically on app startup. Idempotently seed `UnitType` lookup values (`Active`, `Inactive`).

### 3. Business Rules & Constraints
- Users must belong to either the `Applicant` or `PropertyManager` role.
- Roles must be enforced via `[Authorize(Roles = "PropertyManager")]` and `[Authorize(Roles = "Applicant")]` on controllers/actions.
- DB migrations and lookup seeding must execute idempotently on startup.

### 4. Models Needed
#### Entity Framework Core Entities
- `ApplicationUser`: Extends `IdentityUser`. Properties: `UserType` (`Applicant` or `PropertyManager`), `CreatedAt`.
- `UnitType`: `Id` (int), `UnitTypeName` (string), `ActiveBool` (bool).

#### ViewModels & DTOs
- `RegisterViewModel`: `Email`, `Password`, `ConfirmPassword`, `RoleChoice` (`Applicant` or `PropertyManager`).
- `LoginViewModel`: `Email`, `Password`, `RememberMe`.

### 5. Controllers & UI / Razor Components
- `AccountController`: `Register` (GET/POST), `Login` (GET/POST), `Logout` (POST).
- Views: `Views/Account/Register.cshtml`, `Views/Account/Login.cshtml`, `Views/Shared/_Layout.cshtml`.

### 6. Required Tests & Assertions
- **Unit Tests**:
  - `RegisterViewModelTests`: Validate password complexity and required role selection.
- **Integration Tests**:
  - `AuthenticationTests`: Assert successful user registration assigns correct ASP.NET Core Identity Role (`Applicant` vs `PropertyManager`).
  - `DatabaseStartupTests`: Assert DB migrations apply and `UnitType` lookup table is seeded on startup.

---

## Version v0.2: [CORE] Property & Unit Management (Modals, Active UnitType Server Rules & Browsing)

### 1. Objective
Implement Property Manager CRUD for Properties and Units using Razor partial modals, enforce server-side `UnitType` active/inactive validation rules, and enable Applicants to browse available units.

### 2. Business Functions & User Stories
- **Property Manager**: Create, edit, and soft-delete/remove Properties and Units via Bootstrap partial view modals.
- **UnitType Lookup Enforcement**: Manage unit type selection. Inactive unit types remain displayed on existing units but cannot be selected when creating or updating any unit.
- **Applicant Unit Browsing**: Applicants can browse a list of properties and units to select a unit and initiate a rental application.

### 3. Business Rules & Constraints
- **Server-Side UnitType Rule**: On `POST`/`PUT` of a Unit, the server MUST reject the submission if the selected `UnitTypeId` has `ActiveBool == false`. Existing units assigned an inactive `UnitTypeId` retain it visually in the UI.
- Modal interactions must use Razor Partial Views returned by controller actions. Form validation failures re-render the partial inside the modal with error messages; successful saves return HTTP 200/201 and trigger a grid refresh.

### 4. Models Needed
#### Entity Framework Core Entities
- `Property`: `Id` (int), `Name` (string), `StreetAddress` (string), `City` (string), `State` (string), `ZipCode` (string), `CreatedAt` (DateTime).
- `Unit`: `Id` (int), `PropertyID` (int FK), `UnitNumber` (string), `Bedrooms` (int), `MonthlyRent` (decimal), `UnitTypeID` (int FK).

#### ViewModels & DTOs
- `PropertyFormViewModel`: `Id`, `Name`, `StreetAddress`, `City`, `State`, `ZipCode`.
- `UnitFormViewModel`: `Id`, `PropertyID`, `UnitNumber`, `Bedrooms`, `MonthlyRent`, `UnitTypeID`, `AvailableUnitTypes` (SelectList containing only active types for dropdowns, plus current inactive type if editing).
- `UnitBrowseViewModel`: `UnitID`, `PropertyName`, `UnitNumber`, `Bedrooms`, `MonthlyRent`, `UnitTypeName`, `IsAvailable`.

### 5. Controllers & UI / Razor Components
- `PropertiesController`: `Index`, `Create` (GET/POST partial), `Edit` (GET/POST partial), `Delete` (POST).
- `UnitsController`: `Create` (GET/POST partial), `Edit` (GET/POST partial), `Browse` (GET for Applicants).
- Views & Partials: `Views/Properties/Index.cshtml`, `Views/Properties/_PropertyModal.cshtml`, `Views/Units/_UnitModal.cshtml`, `Views/Units/Browse.cshtml`.

### 6. Required Tests & Assertions
- **Unit Tests**:
  - `UnitServiceTests`: Assert server validation fails if `UnitTypeId` is inactive during Unit creation/edit.
  - `UnitServiceTests`: Assert existing units with inactive `UnitTypeId` pass read/display validation.
- **Integration Tests**:
  - `PropertyManagerAccessTests`: Assert `Applicant` role receives HTTP 403 Forbidden when attempting to access `PropertiesController` POST actions.
  - `UnitModalValidationTests`: Assert invalid modal post returns partial view HTML containing `text-danger` validation span elements.

---

## Version v0.3: [CORE] Single-Applicant Rental Application Wizard, Section Partials & Residence History Modals

### 1. Objective
Build the single-page multi-section rental application wizard driven by one view model, utilizing section partial views and residence history modal CRUD.

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
- Only 1 view model drives the single-page application wizard.
- Residences must be managed through modal popups (partial views).
- Controllers must validate application status server-side before accepting section updates (`Status` MUST be `Draft` or `Returned`).

### 4. Models Needed
#### Entity Framework Core Entities
- `Application`: `Id` (int), `UnitID` (int FK), `ApplicantUserID` (string FK), `Date` (DateTime), `ApplicationStatusID` (int FK).
- `Applicant`: `Id` (int), `ApplicationID` (int FK), `UserID` (string FK), `Name` (string), `Phone` (string), `Email` (string), `CurrentAddress` (string).
- `ResidenceHistory`: `Id` (int), `ApplicationID` (int FK), `ApplicantID` (int FK), `Street` (string), `City` (string), `State` (string), `Zip` (string), `LandlordName` (string), `LandlordPhone` (string), `MoveInDate` (DateTime), `MoveOutDate` (DateTime?).
- `ApplicationStatus`: `Id` (int), `Name` (string - `Draft`, `Submitted`, `Returned`, `Approved`, `Denied`, `Withdrawn`).

#### ViewModels & DTOs
- `ApplicationWizardViewModel`: `ApplicationID`, `UnitID`, `CurrentStep` (1, 2, or 3), `IsReadOnly`, `ApplicantInfo` (`ApplicantViewModel`), `Residences` (`List<ResidenceHistoryViewModel>`).
- `ResidenceModalViewModel`: `ResidenceID`, `ApplicationID`, `Street`, `City`, `State`, `Zip`, `LandlordName`, `LandlordPhone`, `MoveInDate`, `MoveOutDate`.

### 5. Controllers & UI / Razor Components
- `ApplicationController`: `Create` (GET - starts application in `Draft`), `Wizard` (GET/POST - handles `Continue`, `Back`, step rendering), `SaveResidence` (POST partial modal), `DeleteResidence` (POST).
- Views & Partials: `Views/Application/Wizard.cshtml`, `Views/Application/Partials/_ApplicantInfoSection.cshtml`, `Views/Application/Partials/_ResidenceHistorySection.cshtml`, `Views/Application/Partials/_SummarySection.cshtml`, `Views/Application/Modals/_ResidenceModal.cshtml`.

### 6. Required Tests & Assertions
- **Unit Tests**:
  - `ApplicationWizardTests`: Assert `Continue` on Step 1 validates required fields (`Name`, `Phone`, `Email`, `CurrentAddress`) and advances `CurrentStep` from 1 to 2 when valid.
  - `ApplicationWizardTests`: Assert `Back` button navigates from Step 2 to Step 1 without invoking DB save.
- **Integration Tests**:
  - `ApplicationSecurityTests`: Assert HTTP POST to update an application in `Submitted` or `Approved` status returns HTTP 400/403.
  - `ResidenceModalTests`: Assert adding a residence history item updates the DB table and re-renders the Step 2 residence table partial.

---

## Version v0.4: [CORE] Application Submission, Active Lease Check & Database Application List Filtering

### 1. Objective
Implement final application submission, server-side active lease checks on submission, and the database-filtered Application List for Applicants and Property Managers.

### 2. Business Functions & User Stories
- **Application Submission**: Applicant clicks `Submit` on the Summary page. System transitions status from `Draft`/`Returned` to `Submitted`.
- **Active Lease Check at Submission**: When applicant clicks `Submit`, server verifies that the target Unit does not currently have an active lease covering today's date. If an active lease exists, reject submission with a user-friendly error message.
- **Application List**:
  - Filtered list of applications by `Status` and `Property`.
  - Filtering MUST be performed in SQL/database via EF Core `IQueryable`, NOT in memory.
  - Applicants see only their own submitted/draft applications; Property Managers see all applications across all properties.

### 3. Business Rules & Constraints
- Application submission must verify unit lease availability.
- Database query for application list MUST execute SQL `WHERE` clauses for filtering (`.Where(a => a.Unit.PropertyID == propertyId && a.ApplicationStatusID == statusId)`).

### 4. Models Needed
#### ViewModels & DTOs
- `ApplicationListFilterViewModel`: `SelectedPropertyID` (int?), `SelectedStatusID` (int?), `AvailableProperties` (SelectList), `AvailableStatuses` (SelectList).
- `ApplicationListItemViewModel`: `ApplicationID`, `ApplicantName`, `PropertyName`, `UnitNumber`, `SubmissionDate`, `StatusName`, `CanEdit`.

### 5. Controllers & UI / Razor Components
- `ApplicationController`: `Submit` (POST action with active lease verification).
- `ApplicationListController`: `Index` (GET with query string filters `propertyId` and `statusId`).
- Views: `Views/ApplicationList/Index.cshtml`, `Views/ApplicationList/_ApplicationFilterBar.cshtml`.

### 6. Required Tests & Assertions
- **Unit Tests**:
  - `LeaseValidationServiceTests`: Assert submission is rejected with error when unit has an active lease (`StartDate <= Today <= EndDate`).
- **Integration Tests**:
  - `ApplicationListDatabaseFilterTests`: Capture generated EF Core SQL log and assert SQL contains `WHERE` clauses for `PropertyID` and `StatusID`.
  - `ApplicationListAuthorizationTests`: Assert Applicant user receives ONLY their own applications in query results.

---

## Version v0.5: [CORE] Property Manager Review Workflow, Audit Timeline & 12-Month Lease Issuance

### 1. Objective
Build the Property Manager review modal (`Approve`, `Return`, `Deny`), status transition handling, status history timeline display, and automatic 12-month lease creation upon approval.

### 2. Business Functions & User Stories
- **Property Manager Review**: Property Manager opens a `Submitted` application and completes a review through a modal:
  - Select Outcome: `Approve`, `Return`, or `Deny`.
  - Comment: Optional for `Approve`, **MANDATORY** for `Return` and `Deny`.
- **Automatic Lease Issuance**: Upon `Approve`, server validates lease availability once more. If clear, it creates a `Lease` record for the unit with `StartDate = Today` and a 12-month term (`EndDate = Today.AddMonths(12)`).
- **Application Status History Timeline**: Application view displays a chronological history of status changes and review outcomes (Who, When, Outcome Status, Comment).
- **Applicant Withdrawal**: Applicant can withdraw a submitted application (status transitions to `Withdrawn`).

### 3. Business Rules & Constraints
- Terminal Application Statuses: `Approved`, `Denied`, `Withdrawn`.
- Comments are mandatory when outcome is `Return` or `Deny`.
- Approval check prevents double-leasing a unit.
- A unit whose lease term covers today (`StartDate <= Today <= EndDate`) is marked unavailable for new leases and applications.

### 4. Models Needed
#### Entity Framework Core Entities
- `Lease`: `Id` (int), `UnitID` (int FK), `ApplicationID` (int FK), `StartDate` (DateTime), `EndDate` (DateTime), `CreatedAt` (DateTime).
- `Review`: `Id` (int), `ApplicationID` (int FK), `UserID` (string FK), `ReviewDate` (DateTime), `OutcomeApplicationStatusID` (int FK), `Comment` (string).

#### ViewModels & DTOs
- `ReviewModalViewModel`: `ApplicationID`, `OutcomeStatusID`, `Comment` (Required if Return/Deny), `AvailableOutcomes` (SelectList: Approved, Returned, Denied).
- `StatusHistoryItemViewModel`: `ReviewDate`, `ReviewerName`, `FromStatusName`, `ToStatusName`, `Comment`.

### 5. Controllers & UI / Razor Components
- `ReviewController`: `ReviewModal` (GET partial), `CompleteReview` (POST action with lease creation logic).
- View Components: `Views/Shared/Components/StatusHistoryTimeline/Default.cshtml` (`StatusHistoryTimelineViewComponent`).
- Views & Modals: `Views/Review/_ReviewModal.cshtml`.

### 6. Required Tests & Assertions
- **Unit Tests**:
  - `ReviewWorkflowTests`: Assert `CompleteReview` fails model validation if outcome is `Return` or `Deny` and `Comment` is empty.
  - `LeaseGenerationTests`: Assert `Approve` outcome creates a `Lease` record with exact 12-month duration (`EndDate == StartDate.AddMonths(12)`).
- **Integration Tests**:
  - `DoubleLeasePreventionTests`: Assert approving an application for a unit that already has an active lease fails and returns an error message.
  - `StatusTimelineTests`: Assert review actions create a `Review` history record displayed in the `StatusHistoryTimelineViewComponent`.

---

## Version v0.6: [CORE MILESTONE] Bogus Idempotent Seeding & Core Automated Test Suite (100% Core Complete)

### 1. Objective
Implement idempotent database seeding using Bogus for .NET and execute the complete automated test suite verifying all core application requirements.

### 2. Business Functions & User Stories
- **Automated Data Seeding**: On application startup, seed the database using Bogus with:
  - Roles: `Applicant`, `PropertyManager`.
  - Accounts: Default Property Manager (`manager@realestate.com`) and Applicant (`applicant@realestate.com`).
  - Lookup Data: `UnitType` (Active and Inactive types), `ApplicationStatus` lookups.
  - Domain Data: Properties, Units, Applications in **every status** (`Draft`, `Submitted`, `Returned`, `Approved`, `Denied`, `Withdrawn`), and active Leases.
- **Idempotency**: Seeding script runs safely on every startup without creating duplicate records.

### 3. Business Rules & Constraints
- Seeding must use Bogus for realistic mock data generation.
- Database must contain applications in every status upon initial seed.

### 4. Models Needed
- `DbInitializer` / `BogusDataSeeder`: C# static seeding utility utilizing `Bogus.Faker`.

### 5. Required Tests & Assertions
- **Integration Suite**:
  - `SeedingIdempotencyTests`: Run `DbInitializer.SeedAsync()` twice; assert total row counts for Properties, Units, and Users remain unchanged.
  - `CoreRequirementCoverageTests`: Assert database contains at least one application for every `ApplicationStatus` (`Draft`, `Submitted`, `Returned`, `Approved`, `Denied`, `Withdrawn`).

> **MILESTONE VERIFICATION**: At v0.6, all non-bonus functional and technical requirements of the assessment are 100% complete, fully functional, and verified by unit/integration tests.

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
- **Security Unit Tests**:
  - `ManagerNotesSecurityTests`: Inspect all `Applicant` ViewModels via reflection and assert `ManagerNotes` property is completely absent.
- **Unit Tests**:
  - `DraftSoftValidationTests`: Assert section saves to DB with invalid email format when in Draft status, but Summary page flags error and disables Submit button.

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
- **Unit & Integration Tests**:
  - `PagedGridSqlTests`: Assert generated SQL contains `OFFSET` and `FETCH NEXT` clauses.
  - `ReviewQueueConcurrencyTests`: Assert claiming an already claimed application by a second manager returns HTTP 409 Conflict.

---

## Version v1.0: [BONUS MILESTONE] Multi-Applicant Co-Signing & Optimistic Concurrency Control

### 1. Objective
Implement multi-applicant support per application via email invitations (Bonus #5) and EF Core optimistic concurrency control (`RowVersion`) to prevent stale overwrites.

### 2. Business Functions & User Stories
- **Bonus #5 - Multi-Applicant Co-Signing**: Allow multiple applicants on a single application. Primary applicant invites co-applicant by email. Any linked applicant can view and edit sections.
- **Optimistic Concurrency**: Concurrent edits to the same section by two applicants detect stale data and reject the second save with a message asking the user to reload.

### 3. Business Rules & Constraints
- Section saves enforce EF Core `[Timestamp]` / `RowVersion` concurrency checks.
- Ownership checks verify current user is linked to the application via `ApplicationApplicant` junction entity.

### 4. Models Needed
#### Entity Framework Core Entities
- `ApplicationApplicant` (Junction): `Id` (int), `ApplicationID` (int FK), `ApplicantID` (int FK), `IsPrimary` (bool), `InvitationEmail` (string).
- Entity modification to `Application`: Add `byte[] RowVersion` concurrency token (`[Timestamp]`).

#### ViewModels & DTOs
- `InviteCoApplicantViewModel`: `ApplicationID`, `CoApplicantEmail`.

### 5. Controllers & UI / Razor Components
- `CoApplicantController`: `Invite` (POST action sending invitation link).
- Modals: `Views/CoApplicant/_InviteModal.cshtml`.

### 6. Required Tests & Assertions
- **Unit & Integration Tests**:
  - `OptimisticConcurrencyTests`: Simulate two simultaneous section saves with identical `RowVersion`; assert second save throws `DbUpdateConcurrencyException` and returns a HTTP 409 stale data reload message.
  - `MultiApplicantAccessTests`: Assert both primary and co-applicant can view and edit the application wizard sections.

---
