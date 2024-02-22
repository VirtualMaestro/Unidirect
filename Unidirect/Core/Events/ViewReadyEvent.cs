using Unidirect.Core.Common;
using Unidirect.Core.View;

namespace Unidirect.Core.Events
{
    public class ViewReadyEvent : IReset<ViewReadyEvent>
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