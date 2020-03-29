using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace PegaTrade.Layer.Models.Helpers
{
    [DataContract]
    public class ResultsItem
    {
        [DataMember] public Types.ResultsType ResultType { get; set; }
        [DataMember] public string Message { get; set; }
        [DataMember] public string Value { get; set; } // In case you need to return something. For example, CreateNewItem() -> NewItem.ID

        public string MessageColor { get; set; }

        public bool IsSuccess => ResultType == Types.ResultsType.Successful;
        public static ResultsItem Success() => Create(Types.ResultsType.Successful, string.Empty);
        public static ResultsItem Success(string message) => Create(Types.ResultsType.Successful, message);

        public static ResultsItem Error(string message) => Create(Types.ResultsType.Error, message);

        public static ResultsItem Create(Types.ResultsType type, string message)
        {
            return new ResultsItem { ResultType = type, Message = message };
        }
    }
}
