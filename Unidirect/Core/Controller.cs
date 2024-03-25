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
        private delegate Mediator MediatorGetter(out bool isCreated);

        private readonly TModel _model;
        private readonly Response _response = new();
        private Dictionary<Type, List<MediatorGetter>> _viewMediatorsMap = new();
        private List<IUpdate> _updateMediators;
        private List<IFixedUpdate> _fixedUpdateMediators;
        private List<IModelUpdate<TModel>> _modelUpdateMediators;
        private List<Action<IActionSender>> _initializers = new(4);
        private HashSet<Action> _finalizers = new(4);
        private Dictionary<Mediator, int> _removableMediators = new();

        private bool _isModelChanged;
        private bool _isInitialized;

        protected TModel Model() => _model;

        public Controller() : this(Activator.CreateInstance<TModel>())
        { }
        
        public Controller(TModel model)
        {
            _model = model;

            EventBus<ViewReadyEvent>.AddListener(_OnViewReadyHandler);
            EventBus<ViewDisposedEvent>.AddListener(_OnViewDisposedHandler);

            _finalizers.Add(OnDispose);
        }

        public void Initialize()
        {
            OnInitialize();
            
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
            if (_updateMediators != null)
                for (var i = 0; i < _updateMediators.Count; i++)
                    _updateMediators[i].OnUpdate();
        }

        public void FixedUpdate()
        {
            OnFixedUpdate();

            if (_fixedUpdateMediators != null)
                for (var i = 0; i < _fixedUpdateMediators.Count; i++)
                    _fixedUpdateMediators[i].OnFixedUpdate();
        }

        protected void MapMediator<TMediator>() where TMediator : Mediator
        {
            var mediator = MediatorStore<TMediator>.Get(out var isCreated);

            if (isCreated)
                _InitializeMediator(mediator);
        }

        protected void MapView<TView, TMediator>()
            where TView : IView where TMediator : Mediator
        {
            _MapView(typeof(TView), MediatorStore<TMediator>.Get, MediatorStore<TMediator>.Clear);
        }

        protected void MapView<TMediator>(IView view) where TMediator : Mediator
        {
            _MapView(view.ViewType, MediatorStore<TMediator>.Get, MediatorStore<TMediator>.Clear);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _MapView(Type viewType, MediatorGetter getter, Action clearFunc)
        {
            if (_viewMediatorsMap.TryGetValue(viewType, out var getters))
                getters.Add(getter);
            else
            {
                getters = new List<MediatorGetter> {getter};
                _viewMediatorsMap.Add(viewType, getters);
            }
            
            _finalizers.Add(clearFunc);
        }

        /// <summary>
        /// Maps LogicAction to System and returns action id that serves as unique identifier for LogicAction.
        /// </summary>
        protected void MapAction<TAction, TSystem>() where TSystem : ISystem<TAction, TModel>
        {
            ActionMapper<TAction, TModel>.MapSystem<TSystem>();
            _finalizers.Add(ActionMapper<TAction, TModel>.Clear);
        }

        /// <summary>
        /// Maps LogicAction to a function handler.
        /// It can be used for communicating between a Mediator and a Controller when any System isn't involved.
        /// </summary>
        protected void MapAction<TAction>(Action<TAction> handler)
        {
            ActionMapper<TAction, TModel>.MapHandler(handler);
            _finalizers.Add(ActionMapper<TAction, TModel>.Clear);
        }

        public TAction GetAction<TAction>()
        {
            return ActionStore<TAction>.Get();
        }

        public Response Send<TAction>(TAction action)
        {
            _response.ActionID = ActionStore<TAction>.ActionId();
            _response.ActionType = typeof(TAction);
            _response.IsModelUpdated = true;
            _response.Command = ResponseCommand.None;

            if (ActionMapper<TAction, TModel>.HasSystem())
            {
                ActionMapper<TAction, TModel>.GetSystem().Process(action, _model, _response);
                OnResponse(_response);
            }
            else
                _response.IsModelUpdated = false;

            _isModelChanged = _response.IsModelUpdated;
            
            if (ActionMapper<TAction, TModel>.HasHandler())
                ActionMapper<TAction, TModel>.GetHandler()(action);

            ActionStore<TAction>.Reset();

            return _response;
        }

        protected virtual void OnInitialize()
        {
            // user definition
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
                _initializers[i].Invoke(this);

            _initializers.Clear();
            _initializers = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _UpdateModelListeners()
        {
            _isModelChanged = false;

            if (_modelUpdateMediators != null)
                for (var i = 0; i < _modelUpdateMediators.Count; i++)
                    _modelUpdateMediators[i].OnModelUpdate(_model);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _InitializeMediator(Mediator mediator)
        {
            _finalizers.Add(mediator.Dispose);

            if (_isInitialized)
                mediator.Init(this);
            else
                _initializers.Add(mediator.Init);

            if (mediator is IUpdate updateMediator)
                _AddToUpdate(updateMediator);

            if (mediator is IFixedUpdate fixedUpdateMediator)
                _AddToFixedUpdate(fixedUpdateMediator);

            if (mediator is IModelUpdate<TModel> modelUpdateMediator)
            {
                _AddToModelUpdate(modelUpdateMediator);

                if (_isInitialized)
                    modelUpdateMediator.OnModelUpdate(_model);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _DeinitializeMediator(Mediator mediator)
        {
            if (mediator is IUpdate updateMediator)
                _RemoveFromUpdate(updateMediator);

            if (mediator is IFixedUpdate fixedUpdateMediator)
                _RemoveFromFixedUpdate(fixedUpdateMediator);

            if (mediator is IModelUpdate<TModel> modelUpdateMediator)
                _RemoveFromModelUpdate(modelUpdateMediator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _AddToUpdate(IUpdate updatedMediator)
        {
            _updateMediators ??= new List<IUpdate>(2);
            _updateMediators.Add(updatedMediator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _RemoveFromUpdate(IUpdate updatedMediator)
        {
            _updateMediators.Remove(updatedMediator);

            if (_updateMediators.Count == 0)
                _updateMediators = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _AddToFixedUpdate(IFixedUpdate fixedUpdatedMediator)
        {
            _fixedUpdateMediators ??= new List<IFixedUpdate>(2);
            _fixedUpdateMediators.Add(fixedUpdatedMediator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _RemoveFromFixedUpdate(IFixedUpdate fixedUpdatedMediator)
        {
            _fixedUpdateMediators.Remove(fixedUpdatedMediator);

            if (_fixedUpdateMediators.Count == 0)
                _fixedUpdateMediators = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _AddToModelUpdate(IModelUpdate<TModel> modelUpdatedMediator)
        {
            _modelUpdateMediators ??= new List<IModelUpdate<TModel>>(2);
            _modelUpdateMediators.Add(modelUpdatedMediator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _RemoveFromModelUpdate(IModelUpdate<TModel> modelUpdatedMediator)
        {
            _modelUpdateMediators.Remove(modelUpdatedMediator);

            if (_modelUpdateMediators.Count == 0)
                _modelUpdateMediators = null;
        }

        private void _OnViewReadyHandler(ViewReadyEvent viewEvent)
        {
            var view = viewEvent.View;
            var viewType = view.ViewType;

            if (_viewMediatorsMap.TryGetValue(viewType, out var getters))
            {
                for (var i = 0; i < getters.Count; i++)
                {
                    var getter = getters[i];
                    var mediator = getter(out var isCreated);

                    if (isCreated)
                    {
                        _InitializeMediator(mediator);

                        if (!mediator.IsKeepUp)
                            _removableMediators[mediator] = 0; 
                    }

                    if (!mediator.IsKeepUp)
                    {
                        var numViews = _removableMediators[mediator];
                        _removableMediators[mediator] = ++numViews;
                    }
                    
                    mediator.OnViewAdded(view);
                }
            }
            else
                _NoMediatorFound(viewType);
        }

        private void _OnViewDisposedHandler(ViewDisposedEvent viewEvent)
        {
            var view = viewEvent.View;
            var viewType = view.ViewType;

            if (_viewMediatorsMap.TryGetValue(viewType, out var getters))
            {
                for (var i = 0; i < getters.Count; i++)
                {
                    var getter = getters[i];
                    var mediator = getter(out _);
                    mediator.OnViewRemoved(view);

                    if (mediator.IsKeepUp) 
                        continue;
                    
                    var numViews = _removableMediators[mediator];
                    --numViews;

                    if (numViews <= 0)
                    {
                        _removableMediators.Remove(mediator);
                        _DeinitializeMediator(mediator);
                        _finalizers.Remove(mediator.Dispose);
                        mediator.Dispose();
                    }
                    else
                        _removableMediators[mediator] = numViews;
                }
            }
            else
                _NoMediatorFound(viewType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void _NoMediatorFound(Type viewType)
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

            _viewMediatorsMap.Clear();
            _viewMediatorsMap = null;
            _updateMediators?.Clear();
            _updateMediators = null;
            _fixedUpdateMediators?.Clear();
            _fixedUpdateMediators = null;
            _finalizers.Clear();
            _finalizers = null;
            _modelUpdateMediators?.Clear();
            _modelUpdateMediators = null;
            _removableMediators?.Clear();
            _removableMediators = null;
        }
    }
}