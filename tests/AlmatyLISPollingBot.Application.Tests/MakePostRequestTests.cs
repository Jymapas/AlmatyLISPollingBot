using AlmatyLISPollingBot.Application.Features.MakePost;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests;

public sealed class MakePostRequestTests
{
    [Fact]
    public void Parse_ShouldReturnExactlyTwoDistinctIds()
    {
        var request = MakePostRequest.Parse("12, 99");

        request.TournamentIds.Should().BeEquivalentTo(new[] { 12, 99 });
    }
}
