using System.Text;
using TutorAggregator.DataEntities;
using TutorAggregator.ServiceInterfaces;
using TutorAggregator.Models;
using TutorAggregator.Data;
using Microsoft.EntityFrameworkCore;

namespace TutorAggregator.Services
{
    public class Node
    {
        public Node((char, List<TutorSearchDTO>) value)
        {
            Value = value;
            ChilrenList = new HashSet<Node>(33);
        }
        public  (char symbol, List<TutorSearchDTO> tutorList) Value;
        public readonly HashSet<Node> ChilrenList;
        public bool isFinished { get; set; }
        public override bool Equals(object obj)
        {
            if (this.GetType() != obj.GetType())
                return false;

            var node = obj as Node;
            return this.Value.symbol == node.Value.symbol;
        }
        public override int GetHashCode()
        {
            return this.Value.symbol.GetHashCode();
        }
    }
    public class TreeSearchService : ISearchService
    {
        private readonly Node _root;
        private readonly DataContext _database;
        public TreeSearchService(DataContext database)
        {
            _root = new Node((' ', null)!);
            _database = database;
            var res = FillTree();
            while (!res.IsCompleted) { }
        }
        public async Task FillTree()
        {
            foreach (var t in _database.Tutors.Where(e => e.SecondName != null))
                await AddItem(new TutorSearchDTO(t));
        }
        public async Task<string?> DeleteItem(TutorSearchDTO tutor)
        {
            if (tutor == null)
                return null!;
            var name = tutor.FullName;
            var currentNode = _root;
            var previousNode = _root;
            var isSuccessfull = false;
            await Task.Run(() =>
            {
                foreach (var symbol in name)
                {
                    previousNode = currentNode;
                    currentNode = currentNode.ChilrenList.FirstOrDefault(n => n.Value.symbol == symbol);
                    if (currentNode == null)
                    {
                        isSuccessfull = false;
                        return;
                    }
                }
                if (!currentNode.isFinished)
                {
                    isSuccessfull = false;
                    return;
                }
                if (currentNode.ChilrenList.Count == 0 && currentNode.Value.tutorList.Count == 1
                                               && currentNode.Value.tutorList[0].Id == tutor.Id)
                {
                    previousNode.ChilrenList.Remove(currentNode);
                    isSuccessfull = true;
                    return;
                }
                var indexToRemove = 0;
                var isFound = false;
                foreach (var t in currentNode.Value.tutorList)
                {
                    if (t.Id == tutor.Id)
                    {
                        isFound = true;
                        break;
                    }
                    indexToRemove++;
                }
                if (!isFound)
                {
                    isSuccessfull = false;
                    return;
                }
                currentNode.Value.tutorList.RemoveAt(indexToRemove);
                if (currentNode.Value.tutorList.Count == 0)
                    currentNode.isFinished = false;
                isSuccessfull = true;
            });
            return isSuccessfull ? "Success" : null;
        }
        public async Task<string?> AddItem(TutorSearchDTO tutor)
        {
            if (tutor == null)
                return null;
            await Task.Run(() =>
            {
                var currentRoot = _root;
                TutorSearchDTO currentTutor = null!;
                for (int i = 0; i < tutor.FullName!.Length; i++)
                {
                    currentTutor = (i == tutor.FullName.Length - 1 ? tutor : null)!;
                    if (!currentRoot.ChilrenList.Any(n => n.Value.symbol == tutor.FullName[i]))
                    {
                        List<TutorSearchDTO> currentNodeValuesList = null!;
                        if (i == tutor.FullName.Length - 1)
                            currentNodeValuesList = new List<TutorSearchDTO>() { currentTutor };
                        var currentNode = new Node((tutor.FullName[i], currentNodeValuesList)!);
                        currentRoot.ChilrenList.Add(currentNode);
                        currentRoot = currentNode;
                        continue;
                    }
                    currentRoot = currentRoot.ChilrenList.First(n => n.Value.symbol == tutor.FullName[i]);
                    if (i == tutor.FullName.Length - 1)
                    {
                        if (currentRoot.Value.tutorList == null)
                            currentRoot.Value.tutorList = new List<TutorSearchDTO>() { currentTutor };
                        else
                            currentRoot.Value.tutorList.Add(currentTutor);
                    }
                }
                currentRoot.isFinished = true;
            });
            return "Success";
        }
        public async IAsyncEnumerable<TutorSearchDTO>? Search(string prefix, TutorSearchFiltersDTO? filtersDTO)
        {
            await foreach (var tutor in ConnectMethod(prefix ?? "", _root))
            {
                if (filtersDTO == null)
                    yield return tutor;
                else if (await SatisfiesAllFilters(tutor, filtersDTO))
                    yield return tutor;
            }
        }

        private async IAsyncEnumerable<TutorSearchDTO> ConnectMethod(string prefix, Node startNode, bool isReturned = false)
        {
            var currentRoot = startNode;
            if (startNode == _root)
            {
                foreach (var character in prefix)
                {
                    currentRoot = currentRoot.ChilrenList.FirstOrDefault(node => node.Value.symbol == character);
                    if (currentRoot == null)
                        yield break;
                }
            }
            if (currentRoot.isFinished && !isReturned)
                foreach (var t in currentRoot.Value.tutorList)
                    yield return t;
            foreach (var childNode in currentRoot.ChilrenList)
            {
                if (childNode.isFinished)
                    foreach (var t in childNode.Value.tutorList)
                        yield return t;
                if (childNode.ChilrenList.Any())
                    await foreach (var t in ConnectMethod(childNode.Value.symbol.ToString(), childNode, childNode.isFinished))
                        yield return t;
            }
        }
        private async Task<bool> SatisfiesAllFilters(TutorSearchDTO tutorDTO, TutorSearchFiltersDTO filtersDTO)
        {
            //var tutorInDB = await _database.Tutors
            //    .Include(e => e.LessonTemplates)
            //    .FirstOrDefaultAsync(e => e.Id == tutorDTO.Id);
            //if (tutorInDB!.LessonTemplates == null || tutorInDB.LessonTemplates.Count == 0)
            //return false;
            var templates = _database.LessonTemplates.Include(e => e.Lessons)
                 .Include(e => e.Tutor)
                 .Where(e => e.Tutor.Id == tutorDTO.Id);
            if (templates == null || templates.Count() == 0)
                return false;
            return templates
                .Any(e => (e.Name == filtersDTO.SubjectName || filtersDTO.SubjectName == null)
                && (e.LessonFormat == filtersDTO.LessonFormat || filtersDTO.LessonFormat == null)
                && (e.DesiredSex == filtersDTO.DesiredSex || filtersDTO.DesiredSex == DesiredSex.All || filtersDTO.DesiredSex == null)
                && (e.DesiredAge == filtersDTO.DesiredAge || filtersDTO.DesiredAge == DesiredAge.All || filtersDTO.DesiredAge == null)
                && (filtersDTO.Place == null || e.Lessons.Any(l => l.Place == filtersDTO.Place)));
        }
    }
}
