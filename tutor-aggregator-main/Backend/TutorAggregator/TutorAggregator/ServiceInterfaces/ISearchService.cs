using TutorAggregator.Models;

namespace TutorAggregator.ServiceInterfaces
{
    public interface ISearchService
    {
        public Task<string?> DeleteItem(TutorSearchDTO person);
        public Task<string?> AddItem(TutorSearchDTO person);
        public IAsyncEnumerable<TutorSearchDTO>? Search(string prefix, TutorSearchFiltersDTO? filtersDTO);
    }
}
