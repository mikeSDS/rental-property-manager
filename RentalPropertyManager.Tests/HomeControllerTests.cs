using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using RentalPropertyManager.Controllers;
using RentalPropertyManager.Models;
using Xunit;

namespace RentalPropertyManager.Tests
{
    public class HomeControllerTests
    {
        [Fact]
        public void Index_ReturnsViewResult()
        {
            var controller = new HomeController();
            var result = controller.Index();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Privacy_ReturnsViewResult()
        {
            var controller = new HomeController();
            var result = controller.Privacy();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Error_ReturnsViewResult()
        {
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(hc => hc.TraceIdentifier).Returns("test-trace-id");

            var controller = new HomeController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = mockHttpContext.Object
                }
            };
            var result = controller.Error();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Error_ViewModelHasRequestId()
        {
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(hc => hc.TraceIdentifier).Returns("test-trace-id");

            var controller = new HomeController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = mockHttpContext.Object
                }
            };
            var actionResult = controller.Error() as ViewResult;
            Assert.NotNull(actionResult?.Model);
            Assert.IsType<ErrorViewModel>(actionResult.Model);
            var model = actionResult.Model as ErrorViewModel;
            Assert.Equal("test-trace-id", model?.RequestId);
        }
    }
}
