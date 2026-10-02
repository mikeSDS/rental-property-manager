# Rental Application Manager

This App was craeted as a Tech Assessment 

Mike Volo
9/30/2026

## Useful Info

- see PROJECT_PLAN.md for breakdown of each version and features
- see /docs folder for common problems and solutions, and other useful information



# Environment Setup Instruction


- I am using Visual Studio 2026
- Installed with ASP Web and Data Storage


- git pull 

## from Terminal:
	dotnet build
	dotnet run
	
	- verify website is up and running at : http://localhost:5062/
	
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

Run all tests 
	- dotnet test

Run tests with verbose output 
	 - dotnet test --verbosity detailed

### Database Seeding

+ The database is seeded with initial data when the application starts, without duplicating existing data. 
  + This is handled in the `DbInitializer.cs` file.
+ If you need to reset the database and reseed it, you can run the sql script located in the `docs` folder. 
  +This script will clear the database without clearing the users