using Unidirect.Core.Logic;

namespace Unidirect.Core.Common
{
    public interface IActionSender
    {
        public Response Send<TLogicAction>(TLogicAction action);
        public TAction GetAction<TAction>();
    }
}