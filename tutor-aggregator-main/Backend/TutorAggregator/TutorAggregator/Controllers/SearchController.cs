using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorAggregator.Services;
using Microsoft.EntityFrameworkCore;
using TutorAggregator.Data;
using TutorAggregator.DataEntities;
using TutorAggregator.Models;
using TutorAggregator.ServiceInterfaces;

namespace TutorAggregator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SearchController : Controller
    {
        private readonly ISearchService _tree;
        public SearchController(ISearchService tree)
        {
            _tree = tree;
        }
        [HttpGet("GetItems")]
        public async IAsyncEnumerable<TutorSearchDTO> GetItemsBySubstring(string? prefix, [FromQuery]TutorSearchFiltersDTO? filtersDTO)
        {
            await foreach (var tutor in _tree.Search(prefix!, filtersDTO!)!)
                yield return tutor;
        }

        [HttpPost("Add")]
        public async Task<ActionResult> AddItemToTree(TutorSearchDTO tutorSearchDTO)
        {
           string? result = null;
           result = await _tree.AddItem(tutorSearchDTO);
           if(result == null)
                return BadRequest("There is no such tutor!");
            return Ok(result);
        }

        [HttpDelete("Remove")]
        public async Task<ActionResult> RemoveItemFromTree(TutorSearchDTO tutorSearchDTO)
        {
            string? result = null;
            result = await _tree.DeleteItem(tutorSearchDTO); ;
            if (result == null)
                return BadRequest("There is no such tutor!");
            return Ok(result);
        }
    }
}
