using Unidirect.Core.View;
using Unidirect.Helpers;

namespace Unidirect.Core.Events
{
    public class ViewDisposedEvent : IEventReset
    {
        public IView View;

        public ViewDisposedEvent Set(IView view)
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