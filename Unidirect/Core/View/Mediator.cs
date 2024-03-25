using Unidirect.Core.Common;
using Unidirect.Core.Logic;

namespace Unidirect.Core.View
{
    public class Mediator
    {
        private IActionSender _sender;

        /// <summary>
        /// If 'true' the mediator will be enabled even if no View was added. Otherwise it will be removed.
        /// </summary>
        public bool IsKeepUp { protected set; get; } = true;
        public bool IsDisposed { private set; get; } = false;
        
        public void Init(IActionSender sender)
        {
            _sender = sender;
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;
            
            OnDispose();
            IsDisposed = true;
        }
        
        /// <summary>
        /// Gets action instance. It reuses instances.
        /// </summary>
        protected TAction GetAction<TAction>() where TAction: class
        {
            return _sender.GetAction<TAction>();
        }
        
        /// <summary>
        /// Sends action to the logic.
        /// Returns 'Response' from the logic.
        /// </summary>
        protected Response Send<TAction>(TAction action)
        {
            return _sender.Send(action);
        }

        /// <summary>
        /// The callback invokes on the initialization phase.
        /// </summary>
        protected virtual void OnInitialize()
        {
            
        }
        
        /// <summary>
        /// The callback invokes when an instance of a View that is associated with the current mediator was created and added to the scene.
        /// </summary>
        public virtual void OnViewAdded(IView view)
        {
            
        }
        
        /// <summary>
        /// The callback invokes when View instance of an associated mediator was removed from the scene.
        /// </summary>
        public virtual void OnViewRemoved(IView view)
        {
            
        }
        
        /// <summary>
        /// The callback invokes when the current mediator is going to be removed.
        /// </summary>
        protected virtual void OnDispose()
        {
            
        }
    }
}