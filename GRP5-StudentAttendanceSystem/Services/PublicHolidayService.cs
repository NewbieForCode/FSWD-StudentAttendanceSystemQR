using Microsoft.Extensions.Caching.Memory;

namespace GRP5_StudentAttendanceSystem.Services
{
    //------------------------------------------------------------
    // Caleb McIntyre - 13/11/2026
    // Gets New Zealand public holidays from the free Nager.Date API
    // (https://date.nager.at), so course sessions can't be booked on one.
    // Only national holidays and Auckland's regional anniversary day are
    // kept, because the campus is in Auckland. Each year's list is cached
    // for 12 hours so the API isn't called on every request.
    //------------------------------------------------------------

    //One holiday, as Nager.Date sends it
    public class PublicHoliday
    {
        public DateOnly Date { get; set; }
        public string LocalName { get; set; } = "";
        public string Name { get; set; } = "";
        public bool Global { get; set; }
        public List<string>? Counties { get; set; }
    }

    public class PublicHolidayService
    {
        //Auckland's region code, used for its anniversary day
        private const string AucklandRegion = "NZ-AUK";

        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PublicHolidayService> _logger;

        public PublicHolidayService(HttpClient httpClient, IMemoryCache cache, ILogger<PublicHolidayService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
        }

        //All the holidays for one year, or null if the API can't be reached
        public async Task<List<PublicHoliday>?> GetHolidaysAsync(int year)
        {
            var cacheKey = "nz-public-holidays-" + year;
            if (_cache.TryGetValue(cacheKey, out List<PublicHoliday>? cached) && cached != null)
            {
                return cached;
            }

            try
            {
                var holidays = await _httpClient.GetFromJsonAsync<List<PublicHoliday>>(
                    $"https://date.nager.at/api/v3/PublicHolidays/{year}/NZ") ?? new List<PublicHoliday>();

                //National holidays, plus regional ones that apply to Auckland
                var relevant = holidays
                    .Where(h => h.Global || (h.Counties != null && h.Counties.Contains(AucklandRegion)))
                    .OrderBy(h => h.Date)
                    .ToList();

                _cache.Set(cacheKey, relevant, TimeSpan.FromHours(12));
                return relevant;
            }
            catch (Exception ex)
            {
                //If the holiday API is down, don't stop the rest of the app working
                _logger.LogWarning(ex, "Couldn't get NZ public holidays for {Year}", year);
                return null;
            }
        }

        //The holiday on this date, or null if it isn't one (or the API can't be reached)
        public async Task<PublicHoliday?> FindHolidayAsync(DateOnly date)
        {
            var holidays = await GetHolidaysAsync(date.Year);
            return holidays?.FirstOrDefault(h => h.Date == date);
        }

        //The next few holidays from today (looks into next year as well)
        public async Task<List<PublicHoliday>> GetUpcomingAsync(int count)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var thisYear = await GetHolidaysAsync(today.Year) ?? new List<PublicHoliday>();
            var nextYear = await GetHolidaysAsync(today.Year + 1) ?? new List<PublicHoliday>();

            return thisYear.Concat(nextYear)
                .Where(h => h.Date >= today)
                .OrderBy(h => h.Date)
                .Take(count)
                .ToList();
        }
    }
}
