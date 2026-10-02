using System;
using System.Reflection;
using System.Linq;
using RentalPropertyManager.ViewModels;
using Xunit;

namespace RentalPropertyManager.Tests
{
    /// <summary>
    /// Tests for form submission routes and validation to catch route/endpoint mismatches.
    /// These tests would have caught the bug where the form was calling /api/properties/{id}
    /// instead of /properties/{id}/edit, which prevented saves from working.
    /// </summary>
    public class PropertiesControllerFormTests
    {
        [Fact]
        public void PropertiesController_Has_Create_POST_Endpoint()
        {
            // Arrange
            var controllerType = typeof(RentalPropertyManager.Controllers.PropertiesController);

            // Act
            var createMethod = controllerType.GetMethod("Create", 
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(PropertyFormViewModel) },
                null);

            // Assert
            Assert.NotNull(createMethod);
            var httpPostAttributes = createMethod.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpPostAttribute), false);
            Assert.NotEmpty(httpPostAttributes);

            var httpPostAttr = httpPostAttributes[0] as Microsoft.AspNetCore.Mvc.HttpPostAttribute;
            Assert.NotNull(httpPostAttr);
            // Should have route "create" which maps to POST /properties/create
            Assert.Equal("create", httpPostAttr.Template);
        }

        [Fact]
        public void PropertiesController_Has_Edit_POST_Endpoint()
        {
            // Arrange
            var controllerType = typeof(RentalPropertyManager.Controllers.PropertiesController);

            // Act
            var editMethod = controllerType.GetMethod("Edit",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(int), typeof(PropertyFormViewModel) },
                null);

            // Assert
            Assert.NotNull(editMethod);
            var httpPostAttributes = editMethod.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpPostAttribute), false);
            Assert.NotEmpty(httpPostAttributes);

            var httpPostAttr = httpPostAttributes[0] as Microsoft.AspNetCore.Mvc.HttpPostAttribute;
            Assert.NotNull(httpPostAttr);
            // Should have route "{id}/edit" which maps to POST /properties/{id}/edit
            Assert.Equal("{id}/edit", httpPostAttr.Template);
        }

        [Fact]
        public void PropertiesController_Has_Delete_POST_Endpoint()
        {
            // Arrange
            var controllerType = typeof(RentalPropertyManager.Controllers.PropertiesController);

            // Act
            var deleteMethod = controllerType.GetMethod("Delete",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(int) },
                null);

            // Assert
            Assert.NotNull(deleteMethod);
            var httpPostAttributes = deleteMethod.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpPostAttribute), false);
            Assert.NotEmpty(httpPostAttributes);

            var httpPostAttr = httpPostAttributes[0] as Microsoft.AspNetCore.Mvc.HttpPostAttribute;
            Assert.NotNull(httpPostAttr);
            // Should have route "{id}/delete" which maps to POST /properties/{id}/delete
            Assert.Equal("{id}/delete", httpPostAttr.Template);
        }

        [Fact]
        public void PropertiesController_Delete_Uses_POST_NotDELETE()
        {
            // Arrange & Act
            var controllerType = typeof(RentalPropertyManager.Controllers.PropertiesController);
            var deleteMethod = controllerType.GetMethod("Delete",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(int) },
                null);

            // Assert
            Assert.NotNull(deleteMethod);

            // Should NOT have HttpDeleteAttribute
            var httpDeleteAttributes = deleteMethod.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpDeleteAttribute), false);
            Assert.Empty(httpDeleteAttributes);

            // Should have HttpPostAttribute instead
            var httpPostAttributes = deleteMethod.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpPostAttribute), false);
            Assert.NotEmpty(httpPostAttributes);
        }

        [Fact]
        public void PropertyFormViewModel_Name_IsRequired()
        {
            // Arrange
            var nameProperty = typeof(PropertyFormViewModel).GetProperty("Name");

            // Act
            var requiredAttribute = nameProperty.GetCustomAttribute<System.ComponentModel.DataAnnotations.RequiredAttribute>();

            // Assert
            Assert.NotNull(requiredAttribute);
        }

        [Fact]
        public void PropertyFormViewModel_StreetAddress_IsOptional()
        {
            // Arrange
            var streetAddressProperty = typeof(PropertyFormViewModel).GetProperty("StreetAddress");

            // Act
            var requiredAttribute = streetAddressProperty.GetCustomAttribute<System.ComponentModel.DataAnnotations.RequiredAttribute>();

            // Assert
            Assert.Null(requiredAttribute);
            // Should be nullable
            Assert.True(streetAddressProperty.PropertyType.Name.Contains("String") || 
                        (streetAddressProperty.PropertyType.IsGenericType && 
                         streetAddressProperty.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)));
        }

        [Fact]
        public void PropertyFormViewModel_City_IsOptional()
        {
            // Arrange
            var cityProperty = typeof(PropertyFormViewModel).GetProperty("City");

            // Act
            var requiredAttribute = cityProperty.GetCustomAttribute<System.ComponentModel.DataAnnotations.RequiredAttribute>();

            // Assert
            Assert.Null(requiredAttribute);
        }

        [Fact]
        public void PropertyFormViewModel_State_IsOptional()
        {
            // Arrange
            var stateProperty = typeof(PropertyFormViewModel).GetProperty("State");

            // Act
            var requiredAttribute = stateProperty.GetCustomAttribute<System.ComponentModel.DataAnnotations.RequiredAttribute>();

            // Assert
            Assert.Null(requiredAttribute);
        }

        [Fact]
        public void PropertyFormViewModel_ZipCode_IsOptional()
        {
            // Arrange
            var zipCodeProperty = typeof(PropertyFormViewModel).GetProperty("ZipCode");

            // Act
            var requiredAttribute = zipCodeProperty.GetCustomAttribute<System.ComponentModel.DataAnnotations.RequiredAttribute>();

            // Assert
            Assert.Null(requiredAttribute);
        }

        [Fact]
        public void PropertyFormViewModel_CanBeValidated_WithOnlyRequiredFields()
        {
            // Arrange
            var viewModel = new PropertyFormViewModel
            {
                Name = "Test Property"
                // All address fields left null/empty
            };

            // Act
            var context = new System.ComponentModel.DataAnnotations.ValidationContext(viewModel);
            var validationResults = new System.Collections.Generic.List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(viewModel, context, validationResults, true);

            // Assert
            Assert.True(isValid, $"Validation failed: {string.Join(", ", validationResults.Select(r => r.ErrorMessage))}");
        }

        [Fact]
        public void PropertyFormViewModel_FailsValidation_WithoutName()
        {
            // Arrange
            var viewModel = new PropertyFormViewModel
            {
                Name = "" // Empty - required field
            };

            // Act
            var context = new System.ComponentModel.DataAnnotations.ValidationContext(viewModel);
            var validationResults = new System.Collections.Generic.List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(viewModel, context, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.NotEmpty(validationResults);
            Assert.True(validationResults.Any(r => r.MemberNames.Contains("Name")));
        }

        [Fact]
        public void PropertyFormViewModel_Respects_StringLengthLimits()
        {
            // Arrange
            var viewModel = new PropertyFormViewModel
            {
                Name = "Valid Name",
                StreetAddress = new string('A', 201) // Exceeds 200 character limit
            };

            // Act
            var context = new System.ComponentModel.DataAnnotations.ValidationContext(viewModel);
            var validationResults = new System.Collections.Generic.List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(viewModel, context, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.NotEmpty(validationResults);
            Assert.True(validationResults.Any(r => r.MemberNames.Contains("StreetAddress")));
        }
    }
}
