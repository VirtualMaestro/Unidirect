#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unidirect.Core.Common;
using Unidirect.Core.Events;
using Unidirect.Core.Logic;
using Unidirect.Core.Mappers;
using Unidirect.Core.View;
using Unidirect.Helpers;

namespace Unidirect.Core
{
#if ENABLE_IL2CPP
     [UnityEngine.Scripting.Preserve]
     [Il2CppSetOption (Option.NullChecks, false)]
     [Il2CppSetOption (Option.ArrayBoundsChecks, false)]
#endif
    public class Controller<TModel> : IActionSender where TModel : class, new()
    {
        private delegate IMediator MediatorCreator(out bool isCreated);
        
        private TModel _model;
        private Response _response = new();
        private Dictionary<Type, MediatorCreator> _viewMediatorsMap = new();
        private List<IUpdate> _updateMediators = new(4);
        private List<IFixedUpdate> _fixedUpdateMediators = new(4);
        private List<IModelUpdate<TModel>> _modelUpdateMediators = new(4);
        private List<Action> _initializers = new(4);
        private HashSet<Action> _finalizers = new(4);

        private bool _isModelChanged;
        private bool _isInitialized;
        
        protected TModel Model() => _model;

        protected Controller(TModel model)
        {
            _model = model;
            EventBus<ViewReadyEvent>.AddListener(_OnViewReadyHandler);
            EventBus<ViewDisposedEvent>.AddListener(_OnViewDisposedHandler);

            _finalizers.Add(OnDispose);
        }

        public void Initialize()
        {
            _RunInitializers();
            _UpdateModelListeners();
            _isInitialized = true;
        }

        public void Update()
        {
            // update controller
            OnUpdate();
            
            // update model's listeners
            if (_isModelChanged)
                _UpdateModelListeners();
            
            // update mediators
            for (var i = 0; i < _updateMediators.Count; i++)
                _updateMediators[i].OnUpdate();
        }

        public void FixedUpdate()
        {
            OnFixedUpdate();
            
            for (var i = 0; i < _fixedUpdateMediators.Count; i++)
                _fixedUpdateMediators[i].OnFixedUpdate();
        }

        protected void MapView<TMediator>() where TMediator: IMediator
        {
            MediatorStore<TMediator>.IsStandAlone = true;

            var mediator = MediatorStore<TMediator>.Get(out var isCreated);
        
            if (isCreated)
                _InitializeMediator(mediator);
        }

        protected void MapView<TView, TMediator>(bool isStandAlone = false) where TView: IView where TMediator: IMediator
        {
            MediatorStore<TMediator>.IsStandAlone = isStandAlone;
            _MapView(typeof(TView), MediatorStore<TMediator>.Get, MediatorStore<TMediator>.Clear);
        }

        protected void MapView<TMediator>(IView view, bool isStandAlone = false) where TMediator: IMediator
        {
            MediatorStore<TMediator>.IsStandAlone = isStandAlone;
            _MapView(view.ViewType, MediatorStore<TMediator>.Get, MediatorStore<TMediator>.Clear);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _MapView(Type viewType, MediatorCreator creationFunc, Action clearFunc)
        {
            _viewMediatorsMap.TryAdd(viewType, creationFunc);
            _finalizers.Add(clearFunc);
        }
        
        /// <summary>
        /// Maps LogicAction to System and returns action id that serves as unique identifier for LogicAction.
        /// </summary>
        protected void MapAction<TAction, TSystem>() 
            where TSystem : ISystem<TAction, TModel>
        {
            ActionSystemMapper<TAction, TModel>.Map<TSystem>();
            _finalizers.Add(ActionSystemMapper<TAction, TModel>.Clear);
        }

        public TAction GetAction<TAction>()
        {
            return ActionStore<TAction>.Get();
        }

        public Response Send<TAction>(TAction action) 
        {
            _response.ActionID = ActionStore<TAction>.ActionId;
            _response.IsSuccess = true;
            _response.Command = ResponseCommand.None;
            
            ActionSystemMapper<TAction, TModel>.Get().Process(action, _model, _response);

            _isModelChanged = _response.IsSuccess;
                
            OnResponse(_response);

            ActionStore<TAction>.Reset();

            return _response;
        }

        protected virtual void OnResponse(Response response)
        {
            // user definition
        }

        protected virtual void OnUpdate()
        {
            // user definition
        }
        
        protected virtual void OnFixedUpdate()
        {
            // user definition
        }
        
        protected virtual void OnDispose()
        {
            // user definition
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _RunInitializers()
        {
            for (var i = 0; i < _initializers.Count; i++)
                _initializers[i].Invoke();
            
            _initializers.Clear();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _UpdateModelListeners()
        {
            _isModelChanged = false;

            for (var i = 0; i < _modelUpdateMediators.Count; i++)
                _modelUpdateMediators[i].OnModelUpdate(_model);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _InitializeMediator(IMediator mediator)
        {
            mediator.Sender = this;
            
            _finalizers.Add(mediator.OnDispose);
            
            if (mediator is IInitialize initializeMediator)
            {
                if (_isInitialized)
                    initializeMediator.OnInitialize();
                else
                    _initializers.Add(initializeMediator.OnInitialize);
            }
            
            if (mediator is IUpdate updateMediator)
                _updateMediators.Add(updateMediator);
            
            if (mediator is IFixedUpdate fixedUpdateMediator)
                _fixedUpdateMediators.Add(fixedUpdateMediator);

            if (mediator is IModelUpdate<TModel> modelUpdateMediator)
            {
                _modelUpdateMediators.Add(modelUpdateMediator);
                
                if (_isInitialized)
                    modelUpdateMediator.OnModelUpdate(_model);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _DeinitializeMediator(IMediator mediator)
        {
            if (mediator is IUpdate updateMediator)
                _updateMediators.Remove(updateMediator);
            
            if (mediator is IFixedUpdate fixedUpdateMediator)
                _fixedUpdateMediators.Remove(fixedUpdateMediator);

            if (mediator is IModelUpdate<TModel> modelUpdateMediator)
                _modelUpdateMediators.Remove(modelUpdateMediator);
        }
        
        private void _OnViewReadyHandler(ViewReadyEvent viewEvent)
        {
            var view = viewEvent.View;
            var viewType = view.ViewType;

            if (_viewMediatorsMap.TryGetValue(viewType, out var mediatorCreator))
            {
                var mediator = mediatorCreator.Invoke(out var isCreated);
                
                if (isCreated)
                    _InitializeMediator(mediator);
                
                mediator.OnViewAdded(view);
            }
            else
                _NoMediatorFound(viewType);
        }

        private void _OnViewDisposedHandler(ViewDisposedEvent viewEvent)
        {
            var view = viewEvent.View;
            var viewType = view.ViewType;

            if (_viewMediatorsMap.TryGetValue(viewType, out var mediatorCreator))
            {
                var mediator = mediatorCreator.Invoke(out _);
                mediator.OnViewDisposed(view);
                
                if (!mediator.IsStandAlone) 
                    _DeinitializeMediator(mediator);
            }
            else
                _NoMediatorFound(viewType);
        }

        private void _NoMediatorFound(Type viewType)
        {
#if DEBUG || DEVELOPMENT_BUILD || UNITY_EDITOR
            throw new Exception($"No Mediator found to handle given View type {viewType}");
#endif
        }

        public void Dispose()
        {
            foreach (var finalizer in _finalizers)
                finalizer();

            EventBus<ViewReadyEvent>.RemoveListener(_OnViewReadyHandler);
            EventBus<ViewDisposedEvent>.RemoveListener(_OnViewDisposedHandler);
            
            _model = null;
            _response = null;
            _viewMediatorsMap.Clear();
            _viewMediatorsMap = null;
            _updateMediators.Clear();
            _updateMediators = null;
            _fixedUpdateMediators.Clear();
            _fixedUpdateMediators = null;
            _finalizers.Clear();
            _finalizers = null;
            _initializers = null;
            _modelUpdateMediators.Clear();
            _modelUpdateMediators = null;
        }
    }
}
