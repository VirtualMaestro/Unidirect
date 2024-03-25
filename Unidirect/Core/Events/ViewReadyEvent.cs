using Unidirect.Core.View;
using Unidirect.Helpers;

namespace Unidirect.Core.Events
{
    public class ViewReadyEvent : IEventReset
    {
        public IView View;

        public ViewReadyEvent Set(IView view)
        {
            View = view;
            return this;
        }

        public void Reset()
        {
            View = null;
        }
    }
}