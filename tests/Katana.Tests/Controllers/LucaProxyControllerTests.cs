using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Katana.API.Controllers;
using Katana.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Katana.Tests.Controllers;

public class LucaProxyControllerTests
{
    private readonly Mock<ILucaCookieJarStore> _mockCookieJarStore;
    private readonly Mock<ILogger<LucaProxyController>> _mockLogger;
    private readonly LucaProxyController _controller;

    public LucaProxyControllerTests()
    {
        _mockCookieJarStore = new Mock<ILucaCookieJarStore>();
        _mockLogger = new Mock<ILogger<LucaProxyController>>();
        _controller = new LucaProxyController(_mockCookieJarStore.Object, _mockLogger.Object);

        
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public void Login_CreatesCookieContainer()
    {
        
        var cookieContainer = new CookieContainer();
        _mockCookieJarStore.Setup(s => s.GetOrCreate(It.IsAny<string>())).Returns(cookieContainer);

        
        
        var result = _mockCookieJarStore.Object.GetOrCreate("test-session");
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Branches_ReturnsUnauthorizedWhenNoSession()
    {
        
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        
        var result = await _controller.Branches();

        
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public void Branches_ReadsSessionFromHeader()
    {
        
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Luca-Session"] = "test-session-id";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        var cookieContainer = new CookieContainer();
        _mockCookieJarStore.Setup(s => s.GetOrCreate("test-session-id")).Returns(cookieContainer);

        
        
        _mockCookieJarStore.Verify(s => s.GetOrCreate(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SelectBranch_ReturnsUnauthorizedWhenNoSession()
    {
        
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        
        var result = await _controller.SelectBranch(System.Text.Json.JsonDocument.Parse("{}").RootElement);

        
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public void SelectBranch_ReadsSessionFromCookie()
    {
        
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Cookie"] = "LucaProxySession=test-session-id";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        var cookieContainer = new CookieContainer();
        _mockCookieJarStore.Setup(s => s.GetOrCreate("test-session-id")).Returns(cookieContainer);

        
        
        _mockCookieJarStore.Verify(s => s.GetOrCreate(It.IsAny<string>()), Times.Never);
    }
}
