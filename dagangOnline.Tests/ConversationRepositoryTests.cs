using System;
using System.Linq;
using System.Threading.Tasks;
using dagangOnline.Data;
using dagangOnline.Domain.Chat;
using dagangOnline.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace dagangOnline.Tests;

public class ConversationRepositoryTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Can_Create_And_Read_Conversation()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new ConversationRepository(context);
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            CustomerId = "cust1",
            Status = ConversationStatus.Open,
            Priority = ConversationPriority.Normal,
            Intent = IntentType.product_information
        };

        // Act
        await repository.AddAsync(conversation);
        var fetched = await repository.GetByIdAsync(conversation.Id);

        // Assert
        Assert.NotNull(fetched);
        Assert.Equal("cust1", fetched.CustomerId);
        Assert.Equal(ConversationStatus.Open, fetched.Status);
        Assert.Equal(ConversationPriority.Normal, fetched.Priority);
    }

    [Fact]
    public async Task Can_Update_State_Transition()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new ConversationRepository(context);
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            CustomerId = "cust2",
            Status = ConversationStatus.Open
        };
        await repository.AddAsync(conversation);

        // Act
        var fetched = await repository.GetByIdAsync(conversation.Id, trackChanges: true);
        fetched!.Status = ConversationStatus.WaitingForAgent;
        await repository.UpdateAsync(fetched);

        var updated = await repository.GetByIdAsync(conversation.Id);

        // Assert
        Assert.Equal(ConversationStatus.WaitingForAgent, updated!.Status);
    }

    [Fact]
    public async Task Can_Add_Message_To_Conversation()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new ConversationRepository(context);
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            CustomerId = "cust3",
            Status = ConversationStatus.Open
        };
        await repository.AddAsync(conversation);

        var message = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Content = "Hello",
            SenderType = SenderType.Customer
        };

        // Act
        await repository.AddMessageAsync(message);
        var fetched = await repository.GetByIdAsync(conversation.Id);

        // Assert
        Assert.NotNull(fetched);
        Assert.Single(fetched.Messages);
        Assert.Equal("Hello", fetched.Messages.First().Content);
        Assert.Equal(SenderType.Customer, fetched.Messages.First().SenderType);
    }
}
