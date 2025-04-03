using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MailKit;
using MailKit.Net.Pop3;
using MailKit.Security;
using Xunit;

namespace CheckMail.App.Tests
{
    public class EmailUserDto
    {
        public string Login { get; set; }
        public string Email { get; set; }

        public string Password { get; set; }
    }

    [Trait("Category", "Integration")]
    public class MailTests : IDisposable
    {
        private IContainer _container;

        public MailTests()
        {
            _container = new ContainerBuilder()
                .WithImage("greenmail/standalone:2.0.1")
                .WithPortBinding(hostPort: 18666, containerPort: 3025)
                .WithPortBinding(hostPort: 18668, containerPort: 3110)
                .WithPortBinding(hostPort: 18667, containerPort: 8080)
                .WithName("theMailContainer")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8080))
                .Build();

            Task.WaitAll(_container.StartAsync());
        }

        [Fact]
        public async Task ShouldSendEmail()
        {
            var httpClient = new HttpClient
                { BaseAddress = new Uri("http://localhost:18667") };

            var usersResponseMessage = await httpClient.GetAsync("api/user");
            var usersResponseMessageString = await usersResponseMessage.Content.ReadAsStreamAsync();
            var users = await JsonSerializer.DeserializeAsync<IEnumerable<EmailUserDto>>(usersResponseMessageString);

            if (!users.Any())
            {
                var result1 = await httpClient.PostAsJsonAsync("api/user",
                    new EmailUserDto { Login = "user1", Email = "user1@localhost", Password = "1234" });
                Assert.True(result1.IsSuccessStatusCode);

                var result2 = await httpClient.PostAsJsonAsync("api/user",
                    new EmailUserDto { Login = "user2", Email = "user2@localhost", Password = "1234" });
                Assert.True(result2.IsSuccessStatusCode);
            }

            var mailStuff = new MailStuff("localhost", 18666);
            mailStuff.SendMail(
                "user1@localhost",
                "user2@localhost",
                "Test Mail",
                "This is a test mail"
            );

            using (var client = new Pop3Client(new ProtocolLogger("pop3.log")))
            {
                client.Connect("localhost", 18668, SecureSocketOptions.Auto);

                client.Authenticate("user2", "1234");

                for (int i = 0; i < client.Count; i++)
                {
                    var message = client.GetMessage(i);

                    Assert.NotNull(message);
                    Assert.Equal("Test Mail", message.Subject);
                    Assert.Equal("This is a test mail", message.TextBody?.Trim());
                }

                client.Disconnect(true);
            }
        }

        public void Dispose()
        {
            if (_container != null)
            {
                _container.StopAsync().GetAwaiter().GetResult();
                _container.DisposeAsync().GetAwaiter().GetResult();
            }
        }
    }
}