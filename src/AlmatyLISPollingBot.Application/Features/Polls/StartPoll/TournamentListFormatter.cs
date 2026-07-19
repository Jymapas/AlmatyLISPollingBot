using System.Globalization;
using System.Net;
using System.Text;
using AlmatyLISPollingBot.Application.Abstractions.ExchangeRates;
using AlmatyLISPollingBot.Application.Contracts.ExchangeRates;
using AlmatyLISPollingBot.Application.Contracts.Tournaments;

namespace AlmatyLISPollingBot.Application.Features.Polls.StartPoll;

public sealed class TournamentListFormatter
{
    private const int TelegramMessageLengthLimit = 4096;
    private static readonly string[] NumberEmoji = { "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣" };
    private static readonly string[] CurrencyPriority = { "KZT", "RUB", "USD" };

    private readonly IExchangeRateProvider exchangeRateProvider;

    public TournamentListFormatter(IExchangeRateProvider exchangeRateProvider)
    {
        this.exchangeRateProvider = exchangeRateProvider;
    }

    public async Task<TournamentListFormattingResult> FormatAsync(
        IReadOnlyList<PollTournamentCandidate> candidates,
        TournamentIdDisplayMode tournamentIdDisplayMode,
        TournamentPaymentCategoriesDisplayMode paymentCategoriesDisplayMode,
        CancellationToken cancellationToken)
    {
        return await FormatAsync(
            candidates,
            tournamentIdDisplayMode,
            paymentCategoriesDisplayMode,
            TournamentDateRangeDisplayMode.WithoutDateRange,
            timeZone: null,
            cancellationToken);
    }

    public async Task<TournamentListFormattingResult> FormatAsync(
        IReadOnlyList<PollTournamentCandidate> candidates,
        TournamentIdDisplayMode tournamentIdDisplayMode,
        TournamentPaymentCategoriesDisplayMode paymentCategoriesDisplayMode,
        TournamentDateRangeDisplayMode dateRangeDisplayMode,
        TimeZoneInfo? timeZone,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (tournamentIdDisplayMode is not (TournamentIdDisplayMode.WithTournamentId or TournamentIdDisplayMode.WithoutTournamentId))
        {
            throw new ArgumentOutOfRangeException(nameof(tournamentIdDisplayMode), tournamentIdDisplayMode, "Unsupported tournament ID display mode.");
        }

        if (paymentCategoriesDisplayMode is not (TournamentPaymentCategoriesDisplayMode.All or TournamentPaymentCategoriesDisplayMode.PrimaryOnly))
        {
            throw new ArgumentOutOfRangeException(
                nameof(paymentCategoriesDisplayMode),
                paymentCategoriesDisplayMode,
                "Unsupported tournament payment categories display mode.");
        }

        if (dateRangeDisplayMode is not (TournamentDateRangeDisplayMode.WithoutDateRange or TournamentDateRangeDisplayMode.WithDateRange))
        {
            throw new ArgumentOutOfRangeException(nameof(dateRangeDisplayMode), dateRangeDisplayMode, "Unsupported tournament date range display mode.");
        }

        if (dateRangeDisplayMode == TournamentDateRangeDisplayMode.WithDateRange && timeZone is null)
        {
            throw new ArgumentNullException(nameof(timeZone));
        }

        var selectedCurrencies = candidates
            .Select(x => SelectPaymentCategories(x.Tournament))
            .Where(x => x.Count > 0)
            .Select(x => x[0].Currency.Trim().ToUpperInvariant())
            .Where(x => !string.Equals(x, "KZT", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var rates = new Dictionary<string, ExchangeRateQuote?>(StringComparer.OrdinalIgnoreCase);
        foreach (var currency in selectedCurrencies)
        {
            rates[currency] = await exchangeRateProvider.GetKztRateAsync(currency, cancellationToken);
        }

        var hasUnconvertedPrices = false;
        var entries = new List<string>(candidates.Count);
        for (var index = 0; index < candidates.Count; index++)
        {
            var candidate = candidates[index];
            entries.Add(FormatCandidate(
                candidate,
                index,
                SelectPaymentCategories(candidate.Tournament),
                rates,
                tournamentIdDisplayMode,
                paymentCategoriesDisplayMode,
                dateRangeDisplayMode,
                timeZone,
                ref hasUnconvertedPrices));
        }

        return new TournamentListFormattingResult(Paginate(entries), hasUnconvertedPrices);
    }

    private static string FormatCandidate(
        PollTournamentCandidate candidate,
        int index,
        IReadOnlyList<TournamentPaymentCategory> paymentCategories,
        IReadOnlyDictionary<string, ExchangeRateQuote?> rates,
        TournamentIdDisplayMode tournamentIdDisplayMode,
        TournamentPaymentCategoriesDisplayMode paymentCategoriesDisplayMode,
        TournamentDateRangeDisplayMode dateRangeDisplayMode,
        TimeZoneInfo? timeZone,
        ref bool hasUnconvertedPrices)
    {
        var tournament = candidate.Tournament;
        var builder = new StringBuilder();
        builder.Append(GetCandidateNumber(index));
        builder.Append(" <a href=\"https://rating.chgk.info/tournament/");
        builder.Append(tournament.Id.ToString(CultureInfo.InvariantCulture));
        builder.Append("\"><b>");
        builder.Append(Escape(TournamentTitleNormalizer.Normalize(tournament.Title)));
        builder.Append("</b></a>\n");
        if (tournamentIdDisplayMode == TournamentIdDisplayMode.WithTournamentId)
        {
            builder.Append("<b>ID:</b> <code>");
            builder.Append(tournament.Id.ToString(CultureInfo.InvariantCulture));
            builder.Append("</code>\n");
        }
        builder.Append("<b>Редакторы:</b> ");
        builder.Append(Escape(FormatEditors(tournament.Editors)));
        builder.Append("\n<b>Вопросы:</b> ");
        builder.Append(tournament.QuestionCount == 0 ? "не указано" : tournament.QuestionCount.ToString(CultureInfo.InvariantCulture));
        builder.Append("   <b>Сложность:</b> ");
        builder.Append(tournament.DifficultyForecast?.ToString("0.##", CultureInfo.InvariantCulture) ?? "не указана");

        if (dateRangeDisplayMode == TournamentDateRangeDisplayMode.WithDateRange)
        {
            builder.Append("\n<b>Период:</b> ");
            builder.Append(FormatDateRange(tournament.DateStart, tournament.DateEnd, timeZone!));
        }

        if (paymentCategories.Count > 0)
        {
            builder.Append('\n');
            builder.Append(FormatPaymentCategories(
                paymentCategories,
                paymentCategoriesDisplayMode,
                rates,
                ref hasUnconvertedPrices));
        }

        if (candidate.IsAvailableAtFirstSlot && !candidate.IsAvailableAtSecondSlot)
        {
            builder.Append("\n❗️ <b>Только первым</b>");
        }
        else if (!candidate.IsAvailableAtFirstSlot && candidate.IsAvailableAtSecondSlot)
        {
            builder.Append("\n❗️ <b>Только вторым</b>");
        }

        if (candidate.IsExcluded)
        {
            builder.Append("\n🚫 <b>Исключён</b>");
        }

        return builder.ToString();
    }

    private static string FormatDateRange(DateTimeOffset dateStart, DateTimeOffset dateEnd, TimeZoneInfo timeZone)
    {
        var start = TimeZoneInfo.ConvertTime(dateStart, timeZone);
        var end = TimeZoneInfo.ConvertTime(dateEnd, timeZone);

        return string.Concat(
            start.ToString("dd.MM HH:mm", CultureInfo.InvariantCulture),
            " — ",
            end.ToString("dd.MM HH:mm", CultureInfo.InvariantCulture));
    }

    private static string GetCandidateNumber(int index)
    {
        return index < NumberEmoji.Length
            ? NumberEmoji[index]
            : string.Concat((index + 1).ToString(CultureInfo.InvariantCulture), ".");
    }

    private static IReadOnlyList<TournamentPaymentCategory> SelectPaymentCategories(TournamentDetails tournament)
    {
        var groups = tournament.PaymentCategories
            .Where(x => !string.IsNullOrWhiteSpace(x.Currency))
            .GroupBy(x => x.Currency.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var currencyCode in CurrencyPriority)
        {
            var preferredGroup = groups.FirstOrDefault(x => string.Equals(x.Key, currencyCode, StringComparison.OrdinalIgnoreCase));
            if (preferredGroup is not null)
            {
                return preferredGroup.ToArray();
            }
        }

        return groups.FirstOrDefault()?.ToArray() ?? Array.Empty<TournamentPaymentCategory>();
    }

    private static string FormatPaymentCategories(
        IReadOnlyList<TournamentPaymentCategory> paymentCategories,
        TournamentPaymentCategoriesDisplayMode paymentCategoriesDisplayMode,
        IReadOnlyDictionary<string, ExchangeRateQuote?> rates,
        ref bool hasUnconvertedPrices)
    {
        var categoriesToDisplay = paymentCategoriesDisplayMode == TournamentPaymentCategoriesDisplayMode.PrimaryOnly
            ? paymentCategories.Take(1).ToArray()
            : paymentCategories;
        var builder = new StringBuilder();
        for (var index = 0; index < categoriesToDisplay.Count; index++)
        {
            var category = categoriesToDisplay[index];
            var price = FormatPrice(category, rates, ref hasUnconvertedPrices);
            var label = FormatCategoryLabel(category, index == 0);

            if (index == 0)
            {
                builder.Append("<b>Стоимость:</b> ");
                if (!string.IsNullOrWhiteSpace(label))
                {
                    builder.Append(Escape(label));
                    builder.Append(" — ");
                }
            }
            else
            {
                builder.Append(Escape(label));
                builder.Append(" — ");
            }

            builder.Append(price);
            if (index < categoriesToDisplay.Count - 1)
            {
                builder.Append('\n');
            }
        }

        return builder.ToString();
    }

    private static string FormatPrice(
        TournamentPaymentCategory category,
        IReadOnlyDictionary<string, ExchangeRateQuote?> rates,
        ref bool hasUnconvertedPrices)
    {
        var currency = category.Currency.Trim().ToUpperInvariant();
        var sourceAmount = string.Concat(
            category.Amount.ToString("0.##", CultureInfo.InvariantCulture),
            GetCurrencySuffix(currency));

        if (string.Equals(currency, "KZT", StringComparison.Ordinal))
        {
            return sourceAmount;
        }

        if (!rates.TryGetValue(currency, out var rate) || rate is null)
        {
            hasUnconvertedPrices = true;
            return sourceAmount;
        }

        return string.Concat(
            sourceAmount,
            " (≈",
            rate.ConvertToTenge(category.Amount).ToString("0", CultureInfo.InvariantCulture),
            "₸)");
    }

    private static string FormatCategoryLabel(TournamentPaymentCategory category, bool isFirstCategory)
    {
        if (string.IsNullOrWhiteSpace(category.Reason)
            || string.Equals(category.Reason, "по умолчанию", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return category.Reason;
    }

    private static string FormatEditors(IReadOnlyList<TournamentEditor> editors)
    {
        var names = editors
            .Select(x => x.DisplayName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        return names.Length == 0 ? "не указаны" : string.Join(", ", names);
    }

    private static IReadOnlyList<string> Paginate(IReadOnlyList<string> entries)
    {
        var pages = new List<string>();
        var currentPage = new StringBuilder();

        foreach (var entry in entries)
        {
            var separatorLength = currentPage.Length == 0 ? 0 : 2;
            if (currentPage.Length > 0 && currentPage.Length + separatorLength + entry.Length > TelegramMessageLengthLimit)
            {
                pages.Add(currentPage.ToString());
                currentPage.Clear();
            }

            if (entry.Length > TelegramMessageLengthLimit)
            {
                foreach (var part in SplitLongEntry(entry))
                {
                    if (currentPage.Length > 0)
                    {
                        pages.Add(currentPage.ToString());
                        currentPage.Clear();
                    }

                    pages.Add(part);
                }

                continue;
            }

            if (currentPage.Length > 0)
            {
                currentPage.Append("\n\n");
            }

            currentPage.Append(entry);
        }

        if (currentPage.Length > 0)
        {
            pages.Add(currentPage.ToString());
        }

        return pages;
    }

    private static IEnumerable<string> SplitLongEntry(string entry)
    {
        for (var start = 0; start < entry.Length; start += TelegramMessageLengthLimit)
        {
            var length = Math.Min(TelegramMessageLengthLimit, entry.Length - start);
            yield return entry.Substring(start, length);
        }
    }

    private static string GetCurrencySuffix(string currencyCode)
    {
        return currencyCode switch
        {
            "KZT" => "₸",
            "RUB" => "₽",
            "USD" => "$",
            "EUR" => "€",
            _ => string.Concat(' ', currencyCode)
        };
    }

    private static string Escape(string value) => WebUtility.HtmlEncode(value);
}
