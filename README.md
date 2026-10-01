# Rental Application Manager

This App was craeted as a Tech Assessment 

Mike Volo
9/30/2026


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
	dotnet test

Run tests with verbose output
	dotnet test --verbosity detailed


### Installs for Testing:
Terminal
	inside /tests folder:
		dotnet add package xunit
		dotnet add package xunit.runner.visualstudio
		dotnet add package Microsoft.EntityFrameworkCore.InMemorycd  --version 10.0.0