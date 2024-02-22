namespace Unidirect.Core.View
{
    public interface IModelUpdate<in TModel>
    {
        public void OnModelUpdate(TModel model);
    }
}