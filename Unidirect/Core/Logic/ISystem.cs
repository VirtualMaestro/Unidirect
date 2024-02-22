namespace Unidirect.Core.Logic
{
    public interface ISystem<in TAction, in TModel>
    {
        public Response Process(TAction action, TModel model, Response response);
    }
}