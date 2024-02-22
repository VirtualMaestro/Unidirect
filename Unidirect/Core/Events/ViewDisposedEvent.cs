using Unidirect.Core.Common;
using Unidirect.Core.View;

namespace Unidirect.Core.Events
{
    public class ViewDisposedEvent : IReset<ViewDisposedEvent>
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