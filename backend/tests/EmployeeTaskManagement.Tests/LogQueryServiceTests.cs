using EmployeeTaskManagement.API.Logging;
using EmployeeTaskManagement.API.Services;
using EmployeeTaskManagement.Application.DTOs.Logging;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Xunit;

namespace EmployeeTaskManagement.Tests
{
    public class LogQueryServiceTests
    {
        private sealed class FakeEnv(string root) : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = Environments.Development;
            public string ApplicationName { get; set; } = "Test";
            public string ContentRootPath { get; set; } = root;
            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }

        [Fact]
        public async Task QueryAsync_FiltersByLevelUserIdTaskIdAndAction()
        {
            var dir = Path.Combine(Path.GetTempPath(), "tf-log-query-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var file = Path.Combine(dir, $"app-{DateTime.UtcNow:yyyyMMdd}.log");
                var lines = new[]
                {
                    JsonSerializer.Serialize(new { timestamp = DateTime.UtcNow.AddMinutes(-2).ToString("o"), level = "Information", category = "TaskService", message = "Task created", UserId = 2, TaskId = 15, Action = "Create", CorrelationId = "c1" }),
                    JsonSerializer.Serialize(new { timestamp = DateTime.UtcNow.AddMinutes(-1).ToString("o"), level = "Warning", category = "TaskService", message = "Access denied on task", UserId = 3, TaskId = 15, Action = "AccessDenied", CorrelationId = "c2" }),
                    JsonSerializer.Serialize(new { timestamp = DateTime.UtcNow.ToString("o"), level = "Error", category = "GlobalException", message = "Failure on task 22", UserId = 2, Path = "/api/tasks/22", CorrelationId = "c3" })
                };
                await File.WriteAllLinesAsync(file, lines);

                var service = new LogQueryService(Options.Create(new FileLoggingOptions { Directory = dir }), new FakeEnv(dir));

                var byWarning = await service.QueryAsync(new SystemLogQueryParameters { Level = "Warning" });
                Assert.Single(byWarning.Items);
                Assert.Equal("c2", byWarning.Items.First().CorrelationId);

                var byTask = await service.QueryAsync(new SystemLogQueryParameters { TaskId = 15 });
                Assert.Equal(2, byTask.TotalCount);

                var byUserAndAction = await service.QueryAsync(new SystemLogQueryParameters { UserId = 2, Action = "Create" });
                Assert.Single(byUserAndAction.Items);
                Assert.Equal("c1", byUserAndAction.Items.First().CorrelationId);
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }
    }
}
