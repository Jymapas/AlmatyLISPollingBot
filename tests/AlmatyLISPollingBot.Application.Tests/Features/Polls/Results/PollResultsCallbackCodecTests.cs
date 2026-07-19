using AlmatyLISPollingBot.Application.Features.Polls.Results;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests.Features.Polls.Results;

public sealed class PollResultsCallbackCodecTests
{
    [Fact]
    public void Encode_ShouldFitTelegramCallbackLimitAndRoundTrip()
    {
        var sessionId = Guid.NewGuid();
        var optionId = Guid.NewGuid();

        var encoded = PollResultsCallbackCodec.Encode(sessionId, optionId, long.MaxValue, exclude: true);

        encoded.Length.Should().BeLessThanOrEqualTo(64);
        PollResultsCallbackCodec.TryDecode(encoded, out var decodedSessionId, out var decodedOptionId, out var voterId, out var exclude).Should().BeTrue();
        decodedSessionId.Should().Be(sessionId);
        decodedOptionId.Should().Be(optionId);
        voterId.Should().Be(long.MaxValue);
        exclude.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("x|x|x|x")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAA|AAAAAAAAAAAAAAAAAAAAAA|AAAAAAAAAAA|2")]
    public void TryDecode_ShouldRejectMalformedOrUnsupportedPayload(string payload)
    {
        PollResultsCallbackCodec.TryDecode(payload, out _, out _, out _, out _).Should().BeFalse();
    }
}
