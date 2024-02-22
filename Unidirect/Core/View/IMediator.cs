using Unidirect.Core.Common;

namespace Unidirect.Core.View
{
    public interface IMediator
    {
        /// <summary>
        /// Allow a mediator to send actions to the system.
        /// </summary>
        public IActionSender Sender { set; }
        
        /// <summary>
        /// If 'true' the mediator will be enabled even if no View was added.
        /// </summary>
        public bool IsStandAlone { set; get; }
        
        /// <summary>
        /// Callback invokes when an instance of a View that is associated with the current mediator was created and added to the scene.
        /// </summary>
        public void OnViewAdded(IView view);
        
        /// <summary>
        /// Callback invokes when View instance of an associated mediator was removed from the scene.
        /// </summary>
        public void OnViewDisposed(IView view);

        /// <summary>
        /// Invokes when the current mediator is going to be removed.
        /// </summary>
        public void OnDispose();
    }
}