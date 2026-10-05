# Rental Application Manager

This App was craeted as a Tech Assessment 

Mike Volo
9/30/2026

## Useful Info

- see PROJECT_PLAN.md for breakdown of each version and features
- see /docs folder for common problems and solutions, and other useful information

slides deck with v1.0 open questions
https://docs.google.com/presentation/d/1wvxNRp0hJ3xo4Q-osjqXx9spaNiiQlaT/edit?usp=sharing&ouid=114661631735137713887&rtpof=true&sd=true


# Environment Setup Instruction


## Prerequisites
- .NET 10 SDK (project targets `net10.0`)
- SQL Server LocalDB (installed with the Visual Studio "Data storage and processing" workload)
- Visual Studio 2026 with the "ASP.NET and web development" workload (optional; the CLI also works)
- Bogus, xUnit and EF Core packages are restored automatically by `dotnet restore`

## Get the code
	git clone https://github.com/mikeSDS/rental-property-manager
	cd rental-property-manager

## Configuration
- The default connection string is in `RentalPropertyManager/appsettings.json` (`DefaultConnection`) and points at `(localdb)\mssqllocaldb`.
- To use a different SQL Server, override it without editing the file:
	dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your connection string>" --project RentalPropertyManager

## from Terminal (run from the repository root):
	dotnet restore
	dotnet build
	dotnet run --project RentalPropertyManager

	- verify website is up and running at : http://localhost:5062/ (or https://localhost:7250/)
	- first run: trust the dev certificate if prompted:  dotnet dev-certs https --trust
	- on startup the app applies EF migrations and seeds data automatically (no manual database step needed)

## Seeded accounts (created on first run)
	- Property Manager: manager@realestate.com / ManagerPassword123!
	- Applicant:        applicant@realestate.com / ApplicantPassword123!
	- Also seeded: properties, units, unit types, and applications in every status with leases, reviews and history

## Database creation/  migration  - (to be included in run above)
	- Install ef tool
	
	dotnet tool install --global dotnet-ef

	- see pending migrations
	dotnet ef migrations list

	- Run db migration
	dotnet ef database update


# User register and Login

	- After creating a user, it must email verified to be able to Login
	- to skip setting up email verification, change this db filed to true for the new user
	
	AspNetUsers.EmailConfirmed


# Testing

This project uses **xUnit** for unit and integration tests.


### Prerequisites for Testing
When you run `dotnet restore`, the following test packages are automatically installed:
- xUnit (test framework)
- Microsoft.AspNetCore.Mvc.Testing (integration testing)
- Microsoft.EntityFrameworkCore.InMemory (test database isolation)

### Running Tests

Run all tests (from the repository root, with the app stopped so build output is not locked)
	- dotnet test RentalPropertyManager.Tests

Run tests with verbose output 
	 - dotnet test --verbosity detailed

### Database Seeding

+ The database is seeded with initial data when the application starts, without duplicating existing data. 
  + This is handled in the `DbInitializer.cs` file.
+ If you need to reset the database and reseed it, you can run the sql script located in the `docs` folder. 
  +This script will clear the database without clearing the users


## Open Question for Clients / Team decisions for this release
 ### Client Relationship and Priorities
   **** What is existing system we are replacing?  *****
       -  what currently works?
	   -  work doesn't work
	 - we can't assume that the design we were given answers the question of:
	      - how do we know it is better?
		  -  It must save the users time and/or money.  
	-  what are project priorities?
	  -  For this assessment, Assumed to be in this order:
	   - Schedule (7 days), Compliance & Integrity, Feature Completeness, Quality, Security, Project Cost, Design/UX, Performance
 ### Current questions for Prod release
	- delete- recommend that all deletes are soft-deletes and can be undone
    - db integrity
		- there should be a unique key on Property+Unitname
		- but only makes sense after soft-delete was implemented
	- Home page	
		menu ?
	- Admin Role
      - to create / disable / reset accounts
	-  12 month lease doc template
	   -  tool for creating legal docs
	-  what are the possible Types ?
       - allow managers to create types?
	-  Branding, Logo, NAme, CSS styles
	- property address seems to be missing ?
	- In the bonus #3, which objects do we want manager notes to be seen on ?
		Assume on Application, Lease, Property, Unit
	- In bonus #5, "Ownership checks" - does this mean "application checks"  ?
	- In bonus #5, we would need a way for an applicant to enter email address of the other applicant
		- for security, sharing an application would be by invitation only, and they would have to know the email address
    - No income data required, or income verification 
    - could there be more than one company in future?
	 
## Prod System and Deploy Questions  
    - for prod, we must turn off th auto db bogus data seeding
		- it should be off by default, and moved to the readme file for developer manual seeding as needed.
    - server deployment options
	- existing Azure instance?
	- note only tested on Windows 11 Pro
	- database to choose ?
    - Integration with existing website - button access
    - Existing data upload process 
		- how many current units?
		- what is existing system?
	- User Identity / email confirm enable

## Future Features Discussion  
    - about page with versioning, help, and how to contact someone for help
	- Income data for applicants 
	    - Applicant upload income verification docs  - security a much bigger concern then...
		- outsource or not store it -  or need security discussion
	- Lease generation and signature process
	- Renewals 
    - More detailed review process steps
	   - check previous landlords
	   - call / check history per previous address
	- sort Product features into future versions Product plan