window.dashboardDragDrop = {
	_isInitialized: false,
	_dotnetHelper: null,
	_draggedId: null,

	init: function ( dotnetHelper ) {
		// ✅ Если это WebView2 — не инициализирую drag-and-drop
		if ( window.isWebView2 ) {
			console.log( 'Drag-and-drop disabled in WebView2' );
			return;
		}

		if ( this._isInitialized ) {
			this._dotnetHelper = dotnetHelper;
			return;
		}

		this._dotnetHelper = dotnetHelper;
		this._isInitialized = true;
		console.log( "Dashboard Drag&Drop initialized" );

		this._onDragStart = this._onDragStart.bind( this );
		this._onDragEnd = this._onDragEnd.bind( this );
		this._onDragOver = this._onDragOver.bind( this );
		this._onDrop = this._onDrop.bind( this );
		this._onMouseUpGlobal = this._onMouseUpGlobal.bind( this );
		this._onMouseDownGlobal = this._onMouseDownGlobal.bind( this );

		document.addEventListener( 'dragstart', this._onDragStart );
		document.addEventListener( 'dragend', this._onDragEnd );
		document.addEventListener( 'dragover', this._onDragOver );
		document.addEventListener( 'drop', this._onDrop );
		document.addEventListener( 'mouseup', this._onMouseUpGlobal );
		document.addEventListener( 'mousedown', this._onMouseDownGlobal );
	},

	dispose: function () {
		if ( !this._isInitialized ) return;

		document.removeEventListener( 'dragstart', this._onDragStart );
		document.removeEventListener( 'dragend', this._onDragEnd );
		document.removeEventListener( 'dragover', this._onDragOver );
		document.removeEventListener( 'drop', this._onDrop );
		document.removeEventListener( 'mouseup', this._onMouseUpGlobal );
		document.removeEventListener( 'mousedown', this._onMouseDownGlobal );

		this._isInitialized = false;
		this._dotnetHelper = null;
		this._draggedId = null;
		console.log( "Dashboard Drag&Drop disposed" );
	},

	_onDragStart: function ( e ) {
		const widget = e.target.closest( '.dashboard-widget' );
		const tab = e.target.closest( '.dashboard-tab' );
		const target = widget || tab;

		if ( target ) {
			target.classList.add( 'dragging' );
			this._draggedId = target.dataset.widgetId;
			e.dataTransfer.effectAllowed = 'move';
			e.dataTransfer.setData( 'text/plain', this._draggedId || '' );
		}
	},

	_onDragEnd: function () {
		document.querySelectorAll( '.dragging' ).forEach( el => el.classList.remove( 'dragging' ) );
		document.querySelectorAll( '.drag-over' ).forEach( el => el.classList.remove( 'drag-over' ) );
		this._draggedId = null; // ✅ ОБЯЗАТЕЛЬНО сбрасываем здесь
	},

	_onDragOver: function ( e ) {
		e.preventDefault();
		e.dataTransfer.dropEffect = 'move';

		const widget = e.target.closest( '.dashboard-widget' );
		if ( widget && !widget.classList.contains( 'dragging' ) ) {
			document.querySelectorAll( '.drag-over' ).forEach( el => {
				if ( el !== widget ) el.classList.remove( 'drag-over' );
			} );
			widget.classList.add( 'drag-over' );
		}
	},

	_onDrop: function ( e ) {
		e.preventDefault();

		const targetWidget = e.target.closest( '.dashboard-widget' );
		const targetTabs = e.target.closest( '.dashboard-tabs' );

		document.querySelectorAll( '.drag-over' ).forEach( el => el.classList.remove( 'drag-over' ) );
		document.querySelectorAll( '.dragging' ).forEach( el => el.classList.remove( 'dragging' ) );

		// ✅ ВОССТАНАВЛИВАЕМ ЭТИ СТРОКИ! Без них код падал с ReferenceError
		const draggedId = this._draggedId;
		this._draggedId = null; // сбрасываем до C#-вызова

		if ( !draggedId ) return;

		if ( document.activeElement ) document.activeElement.blur();

		const isTab = document.querySelector( `.dashboard-tab[data-widget-id="${draggedId}"]` );
		const helper = this._dotnetHelper;

		const safeInvoke = ( method, ...args ) => {
			if ( helper ) {
				setTimeout( () => {
					helper.invokeMethodAsync( method, ...args )
						.catch( err => console.error( `JSInterop error in ${method}:`, err ) );
				}, 0);
			}
		};

		if ( isTab ) {
			if ( !targetTabs && !targetWidget ) {
				safeInvoke( 'OnDropToMain', draggedId );
			} else if ( targetWidget && targetWidget.dataset.widgetId !== draggedId ) {
				safeInvoke( 'OnDropTabToWidget', draggedId, targetWidget.dataset.widgetId );
			}
		} else {
			if ( targetTabs && !targetWidget ) {
				safeInvoke( 'OnDropToTabs', draggedId );
			} else if ( targetWidget && targetWidget.dataset.widgetId !== draggedId ) {
				safeInvoke( 'OnWidgetDrop', draggedId, targetWidget.dataset.widgetId );
			}
		}
	},

	_onMouseUpGlobal: function () {
		document.querySelectorAll( '.dragging' ).forEach( el => el.classList.remove( 'dragging' ) );
		document.querySelectorAll( '.drag-over' ).forEach( el => el.classList.remove( 'drag-over' ) );
		this._draggedId = null; // ✅ И здесь на всякий случай
	},

	_onMouseDownGlobal: function ( e ) {
		if ( e.target.closest( 'button, input, select, textarea, a, .no-resize' ) ) return;

		const widget = e.target.closest( '.dashboard-widget' );
		if ( !widget ) return;

		const rect = widget.getBoundingClientRect();
		const isRightEdge = e.clientX > rect.right - 10;
		if ( !isRightEdge ) return;

		const startX = e.clientX;
		const startWidth = rect.width;
		const helper = this._dotnetHelper;
		const widgetId = widget.dataset.widgetId;

		const onMouseMove = ( me ) => {
			me.preventDefault();
			const newWidth = startWidth + ( me.clientX - startX );
			widget.style.width = Math.max( 200, newWidth ) + 'px';
			widget.style.gridColumn = 'auto';
		};

		const onMouseUp = () => {
			document.removeEventListener( 'mousemove', onMouseMove );
			document.removeEventListener( 'mouseup', onMouseUp );
			document.removeEventListener( 'mouseleave', onMouseUp );

			if ( helper && widgetId ) {
				helper.invokeMethodAsync( 'OnWidgetResize', widgetId, widget.style.width )
					.catch( err => console.error( 'Resize JSInterop error:', err ) );
			}
		};

		document.addEventListener( 'mousemove', onMouseMove );
		document.addEventListener( 'mouseup', onMouseUp );
		document.addEventListener( 'mouseleave', onMouseUp );
	},

	getDraggedId: function () {
		const dragged = document.querySelector( '.dragging' );
		return dragged ? dragged.dataset.widgetId : '';
	}
};